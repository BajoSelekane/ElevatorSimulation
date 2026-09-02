using FluentAssertions;
using ElevatorSimulation.Application.Services;
using ElevatorSimulation.Domain.Entities;
using ElevatorSimulation.Infrastructure.Logging;
using System.Text;
using ElevatorSimulation.ConsoleApp.UI;
using Xunit;

namespace ElevatorSimulation.Integration.Tests
{

    public class ConsoleUITests
    {
        /// <summary>
        /// This stops infinite validation loops from consuming all memory via logging.
        /// </summary>
        private class SafetyTextReader : TextReader
        {
            private readonly TextReader _inner;
            private int _nullReadCount;
            private readonly int _maxNullReads;

            public SafetyTextReader(TextReader inner, int maxNullReads = 0)
            {
                _inner = inner;
                _maxNullReads = maxNullReads;
            }

            public override string ReadLine()
            {
                var line = _inner.ReadLine();
                if (line == null)
                {
                    if (++_nullReadCount > _maxNullReads)
                        throw new InvalidOperationException(
                            "Test input exhausted. The UI is reading more console input than provided. " +
                            "This usually means a validation loop is retrying on EOF. " +
                            "Provide more test inputs or add a max-retry limit in the UI code.");
                }
                else
                {
                    _nullReadCount = 0;
                }
                return line;
            }

            public override int Read()
            {
                int ch = _inner.Read();
                if (ch == -1)
                {
                    if (++_nullReadCount > _maxNullReads)
                        throw new InvalidOperationException(
                            "Test input exhausted during ReadKey/Read operation.");
                }
                else
                {
                    _nullReadCount = 0;
                }
                return ch;
            }
        }

        [Theory]
        // Menu 1 (Call Elevator): needs floor number + passenger count
        [InlineData("1", "5\n2\n")]
        // Menu 2 (Status / Report): usually needs no extra line input
        [InlineData("2", "")]
        // Menu 3 (Add Passenger): needs floor number + passenger count
        [InlineData("3", "5\n2\n")]
        public async Task ConsoleUI_ShouldHandleDifferentMenuOptions(string menuChoice, string extraInputs)
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

            // Build the complete input queue:
            // 1. The menu choice
            // 2. All extra inputs the specific handler needs (floor, passengers, etc.)
            // 3. Multiple "6" commands — one may be eaten by Console.ReadKey(),
            //    another consumed as an empty menu cycle, and the final one exits cleanly.
            var inputBuilder = new StringBuilder();
            inputBuilder.AppendLine(menuChoice);

            if (!string.IsNullOrEmpty(extraInputs))
            {
                inputBuilder.Append(extraInputs);
                // Ensure the last extra input ends with a newline so ReadLine consumes it fully
                if (!extraInputs.EndsWith("\n"))
                    inputBuilder.AppendLine();
            }

            // Redundant exit commands act as both ReadKey characters and menu choices
            inputBuilder.AppendLine("6");
            inputBuilder.AppendLine("6");
            inputBuilder.AppendLine("6");

            using var innerReader = new StringReader(inputBuilder.ToString());
            using var safetyReader = new SafetyTextReader(innerReader, maxNullReads: 2);
            Console.SetIn(safetyReader);

            // Act
            await ui.RunAsync();

            // Assert
            var output = stringWriter.ToString();
            output.Should().NotBeEmpty();
        }
    }
}
    
