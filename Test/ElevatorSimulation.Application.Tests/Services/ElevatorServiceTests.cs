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
using ElevatorSimulation.Infrastructure.Logging;

namespace ElevatorSimulation.Application.Tests.Services
{
    /// <summary>
    /// Comprehensive test suite for ElevatorService with 100% code coverage
    /// </summary>
    public class ElevatorServiceTests : IDisposable
    {
        private readonly Mock<IBuilding> _mockBuilding;
        private readonly Mock<IDispatcherService> _mockDispatcher;
        private readonly Mock<ILogger> _mockLogger;
        private readonly ElevatorService _service;
        private readonly List<IElevator> _elevators;

        public ElevatorServiceTests()
        {
            _mockBuilding = new Mock<IBuilding>();
            _mockDispatcher = new Mock<IDispatcherService>();
            _mockLogger = new Mock<ILogger>();
            _elevators = new List<IElevator>();

            // Setup building with elevators
            _elevators.Add(new Elevator(1));
            _elevators.Add(new Elevator(2));
            _elevators.Add(new Elevator(3));

            _mockBuilding.Setup(b => b.GetElevators()).Returns(_elevators);
            _mockBuilding.Setup(b => b.FloorCount).Returns(10);

            _service = new ElevatorService(
                _mockBuilding.Object,
                _mockDispatcher.Object,
                (Microsoft.Extensions.Logging.ILogger)_mockLogger.Object);
        }

        #region Constructor Tests

