using Xunit;
using FluentAssertions;
using ElevatorSimulation.Application.DTOs;
using ElevatorSimulation.Application.Validators;

namespace ElevatorSimulation.Application.Tests
{
    public class ValidatorsTests
    {
        private readonly FloorRequestValidator _floorValidator;
        private readonly ElevatorRequestValidator _elevatorValidator;
        private readonly PassengerRequestValidator _passengerValidator;

        public ValidatorsTests()
        {
            _floorValidator = new FloorRequestValidator(10);
            _elevatorValidator = new ElevatorRequestValidator(10);
            _passengerValidator = new PassengerRequestValidator(10);
        }

        [Fact]
        public void FloorRequestValidator_ValidRequest_ShouldPass()
        {
            // Arrange
            var request = new FloorRequestDto
            {
                FloorNumber = 5,
                PassengerCount = 2,
                RequestType = "Call"
            };

            // Act
            var result = _floorValidator.Validate(request);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(11)]
        [InlineData(15)]
        public void FloorRequestValidator_InvalidFloor_ShouldFail(int invalidFloor)
        {
            // Arrange
            var request = new FloorRequestDto
            {
                FloorNumber = invalidFloor,
                PassengerCount = 1,
                RequestType = "Call"
            };

            // Act
            var result = _floorValidator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("between 0 and 10"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void FloorRequestValidator_InvalidPassengerCount_ShouldFail(int invalidCount)
        {
            // Arrange
            var request = new FloorRequestDto
            {
                FloorNumber = 5,
                PassengerCount = invalidCount,
                RequestType = "Call"
            };

            // Act
            var result = _floorValidator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("Passenger count"));
        }

        [Fact]
        public void ElevatorRequestValidator_ValidRequest_ShouldPass()
        {
            // Arrange
            var request = new ElevatorRequestDto
            {
                ElevatorId = 1,
                TargetFloor = 5,
                PassengerCount = 2
            };

            // Act
            var result = _elevatorValidator.Validate(request);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ElevatorRequestValidator_InvalidElevatorId_ShouldFail(int invalidId)
        {
            // Arrange
            var request = new ElevatorRequestDto
            {
                ElevatorId = invalidId,
                TargetFloor = 5,
                PassengerCount = 1
            };

            // Act
            var result = _elevatorValidator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("Elevator ID"));
        }

        [Fact]
        public void PassengerRequestValidator_ValidRequest_ShouldPass()
        {
            // Arrange
            var request = new PassengerRequestDto
            {
                Id = 123,
                CurrentFloor = 5,
                DestinationFloor = 10,
                Weight = 70
            };

            // Act
            var result = _passengerValidator.Validate(request);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void PassengerRequestValidator_SameCurrentAndDestination_ShouldFail()
        {
            // Arrange
            var request = new PassengerRequestDto
            {
                Id = 123,
                CurrentFloor = 5,
                DestinationFloor = 5,
                Weight = 70
            };

            // Act
            var result = _passengerValidator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("different"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void PassengerRequestValidator_InvalidWeight_ShouldFail(int invalidWeight)
        {
            // Arrange
            var request = new PassengerRequestDto
            {
                Id = 123,
                CurrentFloor = 5,
                DestinationFloor = 10,
                Weight = invalidWeight
            };

            // Act
            var result = _passengerValidator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("Weight"));
        }
    }
}