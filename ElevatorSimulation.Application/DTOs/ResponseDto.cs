using ElevatorSimulation.Domain.Enums;

namespace ElevatorSimulation.Application.DTOs
{
    public class ElevatorStatusDto
    {
        public int ElevatorId { get; set; }
        // Backwards-compatible alias used in some parts of the codebase
        public int Id { get => ElevatorId; set => ElevatorId = value; }
        public int CurrentFloor { get; set; }
        public ElevatorDirection Direction { get; set; }
        public ElevatorStatus Status { get; set; }
        public ElevatorType Type { get; set; }
        public int PassengerCount { get; set; }
        public int MaxPassengers { get; set; }
        public IList<int> DestinationQueue { get; set; }
        public bool IsMoving { get; set; }
        public bool IsAvailable { get; set; }
        public double OccupancyPercentage { get; set; }
        public int TotalTrips { get; set; }
        public int TotalPassengersServed { get; set; }
        public double TotalDistanceTraveled { get; set; }
        public DateTime LastMovementTime { get; set; }
        public string DisplayStatus { get; set; }

        public ElevatorStatusDto()
        {
            DestinationQueue = new List<int>();
        }

        public override string ToString()
        {
            return $@"
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Elevator {ElevatorId} - {Type}
Status: {DisplayStatus ?? Status.ToString()}
Floor: {CurrentFloor} | Direction: {Direction}
Passengers: {PassengerCount}/{MaxPassengers} ({OccupancyPercentage:F1}%)
Queue: {string.Join(", ", DestinationQueue)}
Trips: {TotalTrips} | Served: {TotalPassengersServed}
Distance: {TotalDistanceTraveled:F1}m
Last Moved: {LastMovementTime:T}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";
        }
    }

    public class BuildingStatusDto
    {
        public int FloorCount { get; set; }
        public int ElevatorCount { get; set; }
        public int TotalPassengersWaiting { get; set; }
        public int TotalPassengersInTransit { get; set; }
        public Dictionary<int, int> PassengersPerFloor { get; set; }
        public List<ElevatorStatusDto> Elevators { get; set; }
        public double AverageWaitTime { get; set; }
        public DateTime Timestamp { get; set; }

        public BuildingStatusDto()
        {
            PassengersPerFloor = new Dictionary<int, int>();
            Elevators = new List<ElevatorStatusDto>();
            Timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            return $@"
╔═══════════════════════════════════════════════╗
║          BUILDING STATUS                      ║
╠═══════════════════════════════════════════════╣
║ Floors: {FloorCount}                          ║
║ Elevators: {ElevatorCount}                    ║
║ Waiting Passengers: {TotalPassengersWaiting}  ║
║ Passengers In Transit: {TotalPassengersInTransit} ║
║ Average Wait Time: {AverageWaitTime:F1}s      ║
║ Updated: {Timestamp:T}                        ║
╚═══════════════════════════════════════════════╝";
        }
    }

    public class DispatchResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public ElevatorStatusDto Elevator { get; set; }
        public int EstimatedWaitTime { get; set; }
        public int Priority { get; set; }
        public DateTime Timestamp { get; set; }

        public DispatchResponseDto()
        {
            Timestamp = DateTime.Now;
            Priority = 1;
            EstimatedWaitTime = 5;
        }

        public override string ToString()
        {
            if (Success)
            {
                return $@"
✅ {Message}
Elevator {Elevator?.ElevatorId} dispatched to target floor.
Estimated wait time: {EstimatedWaitTime} seconds.
Priority: {Priority}";
            }
            else
            {
                return $@"
❌ {Message}
Please try again or contact building management.";
            }
        }
    }
}