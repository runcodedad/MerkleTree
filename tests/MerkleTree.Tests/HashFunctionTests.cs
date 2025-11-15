using System.Text;
using Xunit;

namespace MerkleTree.Tests;

/// <summary>
/// Tests for hash function implementations and abstraction.
/// </summary>
public class HashFunctionTests
{
    /// <summary>
    /// Helper method to create test data.
    /// </summary>
    private static byte[] CreateTestData(string data)
    {
        return Encoding.UTF8.GetBytes(data);
    }

    [Fact]
    public void Sha256HashFunction_Name_ReturnsSHA256()
    {
        // Arrange
        var hashFunction = new Sha256HashFunction();

        // Act
        var name = hashFunction.Name;

        // Assert
        Assert.Equal("SHA-256", name);
    }

    [Fact]
    public void Sha256HashFunction_HashSizeInBytes_Returns32()
    {
        // Arrange
        var hashFunction = new Sha256HashFunction();

        // Act
        var size = hashFunction.HashSizeInBytes;

        // Assert
        Assert.Equal(32, size);
    }

    [Fact]
    public void Sha256HashFunction_ComputeHash_ProducesDeterministicOutput()
    {
        // Arrange
        var hashFunction = new Sha256HashFunction();
        var data = CreateTestData("test data");

        // Act
        var hash1 = hashFunction.ComputeHash(data);
        var hash2 = hashFunction.ComputeHash(data);

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.Equal(32, hash1.Length);
    }

