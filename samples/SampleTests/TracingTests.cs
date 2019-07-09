using System;
using System.Diagnostics;
using Xunit;

namespace SampleTests
{
    public class TracingTests
    {
        [Fact]
        public void EmitCustomEvent()
        {
            CustomEventSource.Log.CustomEvent("Test");
        }

        [Fact]
        public void LaunchDotNetSubProcess()
        {
            var p = Process.Start("dotnet", "new --help");
            Console.WriteLine($"dotnet subprocess: {p.Id}");
            p.WaitForExit();
        }
    }
}
