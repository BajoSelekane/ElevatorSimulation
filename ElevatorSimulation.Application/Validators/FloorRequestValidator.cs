using FluentValidation;
using ElevatorSimulation.Application.DTOs;

namespace ElevatorSimulation.Application.Validators
{
    public class FloorRequestValidator : AbstractValidator<FloorRequestDto>
    {
        private readonly int _maxFloor;

        public FloorRequestValidator(int maxFloor = 10)
        {
            _maxFloor = maxFloor;

            RuleFor(x => x.FloorNumber)
                .GreaterThanOrEqualTo(0)
                .WithMessage($"Floor number must be between 0 and {_maxFloor}")
                .LessThanOrEqualTo(_maxFloor)
                .WithMessage($"Floor number must be between 0 and {_maxFloor}");

            RuleFor(x => x.PassengerCount)
                .GreaterThan(0)
                .WithMessage("Passenger count must be at least 1")
                .LessThanOrEqualTo(10)
                .WithMessage("Passenger count cannot exceed 10");

            RuleFor(x => x.RequestType)
                .NotEmpty()
                .WithMessage("Request type is required")
                .Must(x => x == "Call" || x == "Destination")
                .WithMessage("Request type must be either 'Call' or 'Destination'");
        }

        public ValidationResult ValidateRequest(FloorRequestDto request)
        {
            var result = Validate(request);
            return new ValidationResult
            {
                IsValid = result.IsValid,
                Errors = result.Errors.Select(e => e.ErrorMessage).ToList()
            };
        }
    }

    public class ElevatorRequestValidator : AbstractValidator<ElevatorRequestDto>
    {
        private readonly int _maxFloor;

        public ElevatorRequestValidator(int maxFloor = 10)
        {
            _maxFloor = maxFloor;

            RuleFor(x => x.ElevatorId)
                .GreaterThan(0)
                .WithMessage("Elevator ID must be greater than 0");

            RuleFor(x => x.TargetFloor)
                .GreaterThanOrEqualTo(0)
                .WithMessage($"Target floor must be between 0 and {_maxFloor}")
                .LessThanOrEqualTo(_maxFloor)
                .WithMessage($"Target floor must be between 0 and {_maxFloor}");

            RuleFor(x => x.PassengerCount)
                .GreaterThan(0)
                .WithMessage("Passenger count must be at least 1")
                .LessThanOrEqualTo(10)
                .WithMessage("Passenger count cannot exceed 10");
        }
    }

    public class PassengerRequestValidator : AbstractValidator<PassengerRequestDto>
    {
        private readonly int _maxFloor;

        public PassengerRequestValidator(int maxFloor = 10)
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

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}