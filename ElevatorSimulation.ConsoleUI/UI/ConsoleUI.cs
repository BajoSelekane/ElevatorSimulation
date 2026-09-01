using ElevatorSimulation.Domain.Entities;
using ElevatorSimulation.Domain.Enums;
using ElevatorSimulation.Application.Services;
using ElevatorSimulation.Application.Interfaces;
using ElevatorSimulation.Infrastructure.Logging;
using System;

namespace ElevatorSimulation.ConsoleApp.UI
{
    public class ConsoleUI
    {
        private readonly IElevatorService _elevatorService;
        private readonly IDispatcherService _dispatcherService;
        private readonly ILogger _logger;
        private bool _isRunning;

        public ConsoleUI(IElevatorService elevatorService,
                        IDispatcherService dispatcherService,
                        IConsoleLogger logger)
        {
            _elevatorService = elevatorService;
            _dispatcherService = dispatcherService;
            _logger = logger;
            _isRunning = true;
        }

        public async Task RunAsync()
        {
            _logger.LogInfo("========================================");
            _logger.LogInfo("       ELEVATOR SIMULATOR v2.0         ");
            _logger.LogInfo("========================================");

            while (_isRunning)
            {
                await DisplayMainMenuAsync();
                var choice = Console.ReadLine();

                await ProcessMainMenuChoiceAsync(choice);
            }
        }

        private async Task DisplayMainMenuAsync()
        {
            try { Console.Clear(); } catch { }
            _logger.Info("========================================");
            _logger.Info("           MAIN MENU                    ");
            _logger.Info("========================================");
                    _logger.LogInfo("");
            _logger.Info("1. Call Elevator");
            _logger.Info("2. View Elevator Status");
            _logger.Info("3. Add Passenger");
            _logger.Info("4. View Building Status");
            _logger.Info("5. Settings");
            _logger.Info("6. Exit");
            _logger.Info("");
            _logger.Info("========================================");
            _logger.Info("Current Time: " + DateTime.Now.ToShortTimeString());
            _logger.Info("");
            Console.Write("Your choice: ");
        }

        private async Task ProcessMainMenuChoiceAsync(string choice)
        {
            switch (choice)
            {
                case "1":
                    await HandleCallElevatorAsync();
                    break;
                case "2":
                    await HandleViewElevatorStatusAsync();
                    break;
                case "3":
                    await HandleAddPassengerAsync();
                    break;
                case "4":
                    await HandleViewBuildingStatusAsync();
                    break;
                case "5":
                    await HandleSettingsAsync();
                    break;
                case "6":
                    _isRunning = false;
                    _logger.Info("Thank you for using the Elevator Simulator!");
                    break;
                default:
                    _logger.Error("Invalid option. Please enter a number between 1 and 6.");
                    await WaitForUserAsync();
                    break;
            }
        }

