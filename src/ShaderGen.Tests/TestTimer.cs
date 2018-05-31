using System;
using System.Diagnostics;
using Xunit.Abstractions;

namespace ShaderGen.Tests
{
    /// <summary>
    /// Allows timing of a block of code.
    /// </summary>
    /// <seealso cref="IDisposable" />
    public sealed class TestTimer : IDisposable
    {
        /// <summary>
        /// The action to call once the the object is dipsosed.
        /// </summary>
        private readonly Action<double> _action;

        /// <summary>
        /// The initial time stamp
        /// </summary>
        private readonly long _timeStamp;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestTimer" /> class.
        /// </summary>
        /// <param name="action">The action.</param>
        public TestTimer(Action<double> action)
        {
            _timeStamp = Stopwatch.GetTimestamp();
            _action = action;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestTimer" /> class.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="message">The message.</param>
        public TestTimer(ITestOutputHelper output, string message)
        {
            _timeStamp = Stopwatch.GetTimestamp();
            _action = t => output.WriteLine($"{message} took {t * 1000:#.##}ms");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestTimer" /> class.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="getMessage">The function that will be called to get a message once the operation is complete.</param>
        public TestTimer(ITestOutputHelper output, Func<string> getMessage)
        {
            _timeStamp = Stopwatch.GetTimestamp();
            _action = t => output.WriteLine($"{getMessage()} took {t * 1000:#.##}ms");
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="TestTimer" /> class.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="getMessage">The function that will be called to get a message once the operation is complete.</param>
        public TestTimer(ITestOutputHelper output, Func<double, string> getMessage)
        {
            _timeStamp = Stopwatch.GetTimestamp();
            _action = t => output.WriteLine(getMessage(t));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            double elapsed = ((double)Stopwatch.GetTimestamp() - _timeStamp) / Stopwatch.Frequency;
            try
            {
                _action(elapsed);
            }
            catch
            {
                // ignored
            }
        }
    }
}