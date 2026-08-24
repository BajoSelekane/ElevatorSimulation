namespace ElevatorSimulation.Application.DTOs
{
    public class PassengerRequestDto
    {
        public int Id { get; set; }
        public int CurrentFloor { get; set; }
        public int DestinationFloor { get; set; }
        public double Weight { get; set; }
        public int ElevatorId { get; set; }
        public DateTime RequestTime { get; set; }

        public PassengerRequestDto()
        {
            RequestTime = DateTime.Now;
            Weight = 70;
        }
    }
}