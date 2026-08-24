using Xunit;
using FluentAssertions;
using ElevatorSimulation.Domain.Entities;
using ElevatorSimulation.Domain.Enums;

namespace ElevatorSimulation.Domain.Tests
{
    public class PassengerTests
    {
        [Fact]
        public void Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var passenger = new Passenger(1, 5, 10, 75);

            // Assert
            passenger.Id.Should().Be(1);
            passenger.CurrentFloor.Should().Be(5);
            passenger.DestinationFloor.Should().Be(10);
            passenger.Weight.Should().Be(75);
            passenger.IsWaiting.Should().BeTrue();
            passenger.Status.Should().Be(PassengerStatus.Waiting);
            passenger.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            passenger.BoardedAt.Should().BeNull();
            passenger.CompletedAt.Should().BeNull();
        }

        [Fact]
        public void Constructor_DefaultWeight_ShouldBe70()
        {
            // Arrange & Act
            var passenger = new Passenger(1, 5, 10);

            // Assert
            passenger.Weight.Should().Be(70);
        }

        [Fact]
        public void GetWaitingTime_WhenWaiting_ShouldCalculateCorrectly()
        {
            // Arrange
            var passenger = new Passenger(1, 5, 10);
            System.Threading.Thread.Sleep(100);

            // Act
            var waitTime = passenger.GetWaitingTime();

            // Assert
            waitTime.Should().BeGreaterThan(0);
            waitTime.Should().BeLessThan(1);
        }

        [Fact]
        public void GetWaitingTime_WhenCompleted_ShouldReturnZero()
        {
            // Arrange
            var passenger = new Passenger(1, 5, 10);
            passenger.Status = PassengerStatus.Completed;
            passenger.CompletedAt = DateTime.Now;

            // Act
            var waitTime = passenger.GetWaitingTime();

            // Assert
            waitTime.Should().Be(0);
        }

        [Fact]
        public void BoardedAt_WhenBoarding_ShouldBeSet()
        {
            // Arrange
            var passenger = new Passenger(1, 5, 10);
            var boardTime = DateTime.Now;

            // Act
            passenger.Status = PassengerStatus.Boarding;
            passenger.BoardedAt = boardTime;

            // Assert
            passenger.BoardedAt.Should().Be(boardTime);
            passenger.Status.Should().Be(PassengerStatus.Boarding);
        }
    }
}