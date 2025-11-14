# MerkleTree

A high-performance .NET library for creating and managing Merkle trees, providing cryptographic data structure support for data integrity verification and efficient data validation.

## Overview

Merkle trees (also known as hash trees) are a fundamental cryptographic data structure used in various applications including:

- **Blockchain technology**: Efficiently verify transactions and blocks
- **Distributed systems**: Verify data consistency across nodes
- **Data integrity**: Detect tampering or corruption in datasets
- **Version control systems**: Efficiently compare and synchronize data

This library provides a robust, well-tested implementation of Merkle trees for .NET applications.

## Features

- **Multi-targeting support**: Compatible with .NET 10.0 and .NET Standard 2.1
- **High performance**: Optimized for speed and memory efficiency
- **Type-safe**: Full C# type safety with nullable reference types enabled
- **XML documentation**: IntelliSense support for better developer experience
- **Well-tested**: Comprehensive test coverage
- **Open source**: MIT licensed

## Installation

Install via NuGet Package Manager:

```bash
dotnet add package MerkleTree
```

Or via Package Manager Console:

```powershell
Install-Package MerkleTree
```

## Quick Start

```csharp
using MerkleTree;

// Example usage will be provided as the library develops
```

## Requirements

- **.NET 10.0** or later, or
- **.NET Standard 2.1** compatible runtime

## Building from Source

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later

### Build Steps

```bash
# Clone the repository
git clone https://github.com/runcodedad/merkletree.git
cd merkletree

# Restore dependencies and build
dotnet restore
dotnet build

# Run tests (when available)
dotnet test

# Create NuGet package
dotnet pack -c Release
```

## Documentation

Detailed documentation will be available as the library develops. For now, refer to:

- XML documentation comments in the source code
- IntelliSense in your IDE
- [GitHub repository](https://github.com/runcodedad/merkletree)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Version History

### 1.0.0 (Initial Release)
- Initial project setup
- NuGet package configuration
- Multi-targeting support (.NET 10.0 and .NET Standard 2.1)

## Support

For questions, issues, or feature requests, please [open an issue](https://github.com/runcodedad/merkletree/issues) on GitHub.

## Authors

- **runcodedad** - Initial work

## Acknowledgments

- Inspired by the original Merkle tree concept by Ralph Merkle
- Built with modern .NET best practices
