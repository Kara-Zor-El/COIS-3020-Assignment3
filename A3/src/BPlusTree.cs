using System;
using System.Collections.Generic;
using System.Linq;

namespace BPlusTree;

/// <summary>The methods required by the assignment.</summary>
public interface IBPlusTree<TKey, TValue> where TKey : IComparable<TKey> {
  public void Insert(Record<TKey, TValue> entry);
  public bool Delete(TKey key);
#nullable enable
  public bool Search(TKey key, ref Record<TKey, TValue>? outValue);
#nullable disable
  public void BulkInsert(IEnumerable<Record<TKey, TValue>> entries);
  public List<Record<TKey, TValue>> Range(TKey key1, TKey key2);
  public BPlusTree<TKey, TValue> Merge(BPlusTree<TKey, TValue> tree);
}

/// <summary>An exception to be thrown when a key is inserted that is already in the tree.</summary>
/// <param name="key">The duplicate key that we were attempting to insert</param>
public class DuplicateKeyException(string key) : Exception($"Duplicate key `{key}` inserted into B+ tree") { }

/// <summary>
/// A B+tree data structure.
///
/// A B+tree is a variant of a B-tree in which each node contains only keys,
/// and to which an additional level of indirection is added to allow for efficient range queries.
/// In a B+tree, all values are stored in the leaf nodes, and the internal nodes only store keys
/// to guide the search process.
///
/// See <see href="https://en.wikipedia.org/wiki/B%2B_tree">B+ tree wiki</see> for more information.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the tree</typeparam>
/// <typeparam name="TValue">The type of the values in the tree</typeparam>
public class BPlusTree<TKey, TValue> : IBPlusTree<TKey, TValue> where TKey : IComparable<TKey> {
  // NOTE: For all assignments done for 3020, we started by writing the code in Grain
  // and then ported our implementation to C#.
  // Below is a much more elegant understanding of what the tree structure should look like.
  // Which is more intuitive than abstract classes.
  // See full implementation here: https://github.com/spotandjake/grain-dsa
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

  /// <summary>
  /// Within a B+ tree, all nodes will be either a leaf node or an internal node.
  /// This abstract class is used to represent both types of nodes.
  /// </summary>
  private abstract class Node {
    /// <summary>
    /// The number of keys in the node.
    /// Time Complexity: O(1)
    /// </summary>
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
    /// <summary>Finds the index of the key in the list of keys.</summary>
    /// <param name="key">The key to find</param>
    /// <returns>The index of the key in the list of keys</returns>
    public abstract int ChildIndex(TKey key);
  }
  /// <summary>A leaf node in the B+ tree.</summary>
  /// <param name="Keys">The keys in the leaf node</param>
  /// <param name="Values">The values in the leaf node</param>
  /// <param name="Next">The next leaf node in the linked list of leaves</param>
#nullable enable
  private class LeafNode(IEnumerable<TKey> Keys, IEnumerable<Record<TKey, TValue>> Values, LeafNode? Next) : Node {
    public List<TKey> Keys = Keys.ToList();
    public List<Record<TKey, TValue>> Values = Values.ToList();
    public LeafNode? Next = Next;
#nullable disable
    // Public interface
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
    public override int ChildIndex(TKey key) {
      // NOTE: This is currently `o(n)`, we could make this `o(log n)` with a binary search.
      for (int i = 0; i < this.Keys.Count; i++) {
        if (this.Keys[i].CompareTo(key) > 0) return i;
      }
      return this.Keys.Count;
    }
  }
  /// <summary>An internal node in the B+ tree.</summary>
  /// <param name="Keys">The keys in the internal node</param>
  /// <param name="Children">The children of the internal node</param>
  private class InternalNode(IEnumerable<TKey> Keys, IEnumerable<Node> Children) : Node {
    public List<TKey> Keys = Keys.ToList();
    public List<Node> Children = Children.ToList();
    // Public interface
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
    public override int ChildIndex(TKey key) {
      // NOTE: This is currently `o(n)`, we could make this `o(log n)` with a binary search.
      for (int i = 0; i < this.Keys.Count; i++) {
        if (this.Keys[i].CompareTo(key) > 0) return i;
      }
      return this.Keys.Count;
    }
  }

