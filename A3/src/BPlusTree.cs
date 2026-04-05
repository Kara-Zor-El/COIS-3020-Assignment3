using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BPlusTree;
public class DuplicateKeyException : Exception {
  public DuplicateKeyException() : base("Duplicate key inserted into B+ tree") { }
}

public class BPlusTree<TKey, TValue> where TKey : IComparable<TKey> {
  private abstract class Node { }

  private class LeafNode : Node {
    public List<TKey> Keys = new();
    public List<TValue> Values = new();
    public LeafNode? Next;
    public int Length => this.Keys.Count;
  }

  private class InternalNode : Node {
    public List<TKey> Keys = new();
    public List<Node> Children = new();
    public int Length => this.Keys.Count;
  }

  private readonly int _rank; // Maximum keys per node
  private Node _root;

  /// <summary>
  /// Creates an empty B+ tree with the given rank (must be >= 3)
  /// 
  /// Time Complexity: O(1)
  /// </summary>
  public BPlusTree(int rank) {
    if (rank < 3) throw new ArgumentException("Rank must be at least 3");
    this._rank = rank;
    this._root = new LeafNode();
  }

  /// <summary>
  /// Creates a B+ tree with the given rank and initial records
  /// 
  /// <param name="rank">The rank of the tree</param>
  /// <param name="items">The initial records to insert into the tree</param>
  /// 
  /// Time Complexity: O(n log n)
  /// </summary>
  public BPlusTree(int rank, IEnumerable<TValue> items) : this(rank) {
    this.BulkInsert(items);
  }

  /// <summary>
  /// Checks if the tree is empty
  /// 
  /// Time Complexity: O(1)
  /// </summary>
  public bool IsEmpty => this._root is LeafNode leaf && leaf.Length == 0;

  /// <summary>
  /// Retrieves the value for <paramref name="key"/> or default(TValue) if the key is not found
  /// 
  /// <param name="key">The key to search for</param>
  /// 
  /// Time Complexity: O(m * log_m n + log m)
  /// </summary>
  public bool Search(TKey key, out TValue? value) {
    var leaf = this.SearchHelp(key, this._root) as LeafNode
      ?? throw new InvalidOperationException("SearchHelp must return a leaf.");

    int index = this.LowerBound(leaf.Keys, key);
    if (index >= 0 && index < leaf.Length && leaf.Keys[index].CompareTo(key) == 0) {
      value = leaf.Values[index];
      return true;
    }

    value = default;
    return false;
  }

  /// <summary>
  /// Retrieves all values in the range [start, end]
  /// 
  /// <param name="start">The start of the range</param>
  /// <param name="end">The end of the range</param>
  /// 
  /// Time Complexity: O(m * log_m n + log m)
  /// </summary>
  public List<TValue> Range(TKey start, TKey end) {
    var leaf = this.SearchHelp(start, this._root) as LeafNode
      ?? throw new InvalidOperationException("SearchHelp must return a leaf.");

    var acc = new List<TValue>();
    this.RangeHelp(start, end, leaf, acc);
    return acc;
  }

  /// <summary>
  /// Inserts a value into the tree
  /// 
  /// <param name="value">The value to insert</param>
  /// 
  /// Throws <see cref="DuplicateKeyException"/> if the key is already in the tree
  /// 
  /// Time Complexity: O(log n)
  /// </summary>
  public void Insert(TValue value) {
    var key = GetKey(value);

    // Eagerly split the root when full so we never need to split on the way back up
    if (this.IsNodeFull(this._root)) {
      var (kPrime, rightNode) = this.Split(this._root);
      this._root = new InternalNode {
        Keys = new List<TKey> { kPrime },
        Children = new List<Node> { this._root, rightNode }
      };
    }
    if (!this.InsertHelp(key, value, this._root)) {
      throw new DuplicateKeyException();
    }
  }

