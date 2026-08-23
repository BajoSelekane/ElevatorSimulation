using ElevatorSimulation.Domain.Entities;

namespace ElevatorSimulation.Domain.Interfaces
{
    public interface IDispatcher
    {
        Elevator DispatchElevator(int floorNumber, int passengerCount = 1);
        void AssignElevatorToFloor(Elevator elevator, int floorNumber);
        Elevator GetNearestAvailableElevator(int floorNumber);
        bool IsElevatorAvailableForFloor(Elevator elevator, int floorNumber);
    }
}