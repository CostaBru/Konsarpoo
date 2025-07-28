using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using Konsarpoo.Collections.Allocators;

namespace Konsarpoo.Collections.Tests.Benchmarks
{
    [Config(typeof(SortBench.Config))]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [MemoryDiagnoser]
    public class SortBench
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddJob(Job.MediumRun.WithGcServer(false).WithGcForce(true).WithId("Workstation").WithIterationCount(10));
            }
        }

        private Data<int> data16 = new Data<int>(0, 16);
        private Data<int> data1024 = new Data<int>(0, 1024);
        private Data<int> data = new Data<int>(0, (ushort)(ushort.MaxValue));
        private int[] arr = new int[ushort.MaxValue];
        
        [GlobalSetup]
        public void Setup()
        {
            Data<int>.SetClearArrayOnReturn(false);
            GcArrayPoolMixedAllocator<int>.ClearArrayOnRequest = false;
            
            int j = 0;
            for (int i = (ushort.MaxValue) - 1; i >= 0; i--)
            {
                data16.Add(i);
                data1024.Add(i);
                data.Add(i);
                arr[j] = i;
                j++;
            }
        }

        [Benchmark]
        public void Data_Sort()
        {
            data.Sort();
        }

        [Benchmark]
        public void Data16_Sort()
        {
            data16.Sort();
        }
        
        [Benchmark]
        public void Data1024_Sort()
        {
            data1024.Sort();
        }
        
        [Benchmark(Baseline = true)]
        public void Arr_Sort()
        {
            Array.Sort(arr);
        }
    }
}