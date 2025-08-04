using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;

namespace Konsarpoo.Collections.Tests.Benchmarks
{
    [Config(typeof(Config))]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [MemoryDiagnoser]
    public class SeriPerfTest
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddJob(Job.MediumRun.WithGcServer(false).WithGcForce(true).WithId("Workstation").WithIterationCount(10));
            }
        }

        private Data<double> m_test = new Data<double>();

        public SeriPerfTest()
        {
            m_test.AddRange(Enumerable.Range(0, N).Select(s => (double)s));
        }
        
        [Params(1_000_000)]
        public int N;

        [Benchmark]
        public int Bytes()
        {
            var info = new DataMemorySerializationInfo<double>(new SerializationInfo(typeof(double), new FormatterConverter()));
            
            m_test.SerializeTo(info);

            return 1;
        }
        
        [Benchmark]
        public int Arrays()
        {
            var info = new DataMemorySerializationInfo<double>(new SerializationInfo(typeof(double), new FormatterConverter()), false);
            
            m_test.SerializeTo(info);

            return 1;
        }
    }
}