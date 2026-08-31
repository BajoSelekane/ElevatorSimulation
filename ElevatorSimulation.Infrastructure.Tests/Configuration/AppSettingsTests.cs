using Xunit;
using FluentAssertions;
using ElevatorSimulation.Infrastructure.Configuration;
using System;
using System.Text.Json;

namespace ElevatorSimulation.Infrastructure.Tests.Configuration
{
    /// <summary>
    /// Comprehensive test suite for AppSettings with 100% code coverage
    /// </summary>
    public class AppSettingsTests
    {
        #region Default Values Tests

        [Fact]
        [Trait("Category", "Defaults")]
        public void AppSettings_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var settings = new AppSettings();

            // Assert
            settings.Building.Should().NotBeNull();
            settings.Building.FloorCount.Should().Be(10);
            settings.Building.BaseFloor.Should().Be(0);
            settings.Building.MaxFloor.Should().Be(10);

            settings.Simulation.Should().NotBeNull();
            settings.Simulation.ElevatorSpeed.Should().Be(2);
            settings.Simulation.DoorOpenTime.Should().Be(3);
            settings.Simulation.DoorCloseTime.Should().Be(2);
            settings.Simulation.MaxPassengers.Should().Be(10);
            settings.Simulation.PassengerWeightLimit.Should().Be(800);
            settings.Simulation.SimulationSpeed.Should().Be(100);
            settings.Simulation.EnableRealTime.Should().BeTrue();
            settings.Simulation.MaxQueueSize.Should().Be(100);

            settings.Logging.Should().NotBeNull();
            settings.Logging.EnableConsoleLogging.Should().BeTrue();
            settings.Logging.EnableFileLogging.Should().BeTrue();
            settings.Logging.LogFilePath.Should().Be("logs/elevator_simulation.log");
            settings.Logging.MaxLogFileSizeMB.Should().Be(10);
            settings.Logging.LogRetentionDays.Should().Be(30);
            settings.Logging.MinimumLogLevel.Should().Be(LogLevel.Info);

            settings.Elevators.Should().NotBeNull();
            settings.Elevators.StandardElevatorCount.Should().Be(2);
            settings.Elevators.HighSpeedElevatorCount.Should().Be(0);
            settings.Elevators.FreightElevatorCount.Should().Be(0);
            settings.Elevators.ExpressElevatorCount.Should().Be(0);
            settings.Elevators.DefaultMaxPassengers.Should().Be(10);
            settings.Elevators.HighSpeedMaxPassengers.Should().Be(15);
            settings.Elevators.FreightMaxPassengers.Should().Be(5);
            settings.Elevators.ExpressMaxPassengers.Should().Be(20);
        }

        #endregion

        #region JSON Serialization Tests

        [Fact]
        [Trait("Category", "Serialization")]
        public void AppSettings_ShouldSerializeToJson()
        {
            // Arrange
            var settings = new AppSettings
            {
                Building = new BuildingSettings
                {
                    FloorCount = 20,
                    BaseFloor = 0,
                    MaxFloor = 20
                },
                Simulation = new SimulationSettings
                {
                    ElevatorSpeed = 3,
                    MaxPassengers = 15
                },
                Logging = new LoggingSettings
                {
                    EnableConsoleLogging = false,
                    MinimumLogLevel = LogLevel.Debug
                },
                Elevators = new ElevatorSettings
                {
                    StandardElevatorCount = 3,
                    HighSpeedElevatorCount = 2
                }
            };

            // Act
            var json = JsonSerializer.Serialize(settings);
            var deserialized = JsonSerializer.Deserialize<AppSettings>(json);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized.Building.FloorCount.Should().Be(20);
            deserialized.Simulation.ElevatorSpeed.Should().Be(3);
            deserialized.Logging.EnableConsoleLogging.Should().BeFalse();
            deserialized.Elevators.StandardElevatorCount.Should().Be(3);
            deserialized.Elevators.HighSpeedElevatorCount.Should().Be(2);
        }

