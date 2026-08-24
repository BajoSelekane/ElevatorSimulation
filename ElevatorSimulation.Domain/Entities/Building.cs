using ElevatorSimulation.Domain.Interfaces;
using ElevatorSimulation.Domain.Exceptions;
using System.Collections.ObjectModel;

namespace ElevatorSimulation.Domain.Entities
{
    public class Building : IBuilding
    {
        private readonly List<IElevator> _elevators;
        private readonly List<Floor> _floors;
        private readonly int _floorCount;

        public int FloorCount => _floorCount;
        public IReadOnlyList<IElevator> Elevators => _elevators.AsReadOnly();
        public IReadOnlyList<Floor> Floors => _floors.AsReadOnly();

        public Building(int floorCount)
        {
            if (floorCount < 1)
                throw new ArgumentException("Building must have at least 1 floor.", nameof(floorCount));

            _floorCount = floorCount;
            _elevators = new List<IElevator>();
            _floors = new List<Floor>();

            // Initialize floors
            for (int i = 0; i <= floorCount; i++)
            {
                _floors.Add(new Floor(i));
            }
        }

        public void AddElevator(IElevator elevator)
        {
            if (elevator == null)
                throw new ArgumentNullException(nameof(elevator));

            if (_elevators.Any(e => e.Id == elevator.Id))
                throw new InvalidOperationException($"Elevator with ID {elevator.Id} already exists in the building.");

            _elevators.Add(elevator);
        }

        public void RemoveElevator(int elevatorId)
        {
            var elevator = _elevators.FirstOrDefault(e => e.Id == elevatorId);
            if (elevator == null)
                throw new ElevatorNotFoundException($"Elevator with ID {elevatorId} not found.");

            _elevators.Remove(elevator);
        }

        public IElevator GetElevator(int elevatorId)
        {
            var elevator = _elevators.FirstOrDefault(e => e.Id == elevatorId);
            if (elevator == null)
                throw new ElevatorNotFoundException($"Elevator with ID {elevatorId} not found.");

            return elevator;
        }

        public IReadOnlyList<IElevator> GetElevators()
        {
            return _elevators.AsReadOnly();
        }

        public Floor GetFloor(int floorNumber)
        {
            if (floorNumber < 0 || floorNumber > _floorCount)
                throw new InvalidFloorException($"Floor {floorNumber} does not exist in the building.");

            return _floors[floorNumber];
        }

        public bool IsValidFloor(int floorNumber)
        {
            return floorNumber >= 0 && floorNumber <= _floorCount;
        }

        public int GetPassengerCountOnFloor(int floorNumber)
        {
            var floor = GetFloor(floorNumber);
            return floor.WaitingPassengers.Count;
        }
    }
}