  /// <summary>
  /// Inserts multiple key-value pairs into the tree
  /// 
  /// <param name="items">The items to insert</param>
  /// 
  /// Time Complexity: O(n log n)
  /// </summary>
  public void BulkInsert(IEnumerable<TValue> items) {
    var list = items.ToList();
    if (list.Count == 0) return;

    if (!this.IsEmpty) throw new InvalidOperationException("BulkInsert requires an empty tree");

    list.Sort((a, b) => GetKey(a).CompareTo(GetKey(b)));

    for (int i = 1; i < list.Count; i++) {
      if (GetKey(list[i - 1]).CompareTo(GetKey(list[i])) == 0) {
        throw new DuplicateKeyException();
      }
    }

    // Build leaf level
    var leaves = this.BuildLeaves(list);

    this._root = this.BuildTree(leaves);
  }

  /// <summary>
  /// Deletes a key from the tree
  /// 
  /// <param name="key">The key to delete</param>
  /// <returns>True if the key was deleted, false otherwise</returns>
  /// 
  /// Time Complexity: O(n)
  /// </summary>
  public bool Delete(TKey key) {
    var entries = this.CollectAll(this._root);
    int index = this.LowerBound(entries, key);
    if (index >= entries.Count || GetKey(entries[index]).CompareTo(key) != 0) {
      return false;
    }

    entries.RemoveAt(index);
    this._root = new LeafNode();
    this.BulkInsert(entries);
    return true;
  }

  /// <summary>
  /// Merges two trees into a new tree
  /// 
  /// <param name="other">The other tree to merge</param>
  /// 
  /// Time Complexity: O((n1+n2) log(n1+n2))
  /// </summary>
  public BPlusTree<TKey, TValue> Merge(BPlusTree<TKey, TValue> other) {
    var newTree = new BPlusTree<TKey, TValue>(this._rank);
    var entries = this.CollectAll(this._root).Concat(this.CollectAll(other._root)).ToList();
    entries.Sort((a, b) => GetKey(a).CompareTo(GetKey(b)));
    newTree.BulkInsert(entries);
    return newTree;
  }

  /// <summary>
  /// Finds the index of the key in the list of keys
  /// 
  /// <param name="keys">The list of keys</param>
  /// <param name="key">The key to find</param>
  /// 
  /// Time Complexity: O(m)
  /// </summary>
  private int ChildIndex(List<TKey> keys, TKey key) {
    for (int i = 0; i < keys.Count; i++) {
      if (keys[i].CompareTo(key) > 0) return i;
    }
    return keys.Count;
  }

  /// <summary>
  /// Finds the index of the key in the list of keys
  /// 
  /// <param name="keys">The list of keys</param>
  /// <param name="key">The key to find</param>
  /// 
  /// Time Complexity: O(log m)
  /// </summary>
  private int LowerBound(List<TKey> keys, TKey key) {
    int lo = 0;
    int hi = keys.Count;
    while (lo < hi) {
      int mid = lo + (hi - lo) / 2;
      if (keys[mid].CompareTo(key) < 0) {
        lo = mid + 1;
      } else {
        hi = mid;
      }
    }
    return lo;
  }

  /// <summary>
  /// Finds the index of the key in the list of items
  /// 
  /// <param name="items">The list of items</param>
  /// <param name="key">The key to find</param>
  /// 
  /// Time Complexity: O(log n)
  /// </summary>
  private int LowerBound(List<TValue> items, TKey key) {
    int lo = 0;
    int hi = items.Count;
    while (lo < hi) {
      int mid = lo + (hi - lo) / 2;
      if (GetKey(items[mid]).CompareTo(key) < 0) {
        lo = mid + 1;
      } else {
        hi = mid;
      }
    }
    return lo;
  }


  /// <summary>
  /// Searches for the node containing the key
  /// 
  /// <param name="key">The key to search for</param>
  /// <param name="node">The node to search in</param>
  /// 
  /// Time Complexity: O(m log_m n)
  /// </summary>
  private Node SearchHelp(TKey key, Node node) {
    while (node is InternalNode iNode) {
      int index = this.ChildIndex(iNode.Keys, key);
      node = iNode.Children[index];
    }
    return node;
  }

