using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
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
    public class DataSerializationReport
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddJob(Job.MediumRun.WithGcServer(false).WithGcForce(true).WithId("Workstation")
                    .WithIterationCount(10));
            }
        }

        [Params(1000, 1000_000_00)] public int N;

        [Params(1000, ushort.MaxValue + 1, 1000_000)]
        public int NodeSize;

        private Data<int> m_testDat;

        [IterationSetup]
        public void IterationSetup()
        {
            KonsarpooAllocatorGlobalSetup.SetGcArrayPoolMixedAllocatorSetup(maxDataArrayLen: NodeSize);
            Data<int>.SetClearArrayOnReturn(false);
            GcArrayPoolMixedAllocator<int>.ClearArrayOnRequest = false;

            m_testDat = new Data<int>(0, NodeSize);

            for (int i = 0; i < N; i++)
            {
                m_testDat.Add(i);
            }
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            KonsarpooAllocatorGlobalSetup.SetGcArrayPoolMixedAllocatorSetup(maxDataArrayLen: null);
            Data<int>.SetClearArrayOnReturn(true);
            GcArrayPoolMixedAllocator<int>.ClearArrayOnRequest = true;
        }

        [Benchmark(Baseline = true)]
        public int Data_BinFormatter()
        {
            var fn = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tmp");
            try
            {
                using var fileStream = new FileStream(fn, FileMode.Create);

                var binaryFormatter = new BinaryFormatter();
                
#pragma warning disable SYSLIB0011
                binaryFormatter.Serialize(fileStream, m_testDat);
#pragma warning restore SYSLIB0011
                
                fileStream.Flush();
            }
            finally
            {
                File.Delete(fn);
            }

            return 1;
        }
        
        [Benchmark]
        public int Data_PerArray()
        {
            var fn = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tmp");

            var dataFileSerializationInfo = new DataFileSerialization(fn);
            
            try
            {
                
                dataFileSerializationInfo.BeginWrite();
                
                m_testDat.SerializeTo(dataFileSerializationInfo);
                
                dataFileSerializationInfo.EndWrite();
            }
            finally
            {
                dataFileSerializationInfo.Dispose();
                
                File.Delete(fn);
            }

            return 1;
        }
    }
}