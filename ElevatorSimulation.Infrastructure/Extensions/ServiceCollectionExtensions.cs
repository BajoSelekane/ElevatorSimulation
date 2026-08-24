using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ElevatorSimulation.Infrastructure.Logging;
using ElevatorSimulation.Infrastructure.Configuration;
using ElevatorSimulation.Domain.Interfaces;
using ElevatorSimulation.Domain.Entities;
using ElevatorSimulation.Application.Interfaces;
using ElevatorSimulation.Application.Services;
using ElevatorSimulation.Application.Validators;
using FluentValidation;

namespace ElevatorSimulation.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElevatorSimulation(this IServiceCollection services,
            IConfiguration configuration = null)
        {
            // Configuration
            var settings = new AppSettings();
            if (configuration != null)
            {
                configuration.Bind(settings);
            }
            services.AddSingleton(settings);

            // Validate settings
            var validator = new AppSettingsValidator();
            validator.Validate(settings);

            // Logging
            services.AddSingleton<ILogger, ConsoleLogger>();
            // Provide Microsoft.Extensions.Logging.ILogger via adapter so application services
            // that depend on the Microsoft logging abstraction can be resolved.
            services.AddSingleton<Microsoft.Extensions.Logging.ILogger>(sp =>
            {
                var ourLogger = sp.GetRequiredService<ILogger>();
                return new ElevatorSimulation.Infrastructure.Logging.MicrosoftLoggerAdapter(ourLogger);
            });

            // Domain
            services.AddSingleton<IBuilding>(sp =>
            {
                var building = new Building(settings.Building.FloorCount);
                var elevatorCount = settings.Elevators.StandardElevatorCount;

                for (int i = 1; i <= elevatorCount; i++)
                {
                    var elevator = new Elevator(i, Domain.Enums.ElevatorType.Standard,
                        settings.Elevators.DefaultMaxPassengers);
                    building.AddElevator(elevator);
                }

                // Add high-speed elevators
                for (int i = 1; i <= settings.Elevators.HighSpeedElevatorCount; i++)
                {
                    var id = elevatorCount + i;
                    var elevator = new Elevator(id, Domain.Enums.ElevatorType.HighSpeed,
                        settings.Elevators.HighSpeedMaxPassengers);
                    building.AddElevator(elevator);
                }

                return building;
            });

            // Application Services
            services.AddScoped<IDispatcherService, DispatcherService>();
            services.AddScoped<IElevatorService, ElevatorService>();

            // Validators
            services.AddScoped<FloorRequestValidator>();
            services.AddScoped<ElevatorRequestValidator>();
            services.AddScoped<PassengerRequestValidator>();
            services.AddScoped<BuildingValidator>();
            services.AddScoped<ElevatorValidator>();
            services.AddScoped<PassengerValidator>();

            // FluentValidation
            services.AddValidatorsFromAssemblyContaining<FloorRequestValidator>();

            return services;
        }

        public static IServiceCollection AddSimulationContext(this IServiceCollection services,
            AppSettings settings)
        {
            // Add simulation-specific services
            services.AddSingleton(settings.Simulation);
            services.AddSingleton(settings.Building);
            services.AddSingleton(settings.Elevators);

            return services;
        }
    }
}