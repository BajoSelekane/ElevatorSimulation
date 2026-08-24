using System;
using Microsoft.Extensions.Logging;

namespace ElevatorSimulation.Infrastructure.Logging
{
    // Adapter to satisfy Microsoft.Extensions.Logging.ILogger dependencies
    public class MicrosoftLoggerAdapter : Microsoft.Extensions.Logging.ILogger
    {
        private readonly ILogger _logger;

        public MicrosoftLoggerAdapter(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter != null ? formatter(state, exception) : state?.ToString();

            switch (logLevel)
            {
                case LogLevel.Critical:
                case LogLevel.Error:
                    _logger.LogError(message);
                    if (exception != null) _logger.LogException(exception);
                    break;
                case LogLevel.Warning:
                    _logger.LogWarning(message);
                    break;
                case LogLevel.Information:
                    _logger.LogInfo(message);
                    break;
                case LogLevel.Debug:
                case LogLevel.Trace:
                    _logger.LogDebug(message);
                    break;
                default:
                    _logger.LogInfo(message);
                    break;
            }
        }

        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new NullScope();
            public void Dispose() { }
        }
    }
}