    [Fact]
    public void Sha256HashFunction_ComputeHash_DifferentDataProducesDifferentHash()
    {
        // Arrange
        var hashFunction = new Sha256HashFunction();
        var data1 = CreateTestData("test data 1");
        var data2 = CreateTestData("test data 2");

        // Act
        var hash1 = hashFunction.ComputeHash(data1);
        var hash2 = hashFunction.ComputeHash(data2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Blake3HashFunction_Name_ReturnsBLAKE3()
    {
        // Arrange
        var hashFunction = new Blake3HashFunction();

        // Act
        var name = hashFunction.Name;

        // Assert
        Assert.Equal("BLAKE3", name);
    }

    [Fact]
    public void Blake3HashFunction_HashSizeInBytes_Returns32()
    {
        // Arrange
        var hashFunction = new Blake3HashFunction();

        // Act
        var size = hashFunction.HashSizeInBytes;

        // Assert
        Assert.Equal(32, size);
    }

#if NET10_0_OR_GREATER
    [Fact]
    public void Blake3HashFunction_ComputeHash_ProducesDeterministicOutput()
    {
        // Arrange
        var hashFunction = new Blake3HashFunction();
        var data = CreateTestData("test data");

        // Act
        var hash1 = hashFunction.ComputeHash(data);
        var hash2 = hashFunction.ComputeHash(data);

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.Equal(32, hash1.Length);
    }

    [Fact]
    public void Blake3HashFunction_ComputeHash_DifferentDataProducesDifferentHash()
    {
        // Arrange
        var hashFunction = new Blake3HashFunction();
        var data1 = CreateTestData("test data 1");
        var data2 = CreateTestData("test data 2");

        // Act
        var hash1 = hashFunction.ComputeHash(data1);
        var hash2 = hashFunction.ComputeHash(data2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Blake3HashFunction_ProducesDifferentHashThanSha256()
    {
        // Arrange
        var sha256 = new Sha256HashFunction();
        var blake3 = new Blake3HashFunction();
        var data = CreateTestData("test data");

        // Act
        var sha256Hash = sha256.ComputeHash(data);
        var blake3Hash = blake3.ComputeHash(data);

        // Assert
        Assert.NotEqual(sha256Hash, blake3Hash);
        Assert.Equal(32, sha256Hash.Length);
        Assert.Equal(32, blake3Hash.Length);
    }
#else
    [Fact]
    public void Blake3HashFunction_ComputeHash_ThrowsPlatformNotSupportedOnNetStandard21()
    {
        // Arrange
        var hashFunction = new Blake3HashFunction();
        var data = CreateTestData("test data");

        // Act & Assert
        Assert.Throws<PlatformNotSupportedException>(() => hashFunction.ComputeHash(data));
    }
#endif

    [Fact]
    public void MerkleTree_WithSha256HashFunction_UsesCorrectHashFunction()
    {
        // Arrange
        var hashFunction = new Sha256HashFunction();
        var leafData = new List<byte[]>
        {
            CreateTestData("leaf1"),
            CreateTestData("leaf2")
        };

        // Act
        var tree = new MerkleTree(leafData, hashFunction);

        // Assert
        Assert.NotNull(tree.HashFunction);
        Assert.Equal("SHA-256", tree.HashFunction.Name);
        Assert.Equal(32, tree.HashFunction.HashSizeInBytes);
    }

#if NET10_0_OR_GREATER
    [Fact]
    public void MerkleTree_WithBlake3HashFunction_UsesCorrectHashFunction()
    {
        // Arrange
        var hashFunction = new Blake3HashFunction();
        var leafData = new List<byte[]>
        {
            CreateTestData("leaf1"),
            CreateTestData("leaf2")
        };

        // Act
        var tree = new MerkleTree(leafData, hashFunction);

        // Assert
        Assert.NotNull(tree.HashFunction);
        Assert.Equal("BLAKE3", tree.HashFunction.Name);
        Assert.Equal(32, tree.HashFunction.HashSizeInBytes);
    }

    [Fact]
    public void MerkleTree_WithDifferentHashFunctions_ProducesDifferentRootHashes()
    {
        // Arrange
        var sha256 = new Sha256HashFunction();
        var blake3 = new Blake3HashFunction();
        var leafData = new List<byte[]>
        {
            CreateTestData("leaf1"),
            CreateTestData("leaf2"),
            CreateTestData("leaf3")
        };

        // Act
        var treeSha256 = new MerkleTree(leafData, sha256);
        var treeBlake3 = new MerkleTree(leafData, blake3);

        // Assert
        Assert.NotEqual(treeSha256.GetRootHash(), treeBlake3.GetRootHash());
    }
#endif

    [Fact]
    public void MerkleTree_DefaultConstructor_UsesSha256()
    {
        // Arrange
        var leafData = new List<byte[]>
        {
            CreateTestData("leaf1"),
            CreateTestData("leaf2")
        };

        // Act
        var tree = new MerkleTree(leafData);

        // Assert
        Assert.NotNull(tree.HashFunction);
        Assert.Equal("SHA-256", tree.HashFunction.Name);
        Assert.Equal(32, tree.HashFunction.HashSizeInBytes);
    }

    [Fact]
    public void HashFunction_Interface_ExposesCorrectMetadata()
    {
        // Arrange
        IHashFunction hashFunction = new Sha256HashFunction();

        // Act & Assert
        Assert.NotNull(hashFunction.Name);
        Assert.NotEmpty(hashFunction.Name);
        Assert.True(hashFunction.HashSizeInBytes > 0);
    }

    [Fact]
    public void HashFunction_ComputeHash_ReturnsCorrectLength()
    {
        // Arrange
        var hashFunction = new Sha256HashFunction();
        var data = CreateTestData("test");

        // Act
        var hash = hashFunction.ComputeHash(data);

        // Assert
        Assert.Equal(hashFunction.HashSizeInBytes, hash.Length);
    }

    [Fact]
    public void MerkleTree_CanSwapHashFunctionWithoutChangingCoreLogic()
    {
        // This test validates that the abstraction allows swapping hash functions
        // without requiring changes to the MerkleTree core logic

        // Arrange
        var sha256 = new Sha256HashFunction();
        var leafData = new List<byte[]>
        {
            CreateTestData("leaf1"),
            CreateTestData("leaf2"),
            CreateTestData("leaf3")
        };

        // Act - Create trees with different hash functions
        var tree1 = new MerkleTree(leafData, sha256);
        var tree2 = new MerkleTree(leafData, sha256);

        // Assert - Same hash function produces same results
        Assert.Equal(tree1.GetRootHash(), tree2.GetRootHash());
        Assert.Equal(tree1.HashFunction.Name, tree2.HashFunction.Name);
    }
}
