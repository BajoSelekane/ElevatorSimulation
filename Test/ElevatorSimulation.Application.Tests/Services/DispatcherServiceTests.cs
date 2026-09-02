using ElevatorSimulation.Application.DTOs;
using ElevatorSimulation.Application.Services;
using ElevatorSimulation.Domain.Entities;
using ElevatorSimulation.Domain.Enums;
using ElevatorSimulation.Domain.Exceptions;
using ElevatorSimulation.Domain.Interfaces;
using ElevatorSimulation.Infrastructure.Logging;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Xunit;


namespace ElevatorSimulation.Application.Tests.Services
{
    /// <summary>
    /// Comprehensive test suite for DispatcherService with 100% code coverage
    /// </summary>
    public class DispatcherServiceTests : IDisposable
    {
        private readonly Mock<IBuilding> _mockBuilding;
        private readonly Mock<ILogger> _mockLogger;
        private readonly DispatcherService _dispatcher;
        private readonly List<IElevator> _elevators;

        public DispatcherServiceTests()
        {
            _mockBuilding = new Mock<IBuilding>();
            _mockLogger = new Mock<ILogger>();
            _elevators = [];

            var e1 = new Elevator(1);
            e1.MoveToFloor(5);
            _elevators.Add(e1);

            var e2 = new Elevator(2);
            e2.MoveToFloor(2);
            _elevators.Add(e2);

            var e3 = new Elevator(3);
            e3.MoveToFloor(8);
            _elevators.Add(e3);

            _mockBuilding.Setup(b => b.GetElevators()).Returns(_elevators);
            _mockBuilding.Setup(b => b.FloorCount).Returns(10);
            _mockBuilding.Setup(b => b.IsValidFloor(It.IsAny<int>())).Returns(true);

            _dispatcher = new DispatcherService(_mockBuilding.Object, (Microsoft.Extensions.Logging.ILogger)_mockLogger.Object);
        }

        #region Constructor Tests   

