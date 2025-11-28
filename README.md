F-Profiler
============

[![NuGet](https://img.shields.io/nuget/v/Alma.Profiler.svg)](https://www.nuget.org/packages/Alma.Profiler)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Alma.Profiler.svg)](https://www.nuget.org/packages/Alma.Profiler)
[![Tests](https://github.com/alma-oss/fprofiler/actions/workflows/tests.yaml/badge.svg)](https://github.com/alma-oss/fprofiler/actions/workflows/tests.yaml)

> Library for a Web App Profiler.

---

## Install

Add following into `paket.references`
```
Alma.Profiler
```

## Release
1. Increment version in `Profiler.fsproj`
2. Update `CHANGELOG.md`
3. Commit new version and tag it

## Development
### Requirements
- [dotnet core](https://dotnet.microsoft.com/learn/dotnet/hello-world-tutorial)

### Build
```bash
./build.sh build
```

### Tests
```bash
./build.sh -t tests
```
