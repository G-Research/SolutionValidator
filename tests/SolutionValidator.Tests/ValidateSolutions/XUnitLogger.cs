using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace SolutionValidator.Tests.ValidateSolutions
{
    public class XUnitLogger<T> : ILogger<T>, IDisposable
    {
        private ITestOutputHelper _testOutputHelper;
        private Action<LogLevel, string> _loggingCallback;

        public XUnitLogger(ITestOutputHelper testOutputHelper, Action<LogLevel, string> loggingCallback)
        {
            _testOutputHelper = testOutputHelper;
            _loggingCallback = loggingCallback;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }

        public void Dispose()
        {
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var output = state.ToString();
            _testOutputHelper.WriteLine(output);

            _loggingCallback(logLevel, output);
        }
    }
}
