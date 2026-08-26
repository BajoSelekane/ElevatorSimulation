using Xunit;
using FluentAssertions;
using ElevatorSimulation.Domain.Entities;
using ElevatorSimulation.Domain.Enums;
using ElevatorSimulation.Domain.Exceptions;
using System;
using System.Linq;

namespace ElevatorSimulation.Domain.Tests
{
    [Trait("Category", "Unit")]
    public class ElevatorTests
    {
        [Fact]
        public void Constructor_ShouldInitializeElevatorCorrectly()
        {
            // Arrange & Act
            var elevator = new Elevator(1, ElevatorType.Standard, 10);

            // Assert
            elevator.Id.Should().Be(1);
            elevator.Type.Should().Be(ElevatorType.Standard);
            elevator.MaxPassengers.Should().Be(10);
            elevator.CurrentFloor.Should().Be(0);
            elevator.Status.Should().Be(ElevatorStatus.Stationary);
            elevator.Direction.Should().Be(ElevatorDirection.Idle);
            elevator.PassengerCount.Should().Be(0);
            elevator.DestinationQueue.Should().BeEmpty();
            elevator.TotalTrips.Should().Be(0);
            elevator.TotalPassengersServed.Should().Be(0);
            elevator.TotalDistanceTraveled.Should().Be(0);
        }

        [Theory]
        [InlineData(1, ElevatorType.Standard, 10)]
        [InlineData(2, ElevatorType.HighSpeed, 15)]
        [InlineData(3, ElevatorType.Freight, 5)]
        public void Constructor_WithValidParameters_ShouldSetProperties(int id, ElevatorType type, int maxPassengers)
        {
            // Act
            var elevator = new Elevator(id, type, maxPassengers);

            // Assert
            elevator.Id.Should().Be(id);
            elevator.Type.Should().Be(type);
            elevator.MaxPassengers.Should().Be(maxPassengers);
        }

        [Fact]
        public void MoveToFloor_ValidFloor_ShouldUpdatePositionAndRaiseEvent()
        {
            // Arrange
            var elevator = new Elevator(1);
            var eventRaised = false;
            elevator.ElevatorMoved += (sender, args) => eventRaised = true;

            // Act
            elevator.MoveToFloor(5);

            // Assert
            elevator.CurrentFloor.Should().Be(5);
            elevator.Status.Should().Be(ElevatorStatus.Stationary);
            elevator.Direction.Should().Be(ElevatorDirection.Idle);
            elevator.TotalTrips.Should().Be(1);
            elevator.TotalDistanceTraveled.Should().Be(5);
            eventRaised.Should().BeTrue();
        }

        [Fact]
        public void MoveToFloor_WhenDoorsOpen_ShouldThrowException()
        {
            // Arrange
            var elevator = new Elevator(1);
            elevator.OpenDoors();

            // Act & Assert
            Action act = () => elevator.MoveToFloor(5);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot move elevator while doors are open.");
        }

        [Fact]
        public void MoveToFloor_WhenOutOfService_ShouldThrowException()
        {
            // Arrange
            var elevator = new Elevator(1);
            elevator.SetOutOfService();

            // Act & Assert
            Action act = () => elevator.MoveToFloor(5);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Elevator is out of service.");
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-5)]
        [InlineData(-10)]
        public void MoveToFloor_InvalidFloor_ShouldThrowInvalidFloorException(int invalidFloor)
        {
            // Arrange
            var elevator = new Elevator(1);

            // Act & Assert
            Action act = () => elevator.MoveToFloor(invalidFloor);
            act.Should().Throw<InvalidFloorException>()
                .WithMessage($"Floor {invalidFloor} is invalid. Floor must be 0 or greater.");
        }

        [Fact]
        public void OpenDoors_ShouldChangeStatusToDoorsOpenAndRaiseEvent()
        {
            // Arrange
            var elevator = new Elevator(1);
            var eventRaised = false;
            elevator.DoorsOpened += (sender, args) => eventRaised = true;

            // Act
            elevator.OpenDoors();

            // Assert
            elevator.Status.Should().Be(ElevatorStatus.DoorsOpen);
            eventRaised.Should().BeTrue();
        }

        [Fact]
        public void OpenDoors_WhenOutOfService_ShouldThrowException()
        {
            var elevator = new Elevator(1);
            elevator.SetOutOfService();

            Action act = () => elevator.OpenDoors();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Elevator is out of service.");
        }

        [Fact]
        public void MoveToFloor_SameFloor_ShouldOpenDoors()
        {
            var elevator = new Elevator(1);

            elevator.MoveToFloor(0);

            elevator.CurrentFloor.Should().Be(0);
            elevator.Status.Should().Be(ElevatorStatus.DoorsOpen);
            elevator.TotalTrips.Should().Be(0);
        }

