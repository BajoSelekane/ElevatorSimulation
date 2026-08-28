using Xunit;
using FluentAssertions;
using ElevatorSimulation.Application.DTOs;
using ElevatorSimulation.Application.Services;
using ElevatorSimulation.Domain.Entities;
using ElevatorSimulation.Domain.Enums;
using ElevatorSimulation.Infrastructure.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ElevatorSimulation.Integration.Tests
{
    public class SimulationIntegrationTests : IDisposable
    {
        private readonly Building _building;
        private readonly DispatcherService _dispatcher;
        private readonly ElevatorService _elevatorService;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        public SimulationIntegrationTests()
        {
            var customLogger = new ConsoleLogger(); // your existing implementation
            var msLogger = new ElevatorSimulation.Infrastructure.Logging.MicrosoftLoggerAdapter(customLogger);
            _logger = msLogger;
            _building = new Building(10);

            // Add elevators
            _building.AddElevator(new Elevator(1, ElevatorType.Standard, 10));
            _building.AddElevator(new Elevator(2, ElevatorType.HighSpeed, 15));
            _building.AddElevator(new Elevator(3, ElevatorType.Standard, 10));

            _dispatcher = new DispatcherService(_building, _logger);
            _elevatorService = new ElevatorService(_building, _dispatcher, _logger);
        }

        [Fact]
        public void FullDispatchFlow_ShouldWorkCorrectly()
        {
            // Arrange - Call elevator to floor 5 with 2 passengers
            var request = new FloorRequestDto
            {
                FloorNumber = 5,
                PassengerCount = 2,
                RequestType = "Call"
            };

            // Act
            var dispatchResult = _elevatorService.CallElevator(request);

            // Assert
            dispatchResult.Success.Should().BeTrue();
            dispatchResult.Elevator.Should().NotBeNull();

            var elevator = _building.GetElevator(dispatchResult.Elevator.ElevatorId);
            elevator.CurrentFloor.Should().Be(5);
            elevator.Status.Should().Be(ElevatorStatus.Stationary);

            // Arrange - Add passengers
            var passengerRequest = new PassengerRequestDto
            {
                Id = 1,
                CurrentFloor = 5,
                DestinationFloor = 8,
                Weight = 70
            };

            // Act
            var passengerResult = _elevatorService.AddPassenger(passengerRequest);

            // Assert
            passengerResult.Success.Should().BeTrue();
            elevator.PassengerCount.Should().Be(1);
            elevator.DestinationQueue.Should().Contain(8);

            // Arrange - Move elevator to destination
            // Act
            var moved = _elevatorService.ProcessNextDestination(elevator.Id);

            // Assert
            moved.Should().BeTrue();
            elevator.CurrentFloor.Should().Be(8);
            elevator.PassengerCount.Should().Be(0); // Passenger alighted
        }

        [Fact]
        public void BuildingStatus_ShouldReflectCurrentState()
        {
            // Arrange - Setup some state
            var floorRequest = new FloorRequestDto { FloorNumber = 3, PassengerCount = 1 };
            _elevatorService.CallElevator(floorRequest);

            var passengerRequest = new PassengerRequestDto
            {
                Id = 1,
                CurrentFloor = 3,
                DestinationFloor = 7,
                Weight = 70
            };
            _elevatorService.AddPassenger(passengerRequest);

            // Act
            var status = _elevatorService.GetBuildingStatus();

            // Assert
            status.Should().NotBeNull();
            status.FloorCount.Should().Be(10);
            status.ElevatorCount.Should().Be(3);
            status.Elevators.Should().HaveCount(3);
            status.PassengersPerFloor.Should().NotBeNull();
        }

        [Fact]
        public void MultipleElevatorDispatch_ShouldBalanceLoad()
        {
            // Arrange - Dispatch 5 elevators to different floors
            for (int i = 1; i <= 5; i++)
            {
                var request = new FloorRequestDto
                {
                    FloorNumber = i * 2,
                    PassengerCount = 1
                };
                _elevatorService.CallElevator(request);
            }

            // Act
            var status = _elevatorService.GetBuildingStatus();

            // Assert
            // Check that work is distributed
            var elevatorsUsed = status.Elevators.Where(e => e.TotalTrips > 0).ToList();
            elevatorsUsed.Count.Should().BeGreaterThan(1);
        }

        [Fact]
        public void CapacityLimit_ShouldBeEnforced()
        {
            // Arrange - Elevator with max 2 passengers
            var elevator = new Elevator(4, ElevatorType.Standard, 2);
            _building.AddElevator(elevator);

            // Act - Board 2 passengers
            var passenger1 = new Passenger(1, 0, 5);
            var passenger2 = new Passenger(2, 0, 6);
            elevator.BoardPassenger(passenger1);
            elevator.BoardPassenger(passenger2);

            // Assert - Can't board third
            var passenger3 = new Passenger(3, 0, 7);
            Action act = () => elevator.BoardPassenger(passenger3);
            act.Should().Throw<Domain.Exceptions.CapacityExceededException>();
            elevator.PassengerCount.Should().Be(2);
        }

        [Fact]
        public async Task RealTimeSimulation_ShouldProcessEvents()
        {
            // Arrange
            var taskCompletionSource = new TaskCompletionSource<bool>();

            // Setup event handlers
            var elevator = _building.GetElevator(1);
            elevator.ElevatorMoved += (sender, args) =>
            {
                if (args.FloorNumber == 5)
                {
                    taskCompletionSource.TrySetResult(true);
                }
            };

            // Act
            elevator.AddDestination(5);
            elevator.MoveToNextDestination();

            // Assert
            var result = await Task.WhenAny(taskCompletionSource.Task, Task.Delay(5000));
            result.Should().Be(taskCompletionSource.Task);
            elevator.CurrentFloor.Should().Be(5);
        }

        [Fact]
        public void DispatcherStatistics_ShouldTrackPerformance()
        {
            // Arrange - Make some dispatches
            for (int i = 0; i < 10; i++)
            {
                var request = new FloorRequestDto
                {
                    FloorNumber = i % 10,
                    PassengerCount = new Random().Next(1, 3)
                };
                _elevatorService.CallElevator(request);
            }

            // Act
            var stats = _dispatcher.GetDispatchStatistics();

            // Assert
            stats.TotalCalls.Should().Be(10);
            stats.SuccessfulDispatch.Should().Be(10);
            stats.SystemEfficiency.Should().Be(100);
            stats.CallsPerFloor.Values.Sum().Should().Be(10);
        }

        [Fact]
        public void ElevatorService_GetNearestElevator_ShouldReturnCorrect()
        {
            // Arrange - Position elevators
            var elevator1 = _building.GetElevator(1);
            elevator1.MoveToFloor(2);

            var elevator2 = _building.GetElevator(2);
            elevator2.MoveToFloor(8);

            var elevator3 = _building.GetElevator(3);
            elevator3.MoveToFloor(4);

            // Act
            var nearest = _elevatorService.GetNearestElevatorStatus(5);

            // Assert
            nearest.Should().NotBeNull();
            nearest.ElevatorId.Should().Be(3); // Closest to floor 5
        }

        [Fact]
        public void ResetAllElevators_ShouldResetState()
        {
            // Arrange
            var elevator = _building.GetElevator(1);
            elevator.MoveToFloor(5);
            elevator.AddDestination(7);
            elevator.AddDestination(9);

            // Act
            _elevatorService.ResetAllElevators();

            // Assert
            elevator.CurrentFloor.Should().Be(0);
            elevator.DestinationQueue.Should().BeEmpty();
            elevator.Status.Should().Be(ElevatorStatus.Stationary);
        }

        [Fact]
        public void ProcessingQueue_WhenElevatorOutOfService_ShouldNotProcess()
        {
            // Arrange
            var elevator = _building.GetElevator(1);
            elevator.AddDestination(5);
            elevator.SetOutOfService();

            // Act
            var result = _elevatorService.ProcessNextDestination(1);

            // Assert
            result.Should().BeFalse();
            elevator.CurrentFloor.Should().Be(0);
            elevator.DestinationQueue.Should().Contain(5);
        }

        public void Dispose()
        {
            if (_logger is IDisposable disposableLogger)
            {
                disposableLogger.Dispose();
            }
        }
    }
}