using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BPlusTree {
  /// <summary>
  /// Exception thrown when a duplicate key is inserted into the B+ tree.
  /// </summary>
  public sealed class DuplicateKeyException : Exception {
    public DuplicateKeyException() : base("Duplicate key inserted into B+ tree.") { }
  }

  /// <summary>
  /// Interface that records store in the B+ tree must provide access to the record's key.
  /// </summary>
  public interface IKeyed<KType> {
    KType Key { get; }
  }

  /// <summary>
  /// A B+ tree data structure with generic key type KType and record type TValue.
  /// - All records are stored in leaf nodes
  /// - internal nodes hold only separator keys
  /// - Leaf nodes are linked in a doubly-accessible chain for efficient range queries.
  /// - Insert and Delete are implemented top-down (non-recursively).
  /// - BulkInsert is implemented bottom-up.
  /// </summary>
  public class BPlusTree<KType, RType> where KType : IComparable<KType> where RType : IKeyed<KType> {
    private abstract class Node {}

    private class LeafNode : Node {
      public List<KType> Keys = new List<KType>();
      public List<RType> Values = new List<RType>();
      public LeafNode? Next = null;
    }

    private class InternalNode : Node {
      public List<KType> Keys = new List<KType>();
      public List<Node> Children = new List<Node>();
    }

    /// The rank (order) of the tree. Each node holds at most `rank - 1` keys.
    private readonly int _rank;

    /// The root node of the tree.
    private Node _root;

    /// <summary>
    /// Creates an empty B+ tree with the given rank.
    /// </summary>
    /// <param name="rank">
    /// The rank (order) of the tree (Must be >= 3).
    /// </param>
    public BPlusTree(int rank) {
      if (rank < 3) throw new ArgumentException("Rank must be at least 3");
      _rank = rank;
      _root = new LeafNode();
    }

    /// <summary>
    /// Creates a B+ tree with the given rank and records.
    /// </summary>
    /// <param name="rank">
    /// The rank (order) of the tree (Must be >= 3).
    /// </param>
    /// <param name="records">
    /// The Initial records to insert into the tree.
    /// </param>
    public BPlusTree(int rank, List<RType> records) : this(rank) {
      BulkInsert(records);
    }

    /// <summary>
    /// Returns true if the tree contains no entries.
    /// </summary>
    public bool IsEmpty => _root is LeafNode leaf && leaf.Keys.Count == 0;

    public void Insert(RType record) {
      KType key = record.Key;
      RType? existing = default;
      if (Search(key, ref existing)) throw new DuplicateKeyException();

      // Pre-emptively split root if it is full
      if (IsNodeFull(_root)) {
        var (promotedKey, rightNode) = SplitNode(_root);
        var newRoot = new InternalNode();
        newRoot.Keys.Add(promotedKey);
        newRoot.Children.Add(_root);
        newRoot.Children.Add(rightNode);
        _root = newRoot;
      }

      // Walk down the tree, splitting full children eagerly
      Node current = _root;

    }
  }
}