using FluentValidation;
using ElevatorSimulation.Domain.Entities;

namespace ElevatorSimulation.Application.Validators
{
    public class BuildingValidator : AbstractValidator<Building>
    {
        public BuildingValidator()
        {
            RuleFor(x => x.FloorCount)
                .GreaterThan(0)
                .WithMessage("Building must have at least 1 floor")
                .LessThanOrEqualTo(50)
                .WithMessage("Building cannot have more than 50 floors");

            RuleFor(x => x.Elevators)
                .NotNull()
                .WithMessage("Elevators collection cannot be null");

            RuleFor(x => x.Elevators.Count)
                .GreaterThan(0)
                .WithMessage("Building must have at least 1 elevator")
                .LessThanOrEqualTo(10)
                .WithMessage("Building cannot have more than 10 elevators");
        }
    }

    public class ElevatorValidator : AbstractValidator<Elevator>
    {
        public ElevatorValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Elevator ID must be greater than 0");

            RuleFor(x => x.MaxPassengers)
                .GreaterThan(0)
                .WithMessage("Maximum passengers must be greater than 0")
                .LessThanOrEqualTo(20)
                .WithMessage("Maximum passengers cannot exceed 20");

            RuleFor(x => x.CurrentFloor)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Current floor cannot be negative");
        }
    }

    public class PassengerValidator : AbstractValidator<Passenger>
    {
        private readonly int _maxFloor;

        public PassengerValidator(int maxFloor = 10)
        {
            _maxFloor = maxFloor;

            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Passenger ID must be greater than 0");

            RuleFor(x => x.CurrentFloor)
                .GreaterThanOrEqualTo(0)
                .WithMessage($"Current floor must be between 0 and {_maxFloor}")
                .LessThanOrEqualTo(_maxFloor)
                .WithMessage($"Current floor must be between 0 and {_maxFloor}");

            RuleFor(x => x.DestinationFloor)
                .GreaterThanOrEqualTo(0)
                .WithMessage($"Destination floor must be between 0 and {_maxFloor}")
                .LessThanOrEqualTo(_maxFloor)
                .WithMessage($"Destination floor must be between 0 and {_maxFloor}");

            RuleFor(x => x)
                .Must(x => x.CurrentFloor != x.DestinationFloor)
                .WithMessage("Current floor and destination floor must be different");

            RuleFor(x => x.Weight)
                .GreaterThan(0)
                .WithMessage("Weight must be greater than 0")
                .LessThanOrEqualTo(300)
                .WithMessage("Weight cannot exceed 300 kg");
        }
    }
}