  /// <summary>
  /// Helper function for the Range method
  /// 
  /// <param name="start">The start of the range</param>
  /// <param name="end">The end of the range</param>
  /// <param name="leaf">The leaf node to search in</param>
  /// <param name="acc">The list to store the results</param>
  /// 
  /// Time Complexity: O(n) at worst
  /// </summary>
  private void RangeHelp(TKey start, TKey end, LeafNode leaf, List<TValue> acc) {
    while (leaf != null) {
      bool pastEnd = false;
      for (int i = 0; i < leaf.Length; i++) {
        int cmpStart = leaf.Keys[i].CompareTo(start);
        int cmpEnd = leaf.Keys[i].CompareTo(end);
        if (cmpStart < 0) continue; // before range
        if (cmpEnd > 0) {
          pastEnd = true; // past range
          break;
        }
        acc.Add(leaf.Values[i]);
      }
      if (pastEnd) break;
      leaf = leaf.Next;
    }
  }

  /// <summary>
  /// Helper function for the Insert method
  /// 
  /// <param name="key">The key to insert</param>
  /// <param name="value">The value to insert</param>
  /// <param name="node">The node to insert into</param>
  /// <returns>True if the key was inserted, false otherwise</returns>
  /// 
  /// Throws <see cref="DuplicateKeyException"/> if the key is already in the tree
  /// 
  /// Time Complexity: O(m log_m n)
  /// </summary>
  private bool InsertHelp(TKey key, TValue value, Node node) {
    switch (node) {
      case LeafNode leaf:
        int index = this.ChildIndex(leaf.Keys, key);
        if (index > 0 && leaf.Keys[index - 1].CompareTo(key) == 0) return false;
        if (index < leaf.Length && leaf.Keys[index].CompareTo(key) == 0) return false;
        leaf.Keys.Insert(index, key);
        leaf.Values.Insert(index, value);
        return true;
      case InternalNode iNode:
        int childIndex = this.ChildIndex(iNode.Keys, key);
        var child = iNode.Children[childIndex];

        if (this.IsNodeFull(child)) {
          var (kPrime, rightNode) = this.Split(child);
          iNode.Keys.Insert(childIndex, kPrime);
          iNode.Children.Insert(childIndex + 1, rightNode);
          if (key.CompareTo(kPrime) > 0) {
            childIndex++;
            child = rightNode;
          }
        }
        return this.InsertHelp(key, value, child);
      default: throw new InvalidOperationException("Unknown node type");
    }
  }

  /// <summary>
  /// Splits a node into two nodes
  /// 
  /// <param name="node">The node to split</param>
  /// <returns>A tuple containing the promoted key and the right node</returns>
  /// 
  /// Time Complexity: O(m)
  /// </summary>
  private (TKey promotedKey, Node rightNode) Split(Node node) {
    switch (node) {
      case LeafNode leaf:
        var count = leaf.Length;
        var mid = count / 2;

        var rightLeaf = new LeafNode {
          Keys = leaf.Keys.GetRange(mid, count - mid),
          Values = leaf.Values.GetRange(mid, count - mid),
          Next = leaf.Next
        };

        leaf.Keys = leaf.Keys.GetRange(0, mid);
        leaf.Values = leaf.Values.GetRange(0, mid);
        leaf.Next = rightLeaf;

        return (rightLeaf.Keys[0], rightLeaf);
      case InternalNode iNode:
        int keyCount = iNode.Length;
        int midIndex = keyCount / 2;

        var promotedKey = iNode.Keys[midIndex];
        var rightKeys = iNode.Keys.GetRange(midIndex + 1, keyCount - midIndex - 1);
        var rightChildren = iNode.Children.GetRange(midIndex + 1, keyCount - midIndex);

        var rightNode = new InternalNode {
          Keys = rightKeys,
          Children = rightChildren
        };

        iNode.Keys = iNode.Keys.GetRange(0, midIndex);
        iNode.Children = iNode.Children.GetRange(0, midIndex + 1);

        return (promotedKey, rightNode);
      default: throw new InvalidOperationException("Unknown node type");
    }
  }

