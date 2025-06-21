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
    public class TupleTreePerf
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddJob(Job.MediumRun.WithGcServer(false).WithGcForce(true).WithId("Workstation").WithIterationCount(10));
            }
        }
        
        [Params(100, 1000, 1_000_000)]
        public int N;

        [Benchmark]
        public int TupleTrieMap_Add()
        {
            var testData = new TupleTrieMap<int, int, int, int, int>();
            
            for (int i = 0; i < N; i++)
            {
                testData.Add((i, i, i, i), i);
            }
            
            testData.Dispose();
            
            return testData.Count;
        }
        
        [Benchmark]
        public int Map_Add()
        {
            var data = new Map<(int, int, int, int), int>();

            for (int i = 0; i < N; i++)
            {
                data.Add((i, i, i, i), i);
            }

            return data.Count;
        }

        [Benchmark(Baseline = true)]
        public int Dict_Add()
        {
            var data = new Dictionary<(int, int, int, int), int>();

            for (int i = 0; i < N; i++)
            {
                data.Add((i, i, i, i), i);
            }

            return data.Count;
        }
    }
}