using System.Text.Json.Serialization;

namespace ElevatorSimulation.Infrastructure.Configuration
{
    public class AppSettings
    {
        public BuildingSettings Building { get; set; } = new BuildingSettings();
        public SimulationSettings Simulation { get; set; } = new SimulationSettings();
        public LoggingSettings Logging { get; set; } = new LoggingSettings();
        public ElevatorSettings Elevators { get; set; } = new ElevatorSettings();
    }

    public class BuildingSettings
    {
        public int FloorCount { get; set; } = 10;
        public int BaseFloor { get; set; } = 0;
        public int MaxFloor { get; set; } = 10;
    }

    public class SimulationSettings
    {
        public int ElevatorSpeed { get; set; } = 2; // seconds per floor
        public int DoorOpenTime { get; set; } = 3; // seconds
        public int DoorCloseTime { get; set; } = 2; // seconds
        public int MaxPassengers { get; set; } = 10;
        public int PassengerWeightLimit { get; set; } = 800; // kg
        public int SimulationSpeed { get; set; } = 100; // milliseconds per tick
        public bool EnableRealTime { get; set; } = true;
        public int MaxQueueSize { get; set; } = 100;
    }

    public class LoggingSettings
    {
        public bool EnableConsoleLogging { get; set; } = true;
        public bool EnableFileLogging { get; set; } = true;
        public string LogFilePath { get; set; } = "logs/elevator_simulation.log";
        public int MaxLogFileSizeMB { get; set; } = 10;
        public int LogRetentionDays { get; set; } = 30;
        public LogLevel MinimumLogLevel { get; set; } = LogLevel.Info;
    }

    public class ElevatorSettings
    {
        public int StandardElevatorCount { get; set; } = 2;
        public int HighSpeedElevatorCount { get; set; } = 0;
        public int FreightElevatorCount { get; set; } = 0;
        public int ExpressElevatorCount { get; set; } = 0;
        public int DefaultMaxPassengers { get; set; } = 10;
        public int HighSpeedMaxPassengers { get; set; } = 15;
        public int FreightMaxPassengers { get; set; } = 5;
        public int ExpressMaxPassengers { get; set; } = 20;
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        None
    }

    public class AppSettingsValidator
    {
        public bool Validate(AppSettings settings)
        {
            if (settings.Building.FloorCount < 1)
                throw new InvalidOperationException("Building must have at least 1 floor.");

            if (settings.Elevators.StandardElevatorCount < 1)
                throw new InvalidOperationException("Building must have at least 1 elevator.");

            if (settings.Simulation.MaxPassengers < 1)
                throw new InvalidOperationException("Max passengers must be at least 1.");

            if (settings.Simulation.ElevatorSpeed < 1)
                throw new InvalidOperationException("Elevator speed must be at least 1 second per floor.");

            return true;
        }
    }
}