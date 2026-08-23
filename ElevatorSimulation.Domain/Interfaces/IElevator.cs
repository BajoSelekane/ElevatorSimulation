using ElevatorSimulation.Domain.Entities;
using ElevatorSimulation.Domain.Enums;

namespace ElevatorSimulation.Domain.Interfaces
{
    public interface IElevator
    {
        int Id { get; }
        int CurrentFloor { get; }
        ElevatorDirection Direction { get; }
        ElevatorStatus Status { get; }
        ElevatorType Type { get; }
        int PassengerCount { get; }
        int MaxPassengers { get; }
        IReadOnlyCollection<int> DestinationQueue { get; }
        bool IsMoving { get; }
        bool IsAvailable { get; }
        int TotalTrips { get; }
        int TotalPassengersServed { get; }
        double TotalDistanceTraveled { get; }

        event EventHandler<ElevatorEventArgs> ElevatorMoved;
        event EventHandler<ElevatorEventArgs> ElevatorStopped;
        event EventHandler<ElevatorEventArgs> DoorsOpened;
        event EventHandler<ElevatorEventArgs> DoorsClosed;

        void MoveToFloor(int floorNumber);
        void OpenDoors();
        void CloseDoors();
        void AddDestination(int floorNumber);
        int GetNextDestination();
        void MoveToNextDestination();
        bool CanAcceptPassenger(Passenger passenger);
        void BoardPassenger(Passenger passenger);
        void AlightPassenger(Passenger passenger);
        bool IsPassengerLimitReached();
        void SetOutOfService();
        void SetBackInService();
    }
}