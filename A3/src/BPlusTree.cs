using System;
using System.Collections.Generic;
using System.Linq;

namespace BPlusTree;

#nullable enable

// TODO: Document this
public class DuplicateKeyException : Exception {
  // TODO: Document this
  public DuplicateKeyException() : base("Duplicate key inserted into B+ tree") { }
}

// TODO: Document this
public class BPlusTree<TKey, TValue> where TKey : IComparable<TKey> {
  // enum rec Node<kType, rType> {
  //     Internal {
  //     keys: Box<List<kType>>,
  //     children: Box<List<Node<kType, rType>>>,
  //   },
  //   Leaf {
  //     keys: Box<List<kType>>,
  //     values: Box<List<rType>>,
  //     next: Box<Option<Node<kType, rType>>>,
  //   },
  // }
  // TODO: Document this
  private abstract class Node {
    // TODO: Document
    public abstract int Length { get; }
    /// <summary>
    /// Checks if a node is full.
    /// Time Complexity: O(1)
    /// </summary>
    /// <returns>`true` if the node is full, otherwise `false`</returns>
    public abstract bool IsNodeFull(int rank);
    /// <summary>
    /// Splits a node into two nodes.
    /// Time Complexity: O(1)
    /// </summary>
    /// <returns>A tuple containing the promoted key and the right node</returns>
    public abstract (TKey promotedKey, Node rightNode) Split();
    /// <summary>
    /// Finds the index of the key in the list of keys
    /// 
    /// <param name="keys">The list of keys</param>
    /// <param name="key">The key to find</param>
    /// 
    /// Time Complexity: O(m)
    /// </summary>
      // TODO: Make this abstract and implement it on each node type
    public static int ChildIndex(List<TKey> keys, TKey key) {
      for (int i = 0; i < keys.Count; i++) {
        if (keys[i].CompareTo(key) > 0) return i;
      }
      return keys.Count;
    }
  }
  // TODO: Document this (Consider making them children Node)
  private class LeafNode(List<TKey> Keys, List<Record<TKey, TValue>> Values, LeafNode? Next) : Node {
    public List<TKey> Keys = Keys;
    public List<Record<TKey, TValue>> Values = Values;
    public LeafNode? Next = Next;
    public override int Length => this.Keys.Count;
    public override bool IsNodeFull(int rank) => this.Length >= rank - 1;
    public override (TKey promotedKey, Node rightNode) Split() {
      // Compute the midpoint of the keys and values to split at
      var mid = this.Length / 2;
      // Split the keys and values into two lists for the left and right nodes
      var rightKeys = this.Keys.GetRange(mid, this.Length - mid); // O(1)
      var rightValues = this.Values.GetRange(mid, this.Length - mid); // O(1)
      var leftKeys = this.Keys.GetRange(0, mid); // O(1)
      var leftValues = this.Values.GetRange(0, mid); // O(1)
      // Create our new right node
      var rightLeaf = new LeafNode(rightKeys, rightValues, this.Next);
      // Set the node to be the left node
      this.Keys = leftKeys;
      this.Values = leftValues;
      this.Next = rightLeaf;
      // Return the promoted key and the new right node
      return (rightLeaf.Keys[0], rightLeaf);
    }
  }
  // TODO: Document this
  private class InternalNode(List<TKey> Keys, List<Node> Children) : Node {
    public List<TKey> Keys = Keys;
    public List<Node> Children = Children;

    public override int Length => this.Keys.Count;
    public override bool IsNodeFull(int rank) => this.Length >= rank - 1;
    public override (TKey promotedKey, Node rightNode) Split() {
      // Compute the midpoint of the keys and values to split at
      int mid = this.Length / 2;
      // Split the keys and values into two lists for the left and right nodes
      var promotedKey = this.Keys[mid];
      var rightKeys = this.Keys.GetRange(mid + 1, this.Length - mid - 1); // we do +1 here to skip the promoted key
      var rightChildren = this.Children.GetRange(mid + 1, this.Length - mid);
      var leftKeys = this.Keys.GetRange(0, mid);
      var leftChildren = this.Children.GetRange(0, mid + 1);
      // Create our new right node
      var rightNode = new InternalNode(rightKeys, rightChildren);
      // Set the node to be the left node
      this.Keys = leftKeys;
      this.Children = leftChildren;
      // Return the promoted key and the new right node
      return (promotedKey, rightNode);
    }
  }
  // TODO: Document this
  private readonly int _rank; // Maximum keys per node
                              // TODO: Document this
  private Node _root;