  /// <summary>The rank of the tree (The maximum number of nodes per node).</summary>
  private readonly int _rank;
  /// <summary>The root node of the tree.</summary>
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
  /// Creates a B+ tree with the given rank and initial entries
  /// Time Complexity: See bulkInsert
  /// </summary>
  /// <param name="rank">The rank of the tree (The maximum number of nodes per node)</param>
  /// <param name="entries">The initial entries to insert into the tree</param>
  /// <throws cref="ArgumentException">Thrown if the rank is less than 3</throws>
  public BPlusTree(int rank, IEnumerable<Record<TKey, TValue>> entries) : this(rank) => this.BulkInsert(entries);

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
  /// Searches for the node containing the key.
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
          int index = iNode.ChildIndex(key);
          curr = iNode.Children[index];
          break;
        case LeafNode leaf: return leaf;
        default: throw new InvalidOperationException("Impossible: Somehow an abstract Node was instantiated");
      }
    }
  }

  /// <summary>
  /// Helper function for the Insert method.
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
        int index = leaf.ChildIndex(entry.Key);
        // Check if the key is already in the tree.
        // NOTE: It would be at `index - 1` because childIndex gives us the current insertion position
        if (index > 0 && leaf.Keys[index - 1].CompareTo(entry.Key) == 0) return false;
        // Insert the key-value pair into the leaf node at the appropriate index
        leaf.Keys.Insert(index, entry.Key);
        leaf.Values.Insert(index, entry);
        return true;
      // We are looking for the leaf node to insert into, but currently are on an internal node
      case InternalNode iNode:
        // Look up the childIndex to insert the key-value pair at
        int childIndex = iNode.ChildIndex(entry.Key);
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
      default: throw new InvalidOperationException("Impossible, Unknown node type");
    }
  }
  /// <summary>
  /// Builds the leaves of the tree when we are doing a bulkInsert.
  /// Time Complexity: O(n)
  /// </summary>
  /// <param name="list">The list of items to build the leaves from</param>
  /// <returns>A list of tuples containing the first key of the leaf and the leaf node</returns>
  private List<(TKey firstKey, Node node)> BuildLeaves(List<Record<TKey, TValue>> list) {
    int chunkSize = this._rank - 1; // Max keys per leaf
    var acc = new List<(TKey firstKey, Node leaf)>();
    // C#'s type system is terrible at tracking null, the fact that you can pass a nullable type out of a nullable block seems like a major flaw in the design, but here we are.
#nullable enable
    LeafNode? prev = null;
#nullable disable
    foreach (var chunk in list.Chunk(chunkSize)) {
      // NOTE: If we didn't have to iterate through the chunk to get the keys, this function could be o(n/chunkSize) instead of o(n)
      var curr = new LeafNode(chunk.Select(e => e.Key), chunk, null);
      acc.Add((chunk[0].Key, curr));
      // Update our chain of leaf nodes
      if (prev != null) prev.Next = curr;
      prev = curr;
    }
    return acc;
  }

  /// <summary>
  /// Builds the tree from the leaves.
  /// Time Complexity: O(log n)
  /// </summary>
  /// <param name="nodes">The nodes to build the tree from</param>
  /// <returns>The root of the tree</returns>
  private Node BuildTree(List<(TKey firstKey, Node node)> nodes) {
    List<(TKey firstKey, Node node)> nodeStack = nodes;
    while (nodeStack.Count > 1) nodeStack = this.BuildLevel(nodeStack);
    return nodeStack[0].node;
  }
  /// <summary>
  /// Builds a level of the tree
  /// Time Complexity: O(log n) where m is the number of input nodes
  /// </summary>
  /// <param name="nodes">The nodes to build the level from</param>
  /// <returns>The nodes in the level</returns>
  private List<(TKey firstKey, Node node)> BuildLevel(List<(TKey firstKey, Node node)> nodes) {
    int chunkSize = this._rank - 1;
    var result = new List<(TKey firstKey, Node node)>();

    foreach (var chunk in nodes.Chunk(chunkSize)) {
      var key = chunk.Skip(1).Where(c => c.firstKey != null).Select(c => c.firstKey!);
      var children = chunk.Select(c => c.node);
      var iNode = new InternalNode(key, children);
      result.Add((chunk[0].firstKey, iNode));
    }

    return result;
  }

  /// <summary>
  /// Helper function for the Delete method.
  /// Time Complexity: O(log n)
  /// </summary>
  /// <param name="node">The node to delete from</param>
  /// <param name="key">The key to delete</param>
  /// <returns>A tuple containing `(minKey, deleted, deletedFirst)` respectively</returns>
