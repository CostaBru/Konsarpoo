using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using Konsarpoo.Collections.Allocators;
using Konsarpoo.Collections.Stackalloc;

namespace Konsarpoo.Collections.Tests.Benchmarks
{
    [Config(typeof(Config))]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [MemoryDiagnoser]
    public class SetStructFill
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddJob(Job.MediumRun.WithGcServer(false).WithGcForce(true).WithId("Workstation").WithIterationCount(100));
            }
        }
        
        [IterationSetup]
        public void IterationSetup()
        {
            Data<int>.SetClearArrayOnReturn(false);
            GcArrayPoolMixedAllocator<int>.ClearArrayOnRequest = false;
        }
        
        [IterationCleanup]
        public void IterationCleanup()
        {
            Data<int>.SetClearArrayOnReturn(true);
            GcArrayPoolMixedAllocator<int>.ClearArrayOnRequest = true;
        }

        
        [Params(2, 1000, 10_000)]
        public int N;

        [Benchmark]
        public int SetStruct_Add()
        {
            Span<int> buckets = stackalloc int[N];
            Span<KeyEntry<int>> entriesHash = stackalloc KeyEntry<int>[N];
            var set = new SetRs<int>(ref buckets, ref entriesHash, EqualityComparer<int>.Default);
            
            for (int i = 0; i < N; i++)
            {
                set.Add(i);
            }

            return set.Count;
        }

        [Benchmark]
        public int Set_Add()
        {
            var data = new Set<int>(N);

            for (int i = 0; i < N; i++)
            {
                data.Add(i);
            }
            
            data.Dispose();

            return data.Count;
        }

        [Benchmark(Baseline = true)]
        public int Hashset_Add()
        {
            var data = new HashSet<int>(N);

            for (int i = 0; i < N; i++)
            {
                data.Add(i);
            }

            return data.Count;
        }
    }
}