  /// <summary>
  /// Creates an empty B+ tree with the given rank.
  /// Time Complexity: O(1)
  /// </summary>
  /// <param name="rank">The rank of the tree (The maximum number of nodes per node)</param>
  /// <throws cref="ArgumentException">Thrown if the rank is less than 3</throws>
  public BPlusTree(int rank) {
    if (rank < 3) throw new ArgumentException("Rank must be at least 3");
    this._rank = rank;
    this._root = new LeafNode([], [], null);
  }

  /// <summary>
  /// Creates a B+ tree with the given rank and initial records
  /// Time Complexity: See bulkInsert
  /// </summary>
  /// <param name="rank">The rank of the tree (The maximum number of nodes per node)</param>
  /// <param name="items">The initial records to insert into the tree</param>
  /// <throws cref="ArgumentException">Thrown if the rank is less than 3</throws>
  public BPlusTree(int rank, IEnumerable<Record<TKey, TValue>> items) : this(rank) => this.BulkInsert(items);

  // Private Helpers
  #region PrivateHelpers
  /// <summary>
  /// Helper function to get the leaves in order from a starting leaf node.
  /// Time Complexity: O(n) at worst
  /// </summary>
  /// <param name="leaf">The starting leaf node</param>
  /// <returns>An enumerable of leaf nodes starting from the given leaf node and following the `Next` pointers</returns>
  private static IEnumerable<LeafNode> GetLeavesFromStart(LeafNode leaf) {
    LeafNode next = leaf;
    while (true) {
      yield return next;
      if (next.Next == null) break;
      next = next.Next;
    }
  }
  /// <summary>
  /// Gets the left most leaf key in the tree rooted at the given node.
  /// Time Complexity: O(log n) or O(height)
  /// </summary>
  /// <param name="node">The root node of the search</param>
  /// <returns>The leftmost leaf node of the tree</returns>
  private static LeafNode GetLeftMostLeaf(Node node) {
    Node curr = node;
    // Descend the left most path until we reach a leaf node
    while (true) {
      switch (curr) {
        case InternalNode iNode:
          // Descend to the next level of the tree by following the first child pointer
          curr = iNode.Children[0];
          break;
        // We reached our target
        case LeafNode leaf: return leaf;
        // NOTE: This could never happen but we need to satisfy the type system
        default: throw new InvalidOperationException("Impossible: Somehow an abstract Node was instantiated");
      }
    }
  }
  /// <summary>
  /// Searches for the node containing the key
  /// Time Complexity: O(log n)
  /// </summary>
  /// <param name="key">The key to search for</param>
  /// <param name="node">The node to search in</param>
  /// <returns>The leaf node containing the key, or the leaf node where the key would be if it were in the tree</returns>
  private static LeafNode SearchHelp(TKey key, Node node) {
    // Traverse down the tree until we reach the appropriate leaf node
    Node curr = node;
    while (true) {
      switch (curr) {
        // NOTE: If there was ever a cycle in the tree this would loop infinitely, 
        //       but that should never happen with a correctly implemented B+ tree
        case InternalNode iNode:
          int index = Node.ChildIndex(iNode.Keys, key);
          curr = iNode.Children[index];
          break;
        case LeafNode leaf: return leaf;
        default: throw new InvalidOperationException("Impossible: Somehow an abstract Node was instantiated");
      }
    }
  }
  /// <summary>
  /// Helper function for the Insert method
  /// Time Complexity: O(log n)
  /// </summary>
  /// <param name="entry">The entry to insert</param>
  /// <param name="node">The node to insert into</param>
  /// <returns>`true` if the entry was inserted, otherwise `false`</returns>
  private bool InsertHelp(Record<TKey, TValue> entry, Node node) {
    switch (node) {
      // We found the leaf and now must insert into the leaf
      case LeafNode leaf:
        // Look up the childIndex to insert the key-value pair at
        int index = Node.ChildIndex(leaf.Keys, entry.Key);
        // Check if the key is already in the tree.
        // NOTE: It would be at `index - 1` because childIndex gives us the current insertion position
        if (index > 0 && leaf.Keys[index - 1].CompareTo(entry.Key) == 0) return false;
        // TODO: I don't think we can ever hit this case because of the above note
        if (index < leaf.Length && leaf.Keys[index].CompareTo(entry.Key) == 0) return false;
        // Insert the key-value pair into the leaf node at the appropriate index
        leaf.Keys.Insert(index, entry.Key);
        leaf.Values.Insert(index, entry);
        return true;
      // We are looking for the leaf node to insert into, but currently are on an internal node
      case InternalNode iNode:
        // Look up the childIndex to insert the key-value pair at
        int childIndex = Node.ChildIndex(iNode.Keys, entry.Key);
        var child = iNode.Children[childIndex];
        // Pre-emptively split the child if it is full to guarantee we never have to split on the way back up
        if (child.IsNodeFull(this._rank)) {
          // Split the node
          var (kPrime, rightNode) = child.Split();
          // Insert the promoted key and the new right node into the internal node
          iNode.Keys.Insert(childIndex, kPrime);
          iNode.Children.Insert(childIndex + 1, rightNode);
          // Determine which of the two nodes we should insert the new key-value pair into
          if (entry.Key.CompareTo(kPrime) > 0) child = rightNode;
        }
        // Insert into the child node and return whether the insertion was successful
        return this.InsertHelp(entry, child);
      // NOTE: This is impossible to hit c#'s pattern matching just isn't very smart
      default: throw new InvalidOperationException("Unknown node type");
    }
  }
  /// <summary>
  /// Builds the leaves of the tree when we are doing a bulkInsert.s
  /// Time Complexity: O(n)
  /// </summary>
  /// <param name="list">The list of items to build the leaves from</param>
  /// <returns>A list of tuples containing the first key of the leaf and the leaf node</returns>
  private List<(TKey? firstKey, Node node)> BuildLeaves(List<Record<TKey, TValue>> list) {
    int chunkSize = this._rank - 1; // Max keys per leaf
    var acc = new List<(TKey? firstKey, Node leaf)>();
    var chunks = list.Chunk(chunkSize);
    LeafNode? prev = null;
    foreach (var chunk in chunks) {
      // NOTE: If we didn't have to iterate through the chunk to get the keys, this function could be o(n/chunkSize) instead of o(n)
      prev = new LeafNode(chunk.Select(e => e.Key).ToList(), chunk.ToList(), prev?.Next ?? null);
      acc.Add((chunk[0].Key, prev));
    }
    return acc;
  }

