using Microsoft.Build.Framework;
using Microsoft.Extensions.Logging;

namespace SolutionValidator
{
    public class LoggingProxy : Microsoft.Build.Framework.ILogger
    {
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        private IEventSource _eventSource;

        public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Diagnostic;
        public string Parameters { get; set; }

        public LoggingProxy(Microsoft.Extensions.Logging.ILogger logger)
        {
            _logger = logger;
        }

        public void Initialize(IEventSource eventSource)
        {
            _eventSource = eventSource;
            eventSource.ErrorRaised += EventSource_ErrorRaised;
            eventSource.WarningRaised += EventSource_WarningRaised;
        }

        private void EventSource_WarningRaised(object sender, BuildWarningEventArgs e)
        {
            _logger.LogWarning("MSBuild: {message}", e.Message);
        }

        private void EventSource_ErrorRaised(object sender, BuildErrorEventArgs e)
        {
            _logger.LogError("MSBuild: {message}", e.Message);
        }

        private void EventSource_AnyEventRaised(object sender, BuildEventArgs e)
        {
            _logger.LogInformation("MSBuild: {message}", e.Message);
        }

        public void Shutdown()
        {
            _logger.LogInformation("Shutting down logger");
            _eventSource.ErrorRaised -= EventSource_ErrorRaised;
            _eventSource.WarningRaised -= EventSource_WarningRaised;
        }
    }
}
