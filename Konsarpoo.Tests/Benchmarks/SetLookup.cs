using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;

namespace Konsarpoo.Collections.Tests.Benchmarks
{
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class SetLookup
    {
        private static Set<int>[] m_set;
        private static HashSet<int>[] m_hashset;

        [GlobalSetup]
        public void Setup()
        {
            var data1 = new HashSet<int>();
            
            foreach (var i in Enumerable.Range(0, 2))
            {
                data1.Add(i);
            }
            
            var data2 = new HashSet<int>();
            
            foreach (var i in Enumerable.Range(0, 1000))
            {
                data2.Add(i);
            }
            
            var data3 = new HashSet<int>();
            foreach (var i in Enumerable.Range(0, 1000_000))
            {
                data3.Add(i);
            }
            
            m_set = new [] { data1.ToSet(), data2.ToSet(), data3.ToSet() };
            m_hashset = new[] { data1, data2, data3 };
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
        
        private int GetIndex(int value, IReadOnlyCollection<int> ints)
        {
            var (a, b) = GetLinearTransformParams(0, ints.Max(), 0, ints.Count);

            int index = (int)((a * value) + b);
            return index;
        }
        
        public IEnumerable<int> Range()
        {
            return Enumerable.Range(0, 10);
        }

        [ArgumentsSource(nameof(Range))]
        [BenchmarkCategory("ContainsKey_1000"), Benchmark(Baseline = false)]
        public bool Set_1000_ContainsKey(int value)
        {
            var ints = m_set[1];
            
            value = GetIndex(value, ints);
            
            return m_set[1].Contains(value);
        }
        
        [ArgumentsSource(nameof(Range))]
        [BenchmarkCategory("ContainsKey_1000_000"), Benchmark(Baseline = false)]
        public bool Set_1000_000_ContainsKey(int value)
        {
            var ints = m_set[2];
            
            value = GetIndex(value, ints);
            
            return m_set[2].Contains(value);
        }
        
        [BenchmarkCategory("ContainsKey_2"), Benchmark(Baseline = false)]
        [ArgumentsSource(nameof(Range))]
        public bool Set_2_ContainsKey(int value)
        {
            var ints = m_set[0];
            
            value = GetIndex(value, ints);
            
            return m_set[0].Contains(value);
        }
        
        [ArgumentsSource(nameof(Range))]
        [BenchmarkCategory("ContainsKey_1000"), Benchmark(Baseline = true)]
        public bool HashSet_1000_ContainsKey(int value)
        {
            var ints = m_set[1];
            
            value = GetIndex(value, ints);
            
            return m_hashset[1].Contains(value);
        }
        
        [ArgumentsSource(nameof(Range))]
        [BenchmarkCategory("ContainsKey_1000_000"), Benchmark(Baseline = true)]
        public bool HashSet_1000_000_ContainsKey(int value)
        {
            var ints = m_set[2];
            
            value = GetIndex(value, ints);
            
            return m_hashset[2].Contains(value);
        }
        
        [BenchmarkCategory("ContainsKey_2"), Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(Range))]
        public bool HashSet_2_ContainsKey(int value)
        {
            var ints = m_set[0];
            
            value = GetIndex(value, ints);
            
            return m_hashset[0].Contains(value);
        }
    }
}