  /// <summary>
  /// Builds the tree from the leaves.
  /// Time Complexity: O(log n)
  /// </summary>
  /// <param name="nodes">The nodes to build the tree from</param>
  /// <returns>The root of the tree</returns>
  private Node BuildTree(List<(TKey? firstKey, Node node)> nodes) {
    List<(TKey? firstKey, Node node)> nodeStack = nodes;
    while (nodeStack.Count > 1) nodeStack = this.BuildLevel(nodeStack);
    return nodeStack[0].node;
  }
  /// <summary>
  /// Builds a level of the tree
  /// Time Complexity: O(log n) where m is the number of input nodes
  /// </summary>
  /// <param name="nodes">The nodes to build the level from</param>
  /// <returns>The nodes in the level</returns>
  private List<(TKey? firstKey, Node node)> BuildLevel(List<(TKey? firstKey, Node node)> nodes) {
    // TODO: I think we can clean this up quite a bit using List.chunk
    int chunkSize = this._rank - 1;
    var result = new List<(TKey? firstKey, Node node)>();
    for (int i = 0; i < nodes.Count; i += chunkSize) {
      int endIndex = Math.Min(i + chunkSize, nodes.Count);
      var chunk = nodes.GetRange(i, endIndex - i);
      var key = chunk.Skip(1).Where(c => c.firstKey != null).Select(c => c.firstKey!).ToList();
      var children = chunk.Select(c => c.node).ToList();
      var iNode = new InternalNode(key, children);
      result.Add((chunk[0].firstKey, iNode));
    }
    return result;
  }
  #endregion

  /// <summary>
  /// Checks if the tree is empty
  /// Time Complexity: O(1)
  /// </summary>
  public bool IsEmpty => this._root switch {
    LeafNode leaf => leaf.Length == 0,
    _ => false,
  };

  /// <summary>
  /// Searches the tree for a key.
  /// Time Complexity: O(log n)
  /// </summary>
  /// 
  /// <param name="key">The key to search for</param>
  /// <param name="outValue">The reference to be set to the value if the key is found.</param>
  /// 
  /// <returns>`true` if the key was found, `false` otherwise. The value is returned via the `outValue` parameter.</returns>
  public bool Search(TKey key, ref Record<TKey, TValue>? outValue) {
    // Find the leaf node that should contain the value for the key
    LeafNode leaf = BPlusTree<TKey, TValue>.SearchHelp(key, this._root);
    // Search for the key in the leaf node
    int index = Node.ChildIndex(leaf.Keys, key) - 1;
    if (index >= 0 && index < leaf.Length && leaf.Keys[index].CompareTo(key) == 0) {
      outValue = leaf.Values[index];
      return true;
    } else return false;
  }

