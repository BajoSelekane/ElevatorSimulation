using ElevatorSimulation.Domain.Entities;

namespace ElevatorSimulation.Domain.Interfaces
{
    public interface IDispatcher
    {
        IElevator DispatchElevator(int floorNumber, int passengerCount = 1);
        void AssignElevatorToFloor(IElevator elevator, int floorNumber);
        IElevator GetNearestAvailableElevator(int floorNumber);
        bool IsElevatorAvailableForFloor(IElevator elevator, int floorNumber);
        ElevatorDispatchResult DispatchForPassenger(Passenger passenger);
        void ProcessElevatorQueue(IElevator elevator);
    }

    public class ElevatorDispatchResult
    {
        public IElevator Elevator { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public int EstimatedWaitTime { get; set; }
    }
}