using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using BPlusTree;
using System.Collections.Generic;
using System.Linq;

namespace A3Tests;

[TestClass]
public class BPlusTreeTests {
  #region Constructor tests
  [TestMethod]
  public void Rank3DoesNotThrow()
      => _ = new BPlusTree<int, string>(3);

  [TestMethod]
  public void Rank4DoesNotThrow()
      => _ = new BPlusTree<int, string>(4);

  [TestMethod]
  public void Rank1024DoesNotThrow()
      => _ = new BPlusTree<int, string>(1024);

  [TestMethod]
  public void RankLessThan3Throws()
      => Assert.Throws<ArgumentException>(() => new BPlusTree<int, string>(2));
  #endregion

  #region Insert and Search tests
  [TestMethod]
  public void InsertAndSearchRandomOrderAllKeysFound() {
    var tree = new BPlusTree<int, string>(3);
    var data = new[] { 8, 3, 10, 1, 6, 9, 11, 2, 5, 7 };

    foreach (var x in data)
      tree.Insert(x.ToString());

    foreach (var x in data)
      Assert.AreEqual(x.ToString(), tree.Search(x));
  }

  [TestMethod]
  public void InsertAndSearchLargeDatasetRank20() {
    var tree = new BPlusTree<int, string>(20);
    var data = Enumerable.Range(1, 1000).ToList();

    foreach (var x in data)
      tree.Insert(x.ToString());

    foreach (var x in data)
      Assert.AreEqual(x.ToString(), tree.Search(x));
  }

  [TestMethod]
  public void InsertAndSearchStringKeys() {
    var tree = new BPlusTree<string, string>(3);
    var data = Enumerable.Range(0, 50).Select(i => i.ToString()).ToList();

    foreach (var x in data)
      tree.Insert(x);

    foreach (var x in data)
      Assert.AreEqual(x, tree.Search(x));
  }

  [TestMethod]
  public void InsertDuplicateKeyThrowsDuplicateKeyException() {
    var tree = new BPlusTree<int, string>(3);
    tree.Insert("1");
    Assert.Throws<DuplicateKeyException>(() => tree.Insert("1"));
  }
  #endregion

  #region Search tests
  [TestMethod]
  public void SearchEmptyTreeReturnsNull() {
    var tree = new BPlusTree<int, string>(3);
    Assert.IsNull(tree.Search(11));
  }

  [TestMethod]
  public void SearchExistingKeysReturnsValues() {
    var tree = new BPlusTree<int, string>(3);
    foreach (var x in new[] { 8, 3, 10, 1, 6 })
      tree.Insert(x.ToString());

    Assert.AreEqual("8", tree.Search(8));
    Assert.AreEqual("3", tree.Search(3));
  }

  [TestMethod]
  public void SearchNonExistingKeyReturnsNull() {
    var tree = new BPlusTree<int, string>(3);
    foreach (var x in new[] { 8, 3, 10, 1, 6 })
      tree.Insert(x.ToString());

    Assert.IsNull(tree.Search(5));
    Assert.IsNull(tree.Search(11));
  }

  [TestMethod]
  public void SearchExistingIntValueReturnsTrueAndValue() {
    var tree = new BPlusTree<int, int>(3);
    tree.Insert(7);

    var found = tree.Search(7, out var value);

    Assert.IsTrue(found);
    Assert.AreEqual(7, value);
  }

  [TestMethod]
  public void SearchMissingIntValueReturnsFalseAndDefaultOutValue() {
    var tree = new BPlusTree<int, int>(3);
    tree.Insert(7);

    var found = tree.Search(8, out var value);

    Assert.IsFalse(found);
    Assert.AreEqual(default(int), value);
  }

