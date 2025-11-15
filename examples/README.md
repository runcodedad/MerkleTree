# MerkleTree Examples

This directory contains example code demonstrating how to use the MerkleTree library's streaming capabilities.

## StreamingExample.cs

Demonstrates various ways to build Merkle trees using the `MerkleTreeBuilder` class:

1. **Basic Streaming Build**: Simple example building a tree from a small dataset
2. **Large Dataset with Batch Processing**: Building a tree from 10,000 leaves using batches
3. **Async Streaming**: Using `BuildAsync` with `IAsyncEnumerable<byte[]>`
4. **Comparison with In-Memory**: Verifying streaming produces identical results to the in-memory `MerkleTree` class
5. **Different Hash Functions**: Using SHA-256, SHA-512, and BLAKE3
6. **File Simulation**: Simulating processing 100,000 fixed-size records from a large file

## Running the Examples

These examples are intended as code references. To run them in your own project:

1. Add a reference to the MerkleTree library
2. Copy the example code into your project
3. Call the demonstration methods from your Main method:

```csharp
using Examples;

class Program
{
    static void Main(string[] args)
    {
        StreamingExample.DemonstrateStreamingBuild();
        StreamingExample.DemonstrateFileSimulation();
    }
}
```

## Key Features Demonstrated

- **Memory-Efficient Processing**: Process large datasets without loading everything into memory
- **Deterministic Results**: Streaming produces the same root hash as in-memory construction
- **Flexible Input Sources**: Support for `IEnumerable<byte[]>` and `IAsyncEnumerable<byte[]>`
- **Batch Processing**: Control memory usage by processing leaves in configurable batches
- **Multiple Hash Functions**: Use SHA-256, SHA-512, or BLAKE3
- **Tree Metadata**: Access root hash, tree height, and leaf count
