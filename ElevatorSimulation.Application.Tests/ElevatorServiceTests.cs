using Xunit;
using FluentAssertions;
using Moq;
using ElevatorSimulation.Application.DTOs;
using ElevatorSimulation.Application.Interfaces;
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
    public class ElevatorServiceTests
    {
        private readonly Mock<IBuilding> _mockBuilding;
        private readonly Mock<IDispatcherService> _mockDispatcher;
        private readonly Mock<ILogger> _mockLogger;
        private readonly ElevatorService _service;

        public ElevatorServiceTests()
        {
            _mockBuilding = new Mock<IBuilding>();
            _mockDispatcher = new Mock<IDispatcherService>();
            _mockLogger = new Mock<ILogger>();
            _service = new ElevatorService(_mockBuilding.Object, _mockDispatcher.Object, _mockLogger.Object);
        }

        [Fact]
        public void GetElevatorStatus_ValidId_ShouldReturnStatusDto()
        {
            // Arrange
            var elevator = new Elevator(1);
            elevator.MoveToFloor(5);
            _mockBuilding.Setup(b => b.GetElevator(1)).Returns(elevator);

            // Act
            var result = _service.GetElevatorStatus(1);

            // Assert
            result.Should().NotBeNull();
            result.ElevatorId.Should().Be(1);
            result.CurrentFloor.Should().Be(5);
            result.Status.Should().Be(ElevatorStatus.Stationary);
            result.PassengerCount.Should().Be(0);
        }

        [Fact]
        public void GetElevatorStatus_InvalidId_ShouldThrowException()
        {
            // Arrange
            _mockBuilding.Setup(b => b.GetElevator(999)).Throws<ElevatorNotFoundException>();

            // Act & Assert
            Action act = () => _service.GetElevatorStatus(999);
            act.Should().Throw<ElevatorNotFoundException>();
        }

        [Fact]
        public void GetAllElevators_ShouldReturnAllElevators()
        {
            // Arrange
            var elevators = new List<IElevator>
            {
                new Elevator(1),
                new Elevator(2),
                new Elevator(3)
            };
            _mockBuilding.Setup(b => b.GetElevators()).Returns(elevators);

            // Act
            var results = _service.GetAllElevators();

            // Assert
            results.Should().HaveCount(3);
            results.Select(r => r.ElevatorId).Should().BeEquivalentTo(new[] { 1, 2, 3 });
        }

        [Fact]
        public void GetBuildingStatus_ShouldReturnCompleteStatus()
        {
            // Arrange
            var elevators = new List<IElevator>
            {
                new Elevator(1),
                new Elevator(2)
            };
            _mockBuilding.Setup(b => b.GetElevators()).Returns(elevators);
            _mockBuilding.Setup(b => b.FloorCount).Returns(10);

            var floor = new Floor(5);
            floor.AddWaitingPassenger(new Passenger(1, 5, 10));
            _mockBuilding.Setup(b => b.GetFloor(It.IsAny<int>())).Returns(floor);
            _mockBuilding.Setup(b => b.GetPassengerCountOnFloor(It.IsAny<int>())).Returns(1);

            // Act
            var result = _service.GetBuildingStatus();

            // Assert
            result.Should().NotBeNull();
            result.FloorCount.Should().Be(10);
            result.ElevatorCount.Should().Be(2);
            result.TotalPassengersWaiting.Should().BeGreaterThanOrEqualTo(0);
            result.PassengersPerFloor.Should().NotBeNull();
        }

        [Fact]
        public void CallElevator_ValidRequest_ShouldReturnDispatchResponse()
        {
            // Arrange
            var request = new FloorRequestDto { FloorNumber = 5, PassengerCount = 2 };
            var dispatchResponse = new DispatchResponseDto
            {
                Success = true,
                Message = "Elevator dispatched",
                Elevator = new ElevatorStatusDto { ElevatorId = 1 }
            };
            _mockDispatcher.Setup(d => d.DispatchElevator(request)).Returns(dispatchResponse);

            // Act
            var result = _service.CallElevator(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Elevator dispatched");
            result.Elevator.Should().NotBeNull();
            result.Elevator.ElevatorId.Should().Be(1);
        }

        [Fact]
        public void CallElevator_InvalidFloor_ShouldReturnFailureResponse()
        {
            // Arrange
            var request = new FloorRequestDto { FloorNumber = 20 };
            _mockBuilding.Setup(b => b.IsValidFloor(20)).Returns(false);

            // Act
            var result = _service.CallElevator(request);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Invalid floor number");
        }

        [Fact]
        public void SendElevatorToFloor_ValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var elevator = new Elevator(1);
            _mockBuilding.Setup(b => b.GetElevator(1)).Returns(elevator);
            _mockBuilding.Setup(b => b.IsValidFloor(5)).Returns(true);

            var request = new ElevatorRequestDto
            {
                ElevatorId = 1,
                TargetFloor = 5,
                PassengerCount = 1
            };

            // Act
            var result = _service.SendElevatorToFloor(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Contain("Elevator 1 sent to floor 5");
            result.Elevator.Should().NotBeNull();
        }

        [Fact]
        public void AddPassenger_ValidRequest_ShouldAddAndDispatch()
        {
            // Arrange
            var request = new PassengerRequestDto
            {
                Id = 123,
                CurrentFloor = 5,
                DestinationFloor = 10,
                Weight = 70
            };

            _mockBuilding.Setup(b => b.IsValidFloor(5)).Returns(true);
            _mockBuilding.Setup(b => b.IsValidFloor(10)).Returns(true);

            var floor = new Floor(5);
            _mockBuilding.Setup(b => b.GetFloor(5)).Returns(floor);

            var dispatchResponse = new DispatchResponseDto
            {
                Success = true,
                Message = "Elevator dispatched",
                Elevator = new ElevatorStatusDto { ElevatorId = 1 }
            };
            _mockDispatcher.Setup(d => d.DispatchElevator(It.IsAny<FloorRequestDto>()))
                .Returns(dispatchResponse);

            // Act
            var result = _service.AddPassenger(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Contain("Passenger 123 added");
            floor.WaitingPassengers.Should().Contain(p => p.Id == 123);
        }

        [Fact]
        public void ProcessNextDestination_ValidElevator_ShouldProcess()
        {
            // Arrange
            var elevator = new Elevator(1);
            elevator.AddDestination(5);
            _mockBuilding.Setup(b => b.GetElevator(1)).Returns(elevator);

            // Act
            var result = _service.ProcessNextDestination(1);

            // Assert
            result.Should().BeTrue();
            elevator.CurrentFloor.Should().Be(5);
            elevator.DestinationQueue.Should().BeEmpty();
        }

        [Fact]
        public void ResetAllElevators_ShouldResetAll()
        {
            // Arrange
            var elevators = new List<IElevator>
            {
                new Elevator(1),
                new Elevator(2)
            };
            _mockBuilding.Setup(b => b.GetElevators()).Returns(elevators);

            // Act
            _service.ResetAllElevators();

            // Assert
            foreach (var elevator in elevators)
            {
                elevator.Status.Should().Be(ElevatorStatus.Stationary);
                elevator.CurrentFloor.Should().Be(0);
            }
        }

        [Fact]
        public void GetNearestElevatorStatus_ShouldReturnNearest()
        {
            // Arrange
            var elevator = new Elevator(1);
            _mockDispatcher.Setup(d => d.GetNearestAvailableElevator(5))
                .Returns(elevator);

            // Act
            var result = _service.GetNearestElevatorStatus(5);

            // Assert
            result.Should().NotBeNull();
            result.ElevatorId.Should().Be(1);
        }

        [Fact]
        public void GetNearestElevatorStatus_WhenNoElevator_ShouldReturnNull()
        {
            // Arrange
            _mockDispatcher.Setup(d => d.GetNearestAvailableElevator(5))
                .Returns((IElevator)null);

            // Act
            var result = _service.GetNearestElevatorStatus(5);

            // Assert
            result.Should().BeNull();
        }
    }
}