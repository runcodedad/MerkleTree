# CI/CD Integration Guide for Performance Benchmarks

This guide explains how to integrate the MerkleTree performance benchmarks into your CI/CD pipeline for automated performance regression detection.

## GitHub Actions Integration

### Option 1: Benchmark on Every PR (Full Suite)

Create `.github/workflows/benchmarks.yml`:

```yaml
name: Performance Benchmarks

on:
  pull_request:
    branches: [main, develop]
  push:
    branches: [main, develop]

jobs:
  benchmark:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '10.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build -c Release --no-restore
    
    - name: Run Benchmarks
      run: |
        cd benchmarks/MerkleTree.Benchmarks
        dotnet run -c Release -- --exporters json markdown
    
    - name: Upload Benchmark Results
      uses: actions/upload-artifact@v4
      with:
        name: benchmark-results
        path: benchmarks/MerkleTree.Benchmarks/BenchmarkDotNet.Artifacts/results/
        retention-days: 30
```

### Option 2: Fast Benchmarks on PR, Full on Merge

Create `.github/workflows/benchmarks-pr.yml`:

```yaml
name: Performance Benchmarks (PR)

on:
  pull_request:
    branches: [main]

jobs:
  fast-benchmarks:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '10.0.x'
    
    - name: Run Fast Benchmarks
      run: |
        cd benchmarks/MerkleTree.Benchmarks
        dotnet run -c Release -- --filter *Small* --exporters json markdown
    
    - name: Upload Results
      uses: actions/upload-artifact@v4
      with:
        name: pr-benchmark-results
        path: benchmarks/MerkleTree.Benchmarks/BenchmarkDotNet.Artifacts/results/
```

And `.github/workflows/benchmarks-main.yml`:

```yaml
name: Performance Benchmarks (Full)

on:
  push:
    branches: [main]
  schedule:
    - cron: '0 0 * * 0'  # Weekly on Sunday

jobs:
  full-benchmarks:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '10.0.x'
    
    - name: Run All Benchmarks
      run: |
        cd benchmarks/MerkleTree.Benchmarks
        dotnet run -c Release -- --exporters json markdown html
    
    - name: Upload Results
      uses: actions/upload-artifact@v4
      with:
        name: full-benchmark-results
        path: benchmarks/MerkleTree.Benchmarks/BenchmarkDotNet.Artifacts/
        retention-days: 90
    
    - name: Comment PR with Results
      if: github.event_name == 'pull_request'
      uses: actions/github-script@v7
      with:
        script: |
          const fs = require('fs');
          const results = fs.readFileSync('benchmarks/MerkleTree.Benchmarks/BenchmarkDotNet.Artifacts/results/*-report.md', 'utf8');
          github.rest.issues.createComment({
            issue_number: context.issue.number,
            owner: context.repo.owner,
            repo: context.repo.repo,
            body: '## Benchmark Results\n\n' + results
          });
```

### Option 3: Compare with Baseline