        [Fact]
        public void MoveToFloor_Downward_ShouldUpdateDistance()
        {
            var elevator = new Elevator(1);
            elevator.MoveToFloor(8);

            elevator.MoveToFloor(3);

            elevator.CurrentFloor.Should().Be(3);
            elevator.TotalDistanceTraveled.Should().Be(13);
            elevator.TotalTrips.Should().Be(2);
            elevator.IsMoving.Should().BeFalse();
            elevator.IsAvailable.Should().BeTrue();
        }

        [Fact]
        public void IsPassengerLimitReached_ShouldReflectCapacity()
        {
            var elevator = new Elevator(1, maxPassengers: 1);

            elevator.IsPassengerLimitReached().Should().BeFalse();
            elevator.BoardPassenger(new Passenger(1, 0, 4));
            elevator.IsPassengerLimitReached().Should().BeTrue();
        }

        [Fact]
        public void MoveToNextDestination_EmptyQueue_ShouldThrow()
        {
            var elevator = new Elevator(1);

            Action act = () => elevator.MoveToNextDestination();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("No destinations in queue.");
        }

        [Fact]
        public void MoveToNextDestination_ShouldAlightPassengersAtDestination()
        {
            var elevator = new Elevator(1);
            var passenger = new Passenger(1, 0, 4);
            elevator.BoardPassenger(passenger);

            elevator.MoveToNextDestination();

            elevator.CurrentFloor.Should().Be(4);
            elevator.PassengerCount.Should().Be(0);
        }

        [Fact]
        public void IsAvailable_WhenOutOfService_ShouldBeFalse()
        {
            var elevator = new Elevator(1);
            elevator.SetOutOfService();

            elevator.IsAvailable.Should().BeFalse();
        }

        [Fact]
        public void CloseDoors_ShouldChangeStatusToStationaryAndRaiseEvent()
        {
            // Arrange
            var elevator = new Elevator(1);
            elevator.OpenDoors();
            var eventRaised = false;
            elevator.DoorsClosed += (sender, args) => eventRaised = true;

            // Act
            elevator.CloseDoors();

            // Assert
            elevator.Status.Should().Be(ElevatorStatus.Stationary);
            eventRaised.Should().BeTrue();
        }

        [Fact]
        public void CloseDoors_WhenNotOpen_ShouldThrowException()
        {
            // Arrange
            var elevator = new Elevator(1);

            // Act & Assert
            Action act = () => elevator.CloseDoors();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Doors are not open.");
        }

        [Fact]
        public void AddDestination_ValidFloor_ShouldAddToQueue()
        {
            // Arrange
            var elevator = new Elevator(1);

            // Act
            elevator.AddDestination(5);

            // Assert
            elevator.DestinationQueue.Should().Contain(5);
            elevator.DestinationQueue.Count.Should().Be(1);
        }

        [Fact]
        public void AddDestination_DuplicateFloor_ShouldNotAddDuplicate()
        {
            // Arrange
            var elevator = new Elevator(1);

            // Act
            elevator.AddDestination(5);
            elevator.AddDestination(5);

            // Assert
            elevator.DestinationQueue.Should().ContainSingle("5");
        }

        [Fact]
        public void AddDestination_InvalidFloor_ShouldThrowException()
        {
            // Arrange
            var elevator = new Elevator(1);

            // Act & Assert
            Action act = () => elevator.AddDestination(-1);
            act.Should().Throw<InvalidFloorException>()
                .WithMessage("Floor -1 is invalid.");
        }

        [Fact]
        public void AddDestination_CurrentFloor_ShouldThrowException()
        {
            // Arrange
            var elevator = new Elevator(1);

            // Act & Assert
            Action act = () => elevator.AddDestination(0);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot add destination to current floor.");
        }

        [Fact]
        public void GetNextDestination_WhenQueueHasItems_ShouldReturnNext()
        {
            // Arrange
            var elevator = new Elevator(1);
            elevator.AddDestination(5);
            elevator.AddDestination(3);

            // Act
            var next = elevator.GetNextDestination();

            // Assert
            next.Should().Be(5);
            elevator.DestinationQueue.Count.Should().Be(2); // Queue not modified
        }

        [Fact]
        public void GetNextDestination_WhenQueueEmpty_ShouldThrowException()
        {
            // Arrange
            var elevator = new Elevator(1);

            // Act & Assert
            Action act = () => elevator.GetNextDestination();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("No destinations in queue.");
        }

