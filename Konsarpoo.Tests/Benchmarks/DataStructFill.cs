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
    public class DataStructFill
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddJob(Job.MediumRun.WithGcServer(false).WithGcForce(true).WithId("Workstation").WithIterationCount(10));
            }
        }
        
        [Params(10, 1000, 100_000)]
        public int N;
        
        [Params(16, 1024 * 1024)]
        public int NodeSize;

        [IterationSetup]
        public void IterationSetup()
        {
            KonsarpooAllocatorGlobalSetup.SetGcArrayPoolMixedAllocatorSetup(maxDataArrayLen: NodeSize);
            Data<int>.SetClearArrayOnReturn(false);
            GcArrayPoolMixedAllocator<int>.ClearArrayOnRequest = false;
        }
        
        [IterationCleanup]
        public void IterationCleanup()
        {
            KonsarpooAllocatorGlobalSetup.SetGcArrayPoolMixedAllocatorSetup(maxDataArrayLen: null);
            Data<int>.SetClearArrayOnReturn(true);
            GcArrayPoolMixedAllocator<int>.ClearArrayOnRequest = true;
        }

        [Benchmark]
        public int Data_Add()
        {
            var data = new Data<int>();

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
            var data = new Data<int>();

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
        
        [Benchmark]
        public int Data_Struct_Add()
        {
            Span<int> initStore = stackalloc int[N];

            var storage = new DataStruct<int>(ref initStore);

            for (int i = 0; i < N; i++)
            {
                storage.Add(ref i);
            }

            return storage.Length;
        }

        [Benchmark]
        public int Data_Struct_Ensure()
        {
            Span<int> initStore = stackalloc int[N];
            
            var storage = new DataStruct<int>(ref initStore);
            
            storage.Ensure(N);

            for (int i = 0; i < N; i++)
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