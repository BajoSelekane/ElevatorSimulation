using Microsoft.Extensions.DependencyInjection;
using ElevatorSimulation.Application.Services;
using ElevatorSimulation.Application.Interfaces;
using ElevatorSimulation.ConsoleApp.UI;
using ElevatorSimulation.Infrastructure.Logging;
using ElevatorSimulation.Domain.Entities;
using ElevatorSimulation.Domain.Interfaces;
using System;


namespace ElevatorSimulation.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Title = "Elevator Simulation System v2.0";
            Console.WindowWidth = 120;
            Console.WindowHeight = 40;
            

            try
            {
                var serviceProvider = ConfigureServices();
                var ui = serviceProvider.GetService<ConsoleUI>();

                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    Environment.Exit(0);
                };

                await ui.RunAsync();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Fatal Error: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
        }

        private static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Domain
            var building = new Building(10);
            var logger = new ConsoleLogger();

            // Register building with elevators
            for (int i = 1; i <= 3; i++)
            {
                var elevator = new Elevator(i);
                building.AddElevator(elevator);
            }

            services.AddSingleton(building);
            services.AddSingleton<IBuilding>(sp => building);
            services.AddSingleton<ILogger, ConsoleLogger>();
            // also register Microsoft.Extensions.Logging.ILogger adapter for services that require it
            services.AddSingleton<Microsoft.Extensions.Logging.ILogger>(sp =>
            {
                var ourLogger = sp.GetRequiredService<ElevatorSimulation.Infrastructure.Logging.ILogger>();
                return new ElevatorSimulation.Infrastructure.Logging.MicrosoftLoggerAdapter(ourLogger);
            });
            services.AddSingleton<IConsoleLogger, ConsoleLogger>();
            services.AddSingleton<IElevatorService, ElevatorService>();
            services.AddSingleton<IDispatcherService, DispatcherService>();
            services.AddSingleton<ConsoleUI>();

            return services.BuildServiceProvider();
        }
    }
}