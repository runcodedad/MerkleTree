using Xunit;
using MerkleTree.Core;
using MerkleTree.Cache;
using MerkleTree.Hashing;
using System.Text;

namespace MerkleTree.Tests.Cache;

/// <summary>
/// Tests for cache integration with MerkleTree construction and proof generation.
/// </summary>
public class CacheIntegrationTests
{
    /// <summary>
    /// Helper method to create sample leaf data.
    /// </summary>
    private static List<byte[]> CreateLeafData(int count)
    {
        var leafData = new List<byte[]>();
        for (int i = 0; i < count; i++)
        {
            leafData.Add(Encoding.UTF8.GetBytes($"data{i}"));
        }
        return leafData;
    }

    [Fact]
    public void Constructor_WithNoCacheConfig_BuildsTreeWithoutCache()
    {
        // Arrange
        var leafData = CreateLeafData(5);

        // Act
        var tree = new global::MerkleTree.Core.MerkleTree(leafData);

        // Assert
        Assert.False(tree.HasCache());
        Assert.Null(tree.GetCacheMetadata());
    }

    [Fact]
    public void Constructor_WithCacheConfig_BuildsTreeWithCache()
    {
        // Arrange
        var leafData = CreateLeafData(8);
        var cacheConfig = new CacheConfiguration(1, 2);

        // Act
        var tree = new global::MerkleTree.Core.MerkleTree(leafData, new Sha256HashFunction(), cacheConfig);

        // Assert
        Assert.True(tree.HasCache());
        var metadata = tree.GetCacheMetadata();
        Assert.NotNull(metadata);
        Assert.Equal(1, metadata.StartLevel);
        Assert.Equal(2, metadata.EndLevel);
        Assert.Equal("SHA-256", metadata.HashFunctionName);
    }

    [Fact]
    public void Constructor_WithTopLevelsConfig_BuildsCorrectCache()
    {
        // Arrange - Create tree with height 3 (8 leaves -> height 3)
        var leafData = CreateLeafData(8);
        var tree = new global::MerkleTree.Core.MerkleTree(leafData);
        var treeHeight = tree.GetMetadata().Height;
        
        // Cache top 2 levels
        var cacheConfig = CacheConfiguration.ForTopLevels(treeHeight, 2);

        // Act
        var cachedTree = new global::MerkleTree.Core.MerkleTree(leafData, new Sha256HashFunction(), cacheConfig);

        // Assert
        Assert.True(cachedTree.HasCache());
        var metadata = cachedTree.GetCacheMetadata();
        Assert.NotNull(metadata);
        Assert.Equal(treeHeight - 2, metadata.StartLevel);
        Assert.Equal(treeHeight - 1, metadata.EndLevel);
    }

