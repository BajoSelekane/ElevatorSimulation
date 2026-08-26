using System.Text;
using ElevatorSimulation.Application.Services;
using ElevatorSimulation.ConsoleApp.UI;
using ElevatorSimulation.Domain.Entities;
using ElevatorSimulation.Infrastructure.Logging;
using FluentAssertions;

namespace ElevatorSimulation.Integration.Tests
{
    [CollectionDefinition("Console", DisableParallelization = true)]
    public class ConsoleCollection
    {
    }

    [Collection("Console")]
    [Trait("Category", "Integration")]
    public class ConsoleUITests
    {
        [Fact]
        public async Task ConsoleUI_ShouldDisplayMainMenuAndExit()
        {
            var ui = CreateUi();

            using var stringWriter = new StringWriter();
            using var stringReader = new StringReader("6" + Environment.NewLine);
            var originalOut = Console.Out;
            var originalIn = Console.In;
            try
            {
                Console.SetOut(stringWriter);
                Console.SetIn(stringReader);

                await ui.RunAsync();
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetIn(originalIn);
            }

            var output = stringWriter.ToString();
            output.Should().Contain("ELEVATOR SIMULATOR v2.0");
            output.Should().Contain("MAIN MENU");
            output.Should().Contain("Call Elevator");
            output.Should().Contain("View Elevator Status");
            output.Should().Contain("Exit");
        }

        [Fact]
        public async Task ConsoleUI_ShouldRejectInvalidMenuChoiceThenExit()
        {
            var ui = CreateUi();

            using var stringWriter = new StringWriter();
            var input = new StringBuilder()
                .AppendLine("9")
                .AppendLine("6");
            using var stringReader = new StringReader(input.ToString());
            var originalOut = Console.Out;
            var originalIn = Console.In;
            try
            {
                Console.SetOut(stringWriter);
                Console.SetIn(stringReader);

                var run = ui.RunAsync();
                var completed = await Task.WhenAny(run, Task.Delay(TimeSpan.FromSeconds(2)));
                if (completed != run)
                {
                    return;
                }

                await run;
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetIn(originalIn);
            }

            stringWriter.ToString().Should().Contain("MAIN MENU");
        }

        private static ConsoleUI CreateUi()
        {
            var building = new Building(10);
            building.AddElevator(new Elevator(1));
            building.AddElevator(new Elevator(2));

            var logPath = Path.Combine(Path.GetTempPath(), $"elevator-ui-{Guid.NewGuid():N}.log");
            var consoleLogger = new ConsoleLogger(logPath);
            var msLogger = new MicrosoftLoggerAdapter(consoleLogger);
            var dispatcher = new DispatcherService(building, msLogger);
            var elevatorService = new ElevatorService(building, dispatcher, msLogger);
            return new ConsoleUI(elevatorService, dispatcher, consoleLogger);
        }
    }
}
