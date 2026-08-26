using ElevatorSimulation.Application.DTOs;
using ElevatorSimulation.Application.Interfaces;
using ElevatorSimulation.Domain.Entities;
using ElevatorSimulation.Domain.Enums;
using ElevatorSimulation.Domain.Interfaces;
using ElevatorSimulation.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace ElevatorSimulation.Application.Services
{
    public class DispatcherService : IDispatcherService
    {
        private readonly IBuilding _building;
        private readonly ILogger _logger;
        private readonly object _lockObject = new object();
        private Dictionary<int, int> _elevatorLoadHistory;
        private readonly Queue<FloorRequestDto> _pendingRequests;
        private DispatchStatisticsDto _statistics;

        public DispatcherService(IBuilding building, ILogger logger)
        {
            _building = building ?? throw new ArgumentNullException(nameof(building));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _elevatorLoadHistory = new Dictionary<int, int>();
            _pendingRequests = new Queue<FloorRequestDto>();
            _statistics = new DispatchStatisticsDto();

            // Initialize history
            foreach (var elevator in building.GetElevators())
            {
                _elevatorLoadHistory[elevator.Id] = 0;
                _statistics.ElevatorUtilization[elevator.Id] = 0;
            }
        }

        public DispatchResponseDto DispatchElevator(FloorRequestDto request)
        {
            lock (_lockObject)
            {
                try
                {
                    _logger.LogInformation($"Dispatching elevator to floor {request.FloorNumber}");

                    if (!_building.IsValidFloor(request.FloorNumber))
                    {
                        return new DispatchResponseDto
                        {
                            Success = false,
                            Message = $"Invalid floor number: {request.FloorNumber}"
                        };
                    }

                    var elevator = GetNearestAvailableElevator(request.FloorNumber);

                    if (elevator == null)
                    {
                        _pendingRequests.Enqueue(request);
                        _statistics.FailedDispatch++;

                        return new DispatchResponseDto
                        {
                            Success = false,
                            Message = "No elevators available. Your request has been queued.",
                            EstimatedWaitTime = 10
                        };
                    }

                    // Check capacity
                    if (elevator.PassengerCount + request.PassengerCount > elevator.MaxPassengers)
                    {
                        _pendingRequests.Enqueue(request);
                        _statistics.FailedDispatch++;

                        return new DispatchResponseDto
                        {
                            Success = false,
                            Message = $"Elevator {elevator.Id} is at capacity. Request queued.",
                            EstimatedWaitTime = 15
                        };
                    }

                    // Assign elevator to floor
                    AssignElevatorToFloor(elevator, request.FloorNumber);

                    var response = new DispatchResponseDto
                    {
                        Success = true,
                        Message = $"Elevator {elevator.Id} dispatched to floor {request.FloorNumber}",
                        Elevator = MapToStatusDto(elevator),
                        EstimatedWaitTime = CalculateEstimatedWaitTime(request.FloorNumber),
                        Priority = CalculatePriority(request)
                    };

                    // Update statistics
                    _statistics.TotalCalls++;
                    _statistics.SuccessfulDispatch++;

                    // Ensure CallsPerFloor dictionary exists and update safely
                    if (_statistics.CallsPerFloor == null)
                    {
                        _statistics.CallsPerFloor = new Dictionary<int, int>();
                    }
                    _statistics.CallsPerFloor[request.FloorNumber] =
                        _statistics.CallsPerFloor.TryGetValue(request.FloorNumber, out var currentCount) ? currentCount + 1 : 1;

                    // Ensure elevator load history exists and update
                    if (_elevatorLoadHistory == null)
                    {
                        _elevatorLoadHistory = new Dictionary<int, int>();
                    }
                    if (!_elevatorLoadHistory.ContainsKey(elevator.Id))
                    {
                        _elevatorLoadHistory[elevator.Id] = 0;
                    }
                    _elevatorLoadHistory[elevator.Id]++;

                    // Ensure ElevatorUtilization dictionary exists and update (guard against division by zero)
                    if (_statistics.ElevatorUtilization == null)
                    {
                        _statistics.ElevatorUtilization = new Dictionary<int, int>();
                    }
                    var totalCallsForCalc = Math.Max(1, _statistics.TotalCalls);
                    _statistics.ElevatorUtilization[elevator.Id] =
                        (int)Math.Round((double)_elevatorLoadHistory[elevator.Id] / totalCallsForCalc * 100);

                    // Use structured logging to avoid string formatting issues
                    _logger.LogInformation("Elevator {ElevatorId} dispatched to floor {FloorNumber}", elevator.Id, request.FloorNumber);

                    return response;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error dispatching elevator: {ex.Message}");
                    return new DispatchResponseDto
                    {
                        Success = false,
                        Message = $"Dispatch error: {ex.Message}"
                    };
                }
            }
        }

        public DispatchResponseDto AssignPassengerToElevator(PassengerRequestDto request)
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
                            Message = $"Invalid floor: {request.CurrentFloor}"
                        };
                    }

                    // Find best elevator for passenger
                    var elevator = GetBestElevatorForPassenger(request);

                    if (elevator == null)
                    {
                        return new DispatchResponseDto
                        {
                            Success = false,
                            Message = "No suitable elevator available for passenger"
                        };
                    }

                    var passenger = new Passenger(
                        request.Id > 0 ? request.Id : new Random().Next(1000, 9999),
                        request.CurrentFloor,
                        request.DestinationFloor,
                        request.Weight
                    );

                    elevator.BoardPassenger(passenger);

                    return new DispatchResponseDto
                    {
                        Success = true,
                        Message = $"Passenger assigned to elevator {elevator.Id}",
                        Elevator = MapToStatusDto(elevator),
                        EstimatedWaitTime = CalculateEstimatedTravelTime(elevator, request.CurrentFloor)
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error assigning passenger: {ex.Message}");
                    return new DispatchResponseDto
                    {
                        Success = false,
                        Message = $"Assignment error: {ex.Message}"
                    };
                }
            }
        }

        public IElevator GetNearestAvailableElevator(int floorNumber)
        {
            var elevator = GetNearestAvailableElevatorInternal(floorNumber);
            return elevator; // return domain elevator for callers to operate on
        }

        public bool IsElevatorAvailableForFloor(int elevatorId, int floorNumber)
        {
            try
            {
                var elevator = _building.GetElevator(elevatorId);
                return IsElevatorAvailableForFloorInternal(elevator, floorNumber);
            }
            catch
            {
                return false;
            }
        }

        public void ProcessElevatorQueue(int elevatorId)
        {
            lock (_lockObject)
            {
                try
                {
                    var elevator = _building.GetElevator(elevatorId);

                    if (elevator.DestinationQueue.Count > 0)
                    {
                        elevator.MoveToNextDestination();
                        _logger.LogInformation($"Processed next destination for elevator {elevatorId}");
                    }

                    // Process pending requests if any
                    if (_pendingRequests.Count > 0 && elevator.IsAvailable)
                    {
                        var request = _pendingRequests.Dequeue();
                        DispatchElevator(request);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing elevator queue: {ex.Message}");
                }
            }
        }

        public int CalculateEstimatedWaitTime(int floorNumber)
        {
            try
            {
                var elevator = GetNearestAvailableElevatorInternal(floorNumber);
                if (elevator == null)
                    return 10;

                var distance = Math.Abs(elevator.CurrentFloor - floorNumber);
                var baseTime = distance * 2; // 2 seconds per floor
                var queueTime = elevator.DestinationQueue.Count * 3; // 3 seconds per stop
                var passengerTime = elevator.PassengerCount * 1; // 1 second per passenger

                return baseTime + queueTime + passengerTime + 5; // add 5 seconds for door operations
            }
            catch
            {
                return 10;
            }
        }

        public List<ElevatorStatusDto> GetElevatorsServingFloor(int floorNumber)
        {
            var elevators = _building.GetElevators()
                .Where(e => e.DestinationQueue.Contains(floorNumber) || e.CurrentFloor == floorNumber)
                .ToList();

            return elevators.Select(MapToStatusDto).ToList();
        }

        public void OptimizeDispatchPatterns()
        {
            // Implement optimization logic
            _logger.LogInformation("Optimizing dispatch patterns...");

            var elevators = _building.GetElevators();
            if (elevators.Count < 2)
                return;

            // Simple optimization: balance load by assigning floors based on proximity
            var floorCount = _building.FloorCount;
            var floorsPerElevator = (floorCount + 1) / elevators.Count;

            for (int i = 0; i < elevators.Count; i++)
            {
                var startFloor = i * floorsPerElevator;
                var endFloor = Math.Min((i + 1) * floorsPerElevator, floorCount);
                _logger.LogInformation($"Elevator {elevators[i].Id} optimized for floors {startFloor}-{endFloor}");
            }
        }

        public DispatchStatisticsDto GetDispatchStatistics()
        {
            // Update system efficiency
            _statistics.SystemEfficiency = _statistics.TotalCalls > 0
                ? (double)_statistics.SuccessfulDispatch / _statistics.TotalCalls * 100
                : 0;

            _statistics.LastUpdated = DateTime.Now;
            return _statistics;
        }

        private IElevator GetNearestAvailableElevatorInternal(int floorNumber)
        {
            var elevators = _building.GetElevators()
                .Where(e => IsElevatorAvailableForFloorInternal(e, floorNumber))
                .OrderBy(e => Math.Abs(e.CurrentFloor - floorNumber))
                .ToList();

            if (!elevators.Any())
                return null;

            // If multiple elevators are equally close, choose the one with lower load
            var nearestDistance = Math.Abs(elevators.First().CurrentFloor - floorNumber);
            var candidates = elevators.Where(e => Math.Abs(e.CurrentFloor - floorNumber) == nearestDistance).ToList();

            if (candidates.Count == 1)
                return candidates.First();

            return candidates.OrderBy(e => e.PassengerCount).First();
        }

        private bool IsElevatorAvailableForFloorInternal(IElevator elevator, int floorNumber)
        {
            if (elevator.Status == ElevatorStatus.OutOfService ||
                elevator.Status == ElevatorStatus.Maintenance)
                return false;

            if (elevator.IsPassengerLimitReached())
                return false;

            // Check if elevator can reach this floor (e.g., freight may not service certain floors)
            if (elevator.Type == ElevatorType.Freight)
            {
                // Freight elevators typically service all floors but have capacity restrictions
                return floorNumber >= 0 && floorNumber <= _building.FloorCount;
            }

            return true;
        }

        private IElevator GetBestElevatorForPassenger(PassengerRequestDto request)
        {
            var elevators = _building.GetElevators()
                .Where(e => IsElevatorAvailableForFloorInternal(e, request.CurrentFloor))
                .OrderBy(e =>
                {
                    var distance = Math.Abs(e.CurrentFloor - request.CurrentFloor);
                    var load = e.PassengerCount;
                    var queueLength = e.DestinationQueue.Count;
                    return (distance * 2) + (queueLength * 3) + (load * 1);
                })
                .ToList();

            return elevators.FirstOrDefault();
        }

        private void AssignElevatorToFloor(IElevator elevator, int floorNumber)
        {
            if (elevator.CurrentFloor != floorNumber)
            {
                elevator.AddDestination(floorNumber);
                elevator.MoveToNextDestination();
            }
            else
            {
                elevator.OpenDoors();
                elevator.CloseDoors();
            }
        }

        private int CalculatePriority(FloorRequestDto request)
        {
            // Higher priority for floors with more passengers waiting
            var waitingCount = _building.GetPassengerCountOnFloor(request.FloorNumber);
            var basePriority = 1;

            if (waitingCount >= 5)
                return 5;
            if (waitingCount >= 3)
                return 3;
            if (waitingCount >= 1)
                return 2;

            return basePriority;
        }

        private int CalculateEstimatedTravelTime(IElevator elevator, int targetFloor)
        {
            var distance = Math.Abs(elevator.CurrentFloor - targetFloor);
            var baseTime = 2; // seconds per floor
            return distance * baseTime + 3; // add door operations time
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
    }
}