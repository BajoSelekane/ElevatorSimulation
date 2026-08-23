using ElevatorSimulation.Application.DTOs;
using ElevatorSimulation.Domain.Entities;

namespace ElevatorSimulation.Application.Interfaces
{
    public interface IDispatcherService
    {
        DispatchResponseDto DispatchElevator(FloorRequestDto request);
        DispatchResponseDto AssignPassengerToElevator(PassengerRequestDto request);
        ElevatorStatusDto GetNearestAvailableElevator(int floorNumber);
        bool IsElevatorAvailableForFloor(int elevatorId, int floorNumber);
        void ProcessElevatorQueue(int elevatorId);
        int CalculateEstimatedWaitTime(int floorNumber);
        List<ElevatorStatusDto> GetElevatorsServingFloor(int floorNumber);
        void OptimizeDispatchPatterns();
        DispatchStatisticsDto GetDispatchStatistics();
    }

    public class DispatchStatisticsDto
    {
        public int TotalCalls { get; set; }
        public int SuccessfulDispatch { get; set; }
        public int FailedDispatch { get; set; }
        public double AverageResponseTime { get; set; }
        public Dictionary<int, int> CallsPerFloor { get; set; }
        public Dictionary<int, int> ElevatorUtilization { get; set; }
        public double SystemEfficiency { get; set; }
        public DateTime LastUpdated { get; set; }

        public DispatchStatisticsDto()
        {
            CallsPerFloor = new Dictionary<int, int>();
            ElevatorUtilization = new Dictionary<int, int>();
            LastUpdated = DateTime.Now;
        }

        public override string ToString()
        {
            return $@"
📊 DISPATCH STATISTICS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Total Calls: {TotalCalls}
Successful Dispatch: {SuccessfulDispatch} ({SystemEfficiency:F1}% success)
Failed Dispatch: {FailedDispatch}
Average Response Time: {AverageResponseTime:F1}s
Elevator Utilization: 
{string.Join("\n", ElevatorUtilization.Select(u => $"  E{u.Key}: {u.Value}%"))}
Last Updated: {LastUpdated:T}";
        }
    }
}