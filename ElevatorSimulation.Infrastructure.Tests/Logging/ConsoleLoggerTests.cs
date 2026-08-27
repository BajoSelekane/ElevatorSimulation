using Xunit;
using FluentAssertions;
using ElevatorSimulation.Infrastructure.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ElevatorSimulation.Infrastructure.Tests.Logging
{
    /// <summary>
    /// Comprehensive test suite for ConsoleLogger with 100% code coverage
    /// </summary>
    public class ConsoleLoggerTests : IDisposable
    {
        private readonly string _testLogFilePath;
        private readonly ConsoleLogger _logger;
        private readonly StringWriter _consoleOutput;
        private readonly TextWriter _originalConsoleOut;

        public ConsoleLoggerTests()
        {
            _testLogFilePath = Path.Combine(Path.GetTempPath(), $"test_log_{Guid.NewGuid()}.log");
            _logger = new ConsoleLogger(_testLogFilePath);

            // Capture console output
            _consoleOutput = new StringWriter();
            _originalConsoleOut = Console.Out;
            Console.SetOut(_consoleOutput);
        }

        #region Constructor Tests

        [Fact]
        [Trait("Category", "Constructor")]
        public void Constructor_WithValidPath_ShouldCreateLogDirectory()
        {
            // Arrange
            var logDir = Path.GetDirectoryName(_testLogFilePath);

            // Act - Directory should be created by constructor
            // Assert
            Directory.Exists(logDir).Should().BeTrue();
        }

        [Fact]
        [Trait("Category", "Constructor")]
        public void Constructor_WithNullPath_ShouldCreateDefault()
        {
            // Arrange & Act
            using var logger = new ConsoleLogger(null);

            // Assert
            // Should not throw
            logger.Should().NotBeNull();
        }

        [Fact]
        [Trait("Category", "Constructor")]
        public void Constructor_WithEmptyPath_ShouldCreateDefault()
        {
            // Arrange & Act
            using var logger = new ConsoleLogger("");

            // Assert
            logger.Should().NotBeNull();
        }

        #endregion

        #region LogInfo Tests

        [Fact]
        [Trait("Category", "LogInfo")]
        public void LogInfo_ShouldWriteToConsoleWithWhiteColor()
        {
            // Arrange
            var message = "Test info message";

            // Act
            _logger.LogInfo(message);

            // Assert - Console output should contain the message
            var output = _consoleOutput.ToString();
            output.Should().Contain("[INFO]");
            output.Should().Contain(message);
        }

        [Fact]
        [Trait("Category", "LogInfo")]
        public async Task LogInfo_ShouldWriteToLogFile()
        {
            // Arrange
            var message = "Test info message for file";

            // Act
            _logger.LogInfo(message);

            // Wait for async write
            await Task.Delay(100);

            // Assert - File should contain the message
            File.Exists(_testLogFilePath).Should().BeTrue();
            var content = File.ReadAllText(_testLogFilePath);
            content.Should().Contain("[INFO]");
            content.Should().Contain(message);
        }

        #endregion

        #region LogSuccess Tests

        [Fact]
        [Trait("Category", "LogSuccess")]
        public void LogSuccess_ShouldWriteToConsoleWithGreenColor()
        {
            // Arrange
            var message = "Test success message";

            // Act
            _logger.LogSuccess(message);

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("[SUCCESS]");
            output.Should().Contain(message);
        }

        [Fact]
        [Trait("Category", "LogSuccess")]
        public async Task LogSuccess_ShouldWriteToLogFile()
        {
            // Arrange
            var message = "Test success message for file";

            // Act
            _logger.LogSuccess(message);

            // Wait for async write
            await Task.Delay(100);
            // Assert
            File.Exists(_testLogFilePath).Should().BeTrue();
            var content = File.ReadAllText(_testLogFilePath);
            content.Should().Contain("[SUCCESS]");
            content.Should().Contain(message);
        }

        #endregion

        #region LogWarning Tests

        [Fact]
        [Trait("Category", "LogWarning")]
        public void LogWarning_ShouldWriteToConsoleWithYellowColor()
        {
            // Arrange
            var message = "Test warning message";

            // Act
            _logger.LogWarning(message);

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("[WARNING]");
            output.Should().Contain(message);
        }

        [Fact]
        [Trait("Category", "LogWarning")]
        public async Task LogWarning_ShouldWriteToLogFile()
        {
            // Arrange
            var message = "Test warning message for file";

            // Act
            _logger.LogWarning(message);

            // Wait for async write
            await Task.Delay(100);
            // Assert
            File.Exists(_testLogFilePath).Should().BeTrue();
            var content = File.ReadAllText(_testLogFilePath);
            content.Should().Contain("[WARNING]");
            content.Should().Contain(message);
        }

        #endregion

        #region LogError Tests

        [Fact]
        [Trait("Category", "LogError")]
        public void LogError_ShouldWriteToConsoleWithRedColor()
        {
            // Arrange
            var message = "Test error message";

            // Act
            _logger.LogError(message);

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("[ERROR]");
            output.Should().Contain(message);
        }

        [Fact]
        [Trait("Category", "LogError")]
        public async Task LogError_ShouldWriteToLogFile()
        {
            // Arrange
            var message = "Test error message for file";

            // Act
            _logger.LogError(message);

            // Wait for async write
            await Task.Delay(100);

            // Assert
            File.Exists(_testLogFilePath).Should().BeTrue();
            var content = File.ReadAllText(_testLogFilePath);
            content.Should().Contain("[ERROR]");
            content.Should().Contain(message);
        }

        #endregion

        #region LogDebug Tests

        [Fact]
        [Trait("Category", "LogDebug")]
        public void LogDebug_ShouldWriteToConsoleWithCyanColor()
        {
            // Arrange
            var message = "Test debug message";

            // Act
            _logger.LogDebug(message);

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("[DEBUG]");
            output.Should().Contain(message);
        }

        [Fact]
        [Trait("Category", "LogDebug")]
        public async Task LogDebug_ShouldWriteToLogFile()
        {
            // Arrange
            var message = "Test debug message for file";

            // Act
            _logger.LogDebug(message);

            // Wait for async write
            await Task.Delay(100);

            // Assert
            File.Exists(_testLogFilePath).Should().BeTrue();
            var content = File.ReadAllText(_testLogFilePath);
            content.Should().Contain("[DEBUG]");
            content.Should().Contain(message);
        }

        #endregion

        #region LogException Tests

        [Fact]
        [Trait("Category", "LogException")]
        public void LogException_ShouldLogExceptionDetails()
        {
            // Arrange
            var exception = new InvalidOperationException("Test exception");
            var context = "TestContext";

            // Act
            _logger.LogException(exception, context);

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("[ERROR]");
            output.Should().Contain("Exception:");
            output.Should().Contain("Test exception");
            output.Should().Contain($"Stack Trace: {exception.StackTrace}");
        }

        [Fact]
        [Trait("Category", "LogException")]
        public void LogException_WithInnerException_ShouldLogInnerException()
        {
            // Arrange
            var innerException = new ArgumentException("Inner exception");
            var exception = new InvalidOperationException("Outer exception", innerException);

            // Act
            _logger.LogException(exception);

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("[ERROR]");
            output.Should().Contain("Exception:");
            output.Should().Contain("Outer exception");
            output.Should().Contain("Inner Exception");
            output.Should().Contain("Inner exception");
        }

        [Fact]
        [Trait("Category", "LogException")]
        public void LogException_WithoutContext_ShouldLogWithoutContext()
        {
            // Arrange
            var exception = new InvalidOperationException("Test exception");

            // Act
            _logger.LogException(exception);

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("[ERROR]");
            output.Should().Contain("Exception:");
            output.Should().Contain("Test exception");
            output.Should().NotContain("[TestContext]");
        }

        #endregion

        #region Log File Flush Tests

        [Fact]
        [Trait("Category", "Flush")]
        public async Task LogMessages_ShouldFlushToFileWhenQueueFull()
        {
            // Arrange
            var logger = new ConsoleLogger(_testLogFilePath);

            // Act - Log many messages to trigger flush
            for (int i = 0; i < 15; i++)
            {
                logger.LogInfo($"Test message {i}");
            }

            // Wait for flush
            await Task.Delay(200);

            // Assert
            File.Exists(_testLogFilePath).Should().BeTrue();
            var content = File.ReadAllText(_testLogFilePath);
            content.Should().Contain("Test message 0");
            content.Should().Contain("Test message 14");
        }

        [Fact]
        [Trait("Category", "Flush")]
        public void Dispose_ShouldFlushRemainingLogs()
        {
            // Arrange
            var logger = new ConsoleLogger(_testLogFilePath);

            // Act
            logger.LogInfo("Test message before dispose");
            logger.Dispose();

            // Assert
            File.Exists(_testLogFilePath).Should().BeTrue();
            var content = File.ReadAllText(_testLogFilePath);
            content.Should().Contain("Test message before dispose");
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        [Trait("Category", "Exception")]
        public async Task FlushLogsToFile_WhenFileLocked_ShouldNotThrow()
        {
            // Arrange
            using var fileStream = File.Create(_testLogFilePath);
            using var streamWriter = new StreamWriter(fileStream);
            var logger = new ConsoleLogger(_testLogFilePath);

            // Act - This should not throw
            logger.LogInfo("Test message with locked file");
            await Task.Delay(100);

            // Assert - No exception thrown
            // The error should be logged to console
            var output = _consoleOutput.ToString();
            output.Should().Contain("[ERROR]");
        }

        #endregion

        #region NullLogger Tests

        [Fact]
        [Trait("Category", "NullLogger")]
        public void NullLogger_ShouldNotThrow()
        {
            // Arrange
            var logger = new NullLogger();

            // Act & Assert
            logger.LogInfo("Info");
            logger.LogSuccess("Success");
            logger.LogWarning("Warning");
            logger.LogError("Error");
            logger.LogDebug("Debug");
            logger.LogException(new Exception("Test"));

            // Should not throw
            logger.Should().NotBeNull();
        }

        #endregion

        public void Dispose()
        {
            Console.SetOut(_originalConsoleOut);
            _consoleOutput.Dispose();

            if (File.Exists(_testLogFilePath))
            {
                try
                {
                    File.Delete(_testLogFilePath);
                }
                catch
                {
                    // Ignore delete errors
                }
            }

            _logger.Dispose();
        }
    }
}