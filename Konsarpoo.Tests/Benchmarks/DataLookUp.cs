using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;

namespace Konsarpoo.Collections.Tests.Benchmarks
{
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class DataLookUp
    {
        private static Data<int>[] m_data;
        private static List<int>[] m_list;
        private static int[][] m_array;

        [GlobalSetup]
        public void Setup()
        {
            var data1 = new Data<int>();
            
            data1.AddRange(Enumerable.Range(0, 2));
            
            var data2 = new Data<int>();
            
            data2.AddRange(Enumerable.Range(0, 1000));
            
            var data3 = new Data<int>();
            
            data3.AddRange(Enumerable.Range(0, 1000_000));
            
            m_data = new [] { data1, data2, data3 };
            m_list = new[] { data1.ToList(), data2.ToList(), data3.ToList() };
            m_array = new[] { data1.ToArray(), data2.ToArray(), data3.ToArray() };
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

        [ArgumentsSource(nameof(Range))]
        [BenchmarkCategory("IndexOf_1000"), Benchmark(Baseline = false)]
        public int Data_1000_IndexOf(int value)
        {
            var ints = m_data[1];
            
            value = GetIndex(value, ints);

            return ints.IndexOf(value);
        }

        private int GetIndex(int value, Data<int> ints)
        {
            var (a, b) = GetLinearTransformParams(0, ints[^1], 0, ints.Length);

            int index = (int)((a * value) + b);
            return index;
        }

        [BenchmarkCategory("IndexOf_1"), Benchmark(Baseline = false)]
        [ArgumentsSource(nameof(Range))]
        public int Data_2_IndexOf(int value)
        {
            var ints = m_data[0];
            
            value = GetIndex(value, ints);
            
            return m_data[0].IndexOf(value);
        }

        
        [BenchmarkCategory("BinarySearch_1000_000"), Benchmark(Baseline = false)]
        [ArgumentsSource(nameof(Range))]
        public int Data_BinarySearch(int value)
        {
            var ints = m_data[2];
            
            value = GetIndex(value, ints);
            
            return m_data[2].BinarySearch(value);
        }
        
        [BenchmarkCategory("IndexOf_1000"), Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(Range))]
        public int List_1000_IndexOf(int value)
        {
            var ints = m_data[1];
            
            value = GetIndex(value, ints);
            
            return m_list[1].IndexOf(value);
        }
        
        [BenchmarkCategory("IndexOf_1"), Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(Range))]
        public int List_2_IndexOf(int value)
        {
            var ints = m_data[0];
            
            value = GetIndex(value, ints);
            
            return m_list[0].IndexOf(value);
        }


        [BenchmarkCategory("BinarySearch_1000_000"), Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(Range))]
        public int List_BinarySearch(int value)
        {
            var ints = m_data[2];
            
            value = GetIndex(value, ints);
            
            return m_list[2].BinarySearch(value);
        }
        
        [BenchmarkCategory("IndexOf_1000"), Benchmark(Baseline = false)]
        [ArgumentsSource(nameof(Range))]
        public int Array_1000_IndexOf(int value)
        {
            var ints = m_data[1];
            
            value = GetIndex(value, ints);
            
            return Array.IndexOf(m_array[1], value);
        }
        
        [BenchmarkCategory("IndexOf_1"), Benchmark(Baseline = false)]
        [ArgumentsSource(nameof(Range))]
        public int Array_2_IndexOf(int value)
        {
            var ints = m_data[0];
            
            value = GetIndex(value, ints);
            
            return Array.IndexOf(m_array[0], value);
        }
        
        [BenchmarkCategory("BinarySearch_1000_000"), Benchmark(Baseline = false)]
        [ArgumentsSource(nameof(Range))]
        public int Array_BinarySearch(int value)
        {
            var ints = m_data[0];
            
            value = GetIndex(value, ints);
            
            return Array.BinarySearch(m_array[0], value);
        }
    }
}