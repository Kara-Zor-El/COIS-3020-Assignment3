using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using BPlusTree;

namespace A3Tests;

[TestClass]
public class BPlusTreeTests {
  // Test Helpers
  private static class Helpers {
    /// <summary>
    /// Helper function to build a basic BPlusTree with int keys and string values used commonly throughout testing.
    /// </summary>
    /// <param name="rank">The rank of the tree.</param>
    /// <returns>The built BPlusTree.</returns>
    public static BPlusTree<int, string> BuildRankIntStringTree(int rank) => new BPlusTree<int, string>(rank);
    /// <summary>
    /// Helper function to build a record with int key and a string value where the value is a string representation of the key.
    /// </summary>
    /// <param name="key">The key for the record.</param>
    /// <returns>The built record.</returns>
    public static Record<int, string> IntStringRecord(int key) => new Record<int, string>(key, key.ToString());
    /// <summary>Helper function making search a bit more convenient for testing.</summary>
    /// <param name="tree">The tree to search in.</param>
    /// <param name="key">The key to search for.</param>
    /// <returns>The record if found, otherwise null.</returns>
    public static Record<TKey, TValue>? Search<TKey, TValue>(BPlusTree<TKey, TValue> tree, TKey key) where TKey : IComparable<TKey> {
      Record<TKey, TValue>? result = null;
      if (tree.Search(key, ref result)) return result;
      return null;
    }
  }
  #region Constructor tests
  // NOTE: We just do smoke tests to make sure that constructing a tree with various ranks works.
  //       Behavior will be verified more thoroughly when we test other functions.
  //       The decision to use smoke test was made as we can't inspect the private structure of the tree to verify it.
  [TestMethod]
  public void ConstructSmallTree1() => Assert.IsNotNull(Helpers.BuildRankIntStringTree(3));
  [TestMethod]
  public void ConstructSmallTree2() => Assert.IsNotNull(Helpers.BuildRankIntStringTree(4));
  [TestMethod]
  public void ConstructLargeTree1() => Assert.IsNotNull(Helpers.BuildRankIntStringTree(1024));
  [TestMethod]
  public void ConstructLargeTree2() => Assert.IsNotNull(Helpers.BuildRankIntStringTree(100_000));
  [TestMethod] // Test that ranks less than 3 fail
  public void ConstructTreeWithInvalidRank1() => Assert.Throws<ArgumentException>(() => Helpers.BuildRankIntStringTree(2));
  [TestMethod] // Test that ranks less than 3 fail
  public void ConstructTreeWithInvalidRank2() => Assert.Throws<ArgumentException>(() => Helpers.BuildRankIntStringTree(-1));
  #endregion
  #region Insert and Search tests
  // NOTE: We test both Insert and Search together as it's hard to test them independently.
  [TestMethod]
  public void InsertAndSearchWithOrderedData() {
    var tree = Helpers.BuildRankIntStringTree(3);
    // We test ordered data to make sure that sorted vs non sorted has no difference
    var data = Enumerable.Range(0, 10).ToArray();
    // Ensure that we are not throwing
    foreach (var x in data) tree.Insert(new Record<int, string>(x, x.ToString()));
    foreach (var x in data) {
      Assert.AreEqual(x.ToString(), Helpers.Search(tree, x)?.Value, $"Key {x} should be found with correct value.");
    }
  }
  [TestMethod]
  public void InsertAndSearchWithRandomData() {
    var tree = Helpers.BuildRankIntStringTree(3);
    // We test random data to make sure that sorted vs non sorted has no difference
    var data = new[] { 8, 3, 10, 1, 6, 9, 11, 2, 5, 7 };
    // Ensure that we are not throwing
    foreach (var x in data) tree.Insert(Helpers.IntStringRecord(x));
    foreach (var x in data) {
      Assert.AreEqual(x.ToString(), Helpers.Search(tree, x)?.Value, $"Key {x} should be found with correct value.");
    }
  }
  [TestMethod]
  public void InsertAndSearchWithLargeData() {
    var tree = Helpers.BuildRankIntStringTree(4);
    // We test with a large amount of data to ensure that our implementation can handle larger loads.
    var data = Enumerable.Range(0, 100_000).ToArray();
    // Ensure that we are not throwing
    foreach (var x in data) tree.Insert(Helpers.IntStringRecord(x));
    foreach (var x in data) {
      Assert.AreEqual(x.ToString(), Helpers.Search(tree, x)?.Value, $"Key {x} should be found with correct value.");
    }
  }
  [TestMethod]
  public void InsertAndSearchOnDifferentDataTypeTree() {
    // We test on a different data type to ensure that semantics aren't related to int keys or string values
    var tree = new BPlusTree<char, char>(3);
    // Generate data from a-z
    var data = Enumerable.Range('a', 'z').ToArray();
    // Ensure that we are not throwing
    foreach (var x in data) tree.Insert(new Record<char, char>((char)x, (char)x));
    foreach (var x in data) {
      Assert.AreEqual((char)x, (char?)Helpers.Search(tree, (char)x)?.Value, $"Key {x} should be found with correct value.");
    }
  }
  [TestMethod]
  public void InsertDuplicateKeyFails() {
    var tree = Helpers.BuildRankIntStringTree(3);
    tree.Insert(Helpers.IntStringRecord(1));
    // Ensure that same key and value fails
    Assert.Throws<DuplicateKeyException>(() => tree.Insert(new Record<int, string>(1, "1")));
    // Ensure that same key different value fails
    Assert.Throws<DuplicateKeyException>(() => tree.Insert(new Record<int, string>(1, "3")));
  }
  #endregion

