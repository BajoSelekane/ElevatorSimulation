using ElevatorSimulation.Domain.Enums;
using ElevatorSimulation.Domain.Interfaces;
using ElevatorSimulation.Domain.Exceptions;

namespace ElevatorSimulation.Domain.Entities
{
    public class Elevator : IElevator
    {
        private readonly int _maxPassengers;
        private readonly Queue<int> _destinationQueue;
        private readonly List<Passenger> _passengers;

        public int Id { get; }
        public int CurrentFloor { get; private set; }
        public ElevatorDirection Direction { get; private set; }
        public ElevatorStatus Status { get; private set; }
        public ElevatorType Type { get; }
        public int PassengerCount => _passengers.Count;
        public int MaxPassengers => _maxPassengers;
        public IReadOnlyCollection<int> DestinationQueue => _destinationQueue;
        public bool IsMoving => Status == ElevatorStatus.Moving;

        public Elevator(int id, ElevatorType type = ElevatorType.Standard, int maxPassengers = 10)
        {
            Id = id;
            Type = type;
            _maxPassengers = maxPassengers;
            _passengers = new List<Passenger>();
            _destinationQueue = new Queue<int>();
            CurrentFloor = 0;
            Direction = ElevatorDirection.Idle;
            Status = ElevatorStatus.Stationary;
        }

        public void MoveToFloor(int floorNumber)
        {
            if (floorNumber < 0)
                throw new InvalidFloorException($"Floor {floorNumber} is invalid. Floor must be 0 or greater.");

            if (Status == ElevatorStatus.DoorsOpen)
                throw new InvalidOperationException("Cannot move elevator while doors are open.");

            if (CurrentFloor == floorNumber)
            {
                OpenDoors();
                return;
            }

            Status = ElevatorStatus.Moving;
            Direction = floorNumber > CurrentFloor ? ElevatorDirection.Up : ElevatorDirection.Down;

            // Simulate movement
            CurrentFloor = floorNumber;

            Status = ElevatorStatus.Stationary;
            Direction = ElevatorDirection.Idle;
        }

        public void OpenDoors()
        {
            if (Status == ElevatorStatus.Moving)
                throw new InvalidOperationException("Cannot open doors while elevator is moving.");

            Status = ElevatorStatus.DoorsOpen;
        }

        public void CloseDoors()
        {
            if (Status != ElevatorStatus.DoorsOpen)
                throw new InvalidOperationException("Doors are not open.");

            Status = ElevatorStatus.Stationary;
        }

        public void AddDestination(int floorNumber)
        {
            if (floorNumber < 0)
                throw new InvalidFloorException($"Floor {floorNumber} is invalid.");

            if (floorNumber == CurrentFloor)
                throw new InvalidOperationException("Cannot add destination to current floor.");

            _destinationQueue.Enqueue(floorNumber);
        }

        public int GetNextDestination()
        {
            if (_destinationQueue.Count == 0)
                throw new InvalidOperationException("No destinations in queue.");

            return _destinationQueue.Peek();
        }

        public void MoveToNextDestination()
        {
            if (_destinationQueue.Count == 0)
                throw new InvalidOperationException("No destinations in queue.");

            var nextFloor = _destinationQueue.Dequeue();
            MoveToFloor(nextFloor);
            OpenDoors();
            // Simulate passenger boarding/alighting
            CloseDoors();
        }

        public bool CanAcceptPassenger(Passenger passenger)
        {
            return _passengers.Count < _maxPassengers && passenger.Weight <= 150; // Example weight limit
        }

        public void BoardPassenger(Passenger passenger)
        {
            if (!CanAcceptPassenger(passenger))
                throw new CapacityExceededException($"Elevator {Id} is at maximum capacity.");

            _passengers.Add(passenger);
        }

        public void AlightPassenger(Passenger passenger)
        {
            if (!_passengers.Contains(passenger))
                throw new InvalidOperationException($"Passenger not found in elevator {Id}.");

            _passengers.Remove(passenger);
        }

        public bool IsPassengerLimitReached()
        {
            return _passengers.Count >= _maxPassengers;
        }

        public override string ToString()
        {
            return $"Elevator {Id} | Floor: {CurrentFloor} | Status: {Status} | " +
                   $"Direction: {Direction} | Passengers: {PassengerCount}/{MaxPassengers}";
        }
    }
}