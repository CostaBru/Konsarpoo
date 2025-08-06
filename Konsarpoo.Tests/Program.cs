using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Konsarpoo.Collections.Tests.Benchmarks;

namespace Brudixy.Tests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            /*var mMapVsFile = new MMapVsFile();

            try
            {
                mMapVsFile.Setup();
                
                mMapVsFile.MMap();

                mMapVsFile.File();
            }
            finally
            {
                mMapVsFile.Cleanup();
            }*/


            BenchmarkRunner
                .Run<DataSerializationReport>(
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