  #region Search tests
  [TestMethod]
  public void SearchEmpty() {
    // Searching an empty tree should just return nothing
    var tree = Helpers.BuildRankIntStringTree(3);
    Assert.IsNull(Helpers.Search(tree, 1));
  }
  [TestMethod]
  public void SearchHasValue() {
    // Searching for a key that exists should return the correct value.
    var tree = Helpers.BuildRankIntStringTree(3);
    foreach (var x in new[] { 8, 3, 10, 1, 6 }) {
      tree.Insert(Helpers.IntStringRecord(x));
    }
    Assert.AreEqual("8", Helpers.Search(tree, 8)?.Value);
    Assert.AreEqual("3", Helpers.Search(tree, 3)?.Value);
  }
  [TestMethod]
  public void SearchDoesNotHaveValue() {
    // Searching for a key that does not exist should return null.
    var tree = Helpers.BuildRankIntStringTree(3);
    foreach (var x in new[] { 8, 3, 10, 1, 6 }) {
      tree.Insert(Helpers.IntStringRecord(x));
    }
    Assert.IsNull(Helpers.Search(tree, 5));
    Assert.IsNull(Helpers.Search(tree, 11));
  }
  #endregion

  #region IsEmpty tests
  [TestMethod]
  public void IsEmptyNewTree() {
    // A new tree will be empty
    var tree = Helpers.BuildRankIntStringTree(3);
    Assert.IsTrue(tree.IsEmpty);
  }
  [TestMethod]
  public void IsNotEmptyBasic() {
    // A tree with at least one value should not be empty
    var tree = Helpers.BuildRankIntStringTree(3);
    tree.Insert(Helpers.IntStringRecord(1));
    Assert.IsFalse(tree.IsEmpty);
  }
  // NOTE: It may make sense to test `IsEmpty` after a deletion we test this functionality when testing delete.
  #endregion

