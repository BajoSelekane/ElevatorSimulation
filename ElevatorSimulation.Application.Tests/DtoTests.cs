using ElevatorSimulation.Application.DTOs;
using ElevatorSimulation.Application.Interfaces;
using ElevatorSimulation.Domain.Enums;
using FluentAssertions;

namespace ElevatorSimulation.Application.Tests
{
    [Trait("Category", "Unit")]
    public class DtoTests
    {
        [Fact]
        public void ElevatorStatusDto_IdAlias_ShouldMirrorElevatorId()
        {
            var dto = new ElevatorStatusDto { Id = 4, Type = ElevatorType.Standard, DisplayStatus = "Idle" };

            dto.ElevatorId.Should().Be(4);
            dto.ToString().Should().Contain("Elevator 4");
        }

        [Fact]
        public void BuildingStatusDto_ToString_ShouldIncludeCounts()
        {
            var dto = new BuildingStatusDto { FloorCount = 10, ElevatorCount = 3, TotalPassengersWaiting = 2 };

            dto.ToString().Should().Contain("Floors: 10");
            dto.ToString().Should().Contain("Elevators: 3");
        }

        [Fact]
        public void DispatchResponseDto_ToString_ShouldReflectSuccessAndFailure()
        {
            var success = new DispatchResponseDto
            {
                Success = true,
                Message = "Dispatched",
                Elevator = new ElevatorStatusDto { ElevatorId = 1 }
            };
            var failure = new DispatchResponseDto { Success = false, Message = "Busy" };

            success.ToString().Should().Contain("Dispatched");
            failure.ToString().Should().Contain("Busy");
        }

        [Fact]
        public void DispatchStatisticsDto_ToString_ShouldIncludeTotals()
        {
            var stats = new DispatchStatisticsDto
            {
                TotalCalls = 4,
                SuccessfulDispatch = 3,
                FailedDispatch = 1,
                SystemEfficiency = 75,
                ElevatorUtilization = { [1] = 50 }
            };

            stats.ToString().Should().Contain("Total Calls: 4");
            stats.ToString().Should().Contain("E1: 50%");
        }
    }
}
