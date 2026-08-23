using System;
using System.Collections.Concurrent;
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

    public class ConsoleLogger : ILogger
    {
        private readonly string _logFilePath;
        private readonly ConcurrentQueue<string> _logQueue;
        private readonly object _lockObject = new object();
        private bool _isDisposed;

        public ConsoleLogger(string logFilePath = null)
        {
            _logFilePath = logFilePath ?? "elevator_simulation.log";
            _logQueue = new ConcurrentQueue<string>();
            _isDisposed = false;

            // Ensure log directory exists
            var directory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
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

            lock (_lockObject)
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(formattedMessage);
                Console.ForegroundColor = originalColor;
            }

            // Write to file asynchronously
            _logQueue.Enqueue(formattedMessage);
            if (_logQueue.Count >= 10)
            {
                FlushLogsToFile();
            }
        }

        private void FlushLogsToFile()
        {
            if (string.IsNullOrEmpty(_logFilePath))
                return;

            try
            {
                lock (_lockObject)
                {
                    var entries = new List<string>();
                    while (_logQueue.TryDequeue(out var entry))
                    {
                        entries.Add(entry);
                    }

                    if (entries.Any())
                    {
                        File.AppendAllLines(_logFilePath, entries);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] Failed to write to log file: {ex.Message}");
                Console.ResetColor();
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            FlushLogsToFile();
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }

        ~ConsoleLogger()
        {
            Dispose();
        }
    }

    public class NullLogger : ILogger
    {
        public void LogInfo(string message) { }
        public void LogSuccess(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message) { }
        public void LogDebug(string message) { }
        public void LogException(Exception ex, string context = null) { }
    }
}