  #region Range tests
  // This is just a basic fixture that we perform our tests again
  private static BPlusTree<int, string> BuildRangeFixture() {
    return new BPlusTree<int, string>(3, [
      Helpers.IntStringRecord(8),
      Helpers.IntStringRecord(3),
      Helpers.IntStringRecord(10),
      Helpers.IntStringRecord(1),
      Helpers.IntStringRecord(6),
    ]);
  }
  [TestMethod]
  public void ValidRanges() {
    // Test reading stuff from the middle
    var tree = BuildRangeFixture();
    CollectionAssert.AreEqual(new[] { "3", "6" }, tree.Range(2, 7).Select(x => x.Value).ToArray());
    // Test reading from the lower end
    CollectionAssert.AreEqual(new[] { "1", "3" }, tree.Range(0, 5).Select(x => x.Value).ToArray());
    // Test reading from the upper end
    CollectionAssert.AreEqual(new[] { "8", "10" }, tree.Range(7, 11).Select(x => x.Value).ToArray());
  }
  [TestMethod]
  public void RangeLowerBound() {
    var tree = BuildRangeFixture();
    // Test that we are inclusive on the lower bound
    CollectionAssert.AreEqual(new[] { "1" }, tree.Range(-20, 2).Select(x => x.Value).ToArray());
    // Test that below the lower bound is empty
    CollectionAssert.AreEqual(Array.Empty<string>(), tree.Range(-20, -1).Select(x => x.Value).ToArray());
  }
  [TestMethod]
  public void RangeUpperBound() {
    var tree = BuildRangeFixture();
    // Test that we are inclusive on the upper bound
    CollectionAssert.AreEqual(new[] { "10" }, tree.Range(9, 100).Select(x => x.Value).ToArray());
    // Test that above the upper bound is empty
    CollectionAssert.AreEqual(Array.Empty<string>(), tree.Range(11, 100).Select(x => x.Value).ToArray());
  }
  #endregion

  #region BulkInsert tests
  [TestMethod]
  public void BulkInsertRandomOrder() {
    var tree = Helpers.BuildRankIntStringTree(3);
    // Ensure that the tree is being built correctly this could catch a case where we aren't sorting
    var data = new[] { 8, 3, 10, 1, 6, 9, 11, 2, 5, 7 };
    tree.BulkInsert(data.Select(x => Helpers.IntStringRecord(x)));
    foreach (var x in data) {
      Assert.AreEqual(x.ToString(), Helpers.Search(tree, x)?.Value, $"Key {x} should be found with correct value after bulk insert.");
    }
  }
  [TestMethod]
  public void BulkInsertSortedOrder() {
    // Similar test to our insert sorted
    var tree = Helpers.BuildRankIntStringTree(3);
    var data = Enumerable.Range(1, 20).ToList();
    tree.BulkInsert(data.Select(x => Helpers.IntStringRecord(x)));
    foreach (var x in data) {
      Assert.AreEqual(x.ToString(), Helpers.Search(tree, x)?.Value, $"Key {x} should be found with correct value after bulk insert.");
    }
  }
  [TestMethod]
  public void BulkInsertLarge() {
    // Similar test to our insert large
    var tree = Helpers.BuildRankIntStringTree(4);
    var data = Enumerable.Range(1, 100_000).ToList();
    tree.BulkInsert(data.Select(x => Helpers.IntStringRecord(x)));
    foreach (var x in data) {
      Assert.AreEqual(x.ToString(), Helpers.Search(tree, x)?.Value, $"Key {x} should be found with correct value after bulk insert.");
    }
  }
  [TestMethod]
  public void BulkInsertStringKeys() {
    // Similar test to our different data type test for insert but for bulk insert
    var tree = new BPlusTree<string, string>(3);
    var data = Enumerable.Range(1, 20).Select(i => i.ToString()).ToList();
    tree.BulkInsert(data.Select(x => new Record<string, string>(x, x)));
    foreach (var x in data) {
      Assert.AreEqual(x.ToString(), Helpers.Search(tree, x)?.Value, $"Key {x} should be found with correct value after bulk insert.");
    }
  }
  [TestMethod]
  public void BulkInsertNothing() {
    var tree = Helpers.BuildRankIntStringTree(3);
    // This should just be a no-op and not throw
    tree.BulkInsert(new List<Record<int, string>>());
  }
  [TestMethod]
  public void BulkInsertDuplicateKeys1() {
    var tree = Helpers.BuildRankIntStringTree(3);
    Assert.Throws<DuplicateKeyException>(() => tree.BulkInsert(new[] { Helpers.IntStringRecord(1), Helpers.IntStringRecord(1) }));
  }
  [TestMethod]
  public void BulkInsertDuplicateKeys2() {
    // We also test duplicate keys unordered
    var tree = Helpers.BuildRankIntStringTree(3);
    // This would catch a mistake in our duplicate check algorithm where we only check adjacent keys for duplicates
    Assert.Throws<DuplicateKeyException>(() => tree.BulkInsert(new[] { Helpers.IntStringRecord(1), Helpers.IntStringRecord(2), Helpers.IntStringRecord(3), Helpers.IntStringRecord(5), Helpers.IntStringRecord(1) }));
  }
  [TestMethod]
  public void BulkInsertNonEmptyTree() {
    var tree = Helpers.BuildRankIntStringTree(3);
    tree.Insert(Helpers.IntStringRecord(1));
    Assert.Throws<InvalidOperationException>(() => tree.BulkInsert(new[] { Helpers.IntStringRecord(2), Helpers.IntStringRecord(3) }));
  }
  #endregion

