using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using Konsarpoo.Collections.Allocators;

namespace Konsarpoo.Collections.Tests.Benchmarks
{
    [Config(typeof(Config))]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class MapLookup
    {
        private static Map<int, int>[] m_map;
        private static Dictionary<int, int>[] m_dict;
        
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


        [GlobalSetup]
        public void Setup()
        {
            var data1 = new Dictionary<int, int>();
            
            foreach (var i in Enumerable.Range(0, 2))
            {
                data1.Add(i, i);
            }
            
            var data2 = new Dictionary<int, int>();
            
            foreach (var i in Enumerable.Range(0, 1000))
            {
                data2.Add(i, i);
            }
            
            var data3 = new Dictionary<int, int>();
            foreach (var i in Enumerable.Range(0, 1000_000))
            {
                data3.Add(i, i);
            }
            
            m_map = new [] { data1.ToMap(), data2.ToMap(), data3.ToMap() };
            m_dict = new[] { data1, data2, data3 };
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
        
        private int GetIndex(int value, IReadOnlyDictionary<int, int> ints)
        {
            var (a, b) = GetLinearTransformParams(0, ints.Values.Max(), 0, ints.Count);

            int index = (int)((a * value) + b);
            return index;
        }
        
        public IEnumerable<int> Range()
        {
            return Enumerable.Range(0, 10);
        }

        [ArgumentsSource(nameof(Range))]
        [BenchmarkCategory("ContainsKey_1000"), Benchmark(Baseline = false)]
        public bool Map_1000_ContainsKey(int value)
        {
            var ints = m_map[1];
            
            value = GetIndex(value, ints);
            
            return m_map[1].ContainsKey(value);
        }
        
        [ArgumentsSource(nameof(Range))]
        [BenchmarkCategory("ContainsKey_1000_000"), Benchmark(Baseline = false)]
        public bool Map_1000_000_ContainsKey(int value)
        {
            var ints = m_map[2];
            
            value = GetIndex(value, ints);
            
            return m_map[2].ContainsKey(value);
        }
        
        [BenchmarkCategory("ContainsKey_2"), Benchmark(Baseline = false)]
        [ArgumentsSource(nameof(Range))]
        public bool Map_2_ContainsKey(int value)
        {
            var ints = m_map[0];
            
            value = GetIndex(value, ints);
            
            return m_map[0].ContainsKey(value);
        }
        
        [ArgumentsSource(nameof(Range))]
        [BenchmarkCategory("ContainsKey_1000"), Benchmark(Baseline = true)]
        public bool Dict_1000_ContainsKey(int value)
        {
            var ints = m_map[1];
            
            value = GetIndex(value, ints);
            
            return m_dict[1].ContainsKey(value);
        }
        
        [ArgumentsSource(nameof(Range))]
        [BenchmarkCategory("ContainsKey_1000_000"), Benchmark(Baseline = true)]
        public bool Dict_1000_000_ContainsKey(int value)
        {
            var ints = m_map[2];
            
            value = GetIndex(value, ints);
            
            return m_dict[2].ContainsKey(value);
        }
        
        [BenchmarkCategory("ContainsKey_2"), Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(Range))]
        public bool Dict_2_ContainsKey(int value)
        {
            var ints = m_map[0];
            
            value = GetIndex(value, ints);
            
            return m_dict[0].ContainsKey(value);
        }
    }
}