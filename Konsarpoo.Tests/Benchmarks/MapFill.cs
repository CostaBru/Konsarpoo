using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;

namespace Konsarpoo.Collections.Tests.Benchmarks
{
    [Config(typeof(Config))]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [MemoryDiagnoser]
    public class MapFill
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddJob(Job.MediumRun.WithGcServer(false).WithGcForce(true).WithId("Workstation").WithIterationCount(50));
            }
        }
        
        [Params(2, 1000, 1000_000)]
        public int N;

        [Benchmark]
        public int Map_Add()
        {
            var data = new Map<int, int>();

            for (int i = 0; i < N; i++)
            {
                data.Add(i, i);
            }
            
            data.Dispose();

            return data.Count;
        }

        [Benchmark(Baseline = true)]
        public int Dict_Add()
        {
            var data = new Dictionary<int, int>();

            for (int i = 0; i < N; i++)
            {
                data.Add(i, i);
            }

            return data.Count;
        }
    }
}