        [Fact]
        [Trait("Category", "Constructor")]
        public void Constructor_WithNullBuilding_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new DispatcherService(null, (Microsoft.Extensions.Logging.ILogger)_mockLogger.Object));
            Assert.Equal("building", exception.ParamName);
        }

        [Fact]
        [Trait("Category", "Constructor")]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new DispatcherService(_mockBuilding.Object, null));
            Assert.Equal("logger", exception.ParamName);
        }

        [Fact]
        [Trait("Category", "Constructor")]
        public void Constructor_ShouldInitializeStatistics()
        {
            // Arrange & Act
            var stats = _dispatcher.GetDispatchStatistics();

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(0, stats.TotalCalls);
            Assert.Equal(0, stats.SuccessfulDispatch);
            Assert.Equal(0, stats.FailedDispatch);
            Assert.NotNull(stats.CallsPerFloor);
            Assert.NotNull(stats.ElevatorUtilization);
            Assert.Equal(3, stats.ElevatorUtilization.Count);
            Assert.Contains(1, stats.ElevatorUtilization.Keys);
            Assert.Contains(2, stats.ElevatorUtilization.Keys);
            Assert.Contains(3, stats.ElevatorUtilization.Keys);
        }

        #endregion

        #region DispatchElevator Tests

        [Fact]
        [Trait("Category", "Dispatch")]
        public void DispatchElevator_ValidRequest_ShouldReturnNearestElevator()
        {
            // Arrange
            var request = new FloorRequestDto
            {
                FloorNumber = 3,
                PassengerCount = 1,
                RequestType = "Call"
            };

            // Act
            var result = _dispatcher.DispatchElevator(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("Elevator 2 dispatched to floor 3", result.Message);
            Assert.NotNull(result.Elevator);
            Assert.Equal(2, result.Elevator.ElevatorId);
            Assert.True(result.EstimatedWaitTime > 0);
            Assert.Equal(1, result.Priority);

            var stats = _dispatcher.GetDispatchStatistics();
            Assert.Equal(1, stats.TotalCalls);
            Assert.Equal(1, stats.SuccessfulDispatch);
            Assert.Equal(0, stats.FailedDispatch);
            Assert.Contains(3, stats.CallsPerFloor.Keys);
            Assert.Equal(1, stats.CallsPerFloor[3]);
        }

        [Fact]
        [Trait("Category", "Dispatch")]
        public void DispatchElevator_MultipleFloors_ShouldDispatchNearest()
        {
            // Arrange
            var request1 = new FloorRequestDto { FloorNumber = 3, PassengerCount = 1 };
            var request2 = new FloorRequestDto { FloorNumber = 7, PassengerCount = 1 };

            // Act
            var result1 = _dispatcher.DispatchElevator(request1);
            var result2 = _dispatcher.DispatchElevator(request2);

            // Assert
            Assert.True(result1.Success);
            Assert.True(result2.Success);
            Assert.Equal(2, result1.Elevator.ElevatorId); // Closest to 3
            Assert.Equal(3, result2.Elevator.ElevatorId); // Closest to 7
        }

        [Fact]
        [Trait("Category", "Dispatch")]
        public void DispatchElevator_InvalidFloor_ShouldReturnFailure()
        {
            // Arrange
            var request = new FloorRequestDto { FloorNumber = 20 };
            _mockBuilding.Setup(b => b.IsValidFloor(20)).Returns(false);

            // Act
            var result = _dispatcher.DispatchElevator(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Invalid floor", result.Message);

            var stats = _dispatcher.GetDispatchStatistics();
            Assert.Equal(1, stats.FailedDispatch);
        }

        [Fact]
        [Trait("Category", "Dispatch")]
        public void DispatchElevator_NoElevatorsAvailable_ShouldQueueRequest()
        {
            // Arrange
            _mockBuilding.Setup(b => b.GetElevators()).Returns([]);
            var request = new FloorRequestDto { FloorNumber = 3, PassengerCount = 1 };

            // Act
            var result = _dispatcher.DispatchElevator(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("queued", result.Message.ToLower());
            Assert.Equal(10, result.EstimatedWaitTime);

            var stats = _dispatcher.GetDispatchStatistics();
            Assert.Equal(1, stats.FailedDispatch);
        }

        [Fact]
        [Trait("Category", "Dispatch")]
        public void DispatchElevator_AtCapacity_ShouldQueueRequest()
        {
            // Arrange
            var elevator = _elevators[0] as Elevator;
            Passenger passenger = new(1, 0, 5);
            for (int i = 0; i < elevator.MaxPassengers; i++)
            {
                elevator.BoardPassenger(new Passenger(i + 1, 0, i + 2));
            }

            var request = new FloorRequestDto { FloorNumber = 3, PassengerCount = 2 };

            // Act
            var result = _dispatcher.DispatchElevator(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("capacity", result.Message.ToLower());
            Assert.Contains("queued", result.Message.ToLower());
            Assert.Equal(15, result.EstimatedWaitTime);
        }

        [Fact]
        [Trait("Category", "Dispatch")]
        public void DispatchElevator_ExceptionThrown_ShouldReturnFailure()
        {
            // Arrange
            _mockBuilding.Setup(b => b.GetElevators())
                .Throws(new InvalidOperationException("Test error"));

            var request = new FloorRequestDto { FloorNumber = 3, PassengerCount = 1 };

            // Act
            var result = _dispatcher.DispatchElevator(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("error", result.Message.ToLower());
            _mockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region AssignPassengerToElevator Tests

        [Fact]
        [Trait("Category", "Assign")]
        public void AssignPassengerToElevator_ValidRequest_ShouldAssign()
        {
            // Arrange
            var request = new PassengerRequestDto
            {
                Id = 123,
                CurrentFloor = 3,
                DestinationFloor = 7,
                Weight = 70
            };

            // Act
            var result = _dispatcher.AssignPassengerToElevator(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Contains("assigned to elevator", result.Message);
            Assert.NotNull(result.Elevator);
            Assert.Equal(2, result.Elevator.ElevatorId); // Closest to floor 3
            Assert.True(result.EstimatedWaitTime > 0);
        }

        [Fact]
        [Trait("Category", "Assign")]
        public void AssignPassengerToElevator_InvalidFloor_ShouldReturnFailure()
        {
            // Arrange
            var request = new PassengerRequestDto
            {
                Id = 123,
                CurrentFloor = 20,
                DestinationFloor = 7,
                Weight = 70
            };

            _mockBuilding.Setup(b => b.IsValidFloor(20)).Returns(false);

            // Act
            var result = _dispatcher.AssignPassengerToElevator(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Invalid floor", result.Message);
        }

        [Fact]
        [Trait("Category", "Assign")]
        public void AssignPassengerToElevator_NoSuitableElevator_ShouldReturnFailure()
        {
            // Arrange
            _mockBuilding.Setup(b => b.GetElevators()).Returns([]);

            var request = new PassengerRequestDto
            {
                Id = 123,
                CurrentFloor = 3,
                DestinationFloor = 7,
                Weight = 70
            };

            // Act
            var result = _dispatcher.AssignPassengerToElevator(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("No suitable elevator", result.Message);
        }

        [Fact]
        [Trait("Category", "Assign")]
        public void AssignPassengerToElevator_ExceptionThrown_ShouldReturnFailure()
        {
            // Arrange
            _mockBuilding.Setup(b => b.GetElevators())
                .Throws(new InvalidOperationException("Test error"));

            var request = new PassengerRequestDto
            {
                Id = 123,
                CurrentFloor = 3,
                DestinationFloor = 7,
                Weight = 70
            };

            // Act
            var result = _dispatcher.AssignPassengerToElevator(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("error", result.Message.ToLower());
            _mockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region GetNearestAvailableElevator Tests

        [Fact]
        [Trait("Category", "Nearest")]
        public void GetNearestAvailableElevator_ShouldReturnNearest()
        {
            // Act
            var result = _dispatcher.GetNearestAvailableElevator(3);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Id);
            Assert.Equal(2, result.CurrentFloor);
        }

        [Fact]
        [Trait("Category", "Nearest")]
        public void GetNearestAvailableElevator_NoElevators_ShouldReturnNull()
        {
            // Arrange
            _mockBuilding.Setup(b => b.GetElevators()).Returns([]);

            // Act
            var result = _dispatcher.GetNearestAvailableElevator(3);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        [Trait("Category", "Nearest")]
        public void GetNearestAvailableElevator_AllElevatorsOutOfService_ShouldReturnNull()
        {
            // Arrange
            foreach (var elevator in _elevators)
            {
                elevator.SetOutOfService();
            }

            // Act
            var result = _dispatcher.GetNearestAvailableElevator(3);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region IsElevatorAvailableForFloor Tests

        [Fact]
        [Trait("Category", "Available")]
        public void IsElevatorAvailableForFloor_ValidElevator_ShouldReturnTrue()
        {
            // Act
            var result = _dispatcher.IsElevatorAvailableForFloor(1, 5);

            // Assert
            Assert.True(result);
        }

        [Fact]
        [Trait("Category", "Available")]
        public void IsElevatorAvailableForFloor_OutOfService_ShouldReturnFalse()
        {
            // Arrange
            var elevator = _elevators[0];
            elevator.SetOutOfService();

            // Act
            var result = _dispatcher.IsElevatorAvailableForFloor(1, 5);

            // Assert
            Assert.False(result);
        }

        [Fact]
        [Trait("Category", "Available")]
        public void IsElevatorAvailableForFloor_AtCapacity_ShouldReturnFalse()
        {
            // Arrange
            var elevator = _elevators[0] as Elevator;
            for (int i = 0; i < elevator.MaxPassengers; i++)
            {
                elevator.BoardPassenger(new Passenger(i + 1, 0, i + 2));
            }

            // Act
            var result = _dispatcher.IsElevatorAvailableForFloor(1, 5);

            // Assert
            Assert.False(result);
        }

        [Fact]
        [Trait("Category", "Available")]
        public void IsElevatorAvailableForFloor_InvalidElevator_ShouldReturnFalse()
        {
            // Act
            var result = _dispatcher.IsElevatorAvailableForFloor(999, 5);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region ProcessElevatorQueue Tests

        [Fact]
        [Trait("Category", "Queue")]
        public void ProcessElevatorQueue_WithDestination_ShouldProcess()
        {
            // Arrange
            var elevator = _elevators[0];
            elevator.AddDestination(5);
            _mockBuilding.Setup(b => b.GetElevator(1)).Returns(elevator);

            // Act
            _dispatcher.ProcessElevatorQueue(1);

            // Assert
            Assert.Equal(5, elevator.CurrentFloor);
            Assert.Empty(elevator.DestinationQueue);
            _mockLogger.Verify(l => l.LogInfo(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "Queue")]
        public void ProcessElevatorQueue_WithPendingRequest_ShouldProcess()
        {
            // Arrange
            var elevator = _elevators[0];
            _mockBuilding.Setup(b => b.GetElevator(1)).Returns(elevator);

            // Create a pending request
            _mockBuilding.Setup(b => b.GetElevators()).Returns([]);
            var request = new FloorRequestDto { FloorNumber = 3, PassengerCount = 1 };
            _dispatcher.DispatchElevator(request);

            // Add elevator back
            _mockBuilding.Setup(b => b.GetElevators()).Returns(_elevators);

            // Act
            _dispatcher.ProcessElevatorQueue(1);

            // Assert
            _mockLogger.Verify(l => l.LogInfo(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        [Trait("Category", "Queue")]
        public void ProcessElevatorQueue_ExceptionThrown_ShouldLogError()
        {
            // Arrange
            _mockBuilding.Setup(b => b.GetElevator(1))
                .Throws(new Exception("Test error"));

            // Act
            _dispatcher.ProcessElevatorQueue(1);

            // Assert
            _mockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region CalculateEstimatedWaitTime Tests

        [Fact]
        [Trait("Category", "WaitTime")]
        public void CalculateEstimatedWaitTime_WithElevators_ShouldCalculate()
        {
            // Act
            var waitTime = _dispatcher.CalculateEstimatedWaitTime(3);

            // Assert
            Assert.True(waitTime > 0);
            // Distance from elevator 2 (floor 2) to floor 3 = 1
            // 1 * 2 = 2 + 0 + 0 + 5 = 7
            Assert.Equal(7, waitTime);
        }

        [Fact]
        [Trait("Category", "WaitTime")]
        public void CalculateEstimatedWaitTime_NoElevators_ShouldReturnDefault()
        {
            // Arrange
            _mockBuilding.Setup(b => b.GetElevators()).Returns([]);

            // Act
            var waitTime = _dispatcher.CalculateEstimatedWaitTime(3);

            // Assert
            Assert.Equal(10, waitTime);
        }

        [Fact]
        [Trait("Category", "WaitTime")]
        public void CalculateEstimatedWaitTime_WithQueue_ShouldIncludeQueueTime()
        {
            // Arrange
            var elevator = _elevators[0];
            elevator.AddDestination(5);
            elevator.AddDestination(7);
            elevator.AddDestination(9);

            // Act
            var waitTime = _dispatcher.CalculateEstimatedWaitTime(3);

            // Assert
            // Distance from 5 to 3 = 2 * 2 = 4
            // Queue: 3 stops * 3 = 9
            // Passengers: 0
            // + 5 = 18
            Assert.Equal(18, waitTime);
        }

        #endregion

        #region GetElevatorsServingFloor Tests

        [Fact]
        [Trait("Category", "Serving")]
        public void GetElevatorsServingFloor_ShouldReturnCorrectElevators()
        {
            // Arrange
            var elevator1 = _elevators[0];
            elevator1.AddDestination(5);
            var elevator2 = _elevators[1];
            elevator2.MoveToFloor(5);

            // Act
            var result = _dispatcher.GetElevatorsServingFloor(5);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.ElevatorId == 1);
            Assert.Contains(result, r => r.ElevatorId == 2);
        }

        [Fact]
        [Trait("Category", "Serving")]
        public void GetElevatorsServingFloor_NoElevators_ShouldReturnEmpty()
        {
            // Act
            var result = _dispatcher.GetElevatorsServingFloor(99);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region OptimizeDispatchPatterns Tests

        [Fact]
        [Trait("Category", "Optimize")]
        public void OptimizeDispatchPatterns_ShouldLogOptimization()
        {
            // Act
            _dispatcher.OptimizeDispatchPatterns();

            // Assert
            _mockLogger.Verify(l => l.LogInfo("Optimizing dispatch patterns..."), Times.Once);
            _mockLogger.Verify(l => l.LogInfo(It.IsRegex(@"Elevator \d+ optimized for floors \d+-\d+")),
                Times.AtLeastOnce);
        }

        [Fact]
        [Trait("Category", "Optimize")]
        public void OptimizeDispatchPatterns_SingleElevator_ShouldStillLog()
        {
            // Arrange
            var singleElevator = new List<IElevator> { new Elevator(1) };
            _mockBuilding.Setup(b => b.GetElevators()).Returns(singleElevator);

            // Act
            _dispatcher.OptimizeDispatchPatterns();

            // Assert
            _mockLogger.Verify(l => l.LogInfo("Optimizing dispatch patterns..."), Times.Once);
        }

        #endregion

        #region GetDispatchStatistics Tests

        [Fact]
        [Trait("Category", "Statistics")]
        public void GetDispatchStatistics_ShouldReturnUpdatedStats()
        {
            // Arrange - Make some dispatches
            for (int i = 0; i < 5; i++)
            {
                var request = new FloorRequestDto
                {
                    FloorNumber = i * 2,
                    PassengerCount = 1
                };
                _dispatcher.DispatchElevator(request);
            }

            // Act
            var stats = _dispatcher.GetDispatchStatistics();

            // Assert
            Assert.Equal(5, stats.TotalCalls);
            Assert.Equal(5, stats.SuccessfulDispatch);
            Assert.Equal(0, stats.FailedDispatch);
            Assert.Equal(100, stats.SystemEfficiency);
            Assert.True(stats.AverageResponseTime >= 0);
            Assert.Equal(5, stats.CallsPerFloor.Values.Sum());
            Assert.True(stats.LastUpdated <= DateTime.Now);

            // Check elevator utilization
            Assert.True(stats.ElevatorUtilization.Values.Sum() > 0);
        }

        [Fact]
        [Trait("Category", "Statistics")]
        public void GetDispatchStatistics_WithFailures_ShouldCalculateEfficiency()
        {
            // Arrange
            _mockBuilding.Setup(b => b.GetElevators()).Returns([]);
            var request = new FloorRequestDto { FloorNumber = 3, PassengerCount = 1 };
            _dispatcher.DispatchElevator(request);

            // Act
            var stats = _dispatcher.GetDispatchStatistics();

            // Assert
            Assert.Equal(1, stats.TotalCalls);
            Assert.Equal(0, stats.SuccessfulDispatch);
            Assert.Equal(1, stats.FailedDispatch);
            Assert.Equal(0, stats.SystemEfficiency);
        }

        #endregion

        #region Private Method Coverage Tests

        [Fact]
        [Trait("Category", "Private")]
        public void GetNearestAvailableElevatorInternal_ShouldReturnNearest()
        {
            // Act - Using reflection to test private method
            var method = typeof(DispatcherService).GetMethod("GetNearestAvailableElevatorInternal",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var result = method.Invoke(_dispatcher, [3]) as IElevator;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Id);
        }

        [Fact]
        [Trait("Category", "Private")]
        public void IsElevatorAvailableForFloorInternal_ShouldReturnCorrect()
        {
            // Act - Using reflection to test private method
            var method = typeof(DispatcherService).GetMethod("IsElevatorAvailableForFloorInternal",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var elevator = _elevators[0];
            bool result1 = (bool)method.Invoke(_dispatcher, [elevator, 5]);

            elevator.SetOutOfService();
            var result2 = (bool)method.Invoke(_dispatcher, [elevator, 5]);

            // Assert
            Assert.True(result1);
            Assert.False(result2);
        }

        [Fact]
        [Trait("Category", "Private")]
        public void GetBestElevatorForPassenger_ShouldReturnBest()
        {
            // Act - Using reflection to test private method
            var method = typeof(DispatcherService).GetMethod("GetBestElevatorForPassenger",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var request = new PassengerRequestDto
            {
                Id = 123,
                CurrentFloor = 3,
                DestinationFloor = 7,
                Weight = 70
            };

            var result = method.Invoke(_dispatcher, [request]) as IElevator;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Id);
        }

        [Fact]
        [Trait("Category", "Private")]
        public void AssignElevatorToFloor_ShouldAssignCorrectly()
        {
            // Act - Using reflection to test private method
            var method = typeof(DispatcherService).GetMethod("AssignElevatorToFloor",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var elevator = _elevators[0];
            method.Invoke(_dispatcher, [elevator, 5]);

            // Assert
            Assert.Equal(5, elevator.CurrentFloor);
            Assert.Equal(ElevatorStatus.DoorsOpen, elevator.Status);
        }

        [Fact]
        [Trait("Category", "Private")]
        public void CalculatePriority_ShouldReturnCorrect()
        {
            // Arrange
            _mockBuilding.Setup(b => b.GetPassengerCountOnFloor(3)).Returns(5);

            // Act - Using reflection to test private method
            var method = typeof(DispatcherService).GetMethod("CalculatePriority",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var request = new FloorRequestDto { FloorNumber = 3, PassengerCount = 1 };
            var result = (int)method.Invoke(_dispatcher, [request]);

            // Assert
            Assert.Equal(5, result);
        }

        [Fact]
        [Trait("Category", "Private")]
        public void CalculateEstimatedTravelTime_ShouldReturnCorrect()
        {
            // Act - Using reflection to test private method
            var method = typeof(DispatcherService).GetMethod("CalculateEstimatedTravelTime",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var elevator = _elevators[0];
            elevator.MoveToFloor(5);

            var result = (int)method.Invoke(_dispatcher, [elevator, 8]);

            // Assert
            // Distance 3 * 2 = 6 + 3 = 9
            Assert.Equal(9, result);
        }

        #endregion

        void IDisposable.Dispose()
        {
            _mockBuilding.Reset();
            _mockLogger.Reset();
        }
    }
}