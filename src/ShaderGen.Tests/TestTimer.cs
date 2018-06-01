using System;
using System.Diagnostics;
using Xunit.Abstractions;

namespace ShaderGen.Tests
{
    public sealed class TestTimer : IDisposable
    {
        private readonly Action<double> _action;
        private readonly long _timeStamp;

        public TestTimer(Action<double> action)
        {
            _timeStamp = Stopwatch.GetTimestamp();
            _action = action;
        }

        public TestTimer(ITestOutputHelper output, string message)
        {
            _timeStamp = Stopwatch.GetTimestamp();
            _action = t => output.WriteLine($"{message} took {t * 1000}ms");
        }
        public TestTimer(ITestOutputHelper output, Func<string> getMessage)
        {
            _timeStamp = Stopwatch.GetTimestamp();
            _action = t => output.WriteLine($"{getMessage()} took {t * 1000}ms");
        }
        public TestTimer(ITestOutputHelper output, Func<double, string> getMessage)
        {
            _timeStamp = Stopwatch.GetTimestamp();
            _action = t => output.WriteLine(getMessage(t));
        }

        public void Dispose()
        {
            _action(((double) Stopwatch.GetTimestamp() - _timeStamp) / Stopwatch.Frequency);
        }
    }
}