#nullable enable
  private (TKey? key, bool deleted, bool deletedFirst) DeleteHelp(Node node, TKey key, bool isRoot) {
#nullable disable
    switch (node) {
      case LeafNode leaf: {
          int index = leaf.ChildIndex(key) - 1;
          if (index < 0 || index >= leaf.Length || leaf.Keys[index].CompareTo(key) != 0)
            return (leaf.Length > 0 ? leaf.Keys[0] : default, false, false);
          leaf.Keys.RemoveAt(index);
          leaf.Values.RemoveAt(index);
          return (leaf.Length > 0 ? leaf.Keys[0] : default, true, index == 0);
        }
      case InternalNode iNode: {
          int childIndex = iNode.ChildIndex(key);
          var child = iNode.Children[childIndex];
          // ceil(rank/2) for non-root nodes
          int minKeys = this._rank / 2;
          // Preemptively fix the child if it is at the minimum occupancy. (This can merge siblings if the child is almost empty)
          if (child.Length <= minKeys && iNode.Children.Count > 1) {
            childIndex = this.FixChild(iNode, childIndex);
            child = iNode.Children[childIndex];
          }
          // After a possible merge, the root might now only have one child, so we need to collapse it
          if (isRoot && iNode.Keys.Count == 0 && iNode.Children.Count == 1) {
            this._root = iNode.Children[0];
            // NOTE: This is in a tail call position meaning stack overflow isn't a worry outside of c#
            return this.DeleteHelp(this._root, key, isRoot: true);
          }
          var (minKey, deleted, deletedFirst) = this.DeleteHelp(child, key, isRoot: false);
          // If a child's first key changed due to deletion/rebalance, refresh the separator.
          if (deletedFirst && childIndex > 0 && child.Length > 0 && minKey != null) {
            iNode.Keys[childIndex - 1] = minKey; // the first key of the leftmost leaf
          }
          return (minKey, deleted, childIndex == 0 && deletedFirst);
        }
      default: throw new InvalidOperationException("Unknown node type");
    }
  }

  /// <summary>
  /// Fixes a child node of an internal node to have more than the minimum number of keys
  /// by either:
  /// - rotating a key from a sibling through the parent separator,
  /// - merging the child with a sibling and pulling the separator down
  /// 
  /// <remarks>It may shift left by one after a left-merge</remarks>
  /// </summary>
  /// <param name="parent">The parent node</param>
  /// <param name="childIndex">The index of the child to fix</param>
  /// <returns>The index in parent.Children that the caller should descent into</returns>
  private int FixChild(InternalNode parent, int childIndex) {
    int minKeys = this._rank / 2;
    var child = parent.Children[childIndex];
    // Try to borrow from the left sibling if it exists
    if (childIndex > 0) {
      var leftSibling = parent.Children[childIndex - 1];
      // Ensure that the right sibling has enough keys to donate before we try to borrow from it
      if (leftSibling.Length > minKeys) {
        // Rotate: left sibling donates its rightmost entry through the parent
        if (leftSibling is LeafNode leftLeaf && child is LeafNode childLeaf) {
          // For leaves: copy the separator down into the child, pushing the
          // siblings last key up to the parent separator slot
          int last = leftLeaf.Length - 1;
          childLeaf.Keys.Insert(0, leftLeaf.Keys[last]);
          childLeaf.Values.Insert(0, leftLeaf.Values[last]);
          leftLeaf.Keys.RemoveAt(last);
          leftLeaf.Values.RemoveAt(last);
          // The parent separator between left-sibling and child becomes the new first
          // key of child (already inserted above)
          // and update the separator.
          parent.Keys[childIndex - 1] = childLeaf.Keys[0];
        } else if (leftSibling is InternalNode leftInternal && child is InternalNode childInternal) {
          // For internal nodes: copy the separator down into the child, pushing the
          // siblings last key up to be the new separator
          childInternal.Keys.Insert(0, parent.Keys[childIndex - 1]);
          childInternal.Children.Insert(0, leftInternal.Children[leftInternal.Children.Count - 1]);
          parent.Keys[childIndex - 1] = leftInternal.Keys[leftInternal.Length - 1];
          leftInternal.Keys.RemoveAt(leftInternal.Length - 1);
          leftInternal.Children.RemoveAt(leftInternal.Children.Count - 1);
        }
        return childIndex; // child index is unchanged
      }
    }
    // Try to borrow from the right sibling if it exists
    if (childIndex < parent.Children.Count - 1) {
      var rightSibling = parent.Children[childIndex + 1];
      // Ensure that the right sibling has enough keys to donate before we try to borrow from it
      if (rightSibling.Length > minKeys) {
        if (rightSibling is LeafNode rightLeaf && child is LeafNode childLeaf) {
          childLeaf.Keys.Add(rightLeaf.Keys[0]);
          childLeaf.Values.Add(rightLeaf.Values[0]);
          rightLeaf.Keys.RemoveAt(0);
          rightLeaf.Values.RemoveAt(0);
          parent.Keys[childIndex] = rightLeaf.Keys[0];
        } else if (rightSibling is InternalNode rightInternal && child is InternalNode childInternal) {
          childInternal.Keys.Add(parent.Keys[childIndex]);
          childInternal.Children.Add(rightInternal.Children[0]);
          parent.Keys[childIndex] = rightInternal.Keys[0];
          rightInternal.Keys.RemoveAt(0);
          rightInternal.Children.RemoveAt(0);
        }
        return childIndex; // child index is unchanged
      }
    }
    // Merge: no sibling has enough keys
    // Prefer merging with the left sibling so the target child ends up in the left
    // node and `childIndex` decreases by one.
    // caller must use the returned index
    if (childIndex > 0) {
      // Merge child into left sibling
      var leftSibling = parent.Children[childIndex - 1];
      if (leftSibling is LeafNode leftLeaf && child is LeafNode childLeaf) {
        leftLeaf.Keys.AddRange(childLeaf.Keys);
        leftLeaf.Values.AddRange(childLeaf.Values);
        leftLeaf.Next = childLeaf.Next;
      } else if (leftSibling is InternalNode leftInternal && child is InternalNode childInternal) {
        // Pull the parent separator down into the merged internal node
        leftInternal.Keys.Add(parent.Keys[childIndex - 1]);
        leftInternal.Keys.AddRange(childInternal.Keys);
        leftInternal.Children.AddRange(childInternal.Children);
      }
      parent.Keys.RemoveAt(childIndex - 1);
      parent.Children.RemoveAt(childIndex);
      return childIndex - 1;
    } else {
      // Merge right sibling into child
      var rightSibling = parent.Children[childIndex + 1];
      if (rightSibling is LeafNode rightLeaf && child is LeafNode childLeaf) {
        childLeaf.Keys.AddRange(rightLeaf.Keys);
        childLeaf.Values.AddRange(rightLeaf.Values);
        childLeaf.Next = rightLeaf.Next;
      } else if (rightSibling is InternalNode rightInternal && child is InternalNode childInternal) {
        childInternal.Keys.Add(parent.Keys[childIndex]);
        childInternal.Keys.AddRange(rightInternal.Keys);
        childInternal.Children.AddRange(rightInternal.Children);
      }
      parent.Keys.RemoveAt(childIndex);
      parent.Children.RemoveAt(childIndex + 1);
      return childIndex; // child index is unchanged
    }
    throw new InvalidOperationException("Impossible: Somehow an abstract Node was instantiated");
  }
  #endregion

  /// <summary>
  /// Checks if the tree is empty.
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
#nullable enable
  public bool Search(TKey key, ref Record<TKey, TValue>? outValue) {
#nullable disable
    // Find the leaf node that should contain the value for the key
    LeafNode leaf = BPlusTree<TKey, TValue>.SearchHelp(key, this._root);
    // Search for the key in the leaf node
    int index = leaf.ChildIndex(key) - 1;
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
      throw new DuplicateKeyException(entry.Key.ToString() ?? "unknown");
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
    if (!entries.Any()) return;
    if (!this.IsEmpty) throw new InvalidOperationException("BulkInsert requires an empty tree");
    var list = entries.ToList();
    // Time Complexity: O(n log n)
    list.Sort((a, b) => a.Key.CompareTo(b.Key));
    // Time Complexity: O(n)
    for (int i = 1; i < list.Count; i++) {
      // Because the list is sorted, if there are duplicate keys they will be adjacent to each other, so we just check if there are any adjacent entries with the same key and throw an exception if so.
      if (list[i - 1].Key.CompareTo(list[i].Key) == 0) {
        // We found a duplicate key throw an exception
        throw new DuplicateKeyException(list[i].Key.ToString() ?? "unknown");
      }
    }
    // Build leaf level
    var leaves = this.BuildLeaves(list); // Time Complexity: O(log n)
    this._root = this.BuildTree(leaves); // Time Complexity: O(log n)
  }
  /// <summary>
  /// Deletes a key from the tree.
  /// Time Complexity: O(log n)
  /// </summary>
  /// <param name="key">The key to delete</param>
  /// <returns>`true` if the key was deleted, `false` otherwise</returns>
  public bool Delete(TKey key) {
    // A root that is an internal node with a single child can be collapsed.
    // We do this eagerly so we always have room to merge children during descent.
    if (this._root is InternalNode rootInternal && rootInternal.Children.Count == 1) {
      this._root = rootInternal.Children[0];
    }
    var (_, deleted, _) = this.DeleteHelp(this._root, key, isRoot: true);
    return deleted;
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
    // NOTE: A slightly more efficient way of doing the concat that would also handle the sort is go 
    //       through both enumerable at the same time taking the smaller entry each time into the 
    //       accumulator list. This is O(n + m) and negates the need to sort.
    var entries = tree1Entries.Concat(tree2Entries).ToList();
    // NOTE: This sort is technically o(n log n) but because the trees are already sorted on their own it will require a lot less sorting and will be closer to O(n + M)
    entries.Sort((a, b) => a.Key.CompareTo(b.Key));
    // We can now just insert our entries into the new tree using bulk insert.
    newTree.BulkInsert(entries); // o(log n) as the list is sorted
    return newTree;
  }
  public BPlusTree<TKey, TValue> Merge(BPlusTree<TKey, TValue> other) => Merge(this, other);
}

// NOTE: We expose a program so we can build a binary
internal static class Program {
  private static void Main() {
  }
}