  [TestMethod]
  public void SearchRecordTypeUsesRecordKey() {
    var tree = new BPlusTree<Key, Record<Key>>(3);
    var record = new Record<Key>(new Key(7), new[] { "x", "y" });

    tree.Insert(record);
    var found = tree.Search(new Key(7), out var value);

    Assert.IsTrue(found);
    Assert.IsNotNull(value);
    CollectionAssert.AreEqual(new[] { "x", "y" }, value!.Values);
  }
  #endregion

  #region IsEmpty tests
  [TestMethod]
  public void IsEmptyNewTreeReturnsTrue() {
    var tree = new BPlusTree<int, string>(3);
    Assert.IsTrue(tree.IsEmpty);
  }

  [TestMethod]
  public void IsEmptyAfterInsertReturnsFalse() {
    var tree = new BPlusTree<int, string>(3);
    tree.Insert("1");
    Assert.IsFalse(tree.IsEmpty);
  }

  #endregion

  #region Range tests
  private static BPlusTree<int, string> BuildRangeFixture() {
    return new BPlusTree<int, string>(3, new[] { "8", "3", "10", "1", "6" });
  }

  [TestMethod]
  public void RangeMiddleOfTreeReturnsCorrectValues() {
    var tree = BuildRangeFixture();
    CollectionAssert.AreEqual(new[] { "3", "6" }, tree.Range(2, 7));
  }

  [TestMethod]
  public void RangeLowerSection() {
    var tree = BuildRangeFixture();
    CollectionAssert.AreEqual(new[] { "1", "3" }, tree.Range(0, 5));
  }

  [TestMethod]
  public void RangeUpperSection() {
    var tree = BuildRangeFixture();
    CollectionAssert.AreEqual(new[] { "8", "10" }, tree.Range(7, 11));
  }

  [TestMethod]
  public void RangeLowerBound() {
    var tree = BuildRangeFixture();
    CollectionAssert.AreEqual(new[] { "1" }, tree.Range(-20, 2));
  }

  [TestMethod]
  public void RangeBelowAllKeysReturnsEmpty() {
    var tree = BuildRangeFixture();
    CollectionAssert.AreEqual(new string[0], tree.Range(-20, -1));
  }

  [TestMethod]
  public void RangeUpperBound() {
    var tree = BuildRangeFixture();
    CollectionAssert.AreEqual(new[] { "10" }, tree.Range(9, 100));
  }

  [TestMethod]
  public void RangeAboveAllKeysReturnsEmpty() {
    var tree = BuildRangeFixture();
    CollectionAssert.AreEqual(new string[0], tree.Range(20, 100));
  }

  [TestMethod]
  public void RangeStartGreaterThanEndReturnsEmpty() {
    var tree = BuildRangeFixture();
    CollectionAssert.AreEqual(new string[0], tree.Range(8, 3));
  }

  [TestMethod]
  public void RangeExactBoundaryReturnsSingleValue() {
    var tree = BuildRangeFixture();
    CollectionAssert.AreEqual(new[] { "6" }, tree.Range(6, 6));
  }

  [TestMethod]
  public void RangeSingleKeyTreeBoundaryBehavior() {
    var tree = new BPlusTree<int, string>(3);
    tree.Insert("42");
    CollectionAssert.AreEqual(new[] { "42" }, tree.Range(42, 42));
    CollectionAssert.AreEqual(new string[0], tree.Range(43, 100));
  }
  #endregion

  #region BulkInsert tests
  [TestMethod]
  public void BulkInsertRandomOrderAllKeysFound() {
    var tree = new BPlusTree<int, string>(3);
    var data = new[] { 8, 3, 10, 1, 6, 9, 11, 2, 5, 7 };

    tree.BulkInsert(data.Select(x => x.ToString()));

    foreach (var x in data)
      Assert.AreEqual(x.ToString(), tree.Search(x));
  }

  [TestMethod]
  public void BulkInsertLargeDatasetRank20() {
    var tree = new BPlusTree<int, string>(20);
    var data = Enumerable.Range(1, 1000).ToList();

    tree.BulkInsert(data.Select(x => x.ToString()));

    foreach (var x in data)
      Assert.AreEqual(x.ToString(), tree.Search(x));
  }

