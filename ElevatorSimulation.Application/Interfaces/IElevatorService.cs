using ElevatorSimulation.Application.DTOs;
using ElevatorSimulation.Domain.Entities;

namespace ElevatorSimulation.Application.Interfaces
{
    public interface IElevatorService
    {
        ElevatorStatusDto GetElevatorStatus(int elevatorId);
        List<ElevatorStatusDto> GetAllElevators();
        BuildingStatusDto GetBuildingStatus();
        DispatchResponseDto CallElevator(FloorRequestDto request);
        DispatchResponseDto SendElevatorToFloor(ElevatorRequestDto request);
        DispatchResponseDto AddPassenger(PassengerRequestDto request);
        bool ProcessNextDestination(int elevatorId);
        void ResetAllElevators();
        void UpdateElevatorSpeed(int elevatorId, int speed);
        ElevatorStatusDto GetNearestElevatorStatus(int floorNumber);
    }
}