        [Fact]
        [Trait("Category", "Serialization")]
        public void AppSettings_WithNullValues_ShouldDeserializeCorrectly()
        {
            // Arrange
            var json = "{}";

            // Act
            var settings = JsonSerializer.Deserialize<AppSettings>(json);

            // Assert
            settings.Should().NotBeNull();
            settings.Building.Should().NotBeNull();
            settings.Simulation.Should().NotBeNull();
            settings.Logging.Should().NotBeNull();
            settings.Elevators.Should().NotBeNull();
        }

        #endregion

        #region BuildingSettings Tests

        [Fact]
        [Trait("Category", "Building")]
        public void BuildingSettings_ShouldSetProperties()
        {
            // Arrange
            var settings = new BuildingSettings
            {
                FloorCount = 15,
                BaseFloor = 1,
                MaxFloor = 15
            };

            // Act & Assert
            settings.FloorCount.Should().Be(15);
            settings.BaseFloor.Should().Be(1);
            settings.MaxFloor.Should().Be(15);
        }

        #endregion

        #region SimulationSettings Tests

        [Fact]
        [Trait("Category", "Simulation")]
        public void SimulationSettings_ShouldSetProperties()
        {
            // Arrange
            var settings = new SimulationSettings
            {
                ElevatorSpeed = 5,
                DoorOpenTime = 4,
                DoorCloseTime = 3,
                MaxPassengers = 20,
                PassengerWeightLimit = 1000,
                SimulationSpeed = 50,
                EnableRealTime = false,
                MaxQueueSize = 200
            };

            // Act & Assert
            settings.ElevatorSpeed.Should().Be(5);
            settings.DoorOpenTime.Should().Be(4);
            settings.DoorCloseTime.Should().Be(3);
            settings.MaxPassengers.Should().Be(20);
            settings.PassengerWeightLimit.Should().Be(1000);
            settings.SimulationSpeed.Should().Be(50);
            settings.EnableRealTime.Should().BeFalse();
            settings.MaxQueueSize.Should().Be(200);
        }

        #endregion

        #region LoggingSettings Tests

        [Fact]
        [Trait("Category", "Logging")]
        public void LoggingSettings_ShouldSetProperties()
        {
            // Arrange
            var settings = new LoggingSettings
            {
                EnableConsoleLogging = false,
                EnableFileLogging = false,
                LogFilePath = "custom.log",
                MaxLogFileSizeMB = 20,
                LogRetentionDays = 60,
                MinimumLogLevel = LogLevel.Debug
            };

            // Act & Assert
            settings.EnableConsoleLogging.Should().BeFalse();
            settings.EnableFileLogging.Should().BeFalse();
            settings.LogFilePath.Should().Be("custom.log");
            settings.MaxLogFileSizeMB.Should().Be(20);
            settings.LogRetentionDays.Should().Be(60);
            settings.MinimumLogLevel.Should().Be(LogLevel.Debug);
        }

