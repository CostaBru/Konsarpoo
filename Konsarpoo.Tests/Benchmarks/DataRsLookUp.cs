using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using Konsarpoo.Collections.Allocators;
using Konsarpoo.Collections.Stackalloc;

namespace Konsarpoo.Collections.Tests.Benchmarks
{
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class DataRsLookUp
    {
        [IterationSetup]
        public void IterationSetup()
        {
            KonsarpooAllocatorGlobalSetup.SetGcArrayPoolMixedAllocatorSetup(maxDataArrayLen: 1000);
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
        
        public IEnumerable<int> Range()
        {
            return Enumerable.Range(0, 10);
        }
        
        (float, float) GetLinearTransformParams(float x1, float x2, float y1, float y2)
        {
            float dx = x1 - x2;

            if (dx != 0.0)
            {
                float a = (y1 - y2) / dx;
                float b = y1 - (a * x1);

                return (a, b);
            }

            return (0.0f, 0.0f);
        }
        
        private int GetIndex(int value, IReadOnlyList<int> ints)
        {
            var (a, b) = GetLinearTransformParams(0, ints[^1], 0, ints.Count);

            int index = (int)((a * value) + b);
            return index;
        }
        
        private int GetIndex(int value, ref DataRs<int> ints)
        {
            var (a, b) = GetLinearTransformParams(0, ints[^1], 0, ints.Count);

            int index = (int)((a * value) + b);
            return index;
        }

        [ArgumentsSource(nameof(Range))]
        [BenchmarkCategory("IndexOf_4096"), Benchmark(Baseline = false)]
        public int Data_1000_IndexOf(int value)
        {
            var ints = new Data<int>();
            ints.Ensure(4096);
            
            for (int i = 0; i < 4096; i++)
            {
                ints[i] = i;
            }
            
            value = GetIndex(value, ints);

            return ints.IndexOf(value);
        }
      

        [BenchmarkCategory("IndexOf_1"), Benchmark(Baseline = false)]
        [ArgumentsSource(nameof(Range))]
        public int Data_1_IndexOf(int value)
        {
            var ints = new Data<int>();
            ints.Ensure(1);
            
            for (int i = 0; i < 1; i++)
            {
                ints[i] = i;
            }
            
            value = GetIndex(value, ints);
            
            return ints.IndexOf(value);
        }

        
        [BenchmarkCategory("BinarySearch_4096"), Benchmark(Baseline = false)]
        [ArgumentsSource(nameof(Range))]
        public int Data_BinarySearch(int value)
        {
            var ints = new Data<int>();
            ints.Ensure(4096);
            
            for (int i = 0; i < 4096; i++)
            {
                ints[i] = i;
            }
            
            value = GetIndex(value, ints);
            
            return ints.BinarySearch(value);
        }
        
        [BenchmarkCategory("IndexOf_4096"), Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(Range))]
        public int List_1000_IndexOf(int value)
        {
            var ints = new List<int>(4096);
            
            for (int i = 0; i < 4096; i++)
            {
                ints.Add(i);
            }
            
            value = GetIndex(value, ints);
            
            return ints.IndexOf(value);
        }
        
        [BenchmarkCategory("IndexOf_1"), Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(Range))]
        public int List_1_IndexOf(int value)
        {
            var ints = new List<int>(1);
            
            for (int i = 0; i < 1; i++)
            {
                ints.Add(i);
            }
            
            value = GetIndex(value, ints);
            
            return ints.IndexOf(value);
        }


        [BenchmarkCategory("BinarySearch_4096"), Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(Range))]
        public int List_BinarySearch(int value)
        {
            var ints = new List<int>(4096);
            
            for (int i = 0; i < 4096; i++)
            {
                ints.Add(i);
            }
            
            value = GetIndex(value, ints);
            
            return ints.BinarySearch(value);
        }
        
        [BenchmarkCategory("IndexOf_4096"), Benchmark(Baseline = false)]
        [ArgumentsSource(nameof(Range))]
        public int Array_1000_IndexOf(int value)
        {
            var ints = new int[4096];
            
            for (int i = 0; i < 4096; i++)
            {
                ints[i] = i;
            }
            
            value = GetIndex(value, ints);
            
            return Array.IndexOf(ints, value);
        }
        
        [BenchmarkCategory("IndexOf_1"), Benchmark(Baseline = false)]
        [ArgumentsSource(nameof(Range))]
        public int Array_2_IndexOf(int value)
        {
            var ints = new int[1];
            
            for (int i = 0; i < 1; i++)
            {
                ints[i] = i;
            }
            
            value = GetIndex(value, ints);
            
            return Array.IndexOf(ints, value);
        }
        
        [BenchmarkCategory("BinarySearch_4096"), Benchmark(Baseline = false)]
        [ArgumentsSource(nameof(Range))]
        public int Array_BinarySearch(int value)
        {
            var ints = new int[4096];
            
            for (int i = 0; i < 4096; i++)
            {
                ints[i] = i;
            }
            
            value = GetIndex(value, ints);
            
            return Array.BinarySearch(ints, value);
        }
        
        [BenchmarkCategory("IndexOf_4096"), Benchmark(Baseline = false)]
        [ArgumentsSource(nameof(Range))]
        public int DataRs_1000_IndexOf(int value)
        {
            Span<int> initStore = stackalloc int[4096];
            var ints = new DataRs<int>(ref initStore);
            
            ints.Ensure(4096);
            
            for (int i = 0; i < 4096; i++)
            {
                ints[i] = i;
            }
            
            value = GetIndex(value, ref ints);
            
            return ints.IndexOf(value);
        }
        
        [BenchmarkCategory("IndexOf_1"), Benchmark(Baseline = false)]
        [ArgumentsSource(nameof(Range))]
        public int DataRs_1_IndexOf(int value)
        {
            Span<int> initStore = stackalloc int[1];
            var ints = new DataRs<int>(ref initStore);
            
            ints.Ensure(1);
            
            for (int i = 0; i < 1; i++)
            {
                ints[i] = i;
            }
            
            value = GetIndex(value, ref ints);
            
            return ints.IndexOf(value);
        }
        
        [BenchmarkCategory("BinarySearch_4096"), Benchmark(Baseline = false)]
        [ArgumentsSource(nameof(Range))]
        public int DataRs_BinarySearch(int value)
        {
            Span<int> initStore = stackalloc int[4096];
            var ints = new DataRs<int>(ref initStore);

            ints.Ensure(4096);
            
            for (int i = 0; i < 4096; i++)
            {
                ints[i] = i;
            }
            
            value = GetIndex(value, ref ints);
            
            return ints.BinarySearch(value, (x, y) => x.CompareTo(y));
        }
    }
}