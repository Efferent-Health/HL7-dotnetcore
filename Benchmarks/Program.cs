// to run this targeting multiple frameworks, use 
//   dotnet run -c Release -f net48 --filter "*"
// I'm using net48 just to target the netstandard21 version of the library, and we're configured to bench the local code against the nuget package

using BenchmarkDotNet.Running;

namespace Benchmarks
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(ParseMessageBench).Assembly).Run(args);
        }
    }
}