        [Theory]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Info)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.None)]
        [Trait("Category", "Logging")]
        public void LogLevel_AllValues_ShouldBeValid(LogLevel level)
        {
            // Arrange & Act
            var settings = new LoggingSettings { MinimumLogLevel = level };

            // Assert
            settings.MinimumLogLevel.Should().Be(level);
        }

        #endregion

        #region ElevatorSettings Tests

        [Fact]
        [Trait("Category", "Elevators")]
        public void ElevatorSettings_ShouldSetProperties()
        {
            // Arrange
            var settings = new ElevatorSettings
            {
                StandardElevatorCount = 4,
                HighSpeedElevatorCount = 3,
                FreightElevatorCount = 2,
                ExpressElevatorCount = 1,
                DefaultMaxPassengers = 12,
                HighSpeedMaxPassengers = 18,
                FreightMaxPassengers = 8,
                ExpressMaxPassengers = 25
            };

            // Act & Assert
            settings.StandardElevatorCount.Should().Be(4);
            settings.HighSpeedElevatorCount.Should().Be(3);
            settings.FreightElevatorCount.Should().Be(2);
            settings.ExpressElevatorCount.Should().Be(1);
            settings.DefaultMaxPassengers.Should().Be(12);
            settings.HighSpeedMaxPassengers.Should().Be(18);
            settings.FreightMaxPassengers.Should().Be(8);
            settings.ExpressMaxPassengers.Should().Be(25);
        }

        #endregion

        #region AppSettingsValidator Tests

        [Fact]
        [Trait("Category", "Validator")]
        public void AppSettingsValidator_ValidSettings_ShouldReturnTrue()
        {
            // Arrange
            var settings = new AppSettings();
            var validator = new AppSettingsValidator();

            // Act
            var result = validator.Validate(settings);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        [Trait("Category", "Validator")]
        public void AppSettingsValidator_InvalidFloorCount_ShouldThrowException()
        {
            // Arrange
            var settings = new AppSettings
            {
                Building = new BuildingSettings { FloorCount = 0 }
            };
            var validator = new AppSettingsValidator();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                validator.Validate(settings));
            exception.Message.Should().Contain("at least 1 floor");
        }

        [Fact]
        [Trait("Category", "Validator")]
        public void AppSettingsValidator_NoElevators_ShouldThrowException()
        {
            // Arrange
            var settings = new AppSettings
            {
                Elevators = new ElevatorSettings
                {
                    StandardElevatorCount = 0,
                    HighSpeedElevatorCount = 0,
                    FreightElevatorCount = 0,
                    ExpressElevatorCount = 0
                }
            };
            var validator = new AppSettingsValidator();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                validator.Validate(settings));
            exception.Message.Should().Contain("at least 1 elevator");
        }

        [Fact]
        [Trait("Category", "Validator")]
        public void AppSettingsValidator_InvalidMaxPassengers_ShouldThrowException()
        {
            // Arrange
            var settings = new AppSettings
            {
                Simulation = new SimulationSettings { MaxPassengers = 0 }
            };
            var validator = new AppSettingsValidator();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                validator.Validate(settings));
            exception.Message.Should().Contain("Max passengers must be at least 1");
        }

        [Fact]
        [Trait("Category", "Validator")]
        public void AppSettingsValidator_InvalidElevatorSpeed_ShouldThrowException()
        {
            // Arrange
            var settings = new AppSettings
            {
                Simulation = new SimulationSettings { ElevatorSpeed = 0 }
            };
            var validator = new AppSettingsValidator();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                validator.Validate(settings));
            exception.Message.Should().Contain("Elevator speed must be at least 1");
        }

        #endregion

        #region Configuration File Tests

        [Fact]
        [Trait("Category", "Configuration")]
        public void AppSettings_WithCustomConfiguration_ShouldLoadCorrectly()
        {
            // Arrange
            var json = @"
            {
                ""Building"": {
                    ""FloorCount"": 25,
                    ""BaseFloor"": 0,
                    ""MaxFloor"": 25
                },
                ""Simulation"": {
                    ""ElevatorSpeed"": 4,
                    ""MaxPassengers"": 12,
                    ""EnableRealTime"": true
                },
                ""Elevators"": {
                    ""StandardElevatorCount"": 4,
                    ""HighSpeedElevatorCount"": 2
                }
            }";

            // Act
            var settings = JsonSerializer.Deserialize<AppSettings>(json);

            // Assert
            settings.Should().NotBeNull();
            settings.Building.FloorCount.Should().Be(25);
            settings.Simulation.ElevatorSpeed.Should().Be(4);
            settings.Simulation.MaxPassengers.Should().Be(12);
            settings.Elevators.StandardElevatorCount.Should().Be(4);
            settings.Elevators.HighSpeedElevatorCount.Should().Be(2);
        }

        #endregion
    }
}