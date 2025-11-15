using System.Text;
using MerkleTree;

namespace Examples;

/// <summary>
/// Demonstrates the streaming Merkle tree builder capabilities.
/// </summary>
public class StreamingExample
{
    /// <summary>
    /// Demonstrates building a Merkle tree from streaming data without loading all data into memory at once.
    /// </summary>
    public static void DemonstrateStreamingBuild()
    {
        Console.WriteLine("=== Streaming Merkle Tree Example ===\n");
        
        // Example 1: Simple streaming build
        Console.WriteLine("Example 1: Basic Streaming Build");
        Console.WriteLine("Building Merkle tree from 5 leaf values...");
        
        var builder = new MerkleTreeBuilder();
        var leaves = new[] { "data1", "data2", "data3", "data4", "data5" }
            .Select(s => Encoding.UTF8.GetBytes(s));
        
        var metadata = builder.Build(leaves);
        
        Console.WriteLine($"Root Hash: {Convert.ToHexString(metadata.RootHash)}");
        Console.WriteLine($"Tree Height: {metadata.Height}");
        Console.WriteLine($"Leaf Count: {metadata.LeafCount}\n");
        
        // Example 2: Large dataset with batch processing
        Console.WriteLine("Example 2: Large Dataset with Batch Processing");
        Console.WriteLine("Building Merkle tree from 10,000 leaves in batches of 100...");
        
        var largeLeaves = Enumerable.Range(1, 10000)
            .Select(i => Encoding.UTF8.GetBytes($"leaf{i}"));
        
        var largeMetadata = builder.BuildInBatches(largeLeaves, batchSize: 100);
        
        Console.WriteLine($"Root Hash: {Convert.ToHexString(largeMetadata.RootHash)}");
        Console.WriteLine($"Tree Height: {largeMetadata.Height}");
        Console.WriteLine($"Leaf Count: {largeMetadata.LeafCount}\n");
        
        // Example 3: Async streaming
        Console.WriteLine("Example 3: Async Streaming");
        Console.WriteLine("Building Merkle tree from async stream...");
        
        var asyncTask = DemonstrateAsyncStreaming();
        asyncTask.Wait();
        
        // Example 4: Comparison with in-memory MerkleTree
        Console.WriteLine("\nExample 4: Comparison with In-Memory MerkleTree");
        Console.WriteLine("Verifying that streaming produces same results as in-memory...");
        
        var testData = new[] { "test1", "test2", "test3" }
            .Select(s => Encoding.UTF8.GetBytes(s))
            .ToList();
        
        var streamingResult = builder.Build(testData);
        var inMemoryTree = new MerkleTree.MerkleTree(testData);
        var inMemoryRoot = inMemoryTree.GetRootHash();
        
        bool matches = streamingResult.RootHash.SequenceEqual(inMemoryRoot);
        Console.WriteLine($"Streaming and in-memory results match: {matches}");
        Console.WriteLine($"Root Hash: {Convert.ToHexString(streamingResult.RootHash)}\n");
        
        // Example 5: Different hash functions
        Console.WriteLine("Example 5: Using Different Hash Functions");
        
        var sha256Builder = new MerkleTreeBuilder(new Sha256HashFunction());
        var sha512Builder = new MerkleTreeBuilder(new Sha512HashFunction());
        var blake3Builder = new MerkleTreeBuilder(new Blake3HashFunction());
        
        var sampleData = new[] { "sample1", "sample2" }
            .Select(s => Encoding.UTF8.GetBytes(s));
        
        var sha256Result = sha256Builder.Build(sampleData);
        var sha512Result = sha512Builder.Build(sampleData.Select(d => d.ToArray()));
        var blake3Result = blake3Builder.Build(sampleData.Select(d => d.ToArray()));
        
        Console.WriteLine($"SHA-256 Root: {Convert.ToHexString(sha256Result.RootHash)}");
        Console.WriteLine($"SHA-512 Root: {Convert.ToHexString(sha512Result.RootHash)}");
        Console.WriteLine($"BLAKE3 Root: {Convert.ToHexString(blake3Result.RootHash)}");
    }
    
    /// <summary>
    /// Demonstrates async streaming capabilities.
    /// </summary>
    private static async Task DemonstrateAsyncStreaming()
    {
        var builder = new MerkleTreeBuilder();
        
        // Simulate streaming data from a source (e.g., file, network, database)
        var asyncLeaves = GenerateAsyncLeaves(100);
        
        var metadata = await builder.BuildAsync(asyncLeaves);
        
        Console.WriteLine($"Async Root Hash: {Convert.ToHexString(metadata.RootHash)}");
        Console.WriteLine($"Async Tree Height: {metadata.Height}");
        Console.WriteLine($"Async Leaf Count: {metadata.LeafCount}");
    }
    
    /// <summary>
    /// Simulates generating leaves asynchronously (e.g., from file I/O or network).
    /// </summary>
    private static async IAsyncEnumerable<byte[]> GenerateAsyncLeaves(int count)
    {
        for (int i = 0; i < count; i++)
        {
            // Simulate async I/O delay
            await Task.Delay(1);
            
            yield return Encoding.UTF8.GetBytes($"async_leaf_{i}");
        }
    }
    
    /// <summary>
    /// Demonstrates handling a simulated large file without loading it all into memory.
    /// </summary>
    public static void DemonstrateFileSimulation()
    {
        Console.WriteLine("\n=== Simulated Large File Processing ===\n");
        Console.WriteLine("Simulating processing a large file with 100,000 fixed-size records...");
        Console.WriteLine("(In real usage, this would read from an actual file stream)\n");
        
        var builder = new MerkleTreeBuilder();
        
        // Simulate reading fixed-size records from a large file
        // In real usage, this would use FileStream with buffered reading
        var recordSize = 32; // 32 bytes per record
        var totalRecords = 100000;
        var batchSize = 1000;
        
        var startTime = DateTime.UtcNow;
        
        var records = GenerateFixedSizeRecords(totalRecords, recordSize);
        var metadata = builder.BuildInBatches(records, batchSize);
        
        var elapsed = DateTime.UtcNow - startTime;
        
        Console.WriteLine($"Processed {totalRecords:N0} records in {elapsed.TotalMilliseconds:F2}ms");
        Console.WriteLine($"Root Hash: {Convert.ToHexString(metadata.RootHash)}");
        Console.WriteLine($"Tree Height: {metadata.Height}");
        Console.WriteLine($"Leaf Count: {metadata.LeafCount:N0}");
        Console.WriteLine($"Memory-efficient batch processing used to minimize RAM usage");
    }
    
    /// <summary>
    /// Generates fixed-size records to simulate file data.
    /// </summary>
    private static IEnumerable<byte[]> GenerateFixedSizeRecords(int count, int size)
    {
        for (int i = 0; i < count; i++)
        {
            // Generate a fixed-size record
            var record = new byte[size];
            
            // Fill with deterministic data (in real usage, this would be actual data)
            var idBytes = BitConverter.GetBytes(i);
            Array.Copy(idBytes, 0, record, 0, Math.Min(idBytes.Length, size));
            
            yield return record;
        }
    }
}
