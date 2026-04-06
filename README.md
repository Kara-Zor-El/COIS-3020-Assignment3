# COIS 3020 Assignment 3 - B+ Tree

Authors:
* 🦊 Jake Follest (0797420)
* Kara Wilson (0800561)

## Working with the project

### Building and running the tests
To build the project and run the tests you can use the following commands in the terminal:
```shell
task
```

#### Build
In order to build the project you can run `task build` this will compile all of the source files. If you do not have `task` installed you can run `dotnet build A3 --configuration Release` and `dotnet build A3Tests --configuration Release` to build both the main project and the test project.

#### Test
In order to run the tests you can run `task test` this will build the project and then run all of our unit tests against the implementation. If you do not have `task` installed you can run `dotnet test A3Tests --logger "console;verbosity=detailed" -- {{.CLI_ARGS}}` to run the tests after building the project.

#### Format
In order to format the code you can run `task format` this will run `dotnet format` on both the main project and the test project to format all of the code according to the .editorconfig file. If you do not have `task` installed you can run `dotnet format A3` and `dotnet format A3Tests`.

### Dependencies
This project has no external dependencies for the implementation beyond `MSUnit` which is purely used for testing and is only a development dependency, that comes pre-installed with the .NET SDK so there is no need to install it separately. As for system dependencies we use the following tooling:
* `dotnet` (Required) - As we use c# dotnet is required to build and work with the project
* `task` (Optional, but recommended for ease of use) - [Taskfile](https://taskfile.dev/) is used in order to make development simpler and to provide a consistent interface.
* `nix` (Optional) - Nix is used to manage our development environment and ensure everyone is working with the same versions to see the tooling we use check `flake.nix` for the up to date list. It isn't needed and a user can just install the tools defined there to run the project.


## Implementation

Below is a basic overview of our implementation.

When implementing our project we started with grain hence the [`BplusTree.gr`](./BPlusTree.gr) file, which contains our initial implementation the tests we were using can be found here [spotandjake/grain-dsa](https://github.com/spotandjake/grain-dsa). The reason we start in grain is it enforces better programming practices then c# and ensures we are handling every case we found this increased the c# code quality and made it more robust as imperative and object oriented programming can lead to a lot of book keeping. It's for this reason that you see a lot of pattern matching and recursion in our implementation. As a note recursion is implemented in a [tail call](https://en.wikipedia.org/wiki/Tail_call) manner this allows a good compiler to optimize the recursion directly into a more efficient loop which prevent stack overflows, tail call optimization is not something c# guaranteed.

### Search

In order to implement search on the B+Tree we use our `searchHelp` helper function all this does is take the root of the tree and start searching on it this lets us think at the node level instead of the parent level. When searching there are two different things we do depending on if we are looking at a leaf or internal node.

#### Internal Nodes
When we are looking at an internal node we need to determine which child bucket to traverse into, this is done by using `childIndex` which iterates through the keys of the internal node and finds the first key that is greater than the key we are looking for, when we find this key we know that the key we are looking for must be in the bucket prior to this key. We can then call `searchHelp` on the child node in this bucket to continue our search down the tree.

#### Leaf Nodes
Leaf nodes are actually pretty similar we can use the same process as we do for internal nodes to find the key in the leaf node, we iterate through the keys in the leaf node until we find a key that is greater than the key we are looking for, when we find this key we know that the key we are looking for must be in the bucket prior to this key, if we reach the end of the keys without finding a key greater than the key we are looking for then we know that the key must be in the last bucket, or is not in the tree. Once we find the bucket that the key should be in we double check that they key in this bucket is actually the key we are looking for if it is we return the value if it isn't then we know that the key is not in the tree and we can return null.

### Range

When we are performing a range operation its really rather simple we start by calling our `searchHelp` function to find the leaf node, that contains the first key. We then call our `GetLeavesFromStart` helper which returns all the leaf nodes starting from the leaf node we give it to the end of the tree following the next property of a leaf node. While iterating through the leaves we iterate through the values if the key is greater than the start key we add it to an accumulator list, if the key is greater than the end key we can stop iterating and return the accumulator.

There is a small optimzation we could do here that we don't which is to check the last key and first key of the leaf node to determine if all the values in the leaf are within the range in this case we could just add all of the values in the leaf node. This would save us the iteration and if implemented well by the runtime could turn an `o(n)` operation into an `o(1)` operation. This would hurt performance in the case where we have a lot of small ranges or ranges inside of a leaf but it would help performance in the case where we have a lot of large ranges or ranges that span multiple leaf nodes. We opted not to implement this optimization as it would cloud the implementation and make it less explicit for performance we don't need for our use cases.

### Insert

Insert is one of the more complex operation we have implemented it uses a top down approach. On the way down the tree we check each node if the node is full we pre-emptively split the node so that there will be room to bubble up a key if we have to split a child node, this approach ensures that we never need to backtrack up the tree after a split occurs. As with. most of the other operations we have implemented we are best to think about this at the node level from two perspectives internal nodes and leaf nodes.

#### Internal Nodes
When inserting into an internal node the first step is to check if the node is full and if so split it. Splitting an internal node is a rather simple process we take the keys and children of the internal node and split them at the center, the node we are splitting becomes the left node and we return the right node and promoted key (the first key of the right node) to be inserted into the parent node. After we have performed the split. Once the split is done we can then determine weather to go left or right using the same process as we do in other functions such as search by using `childIndex` to scan for the first key that is greater than the key we are inserting. This will give us the bucket that we need to insert into and we can then call `insertHelp` which is a recursive helper to try to insert on the next node. If the next node is an internal node we repeat the same process otherwise we handle the leaf case.

#### Leaf Nodes
When we are inserting into a leaf node the process is pretty simple we scan the leaf with `childIndex` to find the bucket we need to insert into, if the key already exists in the tree we can return `false` indicating we should throw a `duplicateKey` exception. Otherwise we insert at the `childIndex`.

### BulkInsert

Bulk Insert is a rather interesting operation to implement. The naive approach to bulk insert would be to just call `insert` for each entry, this would give us a time complexity of `o(n log n)` where `n` is the number of entries we are inserting. This because each insert is `o(log n)` and we are doing `n` inserts. This is pretty inefficient however. As part of the requirements for `bulkInsert` is that the tree is initially empty we can take advantage of this and build the tree from the bottom up. We start by chunking the entries into chunks of size `rank - 1` and each chunk is assigned to a leaf node, we also keep track of the first key in each leaf node as this will be used in the next step where we take all the nodes we just created and chunks them into internal nodes of size `rank`, we repeat this process filling the internal nodes with the first key of each child node and the nodes on the level before until we are finally left with a single node which can become the root node.

### Delete

Delete is implemented in a top-down manner. It rebalances on the way down before recursing, so it never has to repair on the way up after removing the key. We start by collapsing any internal nodes with only child so that the tree height can shrink early. We then call `DeleteHelp` for the core logic of our delete function

#### DeleteHelp
When we are deleting a key through the tree, we have two cases, if the current node is a leaf then this is the node where the key must be removed from (if it exists). If the current node is an internal node, we first determine which bucket to traverse to next using the same comparison logic as search. Before descending we check if that child is at minimum occupancy, and if it is, we rebalance it first using `FixChild` so we can safely continue downwards.

After we recurse, if a subtree's minimum key changed because of the delete/rebalance, we update the separator key in the parent so routing still works correctly. If an internal root ends up with one child, we can collapse it which helps shrink the height of the tree cleanly.

#### FixChild
`FixChild` handles the rebalance work needed before descending into a child that is at minimum occupancy. We try to bottom from the left sibling first, then from the right sibling, and if neither can lend, we merge.

For leaf nodes, borrow means moving one key/value pair from a sibling and then updating the parent separator key. For internal nodes, borrow means rotating a separator key through the parent while also moving a child pointer. If we merge, we combine the two sibling nodes and remove the separator key and child pointer from the parent.

This approach keeps delete top-down and predictable, because we only walk one path from root to leaf.

### Merge

Merge itself is very simple there are a few ways to implement merge all with different tradeoffs after considering a few different approaches we decided to implement a rather simple one. We navigate to the left most leaf of both trees and collect all of the key value pairs from both trees into a single list. (You can think of this as if we call `concat(range(start, end, tree1), range(start,end, tree2)))` conceptually). Once we have a list of all keys from the nodes we use `bulkInsert` to create a new tree with the same rank as tree1. This approach is pretty efficient and depending on the sorting algorithm used by the stdlib can be done in `o(n + m)` time where `n` and `m` are the number of key value pairs in tree1 and tree2 respectively. This is actually rather optimal when you consider that we have to look at every key to ensure they are ordered anyways.

Merge could be implemented more similar to bulkInsert but working on the nodes instead of building them from scratch this would be more efficient and if both trees had the same rank could let us merge in `o(log n)` time but this would be a much more complex implementation and has issues when the trees have different ranks.

As a note in our implementation we use c# standard `List.sort` function after concatting the lists of key values pairs from both trees this is `o(n log n)` in the worst case but is `o(n)` if the input lists are already sorted. We could make this `o(n + m)` by bassically traversing both lists at the same time and taking the smaller key value pair at each step to build a new list of key value pairs that is sorted, this would be more efficient but requires more code and makes the impl a little less readable so we opted to use the built in sorting function.

Copyright ©️ 2026 Jake Follest, Kara Wilson
