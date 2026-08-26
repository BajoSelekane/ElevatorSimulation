using Xunit;
using FluentAssertions;
using ElevatorSimulation.Domain.Entities;
using System;

namespace ElevatorSimulation.Domain.Tests
{
    [Trait("Category", "Unit")]
    public class FloorTests
    {
        [Fact]
        public void Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var floor = new Floor(5);

            // Assert
            floor.FloorNumber.Should().Be(5);
            floor.WaitingPassengers.Should().BeEmpty();
            floor.HasElevatorPresent.Should().BeFalse();
            floor.LastServiceTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void AddWaitingPassenger_ValidPassenger_ShouldAdd()
        {
            // Arrange
            var floor = new Floor(5);
            var passenger = new Passenger(1, 5, 10);

            // Act
            floor.AddWaitingPassenger(passenger);

            // Assert
            floor.WaitingPassengers.Should().Contain(passenger);
            passenger.IsWaiting.Should().BeTrue();
            floor.GetWaitingPassengerCount().Should().Be(1);
        }

        [Fact]
        public void AddWaitingPassenger_NullPassenger_ShouldThrowException()
        {
            // Arrange
            var floor = new Floor(5);

            // Act & Assert
            Action act = () => floor.AddWaitingPassenger(null);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void AddWaitingPassenger_WrongFloor_ShouldThrowException()
        {
            // Arrange
            var floor = new Floor(5);
            var passenger = new Passenger(1, 10, 15);

            // Act & Assert
            Action act = () => floor.AddWaitingPassenger(passenger);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Passenger is on floor 10, not floor 5.");
        }

        [Fact]
        public void RemoveWaitingPassenger_ValidPassenger_ShouldRemove()
        {
            // Arrange
            var floor = new Floor(5);
            var passenger = new Passenger(1, 5, 10);
            floor.AddWaitingPassenger(passenger);

            // Act
            var removed = floor.RemoveWaitingPassenger(1);

            // Assert
            removed.Should().Be(passenger);
            floor.WaitingPassengers.Should().BeEmpty();
            passenger.IsWaiting.Should().BeFalse();
        }

        [Fact]
        public void RemoveWaitingPassenger_NotFound_ShouldThrowException()
        {
            // Arrange
            var floor = new Floor(5);

            // Act & Assert
            Action act = () => floor.RemoveWaitingPassenger(999);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Passenger 999 not found on floor 5.");
        }

        [Fact]
        public void ClearWaitingPassengers_ShouldRemoveAll()
        {
            // Arrange
            var floor = new Floor(5);
            var passenger1 = new Passenger(1, 5, 10);
            var passenger2 = new Passenger(2, 5, 8);
            floor.AddWaitingPassenger(passenger1);
            floor.AddWaitingPassenger(passenger2);

            // Act
            floor.ClearWaitingPassengers();

            // Assert
            floor.WaitingPassengers.Should().BeEmpty();
            passenger1.IsWaiting.Should().BeFalse();
            passenger2.IsWaiting.Should().BeFalse();
        }

        [Fact]
        public void HasWaitingPassengers_ShouldReturnCorrect()
        {
            // Arrange
            var floor = new Floor(5);

            // Act & Assert
            floor.HasWaitingPassengers().Should().BeFalse();

            // Arrange
            var passenger = new Passenger(1, 5, 10);
            floor.AddWaitingPassenger(passenger);

            // Act & Assert
            floor.HasWaitingPassengers().Should().BeTrue();
        }

        [Fact]
        public void HasElevatorPresent_ShouldBeSettable()
        {
            var floor = new Floor(1);

            floor.HasElevatorPresent.Should().BeFalse();
            floor.HasElevatorPresent = true;
            floor.HasElevatorPresent.Should().BeTrue();
        }
    }
}