using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using Konsarpoo.Collections.Stackalloc;

namespace Konsarpoo.Collections.Tests.Benchmarks
{
    [Config(typeof(Config))]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [MemoryDiagnoser]
    public class MapStructFill
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddJob(Job.MediumRun.WithGcServer(false).WithGcForce(true).WithId("Workstation").WithIterationCount(10));
            }
        }
        
        [Params(1, 10, 1000, 10_000)]
        public int N;

        [Benchmark]
        public int Map_Add()
        {
            var testData = new Map<int, int>();
            
            for (int i = 0; i < N; i++)
            {
                testData.Add(i, i);
            }
            
            testData.Dispose();
            
            return testData.Count;
        }

        [Benchmark]
        public int MapStruct_Add()
        {
            Span<int> buckets = stackalloc int[N];
            Span<MapStruct<int, int>.Entry> entriesHash = stackalloc MapStruct<int, int>.Entry[N];

            var testData = new MapStruct<int, int>(ref buckets, ref entriesHash);
            
            for (int i = 0; i < N; i++)
            {
                testData.Add(i, i);
            }
            
            return testData.Count;
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