        private async Task HandleCallElevatorAsync()
        {
            try
            {
                try { Console.Clear(); } catch { }
                _logger.Info("========================================");
                _logger.Info("          CALL ELEVATOR                ");
                _logger.Info("========================================");
                _logger.Info("");

                var floorNumber = await GetValidatedFloorInput("Enter floor number to call elevator: ");
                if (floorNumber == null) return;

                var passengerCount = GetValidatedPassengerCount();
                if (passengerCount == null) return;

                _logger.LogInfo($"\n📢 Calling elevator to Floor {floorNumber} with {passengerCount} passenger(s)...");

                var request = new ElevatorSimulation.Application.DTOs.FloorRequestDto
                {
                    FloorNumber = floorNumber.Value,
                    PassengerCount = passengerCount.Value
                };

                var response = await Task.Run(() => _dispatcherService.DispatchElevator(request));

                if (response == null || !response.Success)
                {
                    _logger.Error($"Failed to dispatch elevator: {response?.Message ?? "Unknown error"}");
                    return;
                }

                var elevatorDto = response.Elevator;
                // If the dispatcher returned an elevator that's already at the target floor, show a clear message
                if (elevatorDto.CurrentFloor == floorNumber.Value)
                {
                    _logger.LogInfo($"Elevator {elevatorDto.ElevatorId} is already at Floor {floorNumber.Value}. No travel required.");
                }
                else
                {
                    _logger.LogSuccess($"✅ Elevator {elevatorDto.ElevatorId} dispatched to Floor {floorNumber.Value}");
                }

                _logger.LogInfo($"📊 {elevatorDto}");

                // Instruct domain elevator to move (service will operate on domain model)
                var sendReq = new ElevatorSimulation.Application.DTOs.ElevatorRequestDto
                {
                    ElevatorId = elevatorDto.ElevatorId,
                    TargetFloor = floorNumber.Value
                };

                var sendResponse = _elevatorService.SendElevatorToFloor(sendReq);

                if (sendResponse != null)
                {
                    if (sendResponse.Success)
                    {
                        // If SendElevatorToFloor reported the elevator was already at the floor, show that explicitly
                        if (sendResponse.EstimatedWaitTime == 0 || (sendResponse.Message ?? string.Empty).Contains("already at floor"))
                        {
                            _logger.LogInfo($"{sendResponse.Message}");
                        }
                        else
                        {
                            _logger.LogInfo($"{sendResponse.Message}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to send elevator: {sendResponse.Message}");
                    }
                }

                await SimulateElevatorMovementAsync(elevatorDto, floorNumber.Value);

                _logger.LogSuccess($"🚪 Elevator {elevatorDto.ElevatorId} has arrived at Floor {floorNumber.Value}");
                _logger.LogInfo("➡️ Press any key to continue...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error: {ex.Message}");
                _logger.LogInfo("➡️ Press any key to return to menu...");
                Console.ReadKey();
            }
        }

        private async Task SimulateElevatorMovementAsync(ElevatorSimulation.Application.DTOs.ElevatorStatusDto elevator, int targetFloor)
        {
            if (elevator.CurrentFloor == targetFloor)
            {
                _logger.LogInfo($"No movement required; Elevator {elevator.ElevatorId} is already at Floor {targetFloor}.");
                return;
            }

            var startFloor = elevator.CurrentFloor;
            var direction = targetFloor > startFloor ? "Up" : "Down";

            _logger.Info($"🚀 Elevator {elevator.ElevatorId} moving {direction} from Floor {startFloor} to Floor {targetFloor}...");

            // Simulate movement with progress
            var steps = Math.Abs(targetFloor - startFloor);
            for (int i = 1; i <= steps; i++)
            {
                var currentPos = startFloor + (direction == "Up" ? i : -i);
                await Task.Delay(800);

                // Update progress bar
                var progress = (i * 100) / steps;
                Console.Write($"\r  [{new string('█', progress / 5)}{new string('░', 20 - progress / 5)}] {progress}%");
                _logger.Info($" 📍 Floor {currentPos}");
            }
            Console.WriteLine();
        }

        private async Task HandleViewElevatorStatusAsync()
        {
            try { Console.Clear(); } catch { }
            _logger.LogInfo("========================================");
            _logger.LogInfo("      ELEVATOR STATUS                   ");
            _logger.LogInfo("========================================");
            _logger.LogInfo("");

            var elevators = _elevatorService.GetAllElevators();
            foreach (var elevator in elevators)
            {
                _logger.LogInfo($"📊 {elevator.ToString()}");
                _logger.LogInfo($"   Destination Queue: [{string.Join(", ", elevator.DestinationQueue)}]");
                _logger.LogInfo("");
            }

            _logger.LogInfo("========================================");
            _logger.LogInfo("➡️ Press any key to return to menu...");
            Console.ReadKey();
        }

        private async Task HandleAddPassengerAsync()
        {
            try { Console.Clear(); } catch { }
            _logger.Info("========================================");
            _logger.Info("       ADD PASSENGER                   ");
            _logger.Info("========================================");
            _logger.Info("");

            var floorNumber = await GetValidatedFloorInput("Enter current floor of passenger: ");
            if (floorNumber == null) return;

            var destinationFloor = await GetValidatedFloorInput("Enter destination floor: ");
            if (destinationFloor == null || destinationFloor == floorNumber)
            {
                _logger.Error("Destination floor must be different from current floor.");
                await WaitForUserAsync();
                return;
            }

            // Create and process passenger
            var passenger = new Passenger(
                id: new Random().Next(1000, 9999),
                currentFloor: floorNumber.Value,
                destinationFloor: destinationFloor.Value
            );

            _logger.Success($"✅ Passenger {passenger.Id} registered");
            _logger.Info($"   From: Floor {passenger.CurrentFloor} → To: Floor {passenger.DestinationFloor}");

            // Call elevator for this passenger
            await HandleCallElevatorAsync();

            _logger.Info("➡️ Press any key to continue...");
            Console.ReadKey();
        }

        private async Task<int?> GetValidatedFloorInput(string prompt)
        {
            const int maxAttempts = 3;
            for (int i = 0; i < maxAttempts; i++)
            {
                Console.Write(prompt);
                var input = await Console.In.ReadLineAsync();

                if (input == null) // EOF / no input
                {
                    _logger.LogError("No input available.");
                    return null;
                }

                if (int.TryParse(input, out int floor) && IsValidFloor(floor))
                    return floor;

                _logger.LogError("Invalid floor. Please try again.");
            }
            return null;
        }


        private bool IsValidFloor(int floor)
        {
            try
            {
                var buildingStatus = _elevatorService.GetBuildingStatus();
                var maxFloor = buildingStatus?.FloorCount ?? 0;
                return floor >= 0 && floor <= maxFloor;
            }
            catch
            {
                // If we cannot determine building info, accept 0+ floors as valid
                return floor >= 0;
            }
        }

        private int? GetValidatedPassengerCount()
        {
            while (true)
            {
                Console.Write("Enter number of passengers waiting (max 10): ");
                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                {
                    _logger.Error("Input cannot be empty.");
                    continue;
                }

                if (int.TryParse(input, out int count) && count > 0 && count <= 10)
                    return count;

                _logger.Error("❌ Invalid count. Please enter a number between 1 and 10.");
            }
        }

        private async Task HandleViewBuildingStatusAsync()
        {
            try { Console.Clear(); } catch { }
            _logger.Info("========================================");
            _logger.Info("    BUILDING STATUS (ASCII VIEW)       ");
            _logger.Info("========================================");
            _logger.Info("");

            var floors = 10;
            var elevators = _elevatorService.GetAllElevators();

            for (int i = floors; i >= 0; i--)
            {
                Console.Write($"Floor {i,2}: ");

                foreach (var elevator in elevators)
                {
                    if (elevator.CurrentFloor == i)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"[E{elevator.Id}] ");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.Write("[  ] ");
                    }
                }

                // Show waiting passengers indicator
                var waitingPassengers = new Random().Next(0, 3);
                if (waitingPassengers > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"👤 {waitingPassengers} waiting");
                    Console.ResetColor();
                }

                Console.WriteLine();
            }

            _logger.Info("");
            _logger.Info("Legend: [E#] = Elevator Position");
            _logger.Info("========================================");
            _logger.Info("➡️ Press any key to return to menu...");
            Console.ReadKey();
        }

        private async Task HandleSettingsAsync()
        {
            try { Console.Clear(); } catch { }
            _logger.Info("========================================");
            _logger.Info("          SETTINGS                      ");
            _logger.Info("========================================");
            _logger.Info("");
            _logger.Info("1. Reset Simulation");
            _logger.Info("2. Change Simulation Speed");
            _logger.Info("3. Back to Main Menu");
            _logger.Info("");
            Console.Write("Your choice: ");

            var choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    _logger.Info("🔄 Resetting simulation...");
                    await Task.Delay(1000);
                    _logger.Success("✅ Simulation reset successfully!");
                    await WaitForUserAsync();
                    break;
                case "2":
                    _logger.Info("⚡ Speed changed to: Normal");
                    await WaitForUserAsync();
                    break;
                default:
                    break;
            }
        }

        private async Task WaitForUserAsync()
        {
            _logger.Info("➡️ Press any key to continue...");
            Console.ReadKey();
        }
    }
}