  [TestMethod]
  public void BulkInsertStringKeys() {
    var tree = new BPlusTree<string, string>(3);
    var data = Enumerable.Range(0, 50).Select(i => i.ToString()).ToList();

    tree.BulkInsert(data.Select(x => x));

    foreach (var x in data)
      Assert.AreEqual(x, tree.Search(x));
  }

  [TestMethod]
  public void BulkInsertDuplicateKeysThrowsDuplicateKeyException() {
    var tree = new BPlusTree<int, string>(3);
    Assert.Throws<DuplicateKeyException>(() => tree.BulkInsert(new[] { "1", "1" }));
  }

  [TestMethod]
  public void BulkInsertNonEmptyTreeThrowsInvalidOperationException() {
    var tree = new BPlusTree<int, string>(3);
    tree.Insert("1");
    Assert.Throws<InvalidOperationException>(() => tree.BulkInsert(new[] { "2" }));
  }

  [TestMethod]
  public void BulkInsertDuplicatesNonAdjacentThrowsDuplicateKeyException() {
    var tree = new BPlusTree<int, string>(3);
    Assert.Throws<DuplicateKeyException>(() => tree.BulkInsert(new[] { "2", "1", "2" }));
  }
  #endregion

  #region Delete tests
  [TestMethod]
  public void DeleteNonExistingKeyReturnsFalse() {
    var tree = new BPlusTree<string, string>(3);
    var data = Enumerable.Range(0, 50).Select(i => i.ToString()).ToList();
    tree.BulkInsert(data.Select(x => x));

    Assert.IsFalse(tree.Delete("100"));
  }

  [TestMethod]
  public void DeleteDrainToEmptyTreeBecomesEmpty() {
    var tree = new BPlusTree<string, string>(3);
    var data = Enumerable.Range(0, 50).Select(i => i.ToString()).ToList();
    tree.BulkInsert(data.Select(x => x));

    foreach (var x in data) {
      Assert.AreEqual(x, tree.Search(x), $"Key {x} should exist before delete.");
      Assert.IsTrue(tree.Delete(x), $"Delete({x}) should return true.");
      Assert.IsNull(tree.Search(x), $"Key {x} should not exist after delete.");
    }

    Assert.IsTrue(tree.IsEmpty);
  }

  [TestMethod]
  public void DeleteNonExistingKeyOnEmptyTreeReturnsFalse() {
    var tree = new BPlusTree<string, string>(3);
    Assert.IsFalse(tree.Delete("1"));
  }

  [TestMethod]
  public void DeleteUnderflowAtLeftmostLeafBorrowsFromRight() {
    // Deleting from the leftmost leaf causes underflow; tree should borrow from the right sibling.
    var tree = new BPlusTree<int, string>(4);
    tree.BulkInsert(Enumerable.Range(0, 12).Select(x => x.ToString()));

    Assert.IsTrue(tree.Delete(0));
    Assert.IsTrue(tree.Delete(1));
    Assert.IsTrue(tree.Delete(2));

    Assert.IsNull(tree.Search(0));
    Assert.IsNull(tree.Search(1));
    Assert.IsNull(tree.Search(2));

    foreach (var x in Enumerable.Range(3, 9))
      Assert.AreEqual(x.ToString(), tree.Search(x));
  }

  [TestMethod]
  public void DeleteUnderflowInMiddleLeafBorrowsFromLeft() {
    // Deleting from the middle leaf causes underflow; tree should borrow from the left sibling.
    var tree = new BPlusTree<int, string>(4);
    tree.BulkInsert(Enumerable.Range(0, 12).Select(x => x.ToString()));

    Assert.IsTrue(tree.Delete(5));
    Assert.IsTrue(tree.Delete(4));
    Assert.IsTrue(tree.Delete(3));

    Assert.IsNull(tree.Search(3));
    Assert.IsNull(tree.Search(4));
    Assert.IsNull(tree.Search(5));

    foreach (var x in new[] { 0, 1, 2, 6, 7, 8, 9, 10, 11 })
      Assert.AreEqual(x.ToString(), tree.Search(x));
  }

