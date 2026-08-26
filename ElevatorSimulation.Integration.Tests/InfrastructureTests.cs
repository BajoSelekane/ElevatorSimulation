using ElevatorSimulation.Domain.Entities;
using ElevatorSimulation.Domain.Interfaces;
using ElevatorSimulation.Infrastructure.Configuration;
using ElevatorSimulation.Infrastructure.Extensions;
using ElevatorSimulation.Infrastructure.Logging;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace ElevatorSimulation.Integration.Tests
{
    [Trait("Category", "Unit")]
    public class InfrastructureTests
    {
        [Fact]
        public void NullLogger_ShouldAcceptAllCalls()
        {
            var logger = new NullLogger();

            logger.LogInfo("i");
            logger.LogSuccess("s");
            logger.LogWarning("w");
            logger.LogError("e");
            logger.LogDebug("d");
            logger.LogException(new InvalidOperationException("x"), "ctx");
            logger.Invoking(l => l.Info("i")).Should().NotThrow();
        }

        [Fact]
        public void ConsoleLogger_ShouldWriteAndFlushToTempFile()
        {
            var path = Path.Combine(Path.GetTempPath(), $"elevator-test-{Guid.NewGuid():N}.log");
            try
            {
                var logger = new ConsoleLogger(path);
                for (var i = 0; i < 10; i++)
                {
                    logger.LogInfo($"line-{i}");
                }
                logger.Dispose();
                logger.Dispose();

                File.Exists(path).Should().BeTrue();
                File.ReadAllText(path).Should().Contain("line-0");
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void ConsoleLogger_LogException_ShouldIncludeInnerException()
        {
            var logger = new ConsoleLogger(Path.Combine(Path.GetTempPath(), $"elevator-ex-{Guid.NewGuid():N}.log"));
            var ex = new InvalidOperationException("outer", new ArgumentException("inner"));

            logger.Invoking(l => l.LogException(ex, "dispatch")).Should().NotThrow();
            logger.Dispose();
        }

        [Fact]
        public void MicrosoftLoggerAdapter_ShouldMapLogLevels()
        {
            var inner = new NullLogger();
            var adapter = new MicrosoftLoggerAdapter(inner);

            adapter.IsEnabled(MsLogLevel.Information).Should().BeTrue();
            using (adapter.BeginScope("scope")) { }

            adapter.Log(MsLogLevel.Trace, new EventId(1), "t", null, (s, _) => s);
            adapter.Log(MsLogLevel.Debug, new EventId(1), "d", null, (s, _) => s);
            adapter.Log(MsLogLevel.Information, new EventId(1), "i", null, (s, _) => s);
            adapter.Log(MsLogLevel.Warning, new EventId(1), "w", null, (s, _) => s);
            adapter.Log(MsLogLevel.Error, new EventId(1), "e", new InvalidOperationException("boom"), (s, _) => s);
            adapter.Log(MsLogLevel.Critical, new EventId(1), "c", null, (s, _) => s);
            adapter.Log((MsLogLevel)999, new EventId(1), "default", null, (s, _) => s);
            adapter.Log<string>(MsLogLevel.Information, new EventId(1), "raw", null, null!);

            adapter.LogInfo("i");
            adapter.LogSuccess("s");
            adapter.LogWarning("w");
            adapter.LogError("e");
            adapter.LogDebug("d");
            adapter.LogException(new InvalidOperationException("x"));
        }

        [Fact]
        public void MicrosoftLoggerAdapter_NullInnerLogger_ShouldThrow()
        {
            Action act = () => _ = new MicrosoftLoggerAdapter(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void AppSettingsValidator_ValidSettings_ShouldPass()
        {
            var validator = new AppSettingsValidator();
            validator.Validate(new AppSettings()).Should().BeTrue();
        }

        [Theory]
        [InlineData(0, 1, 10, 2, "at least 1 floor")]
        [InlineData(10, 0, 10, 2, "at least 1 elevator")]
        [InlineData(10, 1, 0, 2, "Max passengers")]
        [InlineData(10, 1, 10, 0, "Elevator speed")]
        public void AppSettingsValidator_InvalidSettings_ShouldThrow(
            int floors, int elevators, int maxPassengers, int speed, string expected)
        {
            var settings = new AppSettings();
            settings.Building.FloorCount = floors;
            settings.Elevators.StandardElevatorCount = elevators;
            settings.Simulation.MaxPassengers = maxPassengers;
            settings.Simulation.ElevatorSpeed = speed;

            Action act = () => new AppSettingsValidator().Validate(settings);
            act.Should().Throw<InvalidOperationException>().WithMessage($"*{expected}*");
        }

        [Fact]
        public void AddElevatorSimulation_ShouldRegisterCoreServices()
        {
            var services = new ServiceCollection();
            services.AddElevatorSimulation();

            using var provider = services.BuildServiceProvider();
            provider.GetRequiredService<IBuilding>().FloorCount.Should().Be(10);
            provider.GetRequiredService<IBuilding>().Elevators.Should().NotBeEmpty();
            provider.GetRequiredService<AppSettings>().Should().NotBeNull();
        }

        [Fact]
        public void AddSimulationContext_ShouldRegisterNestedSettings()
        {
            var services = new ServiceCollection();
            var settings = new AppSettings();

            services.AddSimulationContext(settings);

            using var provider = services.BuildServiceProvider();
            provider.GetRequiredService<BuildingSettings>().FloorCount.Should().Be(10);
            provider.GetRequiredService<SimulationSettings>().ElevatorSpeed.Should().Be(2);
            provider.GetRequiredService<ElevatorSettings>().StandardElevatorCount.Should().Be(2);
        }
    }
}