  /// <summary>
  /// Builds the leaves of the tree
  /// 
  /// <param name="list">The list of items to build the leaves from</param>
  /// <returns>A list of tuples containing the first key of the leaf and the leaf node</returns>
  /// 
  /// Time Complexity: O(n)
  /// </summary>
  private List<(TKey? firstKey, LeafNode leaf)> BuildLeaves(List<TValue> list) {
    int chunkSize = this._rank - 1; // Max keys per leaf
    var result = new List<(TKey? firstKey, LeafNode leaf)>();
    LeafNode? prev = null;

    for (int i = 0; i < list.Count; i += chunkSize) {
      int endIndex = Math.Min(i + chunkSize, list.Count);
      var chunk = list.GetRange(i, endIndex - i);

      var leaf = new LeafNode {
        Keys = chunk.Select(GetKey).ToList(),
        Values = chunk.ToList(),
      };

      if (prev != null) prev.Next = leaf;
      prev = leaf;

      TKey? firstKey = chunk.Count > 0 ? GetKey(chunk[0]) : default;
      result.Add((firstKey, leaf));
    }
    return result;
  }

  private static TKey GetKey(TValue value) {
    if (value is null) {
      throw new ArgumentException("Cannot derive a key from a null value.");
    }

    // Primary path for assignment record types.
    if (value is IRecord<TKey> record) {
      return record.Key;
    }

    // Fast path when value already has the key type.
    if (value is TKey typedKey) {
      return typedKey;
    }

    var targetType = typeof(TKey);

    // Common case for string-like values.
    if (value is string s) {
      if (targetType == typeof(string)) {
        return (TKey)(object)s;
      }

      if (targetType.IsEnum) {
        return (TKey)Enum.Parse(targetType, s, ignoreCase: false);
      }

      var fromString = Convert.ChangeType(s, targetType, CultureInfo.InvariantCulture);
      return (TKey)fromString!;
    }

    // Fallback for numeric and other IConvertible values.
    var converted = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    return (TKey)converted!;
  }

  /// <summary>
  /// Collects all the items in the tree
  /// 
  /// <param name="root">The root of the tree</param>
  /// <returns>A list of tuples containing the key and value of the items</returns>
  /// 
  /// Time Complexity: O(n)
  /// </summary>
  private List<TValue> CollectAll(Node root) {
    var leaf = root;
    while (leaf is InternalNode iNode) {
      leaf = iNode.Children[0];
    }
    var acc = new List<TValue>();
    var cur = leaf as LeafNode;
    while (cur != null) {
      acc.AddRange(cur.Values);
      cur = cur.Next;
    }
    return acc;
  }

  /// <summary>
  /// Checks if a node is full
  /// 
  /// <param name="node">The node to check</param>
  /// <returns>True if the node is full, false otherwise</returns>
  /// 
  /// Time Complexity: O(1)
  /// </summary>
  private bool IsNodeFull(Node node) => node switch {
    LeafNode leaf => leaf.Length >= this._rank - 1,
    InternalNode iNode => iNode.Length >= this._rank - 1,
    _ => throw new InvalidOperationException()
  };

  /// <summary>
  /// Builds the tree from the leaves
  /// 
  /// <param name="leaves">The leaves to build the tree from</param>
  /// <returns>The root of the tree</returns>
  /// 
  /// Time Complexity: O(n)
  /// </summary>
  private Node BuildTree(List<(TKey? firstKey, LeafNode leaf)> leaves) {
    var nodes = leaves.Select(l => (l.firstKey, node: (Node)l.leaf)).ToList();
    return this.BuildTreeHelp(nodes);
  }