  #region Delete tests
  [TestMethod]
  public void DeleteOnEmptyTree() {
    var tree = Helpers.BuildRankIntStringTree(3);
    Assert.IsFalse(tree.Delete(1));
  }
  [TestMethod]
  public void DeleteNonExistingKey() {
    var tree = Helpers.BuildRankIntStringTree(3);
    var data = Enumerable.Range(0, 50).Select(i => Helpers.IntStringRecord(i)).ToList();
    tree.BulkInsert(data);
    // This key does not exist, delete should return false
    Assert.IsFalse(tree.Delete(100));
  }
  [TestMethod]
  public void DeleteAll() {
    var tree = Helpers.BuildRankIntStringTree(3);
    var data = Enumerable.Range(0, 50).Select(i => Helpers.IntStringRecord(i)).ToList();
    tree.BulkInsert(data);
    // Delete all keys and ensure they are deleted and that the tree is empty at the end
    foreach (var x in data) {
      Assert.AreEqual(x.Value, Helpers.Search(tree, x.Key)?.Value, $"Key {x.Key} should exist before delete.");
      Assert.IsTrue(tree.Delete(x.Key), $"Delete({x.Key}) should return true.");
      Assert.IsNull(Helpers.Search(tree, x.Key), $"Key {x.Key} should not exist after delete.");
    }
    // Here is that `IsEmpty` test we mentioned above
    Assert.IsTrue(tree.IsEmpty);
  }
  [TestMethod]
  public void DeleteTwice() {
    var tree = Helpers.BuildRankIntStringTree(3);
    var data = Enumerable.Range(0, 50).Select(i => Helpers.IntStringRecord(i)).ToList();
    tree.BulkInsert(data);
    // Delete a key and then try to delete it again. The first delete should succeed and the second should fail.
    Assert.IsTrue(tree.Delete(5));
    Assert.IsFalse(tree.Delete(5));
  }
  [TestMethod]
  public void DeleteThenReinsertKeyIsAvailableAgain() {
    var tree = Helpers.BuildRankIntStringTree(3);
    tree.Insert(Helpers.IntStringRecord(7));
    Assert.AreEqual("7", Helpers.Search(tree, 7)?.Value, $"Key 7 should be found with correct value after delete and reinsert.");
    Assert.IsTrue(tree.Delete(7));
    Assert.IsNull(Helpers.Search(tree, 7), $"Key 7 should not be found after deletion.");
    tree.Insert(Helpers.IntStringRecord(7));
    Assert.AreEqual("7", Helpers.Search(tree, 7)?.Value, $"Key 7 should be found with correct value after delete and reinsert.");
  }
  // Rebalance testing
  // NOTE: These tests are somewhat implementation dependent, a better way might be to snapshot test these but it felt overcomplicated for this assignment. These tests would still catch any notable edge cases but their rank dependent which means if the rank we use were to change we would need to update the tests and it wouldn't be clear that we are no longer testing what we say.
  [TestMethod]
  public void DeleteUnderflowAtLeftmostLeafBorrowsFromRight() {
    // Deleting from the leftmost leaf causes underflow; tree should borrow from the right sibling.
    var tree = Helpers.BuildRankIntStringTree(4);
    tree.BulkInsert(Enumerable.Range(0, 12).Select(x => Helpers.IntStringRecord(x)));
    Assert.IsTrue(tree.Delete(0));
    Assert.IsTrue(tree.Delete(1));
    Assert.IsTrue(tree.Delete(2));
    Assert.IsNull(Helpers.Search(tree, 0));
    Assert.IsNull(Helpers.Search(tree, 1));
    Assert.IsNull(Helpers.Search(tree, 2));
    foreach (var x in Enumerable.Range(3, 9)) {
      Assert.AreEqual(x.ToString(), Helpers.Search(tree, x)?.Value, $"Key {x} should be found with correct value after leftmost leaf underflow.");
    }
  }

