using BenchmarkDotNet.Running;
using Unio.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(ExtractionBenchmarks).Assembly).Run(args);
