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

Insert is one of the more complex operations we have implemented it uses a top down approach, where we traverse down the tree to find the leaf node that the key should be inserted into, we follow the BST bucket property of the B+tree for this traversal same as we would for search. On the way down if a node is full we split it to allow room to bubble up split children. Once we reach the leaf node we insert the key value pair into the leaf node, if the leaf node is full we split it and bubble up the minimum key of the new leaf to the parent internal node. This process is pretty efficient and runs in o(log n) time as we are only traversing down a single path of the tree and doing a constant amount of work at each node.

### BulkInsert

Bulk Insert uses a botttom up approach we first start by sorting the list of key value pairs after we have sorted the list we can start building the tree from the bottom up. In a first pass we divide up our leaves into keys keeping track of the minimum key in each leaf, then we can use the list of minimum keys along with the leaves to build the next level of the tree making up the internal nodes, we do pretty much the same thing for the internal nodes as we did for the leaves we divide up the internal nodes into groups of the rank of the tree and keep bubble the minimum key up, the keys of the internal node are the minimum keys of the children minus the first one which is not needed. We repeat this process until we are left with a single node which becomes the root of the tree.

This process is pretty efficient and if the input list is already sorted it can run in o(n) time, if the input list is not sorted it runs in o(n log n) due to the sorting step. This is much better than inserting each key value pair one at a time which would run in o(n log n) time even if the input list is already sorted. It's worth noting that while the worst case in both scenarios is o(n log n) even in the worst case the `o` notation hides the constant factor which is much smaller as we are doing far less traversals of the tree in the bulk insert case due to the bottom up approach.

I think another way to implement this efficiently would be to use an accumulator and build the entire tree from the left, the problem I saw with this approach is book keeping feels like it becomes a little more difficult.

### Delete

TODO: 

### Merge

Merge itself is very simple there are a few ways to implement merge all with different tradeoffs after considering a few different approaches we decided to implement a rather simple one. We navigate to the left most leaf of both trees and collect all of the key value pairs from both trees into a single list. (You can think of this as if we call `concat(range(start, end, tree1), range(start,end, tree2)))` conceptually). Once we have a list of all keys from the nodes we use `bulkInsert` to create a new tree with the same rank as tree1. This approach is pretty efficient and depending on the sorting algorithm used by the stdlib can be done in `o(n + m)` time where `n` and `m` are the number of key value pairs in tree1 and tree2 respectively. This is actually rather optimal when you consider that we have to look at every key to ensure they are ordered anyways.

Merge could be implemented more similar to bulkInsert but working on the nodes instead of building them from scratch this would be more efficient and if both trees had the same rank could let us merge in `o(log n)` time but this would be a much more complex implementation and has issues when the trees have different ranks.

As a note in our implementation we use c# standard `List.sort` function after concatting the lists of key values pairs from both trees this is `o(n log n)` in the worst case but is `o(n)` if the input lists are already sorted. We could make this `o(n + m)` by bassically traversing both lists at the same time and taking the smaller key value pair at each step to build a new list of key value pairs that is sorted, this would be more efficient but requires more code and makes the impl a little less readable so we opted to use the built in sorting function.

Copyright ©️ 2026 Jake Follest, Kara Wilson