  /// <summary>
  /// Retrieves all values between the start and end keys, inclusive. 
  /// If no keys are in the range, returns an empty list.
  /// Time Complexity: O(log n)
  /// </summary>
  /// <param name="start">The start of the range</param>
  /// <param name="end">The end of the range</param>
  /// <returns>A list of values corresponding to keys in the range [start, end]</returns>
  public List<Record<TKey, TValue>> Range(TKey start, TKey end) {
    LeafNode leaf = BPlusTree<TKey, TValue>.SearchHelp(start, this._root);
    var acc = new List<Record<TKey, TValue>>();
    // Iterate from the starting leaf node until we reach the end key
    foreach (var l in BPlusTree<TKey, TValue>.GetLeavesFromStart(leaf)) {
      // NOTE: A small optimization we could do here is if values[0] > start && values[-1] < end then we can just add them all
      foreach (var entry in l.Values) {
        // If we are past they end key we can stop iterating
        if (entry.Key.CompareTo(end) > 0) return acc;
        // We don't want to add entries that are before the start key
        if (entry.Key.CompareTo(start) >= 0) acc.Add(entry);
      }
    }
    return acc;
  }

  /// <summary>
  /// Inserts a value into the tree
  /// Time Complexity: O(log n)
  /// </summary>
  /// <param name="entry">The entry to insert</param>
  /// <exception cref="DuplicateKeyException">Thrown if the key is already in the tree</exception>
  public void Insert(Record<TKey, TValue> entry) {
    // Eagerly split the root when full so we never need to split on the way back up
    if (this._root.IsNodeFull(this._rank)) {
      var (kPrime, rightNode) = this._root.Split();
      this._root = new InternalNode([kPrime], [this._root, rightNode]);
    }
    // Insert the key-value pair into the appropriate leaf
    if (!this.InsertHelp(entry, this._root)) {
      // If we couldn't perform the insertion it means the key was already in the tree.
      throw new DuplicateKeyException();
    }
  }
  /// <summary>
  /// Inserts multiple key-value pairs into the tree.
  /// Time Complexity: O(n log n) in the case where the items are not sorted.
  /// Time Complexity: O(log n) in the case where the items are already sorted.
  /// </summary>
  /// <param name="entries">The entries to insert</param>
  /// <exception cref="InvalidOperationException">Thrown if the tree is not empty</exception>
  public void BulkInsert(IEnumerable<Record<TKey, TValue>> entries) {
    if (entries.Any()) return;
    if (!this.IsEmpty) throw new InvalidOperationException("BulkInsert requires an empty tree");
    var list = entries.ToList();
    // Time Complexity: O(n log n)
    list.Sort((a, b) => a.Key.CompareTo(b.Key));
    // Time Complexity: O(n)
    for (int i = 1; i < list.Count; i++) {
      // Because the list is sorted, if there are duplicate keys they will be adjacent to each other, so we just check if there are any adjacent entries with the same key and throw an exception if so.
      if (list[i - 1].Key.CompareTo(list[i].Key) == 0) {
        // We found a duplicate key throw an exception
        throw new DuplicateKeyException();
      }
    }
    // Build leaf level
    var leaves = this.BuildLeaves(list); // Time Complexity: O(log n)
    this._root = this.BuildTree(leaves); // Time Complexity: O(log n)
  }
  /// <summary>
  /// Deletes a key from the tree.
  /// Time Complexity: O(n)
  /// </summary>
  /// <param name="key">The key to delete</param>
  /// <returns>True if the key was deleted, false otherwise</returns>
  public bool Delete(TKey key) {
    // TODO: Replace this impl
    // NOTE: While this could be a lot more efficient it surprisingly doesn't scale that much worse todo the super naive,
    //      implementation of just removing it from the list of entires and rebuilding the tree. If we were to implement the more
    //      efficient deletion algorithm that involves complex rebalancing we could get to O(log n) time complexity, but the constant
    //      factor would be far worse and it would be a lot more code, so we opted for the simpler implementation.
    var firstLeaf = BPlusTree<TKey, TValue>.GetLeftMostLeaf(this._root);
    var entries = BPlusTree<TKey, TValue>.GetLeavesFromStart(firstLeaf).SelectMany(l => l.Values).ToList();
    // TODO: Lets clean this up????
    int index = Node.ChildIndex(entries.Select(e => e.Key).ToList(), key) - 1;
    if (index >= entries.Count || entries[index].Key.CompareTo(key) != 0) {
      return false;
    }
    entries.RemoveAt(index);
    this._root = new LeafNode([], [], null);
    this.BulkInsert(entries);
    return true;
  }
  /// <summary>
  /// Merges two trees into a new tree
  /// Time Complexity: O(n + m) where n and m are the number of entries in the two trees
  /// </summary>
  /// <param name="tree1">The first tree to merge</param>
  /// <param name="tree2">The second tree to merge</param>
  /// <returns>A new tree containing all the entries from both trees</returns>
  public static BPlusTree<TKey, TValue> Merge(BPlusTree<TKey, TValue> tree1, BPlusTree<TKey, TValue> tree2) {
    var newTree = new BPlusTree<TKey, TValue>(tree1._rank);
    // Collect our entries
    var tree1LeftMostLeaf = BPlusTree<TKey, TValue>.GetLeftMostLeaf(tree1._root); // Get the left most leaf
    var tree1Entries = BPlusTree<TKey, TValue>.GetLeavesFromStart(tree1LeftMostLeaf).SelectMany(l => l.Values);
    var tree2LeftMostLeaf = BPlusTree<TKey, TValue>.GetLeftMostLeaf(tree2._root); // Get the left most leaf
    var tree2Entries = BPlusTree<TKey, TValue>.GetLeavesFromStart(tree2LeftMostLeaf).SelectMany(l => l.Values);
    // NOTE: A slightly more efficent way of doing the concat that would also handle the sort is go 
    //       through both enumerable at the same time taking the smaller entry each time into the 
    //       accumulator list. This is O(n + m) and negates the need to sort.
    var entries = tree1Entries.Concat(tree2Entries).ToList();
    // NOTE: This sort is technically o(n log n) but because the trees are already sorted on their own it will require a lot less sorting and will be closer to O(n + M)
    entries.Sort((a, b) => a.Key.CompareTo(b.Key));
    // We can now just insert our entires into the new tree using bulk insert.
    newTree.BulkInsert(entries); // o(log n) as the list is sorted
    return newTree;
  }
  public BPlusTree<TKey, TValue> Merge(BPlusTree<TKey, TValue> other) => Merge(this, other);
  // /// <summary>
  // /// Helper function for the Delete method
  // /// 
  // /// <param name="node">The node to delete from</param>
  // /// <param name="key">The key to delete</param>
  // /// <param name="isRoot">True if the node is the root, false otherwise</param>
  // /// <returns>A tuple containing whether the key was deleted and whether the node underflows</returns>
  // /// 
  // /// Time Complexity: O(m log n)
  // /// </summary>
  // private (bool deleted, bool underflow) DeleteHelp(Node node, TKey key, bool isRoot) {
  //   int maxKeys = this._rank - 1;
  //   int minKeys = isRoot ? 0 : maxKeys / 2;