  /// <summary>
  /// Helper function for the BuildTree method
  /// 
  /// <param name="nodes">The nodes to build the tree from</param>
  /// <returns>The root of the tree</returns>
  /// 
  /// Time Complexity: O(n)
  /// </summary>
  private Node BuildTreeHelp(List<(TKey? firstKey, Node node)> nodes) {
    while (nodes.Count > 1) nodes = this.BuildLevel(nodes);
    return nodes[0].node;
  }

  /// <summary>
  /// Builds a level of the tree
  /// 
  /// <param name="nodes">The nodes to build the level from</param>
  /// <returns>The nodes in the level</returns>
  /// 
  /// Time Complexity: O(m) where m is the number of input nodes
  /// </summary>
  private List<(TKey? firstKey, Node node)> BuildLevel(List<(TKey? firstKey, Node node)> nodes) {
    int chunkSize = this._rank - 1;
    var result = new List<(TKey? firstKey, Node node)>();
    for (int i = 0; i < nodes.Count; i += chunkSize) {
      int endIndex = Math.Min(i + chunkSize, nodes.Count);
      var chunk = nodes.GetRange(i, endIndex - i);

      var key = chunk.Skip(1).Where(c => c.firstKey != null).Select(c => c.firstKey!).ToList();
      var children = chunk.Select(c => c.node).ToList();

      var iNode = new InternalNode { Keys = key, Children = children };
      result.Add((chunk[0].firstKey, iNode));
    }
    return result;
  }

  /// <summary>
  /// Helper function for the Delete method
  /// 
  /// <param name="node">The node to delete from</param>
  /// <param name="key">The key to delete</param>
  /// <param name="isRoot">True if the node is the root, false otherwise</param>
  /// <returns>A tuple containing whether the key was deleted and whether the node underflows</returns>
  /// 
  /// Time Complexity: O(m log n)
  /// </summary>
  private (bool deleted, bool underflow) DeleteHelp(Node node, TKey key, bool isRoot) {
    int maxKeys = this._rank - 1;
    int minKeys = isRoot ? 0 : maxKeys / 2;

    if (node is LeafNode leaf) {
      int index = this.LowerBound(leaf.Keys, key);
      if (index >= leaf.Length || leaf.Keys[index].CompareTo(key) != 0) return (false, false);

      leaf.Keys.RemoveAt(index);
      leaf.Values.RemoveAt(index);
      return (true, leaf.Length < minKeys);
    } else if (node is InternalNode iNode) {
      int index = this.ChildIndex(iNode.Keys, key);
      var child = iNode.Children[index];
      var (deleted, childUnderflow) = this.DeleteHelp(child, key, isRoot: false);
      if (!deleted) return (false, false);

      if (childUnderflow) {
        this.RebalanceChildAt(iNode, index);
      } else {
        this.SyncInternalKeys(iNode);
      }

      return (true, iNode.Length < minKeys);
    }
    throw new InvalidOperationException("Unknown node type");
  }

