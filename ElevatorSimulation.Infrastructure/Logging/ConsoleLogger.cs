using System;
using System.IO;

namespace ElevatorSimulation.Infrastructure.Logging
{
    public interface ILogger
    {
        void LogInfo(string message);
        void LogSuccess(string message);
        void LogWarning(string message);
        void LogError(string message);
        void LogDebug(string message);
        void LogException(Exception ex, string context = null);
    }

    // Backwards-compatible console-specific logger interface
    public interface IConsoleLogger : ILogger { }

    public sealed class ConsoleLogger : ILogger, IConsoleLogger, IDisposable
    {
        private readonly TextWriter _writer;
        private bool _disposed;

        // Accepts null or empty and falls back to Console.Out
        public ConsoleLogger(string logFilePath = null)
        {
            if (string.IsNullOrWhiteSpace(logFilePath))
            {
                _writer = Console.Out;
                return;
            }

            try
            {
                // Ensure directory exists for file path
                var directory = Path.GetDirectoryName(logFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var stream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
                _writer = new StreamWriter(stream) { AutoFlush = true };
            }
            catch
            {
                // On any failure, fall back to console output
                _writer = Console.Out;
            }
        }

        public void LogInfo(string message)
        {
            LogWithColor(message, ConsoleColor.White, "INFO");
        }

        public void LogSuccess(string message)
        {
            LogWithColor(message, ConsoleColor.Green, "SUCCESS");
        }

        public void LogWarning(string message)
        {
            LogWithColor(message, ConsoleColor.Yellow, "WARNING");
        }

        public void LogError(string message)
        {
            LogWithColor(message, ConsoleColor.Red, "ERROR");
        }

        public void LogDebug(string message)
        {
            LogWithColor(message, ConsoleColor.Cyan, "DEBUG");
        }

        public void LogException(Exception ex, string context = null)
        {
            var message = $"Exception: {ex.Message}";
            if (!string.IsNullOrEmpty(context))
                message = $"[{context}] {message}";

            LogError($"{message}\nStack Trace: {ex.StackTrace}");

            if (ex.InnerException != null)
                LogException(ex.InnerException, "Inner Exception");
        }

        private void LogWithColor(string message, ConsoleColor color, string level)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var formattedMessage = $"[{timestamp}] [{level}] {message}";

            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(formattedMessage);
            Console.ForegroundColor = originalColor;

            _writer.WriteLine(formattedMessage);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            if (!ReferenceEquals(_writer, Console.Out))
            {
                _writer.Dispose();
            }
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        ~ConsoleLogger()
        {
            Dispose();
        }
    }

    public class NullLogger : ILogger, IConsoleLogger
    {
        public void LogInfo(string message) { }
        public void LogSuccess(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message) { }
        public void LogDebug(string message) { }
        public void LogException(Exception ex, string context = null) { }
    }

    public static class LoggerExtensions
    {
        public static void Info(this ILogger logger, string message) => logger.LogInfo(message);
        public static void Success(this ILogger logger, string message) => logger.LogSuccess(message);
        public static void Warning(this ILogger logger, string message) => logger.LogWarning(message);
        public static void Error(this ILogger logger, string message) => logger.LogError(message);
        public static void Debug(this ILogger logger, string message) => logger.LogDebug(message);
    }
}