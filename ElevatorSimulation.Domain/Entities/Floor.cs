using System.Collections.ObjectModel;

namespace ElevatorSimulation.Domain.Entities
{
    public class Floor
    {
        public int FloorNumber { get; }
        public List<Passenger> WaitingPassengers { get; }
        public bool HasElevatorPresent { get; set; }
        public DateTime LastServiceTime { get; set; }

        public Floor(int floorNumber)
        {
            FloorNumber = floorNumber;
            WaitingPassengers = new List<Passenger>();
            HasElevatorPresent = false;
            LastServiceTime = DateTime.Now;
        }

        public void AddWaitingPassenger(Passenger passenger)
        {
            if (passenger == null)
                throw new ArgumentNullException(nameof(passenger));

            if (passenger.CurrentFloor != FloorNumber)
                throw new InvalidOperationException($"Passenger is on floor {passenger.CurrentFloor}, not floor {FloorNumber}.");

            WaitingPassengers.Add(passenger);
            passenger.IsWaiting = true;
        }

        public Passenger RemoveWaitingPassenger(int passengerId)
        {
            var passenger = WaitingPassengers.FirstOrDefault(p => p.Id == passengerId);
            if (passenger == null)
                throw new InvalidOperationException($"Passenger {passengerId} not found on floor {FloorNumber}.");

            WaitingPassengers.Remove(passenger);
            passenger.IsWaiting = false;
            return passenger;
        }

        public void ClearWaitingPassengers()
        {
            foreach (var passenger in WaitingPassengers)
            {
                passenger.IsWaiting = false;
            }
            WaitingPassengers.Clear();
        }

        public bool HasWaitingPassengers()
        {
            return WaitingPassengers.Any();
        }

        public int GetWaitingPassengerCount()
        {
            return WaitingPassengers.Count;
        }
    }
}