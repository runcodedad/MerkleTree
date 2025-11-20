# Quick Start Guide - MerkleTree Performance Benchmarks

This guide will help you run your first benchmarks and understand the results.

## Prerequisites

- .NET 10.0 SDK installed
- MerkleTree solution built in Release mode

## Running Your First Benchmark

### Step 1: Navigate to the Benchmark Project

```bash
cd benchmarks/MerkleTree.Benchmarks
```

### Step 2: Run a Simple Benchmark

Let's start with a fast, simple benchmark:

```bash
dotnet run -c Release -- --filter *BuildTree_10Leaves* --job short
```

This will:
- Build the benchmark project in Release mode
- Run only the `BuildTree_10Leaves` benchmark
- Use a short job configuration (fewer iterations for faster results)

### Step 3: Understand the Output

You'll see output like this:

```
BenchmarkDotNet v0.14.0, Ubuntu 24.04.3 LTS
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.100

| Method             | Mean     | Error    | StdDev   | Gen0   | Allocated |
|------------------- |---------:|---------:|---------:|-------:|----------:|
| BuildTree_10Leaves | 22.77 us | 1.921 us | 0.297 us | 0.5493 |   9.39 KB |
```

**What does this mean?**

- **Mean**: Average time to build a tree with 10 leaves is 22.77 microseconds
- **Error**: Statistical error margin (±1.921 μs)
- **StdDev**: Standard deviation across runs (0.297 μs)
- **Gen0**: Garbage collection frequency (0.5493 per 1000 ops)
- **Allocated**: Memory allocated per operation (9.39 KB)

### Step 4: Run Category-Based Benchmarks

Try running all small benchmarks:

```bash
dotnet run -c Release -- --filter *Small* --job short
```

This runs all benchmarks tagged with the "Small" category, which are fast and great for quick checks.

## Common Benchmark Scenarios

### Scenario 1: Measure Tree Building Performance

```bash
# Run all tree building benchmarks
dotnet run -c Release -- --filter *TreeBuilding*
```

**What to look for:**
- How does time scale from 10 to 10,000 leaves?
- Is memory usage proportional to leaf count?
- How do different hash functions compare?

### Scenario 2: Compare Hash Functions

```bash
# Run benchmarks for different hash algorithms
dotnet run -c Release -- --filter *SHA256* *SHA512* *BLAKE3*
```

**What to look for:**
- Which hash function is fastest?
- What's the memory overhead of each?
- Is BLAKE3 faster than SHA-256 on your hardware?

### Scenario 3: Evaluate Cache Performance

```bash
# Run cache-related benchmarks
dotnet run -c Release -- --filter *Cache*
```

**What to look for:**
- Speed improvement with cache enabled
- Cache build overhead
- Memory used by cache

### Scenario 4: Test Proof Operations

```bash
# Run proof generation and verification
dotnet run -c Release -- --filter *Proof*
```

**What to look for:**
- Proof generation time vs tree size
- Verification speed
- Memory efficiency

## Understanding Results

### Good Performance Indicators

✅ **Tree Building (1,000 leaves)**: < 10 ms  
✅ **Proof Generation**: < 1 ms  
✅ **Proof Verification**: < 1 ms  
✅ **Memory**: Proportional to data size (O(n) for in-memory, O(log n) for streaming)

### Performance Patterns

1. **Logarithmic Scaling**: Proof operations should scale logarithmically with tree size
2. **Linear Tree Building**: Building time should scale roughly linearly with leaf count
3. **Consistent Verification**: Verification time should be consistent regardless of tree size
4. **Low GC Pressure**: Few Gen0 collections indicate efficient memory usage

## Using Convenience Scripts

### Unix/Linux/macOS

```bash
# Run all benchmarks
./run-benchmarks.sh all

# Run specific categories
./run-benchmarks.sh tree      # Tree building only
./run-benchmarks.sh proof     # Proof operations
./run-benchmarks.sh cache     # Cache performance
./run-benchmarks.sh fast      # Quick tests only
```

### Windows

```cmd
REM Run all benchmarks
run-benchmarks.cmd all

REM Run specific categories
run-benchmarks.cmd tree
run-benchmarks.cmd proof
run-benchmarks.cmd cache
run-benchmarks.cmd fast
```

## Viewing Results

Results are saved in multiple formats:

### Markdown Reports
```bash
cat BenchmarkDotNet.Artifacts/results/*-report.md
```
Human-readable tables, great for documentation.

### HTML Reports
```bash
# Open in browser
open BenchmarkDotNet.Artifacts/results/*-report.html
```
Interactive tables with sorting and filtering.

### CSV Data
```bash
cat BenchmarkDotNet.Artifacts/results/*-report.csv
```
Perfect for importing into Excel or analysis tools.

### JSON Data
```bash
cat BenchmarkDotNet.Artifacts/results/*-report.json
```
Structured data for automated analysis.

## Tips for Accurate Results

1. **Always use Release mode**: Debug builds are 10-100x slower
2. **Close other applications**: Reduce system noise
3. **Run multiple times**: First run may include warmup overhead
4. **Use consistent hardware**: Compare results on the same machine
5. **Check system load**: Low CPU/memory usage gives better results

## Common Questions

### Q: Why do results vary between runs?

**A**: System load, CPU throttling, and random memory layouts can cause variation. This is normal. Look at the Error and StdDev columns to understand result stability.

### Q: How long do benchmarks take?

**A**: 
- Single benchmark with `--job short`: ~30 seconds
- Small category benchmarks: 2-5 minutes
- Full benchmark suite: 10-30 minutes

### Q: Can I run faster benchmarks?

**A**: Yes! Use:
```bash
dotnet run -c Release -- --filter *Small* --job short
```

### Q: Results show "outliers removed" - is this OK?

**A**: Yes! BenchmarkDotNet automatically removes statistical outliers for more accurate results.

### Q: What if I see high memory allocations?

**A**: Check if you're running streaming benchmarks (which should use less memory) vs in-memory benchmarks. High allocations aren't always bad if they're expected.

## Next Steps

1. **Establish Baseline**: Run full benchmarks and save results
2. **Track Over Time**: Run regularly to detect performance changes
3. **Compare Configurations**: Try different hash functions or options
4. **CI Integration**: Set up automated benchmarking (see CI-INTEGRATION.md)
5. **Contribute**: Help improve benchmark coverage

## Getting Help

- **Documentation**: See [README.md](README.md) for detailed information
- **CI Integration**: See [CI-INTEGRATION.md](CI-INTEGRATION.md) for automation
- **Issues**: Report benchmark issues on GitHub
- **Questions**: Ask in GitHub discussions

## Example: First 5-Minute Benchmark Run

Here's a complete example session:

```bash
# 1. Navigate to benchmarks
cd benchmarks/MerkleTree.Benchmarks

# 2. Run fast benchmarks to get a feel for performance
./run-benchmarks.sh fast

# 3. Look at the summary
cat BenchmarkDotNet.Artifacts/results/*-report.md | head -50

# 4. Run specific benchmark of interest
dotnet run -c Release -- --filter *BuildTree_1000Leaves* --job short

# 5. Save baseline for future comparison
mkdir -p baseline
cp BenchmarkDotNet.Artifacts/results/*-report.json baseline/baseline-$(date +%Y%m%d).json
```

Congratulations! You've run your first MerkleTree benchmarks! 🎉
