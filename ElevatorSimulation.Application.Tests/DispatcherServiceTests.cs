using Xunit;
using FluentAssertions;
using Moq;
using ElevatorSimulation.Application.DTOs;
using ElevatorSimulation.Application.Services;
using ElevatorSimulation.Domain.Entities;
using ElevatorSimulation.Domain.Enums;
using ElevatorSimulation.Domain.Interfaces;
using ElevatorSimulation.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace ElevatorSimulation.Application.Tests
{
    [Trait("Category", "Unit")]
    public class DispatcherServiceTests
    {
        private readonly Mock<IBuilding> _mockBuilding;
        private readonly Mock<ILogger> _mockLogger;
        private readonly DispatcherService _dispatcher;

        public DispatcherServiceTests()
        {
            _mockBuilding = new Mock<IBuilding>();
            _mockLogger = new Mock<ILogger>();
            _dispatcher = new DispatcherService(_mockBuilding.Object, _mockLogger.Object);
        }

        [Fact]
        public void DispatchElevator_WithAvailableElevator_ShouldReturnNearest()
        {
            // Arrange
            var elevator1 = new Elevator(1) { CurrentFloor = 5 };
            var elevator2 = new Elevator(2) { CurrentFloor = 2 };
            var elevators = new List<IElevator> { elevator1, elevator2 };

            _mockBuilding.Setup(b => b.GetElevators()).Returns(elevators);
            _mockBuilding.Setup(b => b.IsValidFloor(3)).Returns(true);
            _mockBuilding.Setup(b => b.FloorCount).Returns(10);

            var request = new FloorRequestDto { FloorNumber = 3, PassengerCount = 1 };

            // Act
            var result = _dispatcher.DispatchElevator(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Elevator.ElevatorId.Should().Be(2); // Closest to floor 3
            result.Message.Should().Contain("Elevator 2 dispatched");
        }

        [Fact]
        public void DispatchElevator_WithNoAvailableElevator_ShouldReturnFailure()
        {
            // Arrange
            _mockBuilding.Setup(b => b.GetElevators()).Returns(new List<IElevator>());
            _mockBuilding.Setup(b => b.IsValidFloor(3)).Returns(true);

            var request = new FloorRequestDto { FloorNumber = 3, PassengerCount = 1 };

            // Act
            var result = _dispatcher.DispatchElevator(request);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("No elevators available");
        }

        [Fact]
        public void DispatchElevator_WithElevatorAtCapacity_ShouldReturnFailure()
        {
            // Arrange
            var elevator = new Elevator(1, maxPassengers: 1);
            elevator.BoardPassenger(new Passenger(1, 0, 5));

            var elevators = new List<IElevator> { elevator };
            _mockBuilding.Setup(b => b.GetElevators()).Returns(elevators);
            _mockBuilding.Setup(b => b.IsValidFloor(3)).Returns(true);
            _mockBuilding.Setup(b => b.FloorCount).Returns(10);

            var request = new FloorRequestDto { FloorNumber = 3, PassengerCount = 2 };

            // Act
            var result = _dispatcher.DispatchElevator(request);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("at capacity");
            result.Priority.Should().Be(1);
        }

        [Fact]
        public void DispatchElevator_WithMultiplePassengers_ShouldDispatchToAppropriateElevator()
        {
            // Arrange
            var elevator1 = new Elevator(1) { CurrentFloor = 5 };
            var elevator2 = new Elevator(2) { CurrentFloor = 2 };
            var elevators = new List<IElevator> { elevator1, elevator2 };

            _mockBuilding.Setup(b => b.GetElevators()).Returns(elevators);
            _mockBuilding.Setup(b => b.IsValidFloor(3)).Returns(true);
            _mockBuilding.Setup(b => b.FloorCount).Returns(10);

            var request = new FloorRequestDto { FloorNumber = 3, PassengerCount = 3 };

            // Act
            var result = _dispatcher.DispatchElevator(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Elevator.ElevatorId.Should().Be(2);
        }

        [Fact]
        public void AssignPassengerToElevator_ValidPassenger_ShouldAssign()
        {
            // Arrange
            var elevator = new Elevator(1);
            var elevators = new List<IElevator> { elevator };
            _mockBuilding.Setup(b => b.GetElevators()).Returns(elevators);
            _mockBuilding.Setup(b => b.IsValidFloor(5)).Returns(true);
            _mockBuilding.Setup(b => b.IsValidFloor(10)).Returns(true);
            _mockBuilding.Setup(b => b.FloorCount).Returns(10);

            var request = new PassengerRequestDto
            {
                Id = 123,
                CurrentFloor = 5,
                DestinationFloor = 10,
                Weight = 70
            };

            // Act
            var result = _dispatcher.AssignPassengerToElevator(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Contain("Passenger assigned to elevator 1");
            elevator.PassengerCount.Should().Be(1);
            elevator.DestinationQueue.Should().Contain(10);
        }

        [Fact]
        public void GetNearestAvailableElevator_ShouldReturnClosest()
        {
            // Arrange
            var elevator1 = new Elevator(1) { CurrentFloor = 1 };
            var elevator2 = new Elevator(2) { CurrentFloor = 8 };
            var elevator3 = new Elevator(3) { CurrentFloor = 4 };
            var elevators = new List<IElevator> { elevator1, elevator2, elevator3 };

            _mockBuilding.Setup(b => b.GetElevators()).Returns(elevators);
            _mockBuilding.Setup(b => b.FloorCount).Returns(10);

            // Act
            var result = _dispatcher.GetNearestAvailableElevator(5);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(3); // Closest to floor 5
        }

        [Fact]
        public void IsElevatorAvailableForFloor_ShouldReturnCorrect()
        {
            var elevator = new Elevator(1);
            _mockBuilding.Setup(b => b.GetElevator(1)).Returns(elevator);
            _mockBuilding.Setup(b => b.FloorCount).Returns(10);

            _dispatcher.IsElevatorAvailableForFloor(1, 5).Should().BeTrue();

            elevator.SetOutOfService();
            _dispatcher.IsElevatorAvailableForFloor(1, 5).Should().BeFalse();
        }

        [Fact]
        public void IsElevatorAvailableForFloor_WhenElevatorMissing_ShouldReturnFalse()
        {
            _mockBuilding.Setup(b => b.GetElevator(9)).Throws(new ElevatorNotFoundException("missing"));

            _dispatcher.IsElevatorAvailableForFloor(9, 1).Should().BeFalse();
        }

        [Fact]
        public void CalculateEstimatedWaitTime_ShouldCalculateCorrectly()
        {
            // Arrange
            var elevator = new Elevator(1) { CurrentFloor = 5 };
            elevator.AddDestination(7);
            elevator.AddDestination(8);

            var elevators = new List<IElevator> { elevator };
            _mockBuilding.Setup(b => b.GetElevators()).Returns(elevators);
            _mockBuilding.Setup(b => b.FloorCount).Returns(10);

            // Act
            var waitTime = _dispatcher.CalculateEstimatedWaitTime(3);

            // Assert
            waitTime.Should().BeGreaterThan(0);
            // Expected: distance(2) * 2 = 4 + queue(2) * 3 = 6 + passenger(0) * 1 = 0 + 5 = 15
            waitTime.Should().Be(15);
        }

        [Fact]
        public void ProcessElevatorQueue_ShouldProcessNextDestination()
        {
            // Arrange
            var elevator = new Elevator(1);
            elevator.AddDestination(5);
            _mockBuilding.Setup(b => b.GetElevator(1)).Returns(elevator);

            // Act
            _dispatcher.ProcessElevatorQueue(1);

            // Assert
            elevator.CurrentFloor.Should().Be(5);
            elevator.DestinationQueue.Should().BeEmpty();
        }

        [Fact]
        public void GetElevatorsServingFloor_ShouldReturnCorrectElevators()
        {
            // Arrange
            var elevator1 = new Elevator(1);
            elevator1.AddDestination(5);
            var elevator2 = new Elevator(2);
            elevator2.MoveToFloor(5);

            var elevators = new List<IElevator> { elevator1, elevator2, new Elevator(3) };
            _mockBuilding.Setup(b => b.GetElevators()).Returns(elevators);

            // Act
            var result = _dispatcher.GetElevatorsServingFloor(5);

            // Assert
            result.Should().HaveCount(2);
            result.Select(r => r.ElevatorId).Should().BeEquivalentTo(new[] { 1, 2 });
        }

        [Fact]
        public void OptimizeDispatchPatterns_ShouldLogOptimization()
        {
            // Arrange
            var elevators = new List<IElevator>
            {
                new Elevator(1),
                new Elevator(2),
                new Elevator(3)
            };
            _mockBuilding.Setup(b => b.GetElevators()).Returns(elevators);
            _mockBuilding.Setup(b => b.FloorCount).Returns(10);

            // Act
            _dispatcher.OptimizeDispatchPatterns();

            // Assert
            _mockLogger.Verify(l => l.Log(
                It.Is<Microsoft.Extensions.Logging.LogLevel>(level => level == Microsoft.Extensions.Logging.LogLevel.Information),
                It.IsAny<Microsoft.Extensions.Logging.EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.AtLeast(3));
        }

        [Fact]
        public void GetDispatchStatistics_ShouldReturnStatistics()
        {
            // Arrange
            var elevators = new List<IElevator>
            {
                new Elevator(1),
                new Elevator(2)
            };
            _mockBuilding.Setup(b => b.GetElevators()).Returns(elevators);
            _mockBuilding.Setup(b => b.IsValidFloor(3)).Returns(true);
            _mockBuilding.Setup(b => b.FloorCount).Returns(10);

            // Make some dispatches
            var request = new FloorRequestDto { FloorNumber = 3, PassengerCount = 1 };
            _dispatcher.DispatchElevator(request);

            // Act
            var stats = _dispatcher.GetDispatchStatistics();

            // Assert
            stats.TotalCalls.Should().Be(1);
            stats.SuccessfulDispatch.Should().Be(1);
            stats.FailedDispatch.Should().Be(0);
            stats.SystemEfficiency.Should().Be(100);
            stats.CallsPerFloor.Should().ContainKey(3);
            stats.ElevatorUtilization.Should().NotBeEmpty();
        }

        [Fact]
        public void DispatchElevator_WithPendingRequests_ShouldQueueWhenNoElevatorAvailable()
        {
            // Arrange
            _mockBuilding.Setup(b => b.GetElevators()).Returns(new List<IElevator>());
            _mockBuilding.Setup(b => b.IsValidFloor(3)).Returns(true);

            var request = new FloorRequestDto { FloorNumber = 3, PassengerCount = 1 };

            // Act
            var result1 = _dispatcher.DispatchElevator(request);

            // Add an elevator
            var elevator = new Elevator(1);
            _mockBuilding.Setup(b => b.GetElevators()).Returns(new List<IElevator> { elevator });
            _mockBuilding.Setup(b => b.FloorCount).Returns(10);

            // Act - this should process the queued request
            var result2 = _dispatcher.DispatchElevator(new FloorRequestDto { FloorNumber = 1, PassengerCount = 1 });

            // Assert
            result1.Success.Should().BeFalse();
            result1.Message.Should().Contain("queued");
            result2.Success.Should().BeTrue();
        }

        [Fact]
        public void Constructor_NullDependencies_ShouldThrow()
        {
            Action noBuilding = () => _ = new DispatcherService(null!, _mockLogger.Object);
            Action noLogger = () => _ = new DispatcherService(_mockBuilding.Object, null!);

            noBuilding.Should().Throw<ArgumentNullException>();
            noLogger.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void DispatchElevator_InvalidFloor_ShouldFail()
        {
            _mockBuilding.Setup(b => b.IsValidFloor(99)).Returns(false);

            var result = _dispatcher.DispatchElevator(new FloorRequestDto { FloorNumber = 99, PassengerCount = 1 });

            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Invalid floor");
        }

        [Fact]
        public void DispatchElevator_WhenAlreadyAtFloor_ShouldOpenDoors()
        {
            var elevator = new Elevator(1) { CurrentFloor = 3 };
            _mockBuilding.Setup(b => b.GetElevators()).Returns(new List<IElevator> { elevator });
            _mockBuilding.Setup(b => b.IsValidFloor(3)).Returns(true);
            _mockBuilding.Setup(b => b.FloorCount).Returns(10);
            _mockBuilding.Setup(b => b.GetPassengerCountOnFloor(3)).Returns(4);

            var result = _dispatcher.DispatchElevator(new FloorRequestDto { FloorNumber = 3, PassengerCount = 1 });

            result.Success.Should().BeTrue();
            result.Priority.Should().Be(3);
        }

        [Fact]
        public void DispatchElevator_WithManyWaiting_ShouldUseHighPriority()
        {
            var elevator = new Elevator(1);
            _mockBuilding.Setup(b => b.GetElevators()).Returns(new List<IElevator> { elevator });
            _mockBuilding.Setup(b => b.IsValidFloor(4)).Returns(true);
            _mockBuilding.Setup(b => b.FloorCount).Returns(10);
            _mockBuilding.Setup(b => b.GetPassengerCountOnFloor(4)).Returns(6);

            var result = _dispatcher.DispatchElevator(new FloorRequestDto { FloorNumber = 4, PassengerCount = 1 });

            result.Success.Should().BeTrue();
            result.Priority.Should().Be(5);
        }

        [Fact]
        public void AssignPassengerToElevator_InvalidFloor_ShouldFail()
        {
            _mockBuilding.Setup(b => b.IsValidFloor(20)).Returns(false);

            var result = _dispatcher.AssignPassengerToElevator(new PassengerRequestDto
            {
                CurrentFloor = 20,
                DestinationFloor = 1
            });

            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Invalid floor");
        }

        [Fact]
        public void AssignPassengerToElevator_WhenNoElevator_ShouldFail()
        {
            _mockBuilding.Setup(b => b.GetElevators()).Returns(new List<IElevator>());
            _mockBuilding.Setup(b => b.IsValidFloor(5)).Returns(true);

            var result = _dispatcher.AssignPassengerToElevator(new PassengerRequestDto
            {
                Id = 1,
                CurrentFloor = 5,
                DestinationFloor = 8
            });

            result.Success.Should().BeFalse();
            result.Message.Should().Contain("No suitable elevator");
        }

        [Fact]
        public void AssignPassengerToElevator_WhenOverweight_ShouldReturnAssignmentError()
        {
            var elevator = new Elevator(1);
            _mockBuilding.Setup(b => b.GetElevators()).Returns(new List<IElevator> { elevator });
            _mockBuilding.Setup(b => b.IsValidFloor(It.IsAny<int>())).Returns(true);
            _mockBuilding.Setup(b => b.FloorCount).Returns(10);

            var result = _dispatcher.AssignPassengerToElevator(new PassengerRequestDto
            {
                Id = 1,
                CurrentFloor = 0,
                DestinationFloor = 8,
                Weight = 200
            });

            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Assignment error");
        }

        [Fact]
        public void CalculateEstimatedWaitTime_WhenNoElevator_ShouldReturnFallback()
        {
            _mockBuilding.Setup(b => b.GetElevators()).Returns(new List<IElevator>());

            _dispatcher.CalculateEstimatedWaitTime(3).Should().Be(10);
        }

        [Fact]
        public void ProcessElevatorQueue_WithPendingRequest_ShouldDispatchQueuedCall()
        {
            _mockBuilding.Setup(b => b.GetElevators()).Returns(new List<IElevator>());
            _mockBuilding.Setup(b => b.IsValidFloor(It.IsAny<int>())).Returns(true);
            _dispatcher.DispatchElevator(new FloorRequestDto { FloorNumber = 4, PassengerCount = 1 });

            var elevator = new Elevator(1);
            _mockBuilding.Setup(b => b.GetElevator(1)).Returns(elevator);
            _mockBuilding.Setup(b => b.GetElevators()).Returns(new List<IElevator> { elevator });
            _mockBuilding.Setup(b => b.FloorCount).Returns(10);
            _mockBuilding.Setup(b => b.GetPassengerCountOnFloor(It.IsAny<int>())).Returns(0);

            _dispatcher.ProcessElevatorQueue(1);

            elevator.CurrentFloor.Should().Be(4);
        }

        [Fact]
        public void ProcessElevatorQueue_WhenElevatorMissing_ShouldNotThrow()
        {
            _mockBuilding.Setup(b => b.GetElevator(9)).Throws(new ElevatorNotFoundException("missing"));

            _dispatcher.Invoking(d => d.ProcessElevatorQueue(9)).Should().NotThrow();
        }

        [Fact]
        public void OptimizeDispatchPatterns_WithSingleElevator_ShouldReturn()
        {
            _mockBuilding.Setup(b => b.GetElevators()).Returns(new List<IElevator> { new Elevator(1) });

            _dispatcher.Invoking(d => d.OptimizeDispatchPatterns()).Should().NotThrow();
        }

        [Fact]
        public void GetNearestAvailableElevator_ShouldPreferLowerLoadWhenTied()
        {
            var closeBusy = new Elevator(1, maxPassengers: 4) { CurrentFloor = 5 };
            closeBusy.BoardPassenger(new Passenger(1, 5, 8));
            var closeFree = new Elevator(2, maxPassengers: 4) { CurrentFloor = 5 };
            _mockBuilding.Setup(b => b.GetElevators()).Returns(new List<IElevator> { closeBusy, closeFree });
            _mockBuilding.Setup(b => b.FloorCount).Returns(10);

            var nearest = _dispatcher.GetNearestAvailableElevator(5);

            nearest!.Id.Should().Be(2);
        }

        [Fact]
        public void DispatchElevator_FreightElevator_ShouldServeValidFloors()
        {
            var freight = new Elevator(9, ElevatorType.Freight, 5);
            _mockBuilding.Setup(b => b.GetElevators()).Returns(new List<IElevator> { freight });
            _mockBuilding.Setup(b => b.IsValidFloor(2)).Returns(true);
            _mockBuilding.Setup(b => b.FloorCount).Returns(10);
            _mockBuilding.Setup(b => b.GetPassengerCountOnFloor(2)).Returns(1);

            var result = _dispatcher.DispatchElevator(new FloorRequestDto { FloorNumber = 2, PassengerCount = 1 });

            result.Success.Should().BeTrue();
            result.Elevator.ElevatorId.Should().Be(9);
            result.Priority.Should().Be(2);
        }

        [Fact]
        public void GetDispatchStatistics_WhenNoCalls_ShouldHaveZeroEfficiency()
        {
            var stats = _dispatcher.GetDispatchStatistics();

            stats.TotalCalls.Should().Be(0);
            stats.SystemEfficiency.Should().Be(0);
        }
    }
}