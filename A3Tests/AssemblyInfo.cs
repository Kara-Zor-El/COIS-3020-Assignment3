using Microsoft.VisualStudio.TestTools.UnitTesting;

// This file is just here to appease a small quirk of MSTest where it doesn't know the default
// parallelization scope. We could run our tests in parallel at the method level but we prefer 
// class level, as our test suite runs quick and is ordered so that failures propagate downwards 
// in other words we would rather run them in order so we can see the minimum failing unit 
// first rather than see something like stress testing failing and not know what caused it.
[assembly: Parallelize(Scope = ExecutionScope.ClassLevel)]
