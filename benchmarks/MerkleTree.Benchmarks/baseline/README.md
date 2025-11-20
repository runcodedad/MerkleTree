# Baseline Performance Results

This directory is for storing baseline benchmark results that can be used for performance regression detection.

## Creating a Baseline

To establish a baseline for your environment:

```bash
cd benchmarks/MerkleTree.Benchmarks
dotnet run -c Release -- --exporters json markdown

# Copy the results to baseline directory
cp BenchmarkDotNet.Artifacts/results/*-report.json baseline/baseline-$(date +%Y%m%d).json
cp BenchmarkDotNet.Artifacts/results/*-report.md baseline/baseline-$(date +%Y%m%d).md
```

## Using Baselines for Comparison

### Manual Comparison

1. Run new benchmarks with the same configuration
2. Compare JSON results programmatically or visually review markdown reports
3. Look for significant changes (typically >10-20% regression is concerning)

### Automated Comparison

Use the provided analysis scripts in `CI-INTEGRATION.md` to automatically detect regressions.

## Baseline Best Practices

1. **Document Environment**: Record hardware specs, OS version, .NET version
2. **Clean State**: Run on a clean system with minimal background processes
3. **Multiple Runs**: Consider averaging multiple baseline runs for stability
4. **Update Regularly**: Refresh baselines when infrastructure or library changes
5. **Version Control**: Consider storing baselines in git or external storage

## Sample Baseline Results

Below is an example of what baseline results might look like for reference.

### Tree Building Performance (Sample)

| Benchmark | Mean | Error | StdDev | Allocated |
|-----------|------|-------|--------|-----------|
| BuildTree_10Leaves | 22.77 μs | 1.92 μs | 0.30 μs | 9.39 KB |
| BuildTree_100Leaves | 197.5 μs | 8.3 μs | 1.3 μs | 85.2 KB |
| BuildTree_1000Leaves | 1.95 ms | 0.08 ms | 0.01 ms | 826 KB |
| BuildTree_10000Leaves | 21.3 ms | 1.1 ms | 0.17 ms | 8.12 MB |

### Proof Generation Performance (Sample)

| Benchmark | Mean | Error | StdDev | Allocated |
|-----------|------|-------|--------|-----------|
| GenerateProof_Tree100 | 8.2 μs | 0.3 μs | 0.05 μs | 1.2 KB |
| GenerateProof_Tree1000 | 12.5 μs | 0.5 μs | 0.08 μs | 2.1 KB |
| GenerateProof_Tree10000 | 18.7 μs | 0.7 μs | 0.11 μs | 3.4 KB |

### Proof Verification Performance (Sample)

| Benchmark | Mean | Error | StdDev |
|-----------|------|-------|--------|
| VerifyProof_Tree100 | 6.8 μs | 0.2 μs | 0.03 μs |
| VerifyProof_Tree1000 | 10.2 μs | 0.4 μs | 0.06 μs |
| VerifyProof_Tree10000 | 14.5 μs | 0.5 μs | 0.08 μs |

**Note**: These are example values for reference only. Actual results will vary based on your hardware and system configuration.

## Interpreting Changes

### Acceptable Variance

- **±5%**: Normal system variance, not concerning
- **±10%**: Worth investigating but may be acceptable
- **±20%**: Significant change, should be understood
- **>30%**: Major change, likely indicates real performance impact

### Common Causes of Changes

1. **Code Changes**: New features, optimizations, or regressions
2. **Dependency Updates**: New versions of libraries or .NET runtime
3. **Hardware Changes**: Different CI runners or upgraded systems
4. **System Load**: Background processes or resource contention
5. **Configuration Changes**: Different benchmark settings or parameters

### When to Update Baseline

- After confirmed performance improvements are merged
- When infrastructure is upgraded (new .NET version, hardware)
- After significant refactoring that intentionally changes performance characteristics
- On a regular schedule (e.g., quarterly) to reflect current state

## Environment Documentation Template

When creating a baseline, document:

```markdown
## Baseline Environment

**Date**: YYYY-MM-DD
**Commit**: <git commit sha>
**Hardware**: <CPU model, cores, RAM>
**OS**: <OS name and version>
**.NET Version**: <dotnet --version output>
**Notes**: <any special considerations>
```

Example:

```markdown
## Baseline Environment

**Date**: 2025-01-15
**Commit**: abc123def456
**Hardware**: AMD EPYC 7763, 4 cores, 16GB RAM
**OS**: Ubuntu 24.04 LTS
**.NET Version**: 10.0.100
**Notes**: Clean VM, no background processes, CPU governor set to performance
```
