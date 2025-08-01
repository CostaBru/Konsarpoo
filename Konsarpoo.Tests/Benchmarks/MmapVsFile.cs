using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
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
    public class MMapVsFile
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddJob(Job.MediumRun.WithGcServer(false).WithGcForce(true).WithId("Workstation")
                    .WithIterationCount(10));
            }
        }

        private string m_binFilePath;
        private string m_mmapFilePath;
        private MemoryMappedDataVariableSizeSerializationInfo m_mmapFile;

        [GlobalSetup]
        public void Setup()
        {
            var data = new Data<double>();

            for (int i = 0; i < 1_000_000; i++)
            {
                data.Add(i);
            }

            var tempFileName = WriteToFile(data);

            m_binFilePath = tempFileName;

            var (mmapFilePath, serializationInfo) = CreateMMap(data);

            m_mmapFile = serializationInfo;
            m_mmapFilePath = mmapFilePath;
        }

        private static (string tempFileName2, MemoryMappedDataVariableSizeSerializationInfo
            memoryMappedDataSerializationInfo)
            CreateMMap(Data<double> data)
        {
            string tempFileName2 = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".bin");

            var estimatedSizeOfArray = data.Count;

            var memoryMappedDataSerializationInfo =
                new MemoryMappedDataVariableSizeSerializationInfo(tempFileName2, 10,
                    data.GetStoreNodesCount(), estimatedSizeOfArray, typeof(double));

            data.SerializeTo(memoryMappedDataSerializationInfo);
            return (tempFileName2, memoryMappedDataSerializationInfo);
        }

        private static string WriteToFile(Data<double> data)
        {
            string tempFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".bin");


            IFormatter formatter = new BinaryFormatter();
            using Stream stream = new FileStream(tempFileName, FileMode.Create, FileAccess.Write, FileShare.None);
#pragma warning disable SYSLIB0011
            formatter.Serialize(stream, data);
#pragma warning restore SYSLIB0011
            stream.Flush();
            return tempFileName;
        }

        [GlobalCleanupAttribute]
        public void Cleanup()
        {
            try
            {
                m_mmapFile.Dispose();
                System.IO.File.Delete(m_mmapFilePath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            System.IO.File.Delete(m_binFilePath);
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

        [Benchmark]
        public int MMap()
        {
            var data = new Data<double>();

            data.DeserializeFrom(m_mmapFile);

            return data.Count;
        }

        [Benchmark]
        public int File()
        {
            var fileStream = new FileStream(m_binFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            var data = SerializeHelper.Deserialize<Data<double>>(fileStream);

            return data.Count;
        }
    }
}