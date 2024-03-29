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
    public class MapFill
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddJob(Job.MediumRun.WithGcServer(false).WithGcForce(true).WithId("Workstation").WithIterationCount(10));
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

        
        [Params(2, 1000, 1_000_000)]
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