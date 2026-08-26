using Xunit;
using FluentAssertions;
using ElevatorSimulation.Application.DTOs;
using ElevatorSimulation.Application.Validators;
using ElevatorSimulation.Domain.Entities;

namespace ElevatorSimulation.Application.Tests
{
    [Trait("Category", "Unit")]
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
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains("between 0 and 10"));
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
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Passenger count"));
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
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Elevator ID"));
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
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains("different"));
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
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Weight"));
        }

        [Fact]
        public void FloorRequestValidator_ValidateRequest_ShouldReturnCustomResult()
        {
            var valid = _floorValidator.ValidateRequest(new FloorRequestDto
            {
                FloorNumber = 3,
                PassengerCount = 1,
                RequestType = "Destination"
            });
            var invalid = _floorValidator.ValidateRequest(new FloorRequestDto
            {
                FloorNumber = 3,
                PassengerCount = 1,
                RequestType = ""
            });

            valid.IsValid.Should().BeTrue();
            invalid.IsValid.Should().BeFalse();
            invalid.Errors.Should().Contain(e => e.Contains("Request type"));
        }

        [Fact]
        public void BuildingValidator_ShouldRequireElevatorsAndFloors()
        {
            var validator = new BuildingValidator();
            var empty = new Building(10);
            var ready = new Building(10);
            ready.AddElevator(new Elevator(1));

            validator.Validate(empty).IsValid.Should().BeFalse();
            validator.Validate(ready).IsValid.Should().BeTrue();
        }

        [Fact]
        public void ElevatorValidator_ShouldValidateIdAndCapacity()
        {
            var validator = new ElevatorValidator();

            validator.Validate(new Elevator(1, maxPassengers: 10)).IsValid.Should().BeTrue();
            validator.Validate(new Elevator(0, maxPassengers: 10)).IsValid.Should().BeFalse();
            validator.Validate(new Elevator(1, maxPassengers: 25)).IsValid.Should().BeFalse();
        }

        [Fact]
        public void PassengerValidator_ShouldRejectSameFloorAndInvalidWeight()
        {
            var validator = new PassengerValidator(10);

            validator.Validate(new Passenger(1, 2, 8, 70)).IsValid.Should().BeTrue();
            validator.Validate(new Passenger(1, 2, 2, 70)).IsValid.Should().BeFalse();
            validator.Validate(new Passenger(1, 2, 8, 0)).IsValid.Should().BeFalse();
            validator.Validate(new Passenger(0, 2, 8, 70)).IsValid.Should().BeFalse();
        }
    }
}