Use [benchmark-action](https://github.com/benchmark-action/github-action-benchmark):

```yaml
name: Continuous Benchmarking

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  benchmark:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '10.0.x'
    
    - name: Run Benchmarks
      run: |
        cd benchmarks/MerkleTree.Benchmarks
        dotnet run -c Release -- --exporters json
    
    - name: Store Benchmark Results
      uses: benchmark-action/github-action-benchmark@v1
      with:
        tool: 'benchmarkdotnet'
        output-file-path: benchmarks/MerkleTree.Benchmarks/BenchmarkDotNet.Artifacts/results/*-report.json
        github-token: ${{ secrets.GITHUB_TOKEN }}
        auto-push: true
        # Show alert with commit comment on detecting possible performance regression
        alert-threshold: '120%'
        comment-on-alert: true
        fail-on-alert: false
```

## Azure DevOps Integration

Create `azure-pipelines-benchmarks.yml`:

```yaml
trigger:
  branches:
    include:
    - main
    - develop

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: UseDotNet@2
  inputs:
    packageType: 'sdk'
    version: '10.0.x'

- task: DotNetCoreCLI@2
  displayName: 'Restore packages'
  inputs:
    command: 'restore'

- task: DotNetCoreCLI@2
  displayName: 'Build'
  inputs:
    command: 'build'
    arguments: '-c Release --no-restore'

- task: PowerShell@2
  displayName: 'Run Benchmarks'
  inputs:
    targetType: 'inline'
    script: |
      cd benchmarks/MerkleTree.Benchmarks
      dotnet run -c Release -- --exporters json markdown

- task: PublishBuildArtifacts@1
  displayName: 'Publish Benchmark Results'
  inputs:
    PathtoPublish: 'benchmarks/MerkleTree.Benchmarks/BenchmarkDotNet.Artifacts/results'
    ArtifactName: 'benchmark-results'
```

## Performance Regression Detection

### Method 1: Threshold-Based Detection

Store baseline results and compare:

```bash
#!/bin/bash
# compare-benchmarks.sh

# Run current benchmarks
cd benchmarks/MerkleTree.Benchmarks
dotnet run -c Release -- --exporters json

# Parse and compare results
CURRENT_RESULTS="BenchmarkDotNet.Artifacts/results/*-report.json"
BASELINE_RESULTS="baseline/benchmark-results.json"

# Use jq to compare mean times
# Example: Check if any benchmark is 20% slower
jq -s '
  .[0] as $baseline | 
  .[1].Benchmarks[] | 
  select(.Statistics.Mean > ($baseline.Benchmarks[] | select(.FullName == .FullName) | .Statistics.Mean) * 1.2)
' "$BASELINE_RESULTS" "$CURRENT_RESULTS"
```

### Method 2: Statistical Comparison

Use BenchmarkDotNet's built-in comparison:

```bash
# Run benchmarks and save results
dotnet run -c Release -- --exporters json

# On next run, compare with baseline
dotnet run -c Release -- --baseline baseline-results.json
```

### Method 3: Automated Analysis Script

Create `analyze-regression.py`:

```python
import json
import sys

def check_regression(baseline_file, current_file, threshold=1.2):
    """
    Check if any benchmark has regressed by more than threshold (default 20%).
    Returns True if regression detected, False otherwise.
    """
    with open(baseline_file) as f:
        baseline = json.load(f)
    
    with open(current_file) as f:
        current = json.load(f)
    
    regressions = []
    
    for curr_bench in current['Benchmarks']:
        name = curr_bench['FullName']
        curr_mean = curr_bench['Statistics']['Mean']
        
        # Find matching baseline
        baseline_bench = next((b for b in baseline['Benchmarks'] if b['FullName'] == name), None)
        
        if baseline_bench:
            baseline_mean = baseline_bench['Statistics']['Mean']
            ratio = curr_mean / baseline_mean
            
            if ratio > threshold:
                regressions.append({
                    'name': name,
                    'baseline': baseline_mean,
                    'current': curr_mean,
                    'ratio': ratio,
                    'increase_pct': (ratio - 1) * 100
                })
    
    if regressions:
        print("⚠️ Performance Regressions Detected:")
        for reg in regressions:
            print(f"  {reg['name']}")
            print(f"    Baseline: {reg['baseline']:.2f} ns")
            print(f"    Current:  {reg['current']:.2f} ns")
            print(f"    Increase: {reg['increase_pct']:.1f}%")
        return True
    else:
        print("✓ No performance regressions detected")
        return False

if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("Usage: python analyze-regression.py <baseline.json> <current.json>")
        sys.exit(1)
    
    has_regression = check_regression(sys.argv[1], sys.argv[2])
    sys.exit(1 if has_regression else 0)
```

## Tracking Results Over Time

### Option 1: GitHub Pages Dashboard

Use [benchmark-action](https://github.com/benchmark-action/github-action-benchmark) to automatically generate a performance tracking dashboard hosted on GitHub Pages.

### Option 2: Custom Database

Store results in a database for historical tracking:

```bash
#!/bin/bash
# store-results.sh

COMMIT_SHA=$(git rev-parse HEAD)
TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

cd benchmarks/MerkleTree.Benchmarks
dotnet run -c Release -- --exporters json

# Upload to database or cloud storage
# Example with PostgreSQL:
psql -h $DB_HOST -U $DB_USER -d benchmarks -c "
INSERT INTO benchmark_results (commit_sha, timestamp, results_json)
VALUES ('$COMMIT_SHA', '$TIMESTAMP', '$(cat BenchmarkDotNet.Artifacts/results/*-report.json)')
"
```

### Option 3: Artifact Storage

Simply store artifacts in CI/CD system:

- GitHub Actions: Use `actions/upload-artifact`
- Azure DevOps: Use `PublishBuildArtifacts`
- Jenkins: Use `archiveArtifacts`

## Best Practices

1. **Run on Consistent Hardware**: Use the same CI/CD runner type for comparisons
2. **Minimize System Noise**: Run benchmarks on dedicated agents when possible
3. **Warm-up Iterations**: Ensure adequate warm-up for stable results
4. **Statistical Significance**: Use multiple iterations for reliable comparisons
5. **Selective Benchmarking**: Run fast benchmarks on PR, full suite on merge
6. **Alert Thresholds**: Set reasonable thresholds (10-20%) to avoid false positives
7. **Document Changes**: Include performance impact in PR descriptions
8. **Historical Tracking**: Keep results for trend analysis
9. **Investigate Regressions**: Don't just detect - understand why performance changed
10. **Celebrate Improvements**: Track performance wins as well as regressions

## Troubleshooting

### CI/CD Timeouts

If benchmarks take too long:

```bash
# Reduce iterations
dotnet run -c Release -- --job short --filter *Small*

# Or run in parallel jobs
dotnet run -c Release -- --filter *TreeBuilding* &
dotnet run -c Release -- --filter *Proof* &
wait
```

### Inconsistent Results

If results vary significantly:

1. Check runner CPU/memory availability
2. Disable CPU frequency scaling if possible
3. Increase iteration count
4. Use `--outliers` flag to remove statistical outliers

### Memory Issues

For memory-intensive benchmarks:

```bash
# Run with reduced datasets
dotnet run -c Release -- --filter "*Small* *Medium*"
```

## Example PR Comment Template

```markdown
## Performance Benchmark Results

| Benchmark | Baseline | Current | Change |
|-----------|----------|---------|--------|
| BuildTree_1000Leaves | 8.23 ms | 8.15 ms | ✅ -0.97% |
| GenerateProof_Tree1000 | 125.3 μs | 128.7 μs | ⚠️ +2.71% |
| VerifyProof_Tree1000 | 98.2 μs | 97.8 μs | ✅ -0.41% |

### Summary
- No significant performance regressions detected
- All changes within acceptable variance (±5%)

[Full Results](link-to-artifacts)
```

## Resources

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [GitHub Actions Benchmark Action](https://github.com/benchmark-action/github-action-benchmark)
- [Performance Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/performance-testing)
