using System.Text;
using Xunit;
using MerkleTree.Core;
using MerkleTree.Hashing;
using MerkleTree.Proofs;
using MerkleTree.Cache;
using MerkleTreeClass = MerkleTree.Core.MerkleTree;

namespace MerkleTree.Tests.Integration;

/// <summary>
/// Integration tests for end-to-end workflows combining multiple library features.
/// </summary>
public class EndToEndWorkflowTests
{
    /// <summary>
    /// Helper method to create leaf data from strings.
    /// </summary>
    private static List<byte[]> CreateLeafData(params string[] data)
    {
        return data.Select(s => Encoding.UTF8.GetBytes(s)).ToList();
    }

    [Fact]
    public void CompleteWorkflow_BuildTree_GenerateProof_VerifyProof()
    {
        // Arrange - Create a dataset
        var leafData = Enumerable.Range(0, 50)
            .Select(i => Encoding.UTF8.GetBytes($"transaction_{i}"))
            .ToList();

        // Act - Build tree
        var tree = new MerkleTreeClass(leafData);
        var rootHash = tree.GetRootHash();

        // Act - Generate proofs for multiple leaves
        var proof0 = tree.GenerateProof(0);
        var proof25 = tree.GenerateProof(25);
        var proof49 = tree.GenerateProof(49);

        // Act - Verify all proofs
        var hashFunction = new Sha256HashFunction();
        var valid0 = proof0.Verify(rootHash, hashFunction);
        var valid25 = proof25.Verify(rootHash, hashFunction);
        var valid49 = proof49.Verify(rootHash, hashFunction);

        // Assert
        Assert.True(valid0, "Proof for leaf 0 should be valid");
        Assert.True(valid25, "Proof for leaf 25 should be valid");
        Assert.True(valid49, "Proof for leaf 49 should be valid");
    }

    [Fact]
    public void CompleteWorkflow_SerializeProof_DeserializeProof_VerifyProof()
    {
        // Arrange
        var leafData = CreateLeafData("data1", "data2", "data3", "data4", "data5");
        var tree = new MerkleTreeClass(leafData);
        var rootHash = tree.GetRootHash();
        var hashFunction = new Sha256HashFunction();

        // Act - Generate and serialize proof
        var originalProof = tree.GenerateProof(2);
        var serialized = originalProof.Serialize();

        // Act - Deserialize proof
        var deserializedProof = MerkleProof.Deserialize(serialized);

        // Act - Verify deserialized proof
        var isValid = deserializedProof.Verify(rootHash, hashFunction);

        // Assert
        Assert.True(isValid, "Deserialized proof should be valid");
        Assert.Equal(originalProof.LeafValue, deserializedProof.LeafValue);
        Assert.Equal(originalProof.LeafIndex, deserializedProof.LeafIndex);
    }

