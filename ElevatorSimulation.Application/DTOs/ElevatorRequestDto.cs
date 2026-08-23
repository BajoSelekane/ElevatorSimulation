namespace ElevatorSimulation.Application.DTOs
{
    public class ElevatorRequestDto
    {
        public int ElevatorId { get; set; }
        public int TargetFloor { get; set; }
        public int PassengerCount { get; set; }
        public DateTime RequestTime { get; set; }

        public ElevatorRequestDto()
        {
            RequestTime = DateTime.Now;
            PassengerCount = 1;
        }
    }
}