  [TestMethod]
  public void DeleteUnderflowInMiddleLeafBorrowsFromLeft() {
    // Deleting from the middle leaf causes underflow; tree should borrow from the left sibling.
    var tree = Helpers.BuildRankIntStringTree(4);
    tree.BulkInsert(Enumerable.Range(0, 12).Select(x => Helpers.IntStringRecord(x)));
    Assert.IsTrue(tree.Delete(5));
    Assert.IsTrue(tree.Delete(4));
    Assert.IsTrue(tree.Delete(3));
    Assert.IsNull(Helpers.Search(tree, 3));
    Assert.IsNull(Helpers.Search(tree, 4));
    Assert.IsNull(Helpers.Search(tree, 5));
    foreach (var x in new[] { 0, 1, 2, 6, 7, 8, 9, 10, 11 }) {
      Assert.AreEqual(x.ToString(), Helpers.Search(tree, x)?.Value, $"Key {x} should be found with correct value after leftmost leaf underflow.");
    }
  }
  [TestMethod]
  public void DeleteUnderflowNoLenderMergesLeaves() {
    // When no sibling can lend, leaves must merge.
    var tree = Helpers.BuildRankIntStringTree(4);
    tree.BulkInsert(Enumerable.Range(0, 9).Select(x => Helpers.IntStringRecord(x)));
    foreach (var x in new[] { 0, 1, 6, 7, 3, 4, 5 }) Assert.IsTrue(tree.Delete(x));
    foreach (var x in new[] { 0, 1, 3, 4, 5, 6, 7 }) {
      Assert.IsNull(Helpers.Search(tree, x), $"Key {x} should not be found after underflow with no lender.");
    }
    Assert.AreEqual("2", Helpers.Search(tree, 2)?.Value, $"Key 2 should be found with correct value after underflow with no lender.");
    Assert.AreEqual("8", Helpers.Search(tree, 8)?.Value, $"Key 8 should be found with correct value after underflow with no lender.");
  }

  [TestMethod]
  public void DeleteInternalRebalanceAndRepeatedRootCollapse() {
    // Heavy deletion with rank 3 exercises internal rebalance and repeated root collapse.
    var tree = Helpers.BuildRankIntStringTree(3);
    tree.BulkInsert(Enumerable.Range(0, 60).Select(x => Helpers.IntStringRecord(x)));
    foreach (var x in Enumerable.Range(0, 59)) Assert.IsTrue(tree.Delete(x));
    Assert.AreEqual("59", Helpers.Search(tree, 59)?.Value, $"Key 59 should be found with correct value after heavy deletion with root collapse.");
    Assert.IsFalse(tree.IsEmpty);
    Assert.IsTrue(tree.Delete(59));
    Assert.IsTrue(tree.IsEmpty);
  }
  #endregion