  [TestMethod]
  public void DeleteUnderflowNoLenderMergesLeaves() {
    // When no sibling can lend, leaves must merge.
    var tree = new BPlusTree<int, string>(4);
    tree.BulkInsert(Enumerable.Range(0, 9).Select(x => x.ToString()));

    foreach (var x in new[] { 0, 1, 6, 7, 3, 4, 5 })
      Assert.IsTrue(tree.Delete(x));

    foreach (var x in new[] { 0, 1, 3, 4, 5, 6, 7 })
      Assert.IsNull(tree.Search(x));

    Assert.AreEqual("2", tree.Search(2));
    Assert.AreEqual("8", tree.Search(8));
  }

  [TestMethod]
  public void DeleteInternalRebalanceAndRepeatedRootCollapse() {
    // Heavy deletion with rank 3 exercises internal rebalance and repeated root collapse.
    var tree = new BPlusTree<int, string>(3);
    tree.BulkInsert(Enumerable.Range(0, 60).Select(x => x.ToString()));

    foreach (var x in Enumerable.Range(0, 59))
      Assert.IsTrue(tree.Delete(x));

    Assert.AreEqual("59", tree.Search(59));
    Assert.IsTrue(tree.Delete(59));
    Assert.IsTrue(tree.IsEmpty);
  }

  [TestMethod]
  public void DeleteSameKeyTwiceFirstTrueSecondFalse() {
    var tree = new BPlusTree<int, string>(3);
    tree.BulkInsert(Enumerable.Range(0, 10).Select(x => x.ToString()));
    Assert.IsTrue(tree.Delete(5));
    Assert.IsFalse(tree.Delete(5));
  }

  [TestMethod]
  public void DeleteThenReinsertKeyIsAvailableAgain() {
    var tree = new BPlusTree<int, string>(3);
    tree.Insert("7");
    Assert.IsTrue(tree.Delete(7));
    tree.Insert("7");
    Assert.AreEqual("7", tree.Search(7));
  }

  [TestMethod]
  public void DeleteUntilOneRemainingThenEmpty() {
    var tree = new BPlusTree<int, string>(3);
    tree.BulkInsert(Enumerable.Range(0, 5).Select(x => x.ToString()));
    foreach (var x in new[] { 0, 1, 2, 3 }) {
      Assert.IsTrue(tree.Delete(x));
    }
    Assert.AreEqual("4", tree.Search(4));
    Assert.IsTrue(tree.Delete(4));
    Assert.IsTrue(tree.IsEmpty);
  }
  #endregion

  #region Merge tests
  /// <summary>Reference tree: keys 0–29, rank 4.</summary>
  private static BPlusTree<int, string> BuildReference() {
    var t = new BPlusTree<int, string>(4);
    t.BulkInsert(Enumerable.Range(0, 30).Select(x => x.ToString()));
    return t;
  }

  [TestMethod]
  public void MergeTwoHalvesContainsAllKeys() {
    var tree1 = new BPlusTree<int, string>(4);
    tree1.BulkInsert(Enumerable.Range(0, 15).Select(x => x.ToString()));

    var tree2 = new BPlusTree<int, string>(4);
    tree2.BulkInsert(Enumerable.Range(15, 15).Select(x => x.ToString()));

    var merged = tree1.Merge(tree2);

    foreach (var x in Enumerable.Range(0, 30))
      Assert.AreEqual(x.ToString(), merged.Search(x));
  }