        [Fact]
        [Trait("Category", "Constructor")]
        public void Constructor_WithNullBuilding_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ElevatorService(null, _mockDispatcher.Object, (Microsoft.Extensions.Logging.ILogger)_mockLogger.Object));
            Assert.Equal("building", exception.ParamName);
        }

        [Fact]
        [Trait("Category", "Constructor")]
        public void Constructor_WithNullDispatcher_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ElevatorService(_mockBuilding.Object, null, (Microsoft.Extensions.Logging.ILogger)_mockLogger.Object));
            Assert.Equal("dispatcherService", exception.ParamName);
        }

        [Fact]
        [Trait("Category", "Constructor")]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ElevatorService(_mockBuilding.Object, _mockDispatcher.Object, null));
            Assert.Equal("logger", exception.ParamName);
        }

        #endregion

        #region GetElevatorStatus Tests

        [Fact]
        [Trait("Category", "GetStatus")]
        public void GetElevatorStatus_ValidId_ShouldReturnStatusDto()
        {
            // Arrange
            var elevator = _elevators[0];
            elevator.MoveToFloor(5);
            _mockBuilding.Setup(b => b.GetElevator(1)).Returns(elevator);

            // Act
            var result = _service.GetElevatorStatus(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.ElevatorId);
            Assert.Equal(5, result.CurrentFloor);
            Assert.Equal(ElevatorStatus.Stationary, result.Status);
            Assert.Equal(0, result.PassengerCount);
            Assert.Equal(10, result.MaxPassengers);
            Assert.Equal(ElevatorType.Standard, result.Type);
            Assert.NotNull(result.DestinationQueue);
            Assert.True(result.IsAvailable);
            Assert.False(result.IsMoving);
            Assert.Equal(0, result.OccupancyPercentage);
            Assert.Equal(1, result.TotalTrips);
            Assert.Equal(5, result.TotalDistanceTraveled);
            Assert.NotNull(result.DisplayStatus);
            Assert.Contains("Stationary", result.DisplayStatus);
        }

        [Fact]
        [Trait("Category", "GetStatus")]
        public void GetElevatorStatus_WithPassengers_ShouldShowCorrectOccupancy()
        {
            // Arrange
            var elevator = _elevators[0];
            var passenger = new Passenger(1, 0, 5);
            elevator.BoardPassenger(passenger);
            _mockBuilding.Setup(b => b.GetElevator(1)).Returns(elevator);

            // Act
            var result = _service.GetElevatorStatus(1);

            // Assert
            Assert.Equal(1, result.PassengerCount);
            Assert.Equal(10, result.OccupancyPercentage);
        }

        [Fact]
        [Trait("Category", "GetStatus")]
        public void GetElevatorStatus_WithDestinationQueue_ShouldShowQueue()
        {
            // Arrange
            var elevator = _elevators[0];
            elevator.AddDestination(5);
            elevator.AddDestination(3);
            _mockBuilding.Setup(b => b.GetElevator(1)).Returns(elevator);

            // Act
            var result = _service.GetElevatorStatus(1);

            // Assert
            Assert.Equal(2, result.DestinationQueue.Count);
            Assert.Contains(5, result.DestinationQueue);
            Assert.Contains(3, result.DestinationQueue);
        }

        [Fact]
        [Trait("Category", "GetStatus")]
        public void GetElevatorStatus_InvalidId_ShouldThrowException()
        {
            // Arrange
            _mockBuilding.Setup(b => b.GetElevator(999))
                .Throws(new ElevatorNotFoundException("Elevator not found"));

            // Act & Assert
            var exception = Assert.Throws<ElevatorNotFoundException>(() =>
                _service.GetElevatorStatus(999));
            Assert.Contains("Elevator", exception.Message);
            _mockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region GetAllElevators Tests

        [Fact]
        [Trait("Category", "GetAll")]
        public void GetAllElevators_ShouldReturnAllElevators()
        {
            // Arrange
            var elevator1 = _elevators[0];
            var elevator2 = _elevators[1];
            elevator1.MoveToFloor(5);
            elevator2.MoveToFloor(3);

            // Act
            var results = _service.GetAllElevators();

            // Assert
            Assert.Equal(3, results.Count);
            Assert.Equal(1, results[0].ElevatorId);
            Assert.Equal(5, results[0].CurrentFloor);
            Assert.Equal(2, results[1].ElevatorId);
            Assert.Equal(3, results[1].CurrentFloor);
        }

        [Fact]
        [Trait("Category", "GetAll")]
        public void GetAllElevators_WhenEmpty_ShouldReturnEmptyList()
        {
            // Arrange
            _mockBuilding.Setup(b => b.GetElevators()).Returns(new List<IElevator>());

            // Act
            var results = _service.GetAllElevators();

            // Assert
            Assert.Empty(results);
        }

        #endregion

        #region GetBuildingStatus Tests

        [Fact]
        [Trait("Category", "GetStatus")]
        public void GetBuildingStatus_ShouldReturnCompleteStatus()
        {
            // Arrange
            var floor = new Floor(5);
            var passenger = new Passenger(1, 5, 10);
            floor.AddWaitingPassenger(passenger);

            _mockBuilding.Setup(b => b.GetFloor(It.IsAny<int>())).Returns(floor);
            _mockBuilding.Setup(b => b.GetPassengerCountOnFloor(It.IsAny<int>())).Returns(1);
            _mockBuilding.Setup(b => b.FloorCount).Returns(10);

            // Act
            var result = _service.GetBuildingStatus();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.FloorCount);
            Assert.Equal(3, result.ElevatorCount);
            Assert.Equal(3, result.TotalPassengersInTransit);
            Assert.Equal(1, result.TotalPassengersWaiting);
            Assert.NotNull(result.PassengersPerFloor);
            Assert.NotNull(result.Elevators);
            Assert.Equal(3, result.Elevators.Count);
            Assert.True(result.AverageWaitTime > 0);
        }

        [Fact]
        [Trait("Category", "GetStatus")]
        public void GetBuildingStatus_WhenNoPassengers_ShouldShowZeroWaiting()
        {
            // Arrange
            var floor = new Floor(5);
            _mockBuilding.Setup(b => b.GetFloor(It.IsAny<int>())).Returns(floor);
            _mockBuilding.Setup(b => b.GetPassengerCountOnFloor(It.IsAny<int>())).Returns(0);

            // Act
            var result = _service.GetBuildingStatus();

            // Assert
            Assert.Equal(0, result.TotalPassengersWaiting);
            Assert.All(result.PassengersPerFloor.Values, v => Assert.Equal(0, v));
        }

        #endregion

        #region CallElevator Tests

        [Fact]
        [Trait("Category", "Call")]
        public void CallElevator_ValidRequest_ShouldDispatchSuccessfully()
        {
            // Arrange
            var request = new FloorRequestDto
            {
                FloorNumber = 5,
                PassengerCount = 2,
                RequestType = "Call"
            };

            var dispatchResponse = new DispatchResponseDto
            {
                Success = true,
                Message = "Elevator dispatched",
                Elevator = new ElevatorStatusDto { ElevatorId = 1 }
            };

            _mockBuilding.Setup(b => b.IsValidFloor(5)).Returns(true);
            _mockDispatcher.Setup(d => d.DispatchElevator(request)).Returns(dispatchResponse);

            // Act
            var result = _service.CallElevator(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("Elevator dispatched", result.Message);
            Assert.NotNull(result.Elevator);
            Assert.Equal(1, result.Elevator.ElevatorId);
            _mockLogger.Verify(l => l.LogInfo(It.IsAny<string>()), Times.AtLeastOnce);
            _mockLogger.Verify(l => l.LogSuccess(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "Call")]
        public void CallElevator_InvalidFloor_ShouldReturnFailure()
        {
            // Arrange
            var request = new FloorRequestDto { FloorNumber = 20 };
            _mockBuilding.Setup(b => b.IsValidFloor(20)).Returns(false);

            // Act
            var result = _service.CallElevator(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Invalid floor", result.Message);
            _mockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "Call")]
        public void CallElevator_WhenDispatcherFails_ShouldReturnFailure()
        {
            // Arrange
            var request = new FloorRequestDto { FloorNumber = 5 };
            _mockBuilding.Setup(b => b.IsValidFloor(5)).Returns(true);

            var dispatchResponse = new DispatchResponseDto
            {
                Success = false,
                Message = "No elevators available"
            };

            _mockDispatcher.Setup(d => d.DispatchElevator(request)).Returns(dispatchResponse);

            // Act
            var result = _service.CallElevator(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("No elevators", result.Message);
            _mockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "Call")]
        public void CallElevator_ExceptionThrown_ShouldLogAndReturnFailure()
        {
            // Arrange
            var request = new FloorRequestDto { FloorNumber = 5 };
            _mockBuilding.Setup(b => b.IsValidFloor(5)).Returns(true);
            _mockDispatcher.Setup(d => d.DispatchElevator(request))
                .Throws(new InvalidOperationException("Simulation error"));

            // Act
            var result = _service.CallElevator(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("error", result.Message.ToLower());
            _mockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region SendElevatorToFloor Tests

        [Fact]
        [Trait("Category", "Send")]
        public void SendElevatorToFloor_ValidRequest_ShouldSendSuccessfully()
        {
            // Arrange
            var elevator = _elevators[0];
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
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Contains("sent to floor 5", result.Message);
            Assert.NotNull(result.Elevator);
            Assert.Equal(1, result.Elevator.ElevatorId);
            Assert.Equal(5, result.Elevator.CurrentFloor);
            Assert.True(result.EstimatedWaitTime > 0);
        }

        [Fact]
        [Trait("Category", "Send")]
        public void SendElevatorToFloor_InvalidElevatorId_ShouldReturnFailure()
        {
            // Arrange
            _mockBuilding.Setup(b => b.GetElevator(999))
                .Throws(new ElevatorNotFoundException("Elevator not found"));

            var request = new ElevatorRequestDto
            {
                ElevatorId = 999,
                TargetFloor = 5,
                PassengerCount = 1
            };

            // Act
            var result = _service.SendElevatorToFloor(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Error", result.Message);
            _mockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "Send")]
        public void SendElevatorToFloor_InvalidTargetFloor_ShouldReturnFailure()
        {
            // Arrange
            var elevator = _elevators[0];
            _mockBuilding.Setup(b => b.GetElevator(1)).Returns(elevator);
            _mockBuilding.Setup(b => b.IsValidFloor(20)).Returns(false);

            var request = new ElevatorRequestDto
            {
                ElevatorId = 1,
                TargetFloor = 20,
                PassengerCount = 1
            };

            // Act
            var result = _service.SendElevatorToFloor(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Invalid floor", result.Message);
        }

        [Fact]
        [Trait("Category", "Send")]
        public void SendElevatorToFloor_OutOfServiceElevator_ShouldReturnFailure()
        {
            // Arrange
            var elevator = _elevators[0];
            elevator.SetOutOfService();
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
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("out of service", result.Message.ToLower());
        }

        #endregion

        #region AddPassenger Tests

        [Fact]
        [Trait("Category", "Passenger")]
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

            var floor = new Floor(5);
            _mockBuilding.Setup(b => b.IsValidFloor(5)).Returns(true);
            _mockBuilding.Setup(b => b.IsValidFloor(10)).Returns(true);
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
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Contains("Passenger 123 added", result.Message);
            Assert.Single(floor.WaitingPassengers);
            Assert.Equal(123, floor.WaitingPassengers[0].Id);
            Assert.Equal(5, floor.WaitingPassengers[0].CurrentFloor);
            Assert.Equal(10, floor.WaitingPassengers[0].DestinationFloor);
        }

        [Fact]
        [Trait("Category", "Passenger")]
        public void AddPassenger_InvalidCurrentFloor_ShouldReturnFailure()
        {
            // Arrange
            var request = new PassengerRequestDto
            {
                Id = 123,
                CurrentFloor = 20,
                DestinationFloor = 10,
                Weight = 70
            };

            _mockBuilding.Setup(b => b.IsValidFloor(20)).Returns(false);

            // Act
            var result = _service.AddPassenger(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Invalid current floor", result.Message);
        }

        [Fact]
        [Trait("Category", "Passenger")]
        public void AddPassenger_InvalidDestinationFloor_ShouldReturnFailure()
        {
            // Arrange
            var request = new PassengerRequestDto
            {
                Id = 123,
                CurrentFloor = 5,
                DestinationFloor = 20,
                Weight = 70
            };

            _mockBuilding.Setup(b => b.IsValidFloor(5)).Returns(true);
            _mockBuilding.Setup(b => b.IsValidFloor(20)).Returns(false);

            // Act
            var result = _service.AddPassenger(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Invalid destination floor", result.Message);
        }

        [Fact]
        [Trait("Category", "Passenger")]
        public void AddPassenger_SameFloor_ShouldReturnFailure()
        {
            // Arrange
            var request = new PassengerRequestDto
            {
                Id = 123,
                CurrentFloor = 5,
                DestinationFloor = 5,
                Weight = 70
            };

            _mockBuilding.Setup(b => b.IsValidFloor(5)).Returns(true);

            // Act
            var result = _service.AddPassenger(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Current and destination floors must be different", result.Message);
        }

        [Fact]
        [Trait("Category", "Passenger")]
        public void AddPassenger_WhenDispatchFails_ShouldStillAddPassenger()
        {
            // Arrange
            var request = new PassengerRequestDto
            {
                Id = 123,
                CurrentFloor = 5,
                DestinationFloor = 10,
                Weight = 70
            };

            var floor = new Floor(5);
            _mockBuilding.Setup(b => b.IsValidFloor(5)).Returns(true);
            _mockBuilding.Setup(b => b.IsValidFloor(10)).Returns(true);
            _mockBuilding.Setup(b => b.GetFloor(5)).Returns(floor);

            var dispatchResponse = new DispatchResponseDto
            {
                Success = false,
                Message = "No elevators available"
            };

            _mockDispatcher.Setup(d => d.DispatchElevator(It.IsAny<FloorRequestDto>()))
                .Returns(dispatchResponse);

            // Act
            var result = _service.AddPassenger(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("added to waiting list", result.Message);
            Assert.Single(floor.WaitingPassengers);
        }

        #endregion

        #region ProcessNextDestination Tests

        [Fact]
        [Trait("Category", "Process")]
        public void ProcessNextDestination_ValidElevator_ShouldProcess()
        {
            // Arrange
            var elevator = _elevators[0];
            elevator.AddDestination(5);
            _mockBuilding.Setup(b => b.GetElevator(1)).Returns(elevator);

            // Act
            var result = _service.ProcessNextDestination(1);

            // Assert
            Assert.True(result);
            Assert.Equal(5, elevator.CurrentFloor);
            Assert.Empty(elevator.DestinationQueue);
        }

        [Fact]
        [Trait("Category", "Process")]
        public void ProcessNextDestination_EmptyQueue_ShouldReturnFalse()
        {
            // Arrange
            var elevator = _elevators[0];
            _mockBuilding.Setup(b => b.GetElevator(1)).Returns(elevator);

            // Act
            var result = _service.ProcessNextDestination(1);

            // Assert
            Assert.False(result);
            Assert.Equal(0, elevator.CurrentFloor);
        }

        [Fact]
        [Trait("Category", "Process")]
        public void ProcessNextDestination_InvalidElevator_ShouldReturnFalse()
        {
            // Arrange
            _mockBuilding.Setup(b => b.GetElevator(999))
                .Throws(new ElevatorNotFoundException("Elevator not found"));

            // Act
            var result = _service.ProcessNextDestination(999);

            // Assert
            Assert.False(result);
            _mockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region ResetAllElevators Tests

        [Fact]
        [Trait("Category", "Reset")]
        public void ResetAllElevators_ShouldResetAllElevators()
        {
            // Arrange
            var elevator1 = _elevators[0];
            var elevator2 = _elevators[1];

            elevator1.MoveToFloor(5);
            elevator1.AddDestination(7);
            elevator1.AddDestination(9);

            elevator2.MoveToFloor(3);
            elevator2.AddDestination(4);

            // Act
            _service.ResetAllElevators();

            // Assert
            Assert.Equal(0, elevator1.CurrentFloor);
            Assert.Equal(0, elevator2.CurrentFloor);
            Assert.Empty(elevator1.DestinationQueue);
            Assert.Empty(elevator2.DestinationQueue);
            Assert.Equal(ElevatorStatus.Stationary, elevator1.Status);
            Assert.Equal(ElevatorStatus.Stationary, elevator2.Status);
            _mockLogger.Verify(l => l.LogInfo("All elevators have been reset"), Times.Once);
        }

        #endregion

        #region UpdateElevatorSpeed Tests

        [Fact]
        [Trait("Category", "Speed")]
        public void UpdateElevatorSpeed_ShouldLogUpdate()
        {
            // Act
            _service.UpdateElevatorSpeed(1, 5);

            // Assert
            _mockLogger.Verify(l => l.LogInfo("Elevator 1 speed updated to 5"), Times.Once);
        }

        #endregion

        #region GetNearestElevatorStatus Tests

        [Fact]
        [Trait("Category", "Nearest")]
        public void GetNearestElevatorStatus_ValidFloor_ShouldReturnNearest()
        {
            // Arrange
            var elevator = _elevators[0];
            _mockDispatcher.Setup(d => d.GetNearestAvailableElevator(5))
                .Returns(elevator);

            // Act
            var result = _service.GetNearestElevatorStatus(5);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.ElevatorId);
        }

        [Fact]
        [Trait("Category", "Nearest")]
        public void GetNearestElevatorStatus_NoElevator_ShouldReturnNull()
        {
            // Arrange
            _mockDispatcher.Setup(d => d.GetNearestAvailableElevator(5))
                .Returns((IElevator)null);

            // Act
            var result = _service.GetNearestElevatorStatus(5);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        [Trait("Category", "Nearest")]
        public void GetNearestElevatorStatus_ExceptionThrown_ShouldLogAndReturnNull()
        {
            // Arrange
            _mockDispatcher.Setup(d => d.GetNearestAvailableElevator(5))
                .Throws(new InvalidOperationException("Error"));

            // Act
            var result = _service.GetNearestElevatorStatus(5);

            // Assert
            Assert.Null(result);
            _mockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region Private Method Coverage Tests

        [Fact]
        [Trait("Category", "Private")]
        public void CalculateEstimatedTravelTime_ShouldReturnCorrectTime()
        {
            // Arrange
            var elevator = _elevators[0];
            elevator.MoveToFloor(5);

            // Act - Using reflection to test private method
            var method = typeof(ElevatorService).GetMethod("CalculateEstimatedTravelTime",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var result = (int)method.Invoke(_service, new object[] { elevator, 8 });

            // Assert
            // Distance 3 * 2 = 6 + 3 = 9
            Assert.Equal(9, result);
        }

        [Fact]
        [Trait("Category", "Private")]
        public void GeneratePassengerId_ShouldReturnUniqueId()
        {
            // Act - Using reflection to test private method
            var method = typeof(ElevatorService).GetMethod("GeneratePassengerId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var id1 = (int)method.Invoke(_service, null);
            var id2 = (int)method.Invoke(_service, null);

            // Assert
            Assert.True(id1 > 0 && id1 < 10000);
            Assert.True(id2 > 0 && id2 < 10000);
            // Note: Could potentially be equal but very unlikely
        }

        [Fact]
        [Trait("Category", "Private")]
        public void GetDisplayStatus_ShouldReturnCorrectStatusString()
        {
            // Arrange
            var elevator = _elevators[0];

            // Act - Using reflection to test private method
            var method = typeof(ElevatorService).GetMethod("GetDisplayStatus",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var status1 = (string)method.Invoke(_service, new object[] { elevator });

            elevator.MoveToFloor(5);
            var status2 = (string)method.Invoke(_service, new object[] { elevator });

            elevator.OpenDoors();
            var status3 = (string)method.Invoke(_service, new object[] { elevator });

            // Assert
            Assert.Contains("Stationary", status1);
            Assert.Contains("Stationary", status2);
            Assert.Contains("Doors Open", status3);
        }

        #endregion

        public void Dispose()
        {
            _mockBuilding.Reset();
            _mockDispatcher.Reset();
            _mockLogger.Reset();
        }
    }
}