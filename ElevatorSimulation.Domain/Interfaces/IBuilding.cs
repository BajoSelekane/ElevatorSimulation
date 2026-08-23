using ElevatorSimulation.Domain.Entities;

namespace ElevatorSimulation.Domain.Interfaces
{
    public interface IBuilding
    {
        int FloorCount { get; }
        IReadOnlyList<IElevator> Elevators { get; }
        IReadOnlyList<Floor> Floors { get; }

        void AddElevator(IElevator elevator);
        void RemoveElevator(int elevatorId);
        IElevator GetElevator(int elevatorId);
        IReadOnlyList<IElevator> GetElevators();
        Floor GetFloor(int floorNumber);
        bool IsValidFloor(int floorNumber);
        int GetPassengerCountOnFloor(int floorNumber);
    }
}