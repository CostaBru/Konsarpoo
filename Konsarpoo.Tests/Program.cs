using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Konsarpoo.Collections.Tests.Benchmarks;

namespace Brudixy.Tests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner
                .Run<MapLookup>(
                    ManualConfig
                        .Create(DefaultConfig.Instance)
                        .WithOptions(ConfigOptions.DisableOptimizationsValidator));

           /*BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
                .Run(args, ManualConfig
                    .Create(DefaultConfig.Instance)
                    .WithOptions(ConfigOptions.DisableOptimizationsValidator)
                    .WithArtifactsPath("/Benchmarks_123")
                    .AddExporter(MarkdownExporter.GitHub));*/
        }
    }
}