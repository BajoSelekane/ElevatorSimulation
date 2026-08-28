using ElevatorSimulation.Domain.Enums;

namespace ElevatorSimulation.Domain.Entities
{
    public class Passenger
    {
        public int Id { get; }
        public int CurrentFloor { get; set; }
        public int DestinationFloor { get; set; }
        public double Weight { get; set; }
        public bool IsWaiting { get; set; }
        public PassengerStatus Status { get; set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? BoardedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public Passenger(int id, int currentFloor, int destinationFloor, double weight = 70)
        {
            Id = id;
            CurrentFloor = currentFloor;
            DestinationFloor = destinationFloor;
            Weight = weight;
            IsWaiting = true;
            Status = PassengerStatus.Waiting;
            CreatedAt = DateTime.Now;
            BoardedAt = null;
            CompletedAt = null;
        }

        public double GetWaitingTime()
        {
            if (Status == PassengerStatus.Completed || CompletedAt.HasValue)
                return 0;

            var span = DateTime.Now - CreatedAt;
            return span.TotalSeconds;
        }
    }
}
