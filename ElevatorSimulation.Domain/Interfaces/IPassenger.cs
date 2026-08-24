using ElevatorSimulation.Domain.Enums;

namespace ElevatorSimulation.Domain.Interfaces
{
    public interface IPassenger
    {
        int Id { get; }
        int CurrentFloor { get; set; }
        int DestinationFloor { get; set; }
        double Weight { get; set; }
        bool IsWaiting { get; set; }
        PassengerStatus Status { get; set; }
        DateTime CreatedAt { get; }
        DateTime? BoardedAt { get; set; }
        DateTime? CompletedAt { get; set; }
        double GetWaitingTime();
    }
}