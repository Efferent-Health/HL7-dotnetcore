using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using HL7.Dotnetcore;
using System.IO;

namespace Benchmarks
{
    /// <summary>
    /// Basic benchmark comparing the nuget version to our local code, for both Framework 4.8 and net8.  Calls a copy of one of the tests
    /// that best reflects my usage scenarios (Parse and GetValue)
    /// </summary>
    [Config(typeof(Config))]
    [MemoryDiagnoser]
    [HideColumns("BuildConfiguration", "NuGetReferences", "Runtime")]
    public class ParseMessageBench
    {
        private readonly string _sampleMessage = File.ReadAllText("Sample-Orm.txt");

/*
| Method                   | Job          | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------- |------------- |---------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| ParseMessageAndGetValues | Local Net4.8 | 96.03 us |  5.445 us | 0.298 us |  1.83 |    0.01 | 40.5273 | 5.9814 | 249.26 KB |        1.18 |
| ParseMessageAndGetValues | Local Net8   | 50.55 us | 21.651 us | 1.187 us |  0.96 |    0.02 | 12.2070 | 2.0142 | 187.36 KB |        0.89 |
| ParseMessageAndGetValues | Nuget Net4.8 | 97.66 us | 38.600 us | 2.116 us |  1.86 |    0.05 | 44.4336 | 6.8359 | 273.48 KB |        1.30 |
| ParseMessageAndGetValues | Nuget Net8   | 52.39 us |  6.528 us | 0.358 us |  1.00 |    0.00 | 13.7329 | 2.3193 | 211.17 KB |        1.00 |


 */

        private class Config : ManualConfig
        {
            public Config()
            {
                var baseJob = Job.ShortRun;

                AddJob(baseJob.WithNuGet("HL7-dotnetcore", "2.37.1").WithRuntime(CoreRuntime.Core80).WithId("Nuget Net8").AsBaseline());
                AddJob(baseJob.WithNuGet("HL7-dotnetcore", "2.37.1").WithRuntime(ClrRuntime.Net48).WithId("Nuget Net4.8"));

                AddJob(baseJob.WithRuntime(ClrRuntime.Net48).WithCustomBuildConfiguration("LOCAL_CODE")
                    .WithId("Local Net4.8")); // custom config to include/exclude nuget reference or target project reference locally

                AddJob(baseJob.WithRuntime(CoreRuntime.Core80).WithCustomBuildConfiguration("LOCAL_CODE")
                    .WithId("Local Net8")); // custom config to include/exclude nuget reference or target project reference locally
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