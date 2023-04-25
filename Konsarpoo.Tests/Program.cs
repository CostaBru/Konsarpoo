using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Konsarpoo.Collections.Tests.Benchmarks;

namespace Brudixy.Tests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner
                .Run<DataFill>(
                    ManualConfig
                        .Create(DefaultConfig.Instance)
                        .WithOptions(ConfigOptions.DisableOptimizationsValidator));

            /*BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
                .Run(args, ManualConfig
                    .Create(DefaultConfig.Instance)
                    .WithOptions(ConfigOptions.DisableOptimizationsValidator));*/
        }
    }
}