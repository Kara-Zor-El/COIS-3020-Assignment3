# COIS 3020 Assignment 3 - B+ Tree

Authors:
* 🦊 Jake Follest (0797420)
* Kara Wilson (0800561)

# BPlusTree


## Implementation

### Make

This function is extremely simple, it just creates a brand new B+tree, with the root node being an empty leaf node.

### Search

In order to implement search we must think in two parts, the first part is finding the leaf that contains the given node this is done by calling the helper function `searchHelp` with `root` and the process is defined below, and the second part is searching through the leaf node to find the value corresponding to the given key.

#### SearchHelp
When we are looking for a key through the tree we have two cases, if the current node we are looking at is a leaf the key must be in this leaf. If the node we are looking at is an internal node we need to determine which bucket to traverse to next. The way we find the bucket is by iterating through the keys in the internal node until we find a key that is greater than the key we are looking for, when we hit this case we know that the key must be in the bucket prior to the key that is greater than it. If we reach the end of the keys without finding a key greater than the key we are looking for then we know that the key must be in the last bucket, or is not in the tree.

#### Searching the leaf
We can actually use the exact same process as described in `searchHelp` directly on the leaf itself to find the value in the leaf, iterate through the leaf until we find the case of `k == key` 

### Range

When we are searching for a range of keys we can use the same process as search to find the leaf that contains the first key. From there we can iterate through the linked list of leaf nodes adding anything where the key is grater than k1 and less than k2 to the result list until we reach a key that is greater than k2, at which point we can stop iterating through the leaf nodes and return the result list.

If we were using vectors over linked lists inside the leaf nodes we could optimize this a little instead of iterating through every key in the leaf nodes we could check the final key in the leaf node to see if it is less than k2, to determine if we need to iterate or are on the last leaf, this saves quite a bit of time but it doesn't make sense on a linked list where you need to traverse the entire list to get the tail.

### Insert

Insert is one of the more complex operations we have implemented it uses a top down approach, where we traverse down the tree to find the leaf node that the key should be inserted into, we follow the BST bucket property of the B+tree for this traversal same as we would for search. On the way down if a node is full we split it to allow room to bubble up split children. Once we reach the leaf node we insert the key value pair into the leaf node, if the leaf node is full we split it and bubble up the minimum key of the new leaf to the parent internal node. This process is pretty efficient and runs in o(log n) time as we are only traversing down a single path of the tree and doing a constant amount of work at each node.

### BulkInsert

Bulk Insert uses a botttom up approach we first start by sorting the list of key value pairs after we have sorted the list we can start building the tree from the bottom up. In a first pass we divide up our leaves into keys keeping track of the minimum key in each leaf, then we can use the list of minimum keys along with the leaves to build the next level of the tree making up the internal nodes, we do pretty much the same thing for the internal nodes as we did for the leaves we divide up the internal nodes into groups of the rank of the tree and keep bubble the minimum key up, the keys of the internal node are the minimum keys of the children minus the first one which is not needed. We repeat this process until we are left with a single node which becomes the root of the tree.

This process is pretty efficient and if the input list is already sorted it can run in o(n) time, if the input list is not sorted it runs in o(n log n) due to the sorting step. This is much better than inserting each key value pair one at a time which would run in o(n log n) time even if the input list is already sorted. It's worth noting that while the worst case in both scenarios is o(n log n) even in the worst case the `o` notation hides the constant factor which is much smaller as we are doing far less traversals of the tree in the bulk insert case due to the bottom up approach.

I think another way to implement this efficiently would be to use an accumulator and build the entire tree from the left, the problem I saw with this approach is book keeping feels like it becomes a little more difficult.

### Delete

TODO: 

### Merge

Merge itself is very simple as it re-uses `bulkInsert` we essentially just take all of the key value pairs from both trees, we do this by calling a helper function `getMinLeaf` which traverses down the left most path of the tree to find the minimum leaf we then follow the `next` collecting all the key values pairs until we reach the end of the linked list of leaf nodes. Once we have all of the key value pairs from both trees we can just merge the two lists together and call `bulkInsert` on the merged list to create a new tree with all of the key value pairs from both trees.

### FromList

Implementing fromList is pretty simple as it's just a matter of bulk inserting all of the key value pairs from the list into an empty tree of the desired rank, this means that the implementation of fromList is just a call to bulkInsert with the list and a new empty tree.

Copyright ©️ 2026 Jake Follest, Kara Wilson
