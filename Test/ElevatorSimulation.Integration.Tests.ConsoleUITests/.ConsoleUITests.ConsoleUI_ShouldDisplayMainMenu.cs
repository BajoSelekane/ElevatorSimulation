using FluentAssertions;
using ElevatorSimulation.Application.Services;
using ElevatorSimulation.Domain.Entities;
using ElevatorSimulation.Infrastructure.Logging;
using System.Text;
using ElevatorSimulation.ConsoleApp.UI;

namespace ElevatorSimulation.Integration.Tests
{
    public class ConsoleUITests
    {
        [Fact]
        public async Task ConsoleUI_ShouldDisplayMainMenu()
        {
            // Arrange
            var building = new Building(10);
            building.AddElevator(new Elevator(1));
            building.AddElevator(new Elevator(2));

            var consoleLogger = new ConsoleLogger();
            var msLogger = new ElevatorSimulation.Infrastructure.Logging.MicrosoftLoggerAdapter(consoleLogger);
            var dispatcher = new DispatcherService(building, msLogger);
            var elevatorService = new ElevatorService(building, dispatcher, msLogger);

            var ui = new ConsoleUI(elevatorService, dispatcher, consoleLogger);

            using var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            // Act - Simulate user choosing to exit immediately
            using var stringReader = new StringReader("6\n");
            Console.SetIn(stringReader);

            await ui.RunAsync();

            // Assert
            var output = stringWriter.ToString();
            output.Should().Contain("ELEVATOR SIMULATOR v2.0");
            output.Should().Contain("MAIN MENU");
            output.Should().Contain("Call Elevator");
            output.Should().Contain("View Elevator Status");
            output.Should().Contain("Exit");
        }

        [Theory]
        [InlineData("1", "5")]
        [InlineData("2")]
        [InlineData("3")]
        public async Task ConsoleUI_ShouldHandleDifferentMenuOptions(string menuChoice, string? extraInput = null)
        {
            // Arrange
            var building = new Building(10);
            building.AddElevator(new Elevator(1));
            building.AddElevator(new Elevator(2));

            var consoleLogger = new ConsoleLogger();
            var msLogger = new ElevatorSimulation.Infrastructure.Logging.MicrosoftLoggerAdapter(consoleLogger);
            var dispatcher = new DispatcherService(building, msLogger);
            var elevatorService = new ElevatorService(building, dispatcher, msLogger);

            var ui = new ConsoleUI(elevatorService, dispatcher, consoleLogger);

            using var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            // Build input string
            var inputBuilder = new StringBuilder();
            inputBuilder.AppendLine(menuChoice);
            if (!string.IsNullOrEmpty(extraInput))
            {
                inputBuilder.AppendLine(extraInput);
            }
            inputBuilder.AppendLine("6"); // Exit

            using var stringReader = new StringReader(inputBuilder.ToString());
            Console.SetIn(stringReader);

            // Act
            await ui.RunAsync();

            // Assert
            var output = stringWriter.ToString();
            output.Should().NotBeEmpty();
            // We can't test exact content as it depends on simulation state
            // But we verify the UI runs without crashing
        }
    }
}
