using BenchmarkDotNet.Attributes;

namespace Konsarpoo.Collections.Tests.Benchmarks
{
    [DisassemblyDiagnoser(maxDepth: 3)]
    [DryJob]
    public class DataAddDisassemblyDry
    {
        [Benchmark]
        public void DataAdd1000()
        {
            var data = new Data<int>();

            for (int i = 0; i < 1000; i++)
            {
                data.Add(i);
            }
            
            data.Dispose();
        }
    }
}