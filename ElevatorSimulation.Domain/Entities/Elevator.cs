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
        private readonly object _lockObject = new object();

        public int Id { get; }
        public int CurrentFloor { get; set; }
        public ElevatorDirection Direction { get; private set; }
        public ElevatorStatus Status { get; private set; }
        public ElevatorType Type { get; }
        public int PassengerCount => _passengers.Count;
        public int MaxPassengers => _maxPassengers;
        public IReadOnlyCollection<int> DestinationQueue => _destinationQueue;
        public bool IsMoving => Status == ElevatorStatus.Moving;
        public bool IsAvailable => Status != ElevatorStatus.OutOfService && !IsMoving;
        public DateTime LastMovementTime { get; private set; }
        public int TotalTrips { get; private set; }
        public int TotalPassengersServed { get; private set; }
        public double TotalDistanceTraveled { get; private set; }

        public event EventHandler<ElevatorEventArgs> ElevatorMoved;
        public event EventHandler<ElevatorEventArgs> ElevatorStopped;
        public event EventHandler<ElevatorEventArgs> DoorsOpened;
        public event EventHandler<ElevatorEventArgs> DoorsClosed;

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
            LastMovementTime = DateTime.Now;
            TotalTrips = 0;
            TotalPassengersServed = 0;
            TotalDistanceTraveled = 0;
        }

        public void MoveToFloor(int floorNumber)
        {
            lock (_lockObject)
            {
                if (floorNumber < 0)
                    throw new InvalidFloorException($"Floor {floorNumber} is invalid. Floor must be 0 or greater.");

                if (Status == ElevatorStatus.DoorsOpen)
                    throw new InvalidOperationException("Cannot move elevator while doors are open.");

                if (Status == ElevatorStatus.OutOfService)
                    throw new InvalidOperationException("Elevator is out of service.");

                if (CurrentFloor == floorNumber)
                {
                    OpenDoors();
                    return;
                }

                Status = ElevatorStatus.Moving;
                Direction = floorNumber > CurrentFloor ? ElevatorDirection.Up : ElevatorDirection.Down;

                // Simulate movement
                var distance = Math.Abs(floorNumber - CurrentFloor);
                TotalDistanceTraveled += distance;
                CurrentFloor = floorNumber;
                LastMovementTime = DateTime.Now;

                if (distance > 0)
                    TotalTrips++;

                Status = ElevatorStatus.Stationary;
                Direction = ElevatorDirection.Idle;

                OnElevatorMoved(new ElevatorEventArgs(this, CurrentFloor));
            }
        }

        public void OpenDoors()
        {
            lock (_lockObject)
            {
                if (Status == ElevatorStatus.Moving)
                    throw new InvalidOperationException("Cannot open doors while elevator is moving.");

                if (Status == ElevatorStatus.OutOfService)
                    throw new InvalidOperationException("Elevator is out of service.");

                Status = ElevatorStatus.DoorsOpen;
                OnDoorsOpened(new ElevatorEventArgs(this, CurrentFloor));
            }
        }

        public void CloseDoors()
        {
            lock (_lockObject)
            {
                if (Status != ElevatorStatus.DoorsOpen)
                    throw new InvalidOperationException("Doors are not open.");

                Status = ElevatorStatus.Stationary;
                OnDoorsClosed(new ElevatorEventArgs(this, CurrentFloor));
            }
        }

        public void AddDestination(int floorNumber)
        {
            lock (_lockObject)
            {
                if (floorNumber < 0)
                    throw new InvalidFloorException($"Floor {floorNumber} is invalid.");

                if (floorNumber == CurrentFloor)
                    throw new InvalidOperationException("Cannot add destination to current floor.");

                if (_destinationQueue.Contains(floorNumber))
                    return; // Avoid duplicates

                _destinationQueue.Enqueue(floorNumber);
            }
        }

        public int GetNextDestination()
        {
            lock (_lockObject)
            {
                if (_destinationQueue.Count == 0)
                    throw new InvalidOperationException("No destinations in queue.");

                return _destinationQueue.Peek();
            }
        }

        public void MoveToNextDestination()
        {
            lock (_lockObject)
            {
                if (_destinationQueue.Count == 0)
                    throw new InvalidOperationException("No destinations in queue.");

                var nextFloor = _destinationQueue.Dequeue();
                MoveToFloor(nextFloor);
                OpenDoors();

                // Process passengers
                ProcessPassengers();

                CloseDoors();
                OnElevatorStopped(new ElevatorEventArgs(this, CurrentFloor));
            }
        }

        public bool CanAcceptPassenger(Passenger passenger)
        {
            lock (_lockObject)
            {
                return _passengers.Count < _maxPassengers && passenger.Weight <= 150;
            }
        }

        public void BoardPassenger(Passenger passenger)
        {
            lock (_lockObject)
            {
                if (!CanAcceptPassenger(passenger))
                    throw new CapacityExceededException($"Elevator {Id} is at maximum capacity.");

                _passengers.Add(passenger);
                passenger.IsWaiting = false;
                TotalPassengersServed++;
                AddDestination(passenger.DestinationFloor);
            }
        }

        public void AlightPassenger(Passenger passenger)
        {
            lock (_lockObject)
            {
                if (!_passengers.Contains(passenger))
                    throw new InvalidOperationException($"Passenger not found in elevator {Id}.");

                _passengers.Remove(passenger);
            }
        }

        public bool IsPassengerLimitReached()
        {
            lock (_lockObject)
            {
                return _passengers.Count >= _maxPassengers;
            }
        }

        public void SetOutOfService()
        {
            lock (_lockObject)
            {
                Status = ElevatorStatus.OutOfService;
                _destinationQueue.Clear();
                _passengers.Clear();
            }
        }

        public void SetBackInService()
        {
            lock (_lockObject)
            {
                Status = ElevatorStatus.Stationary;
                Direction = ElevatorDirection.Idle;
            }
        }

        private void ProcessPassengers()
        {
            var passengersToAlight = _passengers
                .Where(p => p.DestinationFloor == CurrentFloor)
                .ToList();

            foreach (var passenger in passengersToAlight)
            {
                AlightPassenger(passenger);
            }

            // Board waiting passengers (simplified)
            // In real implementation, this would be handled by the dispatcher
        }

        protected virtual void OnElevatorMoved(ElevatorEventArgs e)
        {
            ElevatorMoved?.Invoke(this, e);
        }

        protected virtual void OnElevatorStopped(ElevatorEventArgs e)
        {
            ElevatorStopped?.Invoke(this, e);
        }

        protected virtual void OnDoorsOpened(ElevatorEventArgs e)
        {
            DoorsOpened?.Invoke(this, e);
        }

        protected virtual void OnDoorsClosed(ElevatorEventArgs e)
        {
            DoorsClosed?.Invoke(this, e);
        }

        public override string ToString()
        {
            return $"Elevator {Id} | Floor: {CurrentFloor} | Status: {Status} | " +
                   $"Direction: {Direction} | Passengers: {PassengerCount}/{MaxPassengers} | " +
                   $"Type: {Type} | Trips: {TotalTrips} | Distance: {TotalDistanceTraveled:F1}m";
        }
    }

    public class ElevatorEventArgs : EventArgs
    {
        public IElevator Elevator { get; }
        public int FloorNumber { get; }

        public ElevatorEventArgs(IElevator elevator, int floorNumber)
        {
            Elevator = elevator;
            FloorNumber = floorNumber;
        }
    }
}