  #region Merge tests
  [TestMethod]
  public void MergeSameRank() {
    // Build our data
    var data1 = Enumerable.Range(0, 15).Select(x => new Record<int, string>(x, x.ToString()));
    var data2 = Enumerable.Range(15, 15).Select(x => new Record<int, string>(x, x.ToString()));
    // Construct our trees
    var tree1 = Helpers.BuildRankIntStringTree(3);
    tree1.BulkInsert(data1);
    var tree2 = Helpers.BuildRankIntStringTree(3);
    tree2.BulkInsert(data2);
    // Merge our trees
    var merged = tree1.Merge(tree2);
    // Verify that all keys are present with correct values
    foreach (var x in Enumerable.Range(0, 30)) {
      Assert.AreEqual(x.ToString(), Helpers.Search(merged, x)?.Value, $"Key {x} should be found with correct value in merged tree.");
    }
  }
  [TestMethod]
  public void MergeDifferentRank() {
    // Build our data
    var data1 = Enumerable.Range(0, 15).Select(x => new Record<int, string>(x, x.ToString()));
    var data2 = Enumerable.Range(15, 15).Select(x => new Record<int, string>(x, x.ToString()));
    // Construct our trees
    var tree1 = Helpers.BuildRankIntStringTree(3);
    tree1.BulkInsert(data1);
    var tree2 = Helpers.BuildRankIntStringTree(10);
    tree2.BulkInsert(data2);
    // Merge our trees
    var merged = tree1.Merge(tree2);
    // Verify that all keys are present with correct values
    foreach (var x in Enumerable.Range(0, 30)) {
      Assert.AreEqual(x.ToString(), Helpers.Search(merged, x)?.Value, $"Key {x} should be found with correct value in merged tree.");
    }
  }
  [TestMethod]
  public void MergeOverlappingInvalid() {
    // Build our data
    var data = Enumerable.Range(0, 15).Select(x => new Record<int, string>(x, x.ToString()));
    // Construct our trees
    var tree1 = Helpers.BuildRankIntStringTree(3);
    tree1.BulkInsert(data);
    var tree2 = Helpers.BuildRankIntStringTree(3);
    tree2.BulkInsert(data);
    // Merge our trees
    Assert.Throws<DuplicateKeyException>(() => tree1.Merge(tree2));
  }
  [TestMethod]
  public void MergeWithEmptyTreeReturnsEquivalentToNonEmpty() {
    var tree1 = Helpers.BuildRankIntStringTree(3);
    var data = Enumerable.Range(0, 20).Select(x => x);
    tree1.BulkInsert(data.Select(x => Helpers.IntStringRecord(x)));
    var empty = Helpers.BuildRankIntStringTree(3);
    var merged = tree1.Merge(empty);
    foreach (var x in data) {
      Assert.AreEqual(
        x.ToString(),
        Helpers.Search(merged, x)?.Value,
        $"Key {x} should be found with correct value in merged tree with empty."
      );
    }
  }
  #endregion

  #region Rank stress tests
  [TestMethod]
  public void InsertSearchDeleteMultipleRanksRandomizedData() {
    // NOTE: This is really just to catch any invariant that we may not have already caught with the above tests.
    //       Real failures tend to surface on the specific tests but a stress test is good to catch subtle bugs 
    //       like maybe delete fails after a certain rank or specific load, the idea is if a failure surfaces here 
    //       we treat it as a regression and add a new specific test for a similar or the same case to make sure we 
    //       catch it in the future.
    foreach (var rank in new[] { 3, 4, 20 }) {
      var tree = Helpers.BuildRankIntStringTree(rank);
      // NOTE: We use guid because they are unique and random
      var keys = Enumerable.Range(0, 200).OrderBy(_ => Guid.NewGuid()).ToList();
      // Insert our keys
      foreach (var key in keys) tree.Insert(Helpers.IntStringRecord(key));
      // Ensure all keys exist
      foreach (var key in keys) {
        Assert.AreEqual(key.ToString(), Helpers.Search(tree, key)?.Value, $"Key {key} should be found with correct value for rank {rank}");
      }
      // Delete the first 100 keys
      foreach (var key in keys.Take(100)) {
        Assert.IsTrue(tree.Delete(key), $"Delete should succeed for key {key} and rank {rank}");
        // Validate the deletion
        Assert.IsNull(Helpers.Search(tree, key), $"Search should return null for deleted key {key} and rank {rank}");
      }
      // Ensure the remaining keys still exist
      foreach (var key in keys.Skip(100)) {
        Assert.AreEqual(key.ToString(), Helpers.Search(tree, key)?.Value, $"Key {key} should be found with correct value for rank {rank}");
      }
    }
  }
  #endregion
}