  /// <summary>
  /// Rebalances a child at a given index
  /// 
  /// <param name="parent">The parent node</param>
  /// <param name="index">The index of the child to rebalance</param>
  /// 
  /// Time Complexity: O(m log n)
  /// </summary>
  private void RebalanceChildAt(InternalNode parent, int index) {
    int maxKeys = this._rank - 1;
    int half = maxKeys / 2;
    var child = parent.Children[index];

    // Try borrowing from the left sibling
    if (index > 0) {
      var left = parent.Children[index - 1];
      if (left is LeafNode leftLeaf && child is LeafNode childLeaf && leftLeaf.Length > half) {
        int last = leftLeaf.Length - 1;
        childLeaf.Keys.Insert(0, leftLeaf.Keys[last]);
        childLeaf.Values.Insert(0, leftLeaf.Values[last]);
        leftLeaf.Keys.RemoveAt(last);
        leftLeaf.Values.RemoveAt(last);

        this.SyncInternalKeys(parent);
        return;
      }
      if (left is InternalNode leftInternal && child is InternalNode childInternal && leftInternal.Length > half) {
        int last = leftInternal.Children.Count - 1;
        childInternal.Children.Insert(0, leftInternal.Children[last]);
        leftInternal.Children.RemoveAt(last);
        this.SyncInternalKeys(leftInternal);
        this.SyncInternalKeys(childInternal);
        this.SyncInternalKeys(parent);
        return;
      }
    }

    // Try borrowing from the right sibling
    if (index + 1 < parent.Children.Count) {
      var right = parent.Children[index + 1];
      if (right is LeafNode rightLeaf && child is LeafNode childLeaf && rightLeaf.Length > half) {
        int first = 0;
        childLeaf.Keys.Add(rightLeaf.Keys[first]);
        childLeaf.Values.Add(rightLeaf.Values[first]);
        rightLeaf.Keys.RemoveAt(first);
        rightLeaf.Values.RemoveAt(first);
        this.SyncInternalKeys(parent);
        return;
      }
      if (right is InternalNode rightInternal && child is InternalNode childInternal && rightInternal.Length > half) {
        int first = 0;
        childInternal.Children.Add(rightInternal.Children[first]);
        rightInternal.Children.RemoveAt(first);
        this.SyncInternalKeys(rightInternal);
        this.SyncInternalKeys(childInternal);
        this.SyncInternalKeys(parent);
        return;
      }
    }

    // merging
    if (index > 0) {
      // merge child into left sibling
      var left = parent.Children[index - 1];
      if (left is LeafNode leftLeaf && child is LeafNode childLeaf) {
        leftLeaf.Keys.AddRange(childLeaf.Keys);
        leftLeaf.Values.AddRange(childLeaf.Values);
        leftLeaf.Next = childLeaf.Next;
      } else if (left is InternalNode leftInternal && child is InternalNode childInternal) {
        leftInternal.Children.AddRange(childInternal.Children);
        this.SyncInternalKeys(leftInternal);
      }
      parent.Children.RemoveAt(index);
    } else if (index + 1 < parent.Children.Count) {
      // merge right sibling into child
      var right = parent.Children[index + 1];
      if (right is LeafNode rightLeaf && child is LeafNode childLeaf) {
        childLeaf.Keys.AddRange(rightLeaf.Keys);
        childLeaf.Values.AddRange(rightLeaf.Values);
        childLeaf.Next = rightLeaf.Next;
      } else if (right is InternalNode rightInternal && child is InternalNode childInternal) {
        childInternal.Children.AddRange(rightInternal.Children);
        this.SyncInternalKeys(childInternal);
      }
      parent.Children.RemoveAt(index + 1);
    }
    this.SyncInternalKeys(parent);
  }

  /// <summary>
  /// Syncs the keys of an internal node
  /// 
  /// <param name="node">The node to sync</param>
  /// 
  /// Time Complexity: O(m * height) where h is the subtree height
  /// </summary>
  private void SyncInternalKeys(InternalNode node) {
    node.Keys.Clear();
    for (int i = 1; i < node.Children.Count; i++) {
      var key = this.LeftmostLeafKey(node.Children[i]);
      if (key != null) node.Keys.Add(key);
    }
  }

  /// <summary>
  /// Finds the leftmost leaf key of a node
  /// 
  /// <param name="node">The node to find the leftmost leaf key of</param>
  /// <returns>The leftmost leaf key of the node</returns>
  /// 
  /// Time Complexity: O(height) where h is the subtree height
  /// Approx O(log n)
  /// </summary>
  private TKey? LeftmostLeafKey(Node node) {
    while (node is InternalNode iNode) {
      node = iNode.Children[0];
    }
    return node is LeafNode leaf && leaf.Length > 0 ? leaf.Keys[0] : default(TKey);
  }
}

internal static class Program {
  private static void Main() {
  }
}
