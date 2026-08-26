using Xunit;
using FluentAssertions;
using ElevatorSimulation.Domain.Entities;
using ElevatorSimulation.Domain.Exceptions;
using System;
using System.Linq;

namespace ElevatorSimulation.Domain.Tests
{
    public class BuildingTests
    {
        [Fact]
        public void Constructor_ShouldCreateBuildingWithCorrectFloors()
        {
            // Arrange & Act
            var building = new Building(10);

            // Assert
            building.FloorCount.Should().Be(10);
            building.Floors.Should().HaveCount(11); // 0-10
            building.Elevators.Should().BeEmpty();
        }

        [Fact]
        public void Constructor_WithInvalidFloorCount_ShouldThrowException()
        {
            // Act & Assert
            Action act = () => new Building(0);
            act.Should().Throw<ArgumentException>()
                .WithMessage("Building must have at least >= 1 floor.");
        }

        [Fact]
        public void AddElevator_ShouldAddToBuilding()
        {
            // Arrange
            var building = new Building(10);
            var elevator = new Elevator(1);

            // Act
            building.AddElevator(elevator);

            // Assert
            building.Elevators.Should().Contain(elevator);
            building.Elevators.Count.Should().Be(1);
        }

        [Fact]
        public void AddElevator_DuplicateId_ShouldThrowException()
        {
            // Arrange
            var building = new Building(10);
            var elevator1 = new Elevator(1);
            var elevator2 = new Elevator(1);
            building.AddElevator(elevator1);

            // Act & Assert
            Action act = () => building.AddElevator(elevator2);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"Elevator with ID 1 already exists in the building.");
        }

        [Fact]
        public void AddElevator_NullElevator_ShouldThrowException()
        {
            // Arrange
            var building = new Building(10);

            // Act & Assert
            Action act = () => building.AddElevator(null);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void RemoveElevator_ShouldRemoveFromBuilding()
        {
            // Arrange
            var building = new Building(10);
            var elevator = new Elevator(1);
            building.AddElevator(elevator);

            // Act
            building.RemoveElevator(1);

            // Assert
            building.Elevators.Should().BeEmpty();
        }

        [Fact]
        public void RemoveElevator_NotFound_ShouldThrowException()
        {
            // Arrange
            var building = new Building(10);

            // Act & Assert
            Action act = () => building.RemoveElevator(999);
            act.Should().Throw<ElevatorNotFoundException>()
                .WithMessage("Elevator with ID 999 not found.");
        }

        [Fact]
        public void GetElevator_ShouldReturnCorrectElevator()
        {
            // Arrange
            var building = new Building(10);
            var elevator = new Elevator(1);
            building.AddElevator(elevator);

            // Act
            var result = building.GetElevator(1);

            // Assert
            result.Should().Be(elevator);
        }

        [Fact]
        public void GetElevator_NotFound_ShouldThrowException()
        {
            // Arrange
            var building = new Building(10);

            // Act & Assert
            Action act = () => building.GetElevator(999);
            act.Should().Throw<ElevatorNotFoundException>()
                .WithMessage("Elevator with ID 999 not found.");
        }

        [Fact]
        public void GetFloor_ValidFloor_ShouldReturnFloor()
        {
            // Arrange
            var building = new Building(10);

            // Act
            var floor = building.GetFloor(5);

            // Assert
            floor.Should().NotBeNull();
            floor.FloorNumber.Should().Be(5);
        }

        [Fact]
        public void GetFloor_InvalidFloor_ShouldThrowException()
        {
            // Arrange
            var building = new Building(10);

            // Act & Assert
            Action act = () => building.GetFloor(20);
            act.Should().Throw<InvalidFloorException>()
                .WithMessage("Floor 20 does not exist in the building.");
        }

        [Fact]
        public void IsValidFloor_ShouldReturnCorrect()
        {
            // Arrange
            var building = new Building(10);

            // Act & Assert
            building.IsValidFloor(5).Should().BeTrue();
            building.IsValidFloor(10).Should().BeTrue();
            building.IsValidFloor(0).Should().BeTrue();
            building.IsValidFloor(-1).Should().BeFalse();
            building.IsValidFloor(11).Should().BeFalse();
        }

        [Fact]
        public void GetPassengerCountOnFloor_ShouldReturnCorrectCount()
        {
            // Arrange
            var building = new Building(10);
            var floor = building.GetFloor(5);
            var passenger = new Passenger(1, 5, 10);
            floor.AddWaitingPassenger(passenger);

            // Act
            var count = building.GetPassengerCountOnFloor(5);

            // Assert
            count.Should().Be(1);
        }

        [Fact]
        public void GetPassengerCountOnFloor_InvalidFloor_ShouldThrowException()
        {
            // Arrange
            var building = new Building(10);

            // Act & Assert
            Action act = () => building.GetPassengerCountOnFloor(20);
            act.Should().Throw<InvalidFloorException>();
        }
    }
}