  [TestMethod]
  public void MergeDifferentRanksContainsAllKeys() {
    var tree1 = new BPlusTree<int, string>(4);
    tree1.BulkInsert(Enumerable.Range(0, 15).Select(x => x.ToString()));

    var tree2 = new BPlusTree<int, string>(10);
    tree2.BulkInsert(Enumerable.Range(15, 15).Select(x => x.ToString()));

    var merged = tree1.Merge(tree2);   // rank of result = tree1's rank = 4

    foreach (var x in Enumerable.Range(0, 30))
      Assert.AreEqual(x.ToString(), merged.Search(x));
  }

  [TestMethod]
  public void MergeOverlappingKeysThrowsDuplicateKeyException() {
    var tree1 = new BPlusTree<int, string>(4);
    tree1.BulkInsert(new[] { "1", "2", "3" });

    var tree2 = new BPlusTree<int, string>(4);
    tree2.BulkInsert(new[] { "3", "4" });

    Assert.Throws<DuplicateKeyException>(() => tree1.Merge(tree2));
  }

  [TestMethod]
  public void MergeWithEmptyTreeReturnsEquivalentToNonEmpty() {
    var tree1 = new BPlusTree<int, string>(4);
    tree1.BulkInsert(Enumerable.Range(0, 20).Select(x => x.ToString()));
    var empty = new BPlusTree<int, string>(4);

    var merged = tree1.Merge(empty);
    foreach (var x in Enumerable.Range(0, 20))
      Assert.AreEqual(x.ToString(), merged.Search(x));
  }
  #endregion

  #region FromList tests
  [TestMethod]
  public void FromListAllKeysFound() {
    var data = Enumerable.Range(0, 30).Select(x => x.ToString()).ToList();
    var tree = new BPlusTree<int, string>(4, data);

    foreach (var x in Enumerable.Range(0, 30))
      Assert.AreEqual(x.ToString(), tree.Search(x));
  }

  [TestMethod]
  public void FromListDuplicateKeysThrowsDuplicateKeyException() {
    var data = new List<string> { "1", "2", "1" };
    Assert.Throws<DuplicateKeyException>(() => new BPlusTree<int, string>(4, data));
  }
  #endregion

  #region Rank stress tests
  [TestMethod]
  public void InsertSearchDeleteMultipleRanksRandomizedData() {
    foreach (var rank in new[] { 3, 4, 20 }) {
      var tree = new BPlusTree<int, string>(rank);
      var keys = Enumerable.Range(0, 200).OrderBy(_ => Guid.NewGuid()).ToList();

      foreach (var key in keys)
        tree.Insert(key.ToString());

      foreach (var key in keys)
        Assert.AreEqual(key.ToString(), tree.Search(key), $"Missing key {key} for rank {rank}");

      foreach (var key in keys.Take(100)) {
        Assert.IsTrue(tree.Delete(key), $"Delete should succeed for key {key} and rank {rank}");
        Assert.IsNull(tree.Search(key), $"Deleted key {key} should be absent for rank {rank}");
      }

      foreach (var key in keys.Skip(100))
        Assert.AreEqual(key.ToString(), tree.Search(key), $"Remaining key {key} missing for rank {rank}");
    }
  }

  [TestMethod]
  public void InsertSequentialAndReverseAllKeysFound() {
    var asc = new BPlusTree<int, string>(4);
    foreach (var key in Enumerable.Range(0, 200))
      asc.Insert(key.ToString());
    foreach (var key in Enumerable.Range(0, 200))
      Assert.AreEqual(key.ToString(), asc.Search(key));

    var desc = new BPlusTree<int, string>(4);
    foreach (var key in Enumerable.Range(0, 200).Reverse())
      desc.Insert(key.ToString());
    foreach (var key in Enumerable.Range(0, 200))
      Assert.AreEqual(key.ToString(), desc.Search(key));
  }
  #endregion
}

internal static class BPlusTreeTestExtensions {
  public static TValue? Search<TKey, TValue>(this BPlusTree<TKey, TValue> tree, TKey key) where TKey : IComparable<TKey> {
    return tree.Search(key, out var value) ? value : default;
  }
}