  //   if (node is LeafNode leaf) {
  //     int index = this.LowerBound(leaf.Keys, key);
  //     if (index >= leaf.Length || leaf.Keys[index].CompareTo(key) != 0) return (false, false);

  //     leaf.Keys.RemoveAt(index);
  //     leaf.Values.RemoveAt(index);
  //     return (true, leaf.Length < minKeys);
  //   } else if (node is InternalNode iNode) {
  //     int index = this.ChildIndex(iNode.Keys, key);
  //     var child = iNode.Children[index];
  //     var (deleted, childUnderflow) = this.DeleteHelp(child, key, isRoot: false);
  //     if (!deleted) return (false, false);

  //     if (childUnderflow) {
  //       this.RebalanceChildAt(iNode, index);
  //     } else {
  //       this.SyncInternalKeys(iNode);
  //     }

  //     return (true, iNode.Length < minKeys);
  //   }
  //   throw new InvalidOperationException("Unknown node type");
  // }

  // /// <summary>
  // /// Rebalances a child at a given index
  // /// 
  // /// <param name="parent">The parent node</param>
  // /// <param name="index">The index of the child to rebalance</param>
  // /// 
  // /// Time Complexity: O(m log n)
  // /// </summary>
  // private void RebalanceChildAt(InternalNode parent, int index) {
  //   int maxKeys = this._rank - 1;
  //   int half = maxKeys / 2;
  //   var child = parent.Children[index];

