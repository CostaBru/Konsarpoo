﻿using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;

namespace Konsarpoo.Collections.Tests.Benchmarks
{
    [Config(typeof(Config))]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [MemoryDiagnoser]
    public class DataFill
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
        
        [Params(16, 1024 * 1024)]
        public int NodeSize;

        [Benchmark]
        public int Data_Add()
        {
            var data = new Data<int>(0, NodeSize, (null, null));

            for (int i = 0; i < N; i++)
            {
                data.Add(i);
            }
            
            data.Dispose();

            return data.Count;
        }

        [Benchmark]
        public int Data_Ensure()
        {
            var data = new Data<int>(0, NodeSize, (null, null));

            data.Ensure(N);
            
            var storage = data.GetRoot()?.Storage;

            if (storage != null)
            {
                for (int i = 0; i < N && i < storage.Length; i++)
                {
                    storage[i] = i;
                }
            }
            else
            {
                for (int i = 0; i < N; i++)
                {
                    data.ValueByRef(i) = i;
                }
            }
            
            data.Dispose();

            return data.Count;
        }

        [Benchmark]
        public int Set_Array()
        {
            var storage = new int[N];

            for (int i = 0; i < N && i < storage.Length; i++)
            {
                storage[i] = i;
            }

            return storage.Length;
        }

        [Benchmark(Baseline = true)]
        public int List_Add()
        {
            var data = new List<int>();

            for (int i = 0; i < N; i++)
            {
                data.Add(i);
            }

            return data.Count;
        }
    }
}