using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

using HL7.Dotnetcore;
using System.IO;

namespace Benchmarks
{
    /// <summary>
    /// Basic benchmark comparing the nuget version to the local code, for both .NET Framework 4.8 and .NET 8.0.  
    /// Calls a copy of one of the tests that best reflects the usage scenarios (Parse and GetValue)
    /// </summary>
    [Config(typeof(Config))]
    [MemoryDiagnoser]
    [HideColumns("BuildConfiguration", "Error", "StdDev", "RatioSD")]
    public class ParseMessageBench
    {

/*
| Method                   | Job          | Runtime            | NuGetReferences     | Mean     | Ratio | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------- |------------- |------------------- |-------------------- |---------:|------:|--------:|-------:|----------:|------------:|
| ParseMessageAndGetValues | Net4.8 Local | .NET Framework 4.8 | Default             | 75.54 us |  1.59 | 31.7383 | 4.3945 | 195.49 KB |        1.08 |
| ParseMessageAndGetValues | Net4.8 Nuget | .NET Framework 4.8 | HL7-dotnetcore 2.39 | 99.49 us |  2.10 | 40.5273 | 6.1035 | 249.25 KB |        1.38 |
| ParseMessageAndGetValues | Net8 Local   | .NET 8.0           | Default             | 33.67 us |  0.71 |  8.3008 | 1.3428 | 127.68 KB |        0.71 |
| ParseMessageAndGetValues | Net8 Nuget   | .NET 8.0           | HL7-dotnetcore 2.39 | 47.40 us |  1.00 | 11.7798 | 1.9531 |  180.7 KB |        1.00 |
 */

        internal static readonly string _sampleMessage = File.ReadAllText("Sample-Orm.txt");

        private class Config : ManualConfig
        {
            public Config()
            {
                var baseJob = Job.ShortRun;

                AddJob(baseJob.WithNuGet("HL7-dotnetcore", "2.39").WithRuntime(CoreRuntime.Core80).WithId("Net8 Nuget").AsBaseline());
                AddJob(baseJob.WithNuGet("HL7-dotnetcore", "2.39").WithRuntime(ClrRuntime.Net48).WithId("Net4.8 Nuget"));
                
                AddJob(baseJob.WithRuntime(ClrRuntime.Net48).WithCustomBuildConfiguration("LOCAL_CODE")
                    .WithId("Net4.8 Local")); // custom config to include/exclude nuget reference or target project reference locally
                
                AddJob(baseJob.WithRuntime(CoreRuntime.Core80).WithCustomBuildConfiguration("LOCAL_CODE")
                    .WithId("Net8 Local")); // custom config to include/exclude nuget reference or target project reference locally

            }
        }

        [Benchmark]
        public void ParseMessageAndGetValues()
        {
            var msg = new Message(_sampleMessage);
            msg.ParseMessage(true);

            var ack = msg.GetACK(true);
            string sendingApp = ack.GetValue("MSH.3");
            string sendingFacility = ack.GetValue("MSH.4");
            string receivingApp = ack.GetValue("MSH.5");
            string receivingFacility = ack.GetValue("MSH.6");
            string messageType = ack.GetValue("MSH.9");
        }
    }
}