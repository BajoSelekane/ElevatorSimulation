namespace ElevatorSimulation.Application.DTOs
{
    public class FloorRequestDto
    {
        public int FloorNumber { get; set; }
        public int PassengerCount { get; set; }
        public DateTime RequestTime { get; set; }
        public string RequestType { get; set; } // "Call", "Destination"

        public FloorRequestDto()
        {
            RequestTime = DateTime.Now;
            PassengerCount = 1;
            RequestType = "Call";
        }

        public override string ToString()
        {
            return $"Floor {FloorNumber} | Passengers: {PassengerCount} | Type: {RequestType} | Time: {RequestTime:T}";
        }
    }
}