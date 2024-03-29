﻿using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using Konsarpoo.Collections.Allocators;

namespace Konsarpoo.Collections.Tests.Benchmarks
{
    [Config(typeof(Config))]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [MemoryDiagnoser]
    public class SetFill
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

        
        [Params(2, 1000, 1000_000)]
        public int N;

        [Benchmark]
        public int Set_Add()
        {
            var data = new Set<int>();

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
            var data = new HashSet<int>();

            for (int i = 0; i < N; i++)
            {
                data.Add(i);
            }

            return data.Count;
        }
    }
}