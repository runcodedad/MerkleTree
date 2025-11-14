namespace MerkleTree;

/// <summary>
/// Represents a node in a Merkle tree structure.
/// </summary>
/// <remarks>
/// This is a placeholder class for the initial library setup.
/// Full implementation will be provided in future releases.
/// </remarks>
public class MerkleTreeNode
{
    /// <summary>
    /// Gets or sets the hash value of this node.
    /// </summary>
    public byte[]? Hash { get; set; }

    /// <summary>
    /// Gets or sets the left child node.
    /// </summary>
    public MerkleTreeNode? Left { get; set; }

    /// <summary>
    /// Gets or sets the right child node.
    /// </summary>
    public MerkleTreeNode? Right { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MerkleTreeNode"/> class.
    /// </summary>
    public MerkleTreeNode()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MerkleTreeNode"/> class with the specified hash.
    /// </summary>
    /// <param name="hash">The hash value for this node.</param>
    public MerkleTreeNode(byte[] hash)
    {
        Hash = hash;
    }
}