  //   // Try borrowing from the left sibling
  //   if (index > 0) {
  //     var left = parent.Children[index - 1];
  //     if (left is LeafNode leftLeaf && child is LeafNode childLeaf && leftLeaf.Length > half) {
  //       int last = leftLeaf.Length - 1;
  //       childLeaf.Keys.Insert(0, leftLeaf.Keys[last]);
  //       childLeaf.Values.Insert(0, leftLeaf.Values[last]);
  //       leftLeaf.Keys.RemoveAt(last);
  //       leftLeaf.Values.RemoveAt(last);

  //       this.SyncInternalKeys(parent);
  //       return;
  //     }
  //     if (left is InternalNode leftInternal && child is InternalNode childInternal && leftInternal.Length > half) {
  //       int last = leftInternal.Children.Count - 1;
  //       childInternal.Children.Insert(0, leftInternal.Children[last]);
  //       leftInternal.Children.RemoveAt(last);
  //       this.SyncInternalKeys(leftInternal);
  //       this.SyncInternalKeys(childInternal);
  //       this.SyncInternalKeys(parent);
  //       return;
  //     }
  //   }

  //   // Try borrowing from the right sibling
  //   if (index + 1 < parent.Children.Count) {
  //     var right = parent.Children[index + 1];
  //     if (right is LeafNode rightLeaf && child is LeafNode childLeaf && rightLeaf.Length > half) {
  //       int first = 0;
  //       childLeaf.Keys.Add(rightLeaf.Keys[first]);
  //       childLeaf.Values.Add(rightLeaf.Values[first]);
  //       rightLeaf.Keys.RemoveAt(first);
  //       rightLeaf.Values.RemoveAt(first);
  //       this.SyncInternalKeys(parent);
  //       return;
  //     }
  //     if (right is InternalNode rightInternal && child is InternalNode childInternal && rightInternal.Length > half) {
  //       int first = 0;
  //       childInternal.Children.Add(rightInternal.Children[first]);
  //       rightInternal.Children.RemoveAt(first);
  //       this.SyncInternalKeys(rightInternal);
  //       this.SyncInternalKeys(childInternal);
  //       this.SyncInternalKeys(parent);
  //       return;
  //     }
  //   }

  //   // merging
  //   if (index > 0) {
  //     // merge child into left sibling
  //     var left = parent.Children[index - 1];
  //     if (left is LeafNode leftLeaf && child is LeafNode childLeaf) {
  //       leftLeaf.Keys.AddRange(childLeaf.Keys);
  //       leftLeaf.Values.AddRange(childLeaf.Values);
  //       leftLeaf.Next = childLeaf.Next;
  //     } else if (left is InternalNode leftInternal && child is InternalNode childInternal) {
  //       leftInternal.Children.AddRange(childInternal.Children);
  //       this.SyncInternalKeys(leftInternal);
  //     }
  //     parent.Children.RemoveAt(index);
  //   } else if (index + 1 < parent.Children.Count) {
  //     // merge right sibling into child
  //     var right = parent.Children[index + 1];
  //     if (right is LeafNode rightLeaf && child is LeafNode childLeaf) {
  //       childLeaf.Keys.AddRange(rightLeaf.Keys);
  //       childLeaf.Values.AddRange(rightLeaf.Values);
  //       childLeaf.Next = rightLeaf.Next;
  //     } else if (right is InternalNode rightInternal && child is InternalNode childInternal) {
  //       childInternal.Children.AddRange(rightInternal.Children);
  //       this.SyncInternalKeys(childInternal);
  //     }
  //     parent.Children.RemoveAt(index + 1);
  //   }
  //   this.SyncInternalKeys(parent);
  // }

  // /// <summary>
  // /// Syncs the keys of an internal node
  // /// 
  // /// <param name="node">The node to sync</param>
  // /// 
  // /// Time Complexity: O(m * height) where h is the subtree height
  // /// </summary>
  // private void SyncInternalKeys(InternalNode node) {
  //   node.Keys.Clear();
  //   for (int i = 1; i < node.Children.Count; i++) {
  //     var key = this.LeftmostLeafKey(node.Children[i]);
  //     if (key != null) node.Keys.Add(key);
  //   }
  // }
}

internal static class Program {
  private static void Main() {
  }
}
