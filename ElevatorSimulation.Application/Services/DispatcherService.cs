using ElevatorSimulation.Domain.Entities;
using ElevatorSimulation.Domain.Interfaces;
using ElevatorSimulation.Domain.Exceptions;
using ElevatorSimulation.Application.Interfaces;

namespace ElevatorSimulation.Application.Services
{
    public class DispatcherService : IDispatcherService
    {
        private readonly IBuilding _building;
        private readonly IElevatorService _elevatorService;

        public DispatcherService(IBuilding building, IElevatorService elevatorService)
        {
            _building = building;
            _elevatorService = elevatorService;
        }

        public Elevator DispatchElevator(int floorNumber, int passengerCount = 1)
        {
            var availableElevators = _building.GetElevators()
                .Where(e => e.Status != Domain.Enums.ElevatorStatus.OutOfService)
                .ToList();

            if (!availableElevators.Any())
                throw new ElevatorNotFoundException("No available elevators in the building.");

            // Find nearest available elevator
            var nearestElevator = FindNearestElevator(availableElevators, floorNumber);

            if (nearestElevator == null)
                throw new ElevatorNotFoundException("No suitable elevator found.");

            // Check capacity
            if (nearestElevator.IsPassengerLimitReached() ||
                nearestElevator.PassengerCount + passengerCount > nearestElevator.MaxPassengers)
            {
                // Try to find another elevator
                var alternateElevator = FindAlternateElevator(availableElevators, floorNumber, passengerCount);
                if (alternateElevator != null)
                {
                    nearestElevator = alternateElevator;
                }
                else
                {
                    throw new CapacityExceededException($"All elevators are at capacity. Please wait.");
                }
            }

            AssignElevatorToFloor(nearestElevator, floorNumber);
            return nearestElevator;
        }

        public void AssignElevatorToFloor(Elevator elevator, int floorNumber)
        {
            if (elevator.CurrentFloor != floorNumber)
            {
                elevator.AddDestination(floorNumber);
                elevator.MoveToNextDestination();
            }
            else
            {
                elevator.OpenDoors();
                // Simulate passenger boarding
                System.Threading.Thread.Sleep(2000);
                elevator.CloseDoors();
            }
        }

        public Elevator GetNearestAvailableElevator(int floorNumber)
        {
            var elevators = _building.GetElevators()
                .Where(e => e.Status != Domain.Enums.ElevatorStatus.OutOfService)
                .ToList();

            return FindNearestElevator(elevators, floorNumber);
        }

        public bool IsElevatorAvailableForFloor(Elevator elevator, int floorNumber)
        {
            if (elevator.Status == Domain.Enums.ElevatorStatus.OutOfService)
                return false;

            if (elevator.IsPassengerLimitReached())
                return false;

            // Check if elevator can physically reach the floor
            if (elevator.Type == Domain.Enums.ElevatorType.Freight)
            {
                // Freight elevators might not service certain floors
                return floorNumber >= 0 && floorNumber <= _building.FloorCount;
            }

            return true;
        }

        private Elevator FindNearestElevator(List<Elevator> elevators, int floorNumber)
        {
            return elevators
                .Where(e => IsElevatorAvailableForFloor(e, floorNumber))
                .OrderBy(e => Math.Abs(e.CurrentFloor - floorNumber))
                .FirstOrDefault();
        }

        private Elevator FindAlternateElevator(List<Elevator> elevators, int floorNumber, int passengerCount)
        {
            return elevators
                .Where(e => IsElevatorAvailableForFloor(e, floorNumber) &&
                           e.PassengerCount + passengerCount <= e.MaxPassengers)
                .OrderBy(e => Math.Abs(e.CurrentFloor - floorNumber))
                .Skip(1) // Skip the first one (which was over capacity)
                .FirstOrDefault();
        }
    }
}