    [Fact]
    public async Task CompleteWorkflow_StreamingTree_WithCache_GenerateProof_VerifyProof()
    {
        // Arrange - Create a larger dataset
        var leafData = Enumerable.Range(0, 100)
            .Select(i => Encoding.UTF8.GetBytes($"block_{i}"))
            .ToList();

        async IAsyncEnumerable<byte[]> GetAsyncLeaves()
        {
            foreach (var leaf in leafData)
            {
                await Task.Yield();
                yield return leaf;
            }
        }

        var tempFile = Path.Combine(Path.GetTempPath(), $"e2e_test_{Guid.NewGuid():N}.cache");

        try
        {
            // Act - Build tree with cache
            var stream = new MerkleTreeStream();
            var cacheConfig = new CacheConfiguration(tempFile, topLevelsToCache: 3);
            var metadata = await stream.BuildAsync(GetAsyncLeaves(), cacheConfig);

            // Act - Load cache and generate proof
            var cache = CacheFileManager.LoadCache(tempFile);
            var proof = await stream.GenerateProofAsync(GetAsyncLeaves(), 50, 100, cache);

            // Act - Verify proof
            var hashFunction = new Sha256HashFunction();
            var isValid = proof.Verify(metadata.RootHash, hashFunction);

            // Assert
            Assert.True(isValid, "Proof should be valid");
            Assert.Equal(50, proof.LeafIndex);
            Assert.True(cache.Statistics.Hits > 0, "Cache should have been used");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task CompleteWorkflow_CompareInMemoryAndStreamingResults()
    {
        // Arrange - Create same dataset for both approaches
        var leafData = Enumerable.Range(0, 75)
            .Select(i => Encoding.UTF8.GetBytes($"data_{i}"))
            .ToList();

        async IAsyncEnumerable<byte[]> GetAsyncLeaves()
        {
            foreach (var leaf in leafData)
            {
                await Task.Yield();
                yield return leaf;
            }
        }

        // Act - Build in-memory tree
        var inMemoryTree = new MerkleTreeClass(leafData);
        var inMemoryRoot = inMemoryTree.GetRootHash();
        var inMemoryProof = inMemoryTree.GenerateProof(30);

        // Act - Build streaming tree
        var streamingTree = new MerkleTreeStream();
        var streamingMetadata = await streamingTree.BuildAsync(GetAsyncLeaves());
        var streamingProof = await streamingTree.GenerateProofAsync(GetAsyncLeaves(), 30, 75);

        // Assert - Both should produce identical results
        Assert.Equal(inMemoryRoot, streamingMetadata.RootHash);
        Assert.Equal(inMemoryProof.LeafValue, streamingProof.LeafValue);
        Assert.Equal(inMemoryProof.LeafIndex, streamingProof.LeafIndex);
        Assert.Equal(inMemoryProof.TreeHeight, streamingProof.TreeHeight);
        Assert.Equal(inMemoryProof.SiblingHashes.Length, streamingProof.SiblingHashes.Length);

        for (int i = 0; i < inMemoryProof.SiblingHashes.Length; i++)
        {
            Assert.Equal(inMemoryProof.SiblingHashes[i], streamingProof.SiblingHashes[i]);
            Assert.Equal(inMemoryProof.SiblingIsRight[i], streamingProof.SiblingIsRight[i]);
        }
    }

    [Fact]
    public void CompleteWorkflow_DifferentHashFunctions_ProduceDifferentTrees()
    {
        // Arrange
        var leafData = CreateLeafData("data1", "data2", "data3", "data4");

        // Act - Build trees with different hash functions
        var treeSHA256 = new MerkleTreeClass(leafData, new Sha256HashFunction());
        var treeSHA512 = new MerkleTreeClass(leafData, new Sha512HashFunction());
        var treeBLAKE3 = new MerkleTreeClass(leafData, new Blake3HashFunction());

        // Act - Generate proofs
        var proofSHA256 = treeSHA256.GenerateProof(1);
        var proofSHA512 = treeSHA512.GenerateProof(1);
        var proofBLAKE3 = treeBLAKE3.GenerateProof(1);

        // Assert - Different hash functions produce different roots
        Assert.NotEqual(treeSHA256.GetRootHash(), treeSHA512.GetRootHash());
        Assert.NotEqual(treeSHA256.GetRootHash(), treeBLAKE3.GetRootHash());
        Assert.NotEqual(treeSHA512.GetRootHash(), treeBLAKE3.GetRootHash());

        // Assert - But each proof verifies with its own hash function
        Assert.True(proofSHA256.Verify(treeSHA256.GetRootHash(), new Sha256HashFunction()));
        Assert.True(proofSHA512.Verify(treeSHA512.GetRootHash(), new Sha512HashFunction()));
        Assert.True(proofBLAKE3.Verify(treeBLAKE3.GetRootHash(), new Blake3HashFunction()));
    }

    [Fact]
    public void CompleteWorkflow_ProofSerialization_WithDifferentHashFunctions()
    {
        // Arrange
        var leafData = CreateLeafData("data1", "data2", "data3");

        // Act - Build tree and generate proof with SHA-512
        var tree = new MerkleTreeClass(leafData, new Sha512HashFunction());
        var proof = tree.GenerateProof(1);
        var serialized = proof.Serialize();

        // Act - Deserialize and verify
        var deserializedProof = MerkleProof.Deserialize(serialized);
        var isValid = deserializedProof.Verify(tree.GetRootHash(), new Sha512HashFunction());

        // Assert
        Assert.True(isValid, "Deserialized proof with SHA-512 should be valid");
        Assert.Equal(64, tree.GetRootHash().Length); // SHA-512 produces 64-byte hashes
    }

    [Fact]
    public async Task CompleteWorkflow_MultipleConcurrentProofGenerations()
    {
        // Arrange - Create dataset
        var leafData = Enumerable.Range(0, 100)
            .Select(i => Encoding.UTF8.GetBytes($"item_{i}"))
            .ToList();

        async IAsyncEnumerable<byte[]> GetAsyncLeaves()
        {
            foreach (var leaf in leafData)
            {
                await Task.Yield();
                yield return leaf;
            }
        }

        var stream = new MerkleTreeStream();
        var metadata = await stream.BuildAsync(GetAsyncLeaves());

        // Act - Generate multiple proofs concurrently
        var proofTasks = new[]
        {
            stream.GenerateProofAsync(GetAsyncLeaves(), 10, 100),
            stream.GenerateProofAsync(GetAsyncLeaves(), 25, 100),
            stream.GenerateProofAsync(GetAsyncLeaves(), 50, 100),
            stream.GenerateProofAsync(GetAsyncLeaves(), 75, 100),
            stream.GenerateProofAsync(GetAsyncLeaves(), 99, 100)
        };

        var proofs = await Task.WhenAll(proofTasks);

        // Assert - All proofs should be valid
        var hashFunction = new Sha256HashFunction();
        foreach (var proof in proofs)
        {
            Assert.True(proof.Verify(metadata.RootHash, hashFunction), 
                $"Proof for leaf {proof.LeafIndex} should be valid");
        }
    }

    [Fact]
    public void CompleteWorkflow_NonPowerOfTwoLeaves_AllProofsValid()
    {
        // Arrange - Use various non-power-of-two counts
        var testCounts = new[] { 3, 5, 7, 9, 11, 13, 15, 17, 19, 21 };

        foreach (var count in testCounts)
        {
            var leafData = Enumerable.Range(0, count)
                .Select(i => Encoding.UTF8.GetBytes($"data_{i}"))
                .ToList();

            var tree = new MerkleTreeClass(leafData);
            var rootHash = tree.GetRootHash();
            var hashFunction = new Sha256HashFunction();

            // Act - Generate and verify proof for each leaf
            for (int i = 0; i < count; i++)
            {
                var proof = tree.GenerateProof(i);
                var isValid = proof.Verify(rootHash, hashFunction);

                // Assert
                Assert.True(isValid, $"Proof for leaf {i} in tree with {count} leaves should be valid");
            }
        }
    }

    [Fact]
    public async Task CompleteWorkflow_CacheSerialization_RoundTrip()
    {
        // Arrange - Build tree with cache
        var leafData = Enumerable.Range(0, 50)
            .Select(i => Encoding.UTF8.GetBytes($"data_{i}"))
            .ToList();

        async IAsyncEnumerable<byte[]> GetAsyncLeaves()
        {
            foreach (var leaf in leafData)
            {
                await Task.Yield();
                yield return leaf;
            }
        }

        var tempFile1 = Path.Combine(Path.GetTempPath(), $"cache1_{Guid.NewGuid():N}.cache");
        var tempFile2 = Path.Combine(Path.GetTempPath(), $"cache2_{Guid.NewGuid():N}.cache");

        try
        {
            var stream = new MerkleTreeStream();
            var cacheConfig = new CacheConfiguration(tempFile1, topLevelsToCache: 3);
            await stream.BuildAsync(GetAsyncLeaves(), cacheConfig);

            // Act - Load cache, serialize, save to new file
            var cache1 = CacheFileManager.LoadCache(tempFile1);
            var serialized = CacheSerializer.Serialize(cache1);
            await File.WriteAllBytesAsync(tempFile2, serialized);

            // Act - Load from new file
            var cache2 = CacheFileManager.LoadCache(tempFile2);

            // Assert - Both caches should be functionally identical
            Assert.Equal(cache1.Metadata.TreeHeight, cache2.Metadata.TreeHeight);
            Assert.Equal(cache1.Metadata.HashFunctionName, cache2.Metadata.HashFunctionName);
            Assert.Equal(cache1.Metadata.StartLevel, cache2.Metadata.StartLevel);
            Assert.Equal(cache1.Metadata.EndLevel, cache2.Metadata.EndLevel);
            Assert.Equal(cache1.Levels.Count, cache2.Levels.Count);
        }
        finally
        {
            if (File.Exists(tempFile1)) File.Delete(tempFile1);
            if (File.Exists(tempFile2)) File.Delete(tempFile2);
        }
    }

    [Fact]
    public void CompleteWorkflow_TreeMetadata_IsAccurate()
    {
        // Arrange - Test different tree sizes
        var testSizes = new[] { 1, 2, 4, 8, 16, 32, 64, 100 };

        foreach (var size in testSizes)
        {
            var leafData = Enumerable.Range(0, size)
                .Select(i => Encoding.UTF8.GetBytes($"leaf_{i}"))
                .ToList();

            // Act
            var tree = new MerkleTreeClass(leafData);
            var metadata = tree.GetMetadata();

            // Assert
            Assert.Equal(size, metadata.LeafCount);
            Assert.Equal(tree.GetRootHash(), metadata.RootHash);
            Assert.Equal(tree.Root, metadata.Root);

            // Verify height calculation
            int expectedHeight = size == 1 ? 0 : (int)Math.Ceiling(Math.Log2(size));
            Assert.Equal(expectedHeight, metadata.Height);
        }
    }
}
