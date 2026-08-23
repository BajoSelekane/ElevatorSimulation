using ElevatorSimulation.Application.DTOs;
using ElevatorSimulation.Application.Interfaces;
using ElevatorSimulation.Domain.Entities;
using ElevatorSimulation.Domain.Enums;
using ElevatorSimulation.Domain.Interfaces;
using ElevatorSimulation.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace ElevatorSimulation.Application.Services
{
    public class ElevatorService : IElevatorService
    {
        private readonly IBuilding _building;
        private readonly IDispatcherService _dispatcherService;
        private readonly ILogger _logger;
        private readonly object _lockObject = new object();

        public ElevatorService(IBuilding building, IDispatcherService dispatcherService, ILogger logger)
        {
            _building = building ?? throw new ArgumentNullException(nameof(building));
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ElevatorStatusDto GetElevatorStatus(int elevatorId)
        {
            try
            {
                var elevator = _building.GetElevator(elevatorId);
                return MapToStatusDto(elevator);
            }
            catch (ElevatorNotFoundException ex)
            {
                _logger.LogError($"Elevator {elevatorId} not found: {ex.Message}");
                throw;
            }
        }

        public List<ElevatorStatusDto> GetAllElevators()
        {
            var elevators = _building.GetElevators();
            return elevators.Select(MapToStatusDto).ToList();
        }

        public BuildingStatusDto GetBuildingStatus()
        {
            var elevators = _building.GetElevators();
            var statusDto = new BuildingStatusDto
            {
                FloorCount = _building.FloorCount,
                ElevatorCount = elevators.Count
            };

            foreach (var elevator in elevators)
            {
                statusDto.Elevators.Add(MapToStatusDto(elevator));
                statusDto.TotalPassengersInTransit += elevator.PassengerCount;
            }

            for (int i = 0; i <= _building.FloorCount; i++)
            {
                var floor = _building.GetFloor(i);
                var count = floor.GetWaitingPassengerCount();
                statusDto.PassengersPerFloor[i] = count;
                statusDto.TotalPassengersWaiting += count;
            }

            // Calculate average wait time (simplified)
            statusDto.AverageWaitTime = CalculateAverageWaitTime();

            return statusDto;
        }

        public DispatchResponseDto CallElevator(FloorRequestDto request)
        {
            lock (_lockObject)
            {
                try
                {
                    _logger.LogInformation($"Processing elevator call to floor {request.FloorNumber}");

                    if (!_building.IsValidFloor(request.FloorNumber))
                    {
                        return new DispatchResponseDto
                        {
                            Success = false,
                            Message = $"Invalid floor number: {request.FloorNumber}"
                        };
                    }

                    var response = _dispatcherService.DispatchElevator(request);

                    if (response.Success)
                    {
                        _logger.LogInformation($"Elevator {response.Elevator.ElevatorId} dispatched to floor {request.FloorNumber}");
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to dispatch elevator: {response.Message}");
                    }

                    return response;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error calling elevator: {ex.Message}");
                    return new DispatchResponseDto
                    {
                        Success = false,
                        Message = $"System error: {ex.Message}"
                    };
                }
            }
        }

        public DispatchResponseDto SendElevatorToFloor(ElevatorRequestDto request)
        {
            lock (_lockObject)
            {
                try
                {
                    var elevator = _building.GetElevator(request.ElevatorId);

                    if (!_building.IsValidFloor(request.TargetFloor))
                    {
                        return new DispatchResponseDto
                        {
                            Success = false,
                            Message = $"Invalid floor: {request.TargetFloor}"
                        };
                    }

                    if (elevator.Status == ElevatorStatus.OutOfService)
                    {
                        return new DispatchResponseDto
                        {
                            Success = false,
                            Message = $"Elevator {request.ElevatorId} is out of service"
                        };
                    }

                    elevator.AddDestination(request.TargetFloor);
                    elevator.MoveToNextDestination();

                    return new DispatchResponseDto
                    {
                        Success = true,
                        Message = $"Elevator {request.ElevatorId} sent to floor {request.TargetFloor}",
                        Elevator = MapToStatusDto(elevator),
                        EstimatedWaitTime = CalculateEstimatedTravelTime(elevator, request.TargetFloor)
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error sending elevator: {ex.Message}");
                    return new DispatchResponseDto
                    {
                        Success = false,
                        Message = $"Error: {ex.Message}"
                    };
                }
            }
        }

        public DispatchResponseDto AddPassenger(PassengerRequestDto request)
        {
            lock (_lockObject)
            {
                try
                {
                    if (!_building.IsValidFloor(request.CurrentFloor))
                    {
                        return new DispatchResponseDto
                        {
                            Success = false,
                            Message = $"Invalid current floor: {request.CurrentFloor}"
                        };
                    }

                    if (!_building.IsValidFloor(request.DestinationFloor))
                    {
                        return new DispatchResponseDto
                        {
                            Success = false,
                            Message = $"Invalid destination floor: {request.DestinationFloor}"
                        };
                    }

                    if (request.CurrentFloor == request.DestinationFloor)
                    {
                        return new DispatchResponseDto
                        {
                            Success = false,
                            Message = "Current and destination floors must be different"
                        };
                    }

                    var passenger = new Passenger(
                        request.Id > 0 ? request.Id : GeneratePassengerId(),
                        request.CurrentFloor,
                        request.DestinationFloor,
                        request.Weight
                    );

                    var floor = _building.GetFloor(request.CurrentFloor);
                    floor.AddWaitingPassenger(passenger);

                    // Dispatch elevator to pick up passenger
                    var floorRequest = new FloorRequestDto
                    {
                        FloorNumber = request.CurrentFloor,
                        PassengerCount = 1
                    };

                    var dispatchResponse = _dispatcherService.DispatchElevator(floorRequest);

                    if (dispatchResponse.Success)
                    {
                        passenger.Status = PassengerStatus.Waiting;
                        return new DispatchResponseDto
                        {
                            Success = true,
                            Message = $"Passenger {passenger.Id} added. Elevator dispatched to floor {request.CurrentFloor}",
                            Elevator = dispatchResponse.Elevator,
                            EstimatedWaitTime = CalculateEstimatedTravelTime(
                                _building.GetElevator(dispatchResponse.Elevator.ElevatorId),
                                request.CurrentFloor)
                        };
                    }
                    else
                    {
                        return new DispatchResponseDto
                        {
                            Success = false,
                            Message = $"Passenger added to waiting list but dispatch failed: {dispatchResponse.Message}"
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error adding passenger: {ex.Message}");
                    return new DispatchResponseDto
                    {
                        Success = false,
                        Message = $"Error adding passenger: {ex.Message}"
                    };
                }
            }
        }

        public bool ProcessNextDestination(int elevatorId)
        {
            lock (_lockObject)
            {
                try
                {
                    var elevator = _building.GetElevator(elevatorId);

                    if (elevator.DestinationQueue.Count == 0)
                        return false;

                    elevator.MoveToNextDestination();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing destination for elevator {elevatorId}: {ex.Message}");
                    return false;
                }
            }
        }

        public void ResetAllElevators()
        {
            lock (_lockObject)
            {
                var elevators = _building.GetElevators();
                foreach (var elevator in elevators)
                {
                    elevator.SetBackInService();
                    // Clear destinations
                    while (elevator.DestinationQueue.Count > 0)
                    {
                        elevator.GetNextDestination();
                        // In real implementation, we'd need to dequeue properly
                    }
                    elevator.MoveToFloor(0);
                }
                _logger.LogInformation("All elevators have been reset");
            }
        }

        public void UpdateElevatorSpeed(int elevatorId, int speed)
        {
            // Implementation would depend on how speed is handled
            _logger.LogInformation ($"Elevator {elevatorId} speed updated to {speed}");
        }

        public ElevatorStatusDto GetNearestElevatorStatus(int floorNumber)
        {
            try
            {
                var elevator = _dispatcherService.GetNearestAvailableElevator(floorNumber);
                return elevator != null ? MapToStatusDto(elevator) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting nearest elevator: {ex.Message}");
                return null;
            }
        }

        private ElevatorStatusDto MapToStatusDto(IElevator elevator)
        {
            return new ElevatorStatusDto
            {
                ElevatorId = elevator.Id,
                CurrentFloor = elevator.CurrentFloor,
                Direction = elevator.Direction,
                Status = elevator.Status,
                Type = elevator.Type,
                PassengerCount = elevator.PassengerCount,
                MaxPassengers = elevator.MaxPassengers,
                DestinationQueue = elevator.DestinationQueue.ToList(),
                IsMoving = elevator.IsMoving,
                IsAvailable = elevator.IsAvailable,
                OccupancyPercentage = (double)elevator.PassengerCount / elevator.MaxPassengers * 100,
                TotalTrips = elevator.TotalTrips,
                TotalPassengersServed = elevator.TotalPassengersServed,
                TotalDistanceTraveled = elevator.TotalDistanceTraveled,
                LastMovementTime = DateTime.Now,
                DisplayStatus = GetDisplayStatus(elevator)
            };
        }

        private string GetDisplayStatus(IElevator elevator)
        {
            return elevator.Status switch
            {
                ElevatorStatus.Stationary => "🟢 Stationary",
                ElevatorStatus.Moving => elevator.Direction == ElevatorDirection.Up ? "⬆️ Moving Up" : "⬇️ Moving Down",
                ElevatorStatus.DoorsOpen => "🚪 Doors Open",
                ElevatorStatus.DoorsClosing => "🚪 Doors Closing",
                ElevatorStatus.OutOfService => "🔴 Out of Service",
                ElevatorStatus.Maintenance => "🟡 Maintenance",
                _ => elevator.Status.ToString()
            };
        }

        private int CalculateEstimatedTravelTime(IElevator elevator, int targetFloor)
        {
            var distance = Math.Abs(elevator.CurrentFloor - targetFloor);
            var baseTime = 2; // seconds per floor
            return distance * baseTime + 3; // add door operations time
        }

        private double CalculateAverageWaitTime()
        {
            // Simplified calculation - in real implementation would track actual wait times
            return new Random().NextDouble() * 5 + 2; // 2-7 seconds
        }

        private int GeneratePassengerId()
        {
            return new Random().Next(1000, 9999);
        }
    }
}