    [Fact]
    public void SaveCache_WithValidCache_WritesFile()
    {
        // Arrange
        var leafData = CreateLeafData(8);
        var cacheConfig = new CacheConfiguration(1, 2);
        var tree = new global::MerkleTree.Core.MerkleTree(leafData, new Sha256HashFunction(), cacheConfig);
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            tree.SaveCache(tempFile);

            // Assert
            Assert.True(File.Exists(tempFile));
            var fileInfo = new FileInfo(tempFile);
            Assert.True(fileInfo.Length > 0);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveCache_WithoutCache_ThrowsInvalidOperationException()
    {
        // Arrange
        var leafData = CreateLeafData(5);
        var tree = new global::MerkleTree.Core.MerkleTree(leafData);
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => tree.SaveCache(tempFile));
            Assert.Contains("No cache is available", ex.Message);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void LoadWithCache_WithValidCacheFile_LoadsCacheCorrectly()
    {
        // Arrange
        var leafData = CreateLeafData(8);
        var cacheConfig = new CacheConfiguration(1, 2);
        var originalTree = new global::MerkleTree.Core.MerkleTree(leafData, new Sha256HashFunction(), cacheConfig);
        var tempFile = Path.GetTempFileName();

        try
        {
            // Save cache
            originalTree.SaveCache(tempFile);

            // Act - Load tree with cache
            var loadedTree = global::MerkleTree.Core.MerkleTree.LoadWithCache(leafData, new Sha256HashFunction(), tempFile);

            // Assert
            Assert.True(loadedTree.HasCache());
            Assert.Equal(originalTree.GetRootHash(), loadedTree.GetRootHash());
            
            var metadata = loadedTree.GetCacheMetadata();
            Assert.NotNull(metadata);
            Assert.Equal(1, metadata.StartLevel);
            Assert.Equal(2, metadata.EndLevel);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void LoadWithCache_WithIncompatibleHashFunction_ThrowsArgumentException()
    {
        // Arrange
        var leafData = CreateLeafData(8);
        var cacheConfig = new CacheConfiguration(1, 2);
        var originalTree = new global::MerkleTree.Core.MerkleTree(leafData, new Sha256HashFunction(), cacheConfig);
        var tempFile = Path.GetTempFileName();

        try
        {
            // Save cache with SHA256
            originalTree.SaveCache(tempFile);

            // Act & Assert - Try to load with SHA512
            var ex = Assert.Throws<ArgumentException>(() =>
                global::MerkleTree.Core.MerkleTree.LoadWithCache(leafData, new Sha512HashFunction(), tempFile));
            Assert.Contains("hash function", ex.Message.ToLower());
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void GenerateProof_WithCache_UsesCache()
    {
        // Arrange
        var leafData = CreateLeafData(8);
        var cacheConfig = new CacheConfiguration(1, 2);
        var tree = new global::MerkleTree.Core.MerkleTree(leafData, new Sha256HashFunction(), cacheConfig);

        // Act
        var proof = tree.GenerateProof(3);

        // Assert
        Assert.NotNull(proof);
        
        // Should have had cache hits (at least for levels 1 and 2)
        var stats = tree.CacheStatistics;
        Assert.True(stats.Hits > 0, "Expected cache hits but got none");
    }

    [Fact]
    public void GenerateProof_WithoutCache_NoStatistics()
    {
        // Arrange
        var leafData = CreateLeafData(8);
        var tree = new global::MerkleTree.Core.MerkleTree(leafData);

        // Act
        var proof = tree.GenerateProof(3);

        // Assert
        Assert.NotNull(proof);
        
        // Should have no cache hits or misses
        var stats = tree.CacheStatistics;
        Assert.Equal(0, stats.Hits);
        Assert.Equal(0, stats.Misses);
        Assert.Equal(0, stats.TotalLookups);
    }

    [Fact]
    public void GenerateProof_WithPartialCache_HasHitsAndMisses()
    {
        // Arrange - Cache only level 2, not level 0 or 1
        var leafData = CreateLeafData(8);
        var cacheConfig = new CacheConfiguration(2, 2);
        var tree = new global::MerkleTree.Core.MerkleTree(leafData, new Sha256HashFunction(), cacheConfig);

        // Act
        var proof = tree.GenerateProof(3);

        // Assert
        Assert.NotNull(proof);
        
        var stats = tree.CacheStatistics;
        // Should have both hits (level 2) and misses (levels 0, 1)
        Assert.True(stats.Hits > 0, "Expected cache hits");
        Assert.True(stats.Misses > 0, "Expected cache misses");
    }

    [Fact]
    public void GenerateProof_WithCacheAndWithout_ProduceSameProof()
    {
        // Arrange
        var leafData = CreateLeafData(8);
        var treeWithoutCache = new global::MerkleTree.Core.MerkleTree(leafData);
        var cacheConfig = new CacheConfiguration(1, 2);
        var treeWithCache = new global::MerkleTree.Core.MerkleTree(leafData, new Sha256HashFunction(), cacheConfig);

        // Act
        var proofWithoutCache = treeWithoutCache.GenerateProof(3);
        var proofWithCache = treeWithCache.GenerateProof(3);

        // Assert
        Assert.Equal(proofWithoutCache.LeafIndex, proofWithCache.LeafIndex);
        Assert.Equal(proofWithoutCache.TreeHeight, proofWithCache.TreeHeight);
        Assert.Equal(proofWithoutCache.LeafValue, proofWithCache.LeafValue);
        Assert.Equal(proofWithoutCache.SiblingHashes.Length, proofWithCache.SiblingHashes.Length);
        
        for (int i = 0; i < proofWithoutCache.SiblingHashes.Length; i++)
        {
            Assert.Equal(proofWithoutCache.SiblingHashes[i], proofWithCache.SiblingHashes[i]);
            Assert.Equal(proofWithoutCache.SiblingIsRight[i], proofWithCache.SiblingIsRight[i]);
        }

        // Both proofs should verify
        var rootHash = treeWithoutCache.GetRootHash();
        Assert.True(proofWithoutCache.Verify(rootHash, new Sha256HashFunction()));
        Assert.True(proofWithCache.Verify(rootHash, new Sha256HashFunction()));
    }

    [Fact]
    public void CacheStatistics_Reset_ClearsStatistics()
    {
        // Arrange
        var leafData = CreateLeafData(8);
        var cacheConfig = new CacheConfiguration(1, 2);
        var tree = new global::MerkleTree.Core.MerkleTree(leafData, new Sha256HashFunction(), cacheConfig);
        
        // Generate some proofs to accumulate statistics
        tree.GenerateProof(0);
        tree.GenerateProof(1);
        
        var statsBeforeReset = tree.CacheStatistics;
        Assert.True(statsBeforeReset.TotalLookups > 0);

        // Act
        tree.CacheStatistics.Reset();

        // Assert
        Assert.Equal(0, tree.CacheStatistics.Hits);
        Assert.Equal(0, tree.CacheStatistics.Misses);
        Assert.Equal(0, tree.CacheStatistics.TotalLookups);
    }

    [Fact]
    public void CacheStatistics_HitRate_CalculatesCorrectly()
    {
        // Arrange
        var leafData = CreateLeafData(16);
        var cacheConfig = new CacheConfiguration(2, 3);
        var tree = new global::MerkleTree.Core.MerkleTree(leafData, new Sha256HashFunction(), cacheConfig);

        // Act
        tree.GenerateProof(0);

        // Assert
        var stats = tree.CacheStatistics;
        if (stats.TotalLookups > 0)
        {
            double expectedHitRate = (double)stats.Hits / stats.TotalLookups * 100.0;
            Assert.Equal(expectedHitRate, stats.HitRate, 0.01);
        }
    }

    [Fact]
    public void CacheConfiguration_Disabled_CreatesDisabledConfig()
    {
        // Act
        var config = CacheConfiguration.Disabled();

        // Assert
        Assert.False(config.IsEnabled);
    }

    [Fact]
    public void CacheConfiguration_ForTopLevels_WithInvalidParams_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => CacheConfiguration.ForTopLevels(-1, 2));
        Assert.Throws<ArgumentException>(() => CacheConfiguration.ForTopLevels(5, 0));
        Assert.Throws<ArgumentException>(() => CacheConfiguration.ForTopLevels(5, 10));
    }

    [Fact]
    public void SaveAndLoadCache_RoundTrip_PreservesData()
    {
        // Arrange
        var leafData = CreateLeafData(16);
        var cacheConfig = new CacheConfiguration(1, 3);
        var originalTree = new global::MerkleTree.Core.MerkleTree(leafData, new Sha256HashFunction(), cacheConfig);
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act - Save and load
            originalTree.SaveCache(tempFile);
            var loadedTree = global::MerkleTree.Core.MerkleTree.LoadWithCache(leafData, new Sha256HashFunction(), tempFile);

            // Generate proofs from both trees
            var originalProof = originalTree.GenerateProof(5);
            var loadedProof = loadedTree.GenerateProof(5);

            // Assert - Proofs should be identical
            Assert.Equal(originalProof.LeafIndex, loadedProof.LeafIndex);
            Assert.Equal(originalProof.TreeHeight, loadedProof.TreeHeight);
            Assert.Equal(originalProof.SiblingHashes.Length, loadedProof.SiblingHashes.Length);

            for (int i = 0; i < originalProof.SiblingHashes.Length; i++)
            {
                Assert.Equal(originalProof.SiblingHashes[i], loadedProof.SiblingHashes[i]);
            }
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Constructor_WithLevelZeroCache_CachesLeaves()
    {
        // Arrange
        var leafData = CreateLeafData(8);
        var cacheConfig = new CacheConfiguration(0, 1);

        // Act
        var tree = new global::MerkleTree.Core.MerkleTree(leafData, new Sha256HashFunction(), cacheConfig);

        // Assert
        Assert.True(tree.HasCache());
        var metadata = tree.GetCacheMetadata();
        Assert.NotNull(metadata);
        Assert.Equal(0, metadata.StartLevel);
    }

    [Fact]
    public void GenerateProof_MultipleProofs_AccumulatesStatistics()
    {
        // Arrange
        var leafData = CreateLeafData(16);
        var cacheConfig = new CacheConfiguration(1, 2);
        var tree = new global::MerkleTree.Core.MerkleTree(leafData, new Sha256HashFunction(), cacheConfig);

        // Act - Generate multiple proofs
        tree.GenerateProof(0);
        tree.GenerateProof(5);
        tree.GenerateProof(10);

        // Assert
        var stats = tree.CacheStatistics;
        Assert.True(stats.TotalLookups > 0);
        // With cache enabled for levels 1-2, we should have hits
        Assert.True(stats.Hits > 0);
    }
}