        [Fact]
        public void MoveToNextDestination_ShouldProcessQueueAndRaiseEvents()
        {
            // Arrange
            var elevator = new Elevator(1);
            elevator.AddDestination(5);
            var movedRaised = false;
            var stoppedRaised = false;

            elevator.ElevatorMoved += (sender, args) => movedRaised = true;
            elevator.ElevatorStopped += (sender, args) => stoppedRaised = true;

            // Act
            elevator.MoveToNextDestination();

            // Assert
            elevator.CurrentFloor.Should().Be(5);
            elevator.DestinationQueue.Should().BeEmpty();
            elevator.Status.Should().Be(ElevatorStatus.Stationary);
            movedRaised.Should().BeTrue();
            stoppedRaised.Should().BeTrue();
            elevator.TotalTrips.Should().Be(1);
        }

        [Fact]
        public void BoardPassenger_WhenCapacityAvailable_ShouldAddPassenger()
        {
            // Arrange
            var elevator = new Elevator(1);
            var passenger = new Passenger(1, 0, 5);

            // Act
            elevator.BoardPassenger(passenger);

            // Assert
            elevator.PassengerCount.Should().Be(1);
            elevator.TotalPassengersServed.Should().Be(1);
            passenger.IsWaiting.Should().BeFalse();
            elevator.DestinationQueue.Should().Contain(5);
        }

        [Fact]
        public void BoardPassenger_WhenCapacityFull_ShouldThrowException()
        {
            // Arrange
            var elevator = new Elevator(1, maxPassengers: 1);
            var passenger1 = new Passenger(1, 0, 5);
            var passenger2 = new Passenger(2, 0, 6);
            elevator.BoardPassenger(passenger1);

            // Act & Assert
            Action act = () => elevator.BoardPassenger(passenger2);
            act.Should().Throw<CapacityExceededException>()
                .WithMessage($"Elevator {elevator.Id} is at maximum capacity.");
        }

        [Fact]
        public void CanAcceptPassenger_WithCapacityAndWeight_ShouldReturnCorrect()
        {
            // Arrange
            var elevator = new Elevator(1);
            var passenger = new Passenger(1, 0, 5, 70);
            var heavyPassenger = new Passenger(2, 0, 6, 200);

            // Act & Assert
            elevator.CanAcceptPassenger(passenger).Should().BeTrue();
            elevator.CanAcceptPassenger(heavyPassenger).Should().BeFalse();
        }

        [Fact]
        public void AlightPassenger_WhenPassengerExists_ShouldRemove()
        {
            // Arrange
            var elevator = new Elevator(1);
            var passenger = new Passenger(1, 0, 5);
            elevator.BoardPassenger(passenger);

            // Act
            elevator.AlightPassenger(passenger);

            // Assert
            elevator.PassengerCount.Should().Be(0);
        }

        [Fact]
        public void AlightPassenger_WhenPassengerNotFound_ShouldThrowException()
        {
            // Arrange
            var elevator = new Elevator(1);
            var passenger = new Passenger(1, 0, 5);

            // Act & Assert
            Action act = () => elevator.AlightPassenger(passenger);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"Passenger not found in elevator {elevator.Id}.");
        }

        [Fact]
        public void SetOutOfService_ShouldClearStateAndMarkOutOfService()
        {
            // Arrange
            var elevator = new Elevator(1);
            var passenger = new Passenger(1, 0, 5);
            elevator.BoardPassenger(passenger);
            elevator.AddDestination(5);

            // Act
            elevator.SetOutOfService();

            // Assert
            elevator.Status.Should().Be(ElevatorStatus.OutOfService);
            elevator.PassengerCount.Should().Be(0);
            elevator.DestinationQueue.Should().BeEmpty();
        }

        [Fact]
        public void SetBackInService_ShouldRestoreService()
        {
            // Arrange
            var elevator = new Elevator(1);
            elevator.SetOutOfService();

            // Act
            elevator.SetBackInService();

            // Assert
            elevator.Status.Should().Be(ElevatorStatus.Stationary);
            elevator.Direction.Should().Be(ElevatorDirection.Idle);
        }

        [Fact]
        public void ToString_ShouldReturnFormattedString()
        {
            // Arrange
            var elevator = new Elevator(1, ElevatorType.Standard, 10);
            elevator.MoveToFloor(5);

            // Act
            var result = elevator.ToString();

            // Assert
            result.Should().Contain("Elevator 1");
            result.Should().Contain("Floor: 5");
            result.Should().Contain("Stationary");
            result.Should().Contain("Passengers: 0/10");
            result.Should().Contain("Type: Standard");
            result.Should().Contain("Trips: 1");
        }
    }
}