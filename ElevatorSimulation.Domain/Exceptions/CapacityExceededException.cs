namespace ElevatorSimulation.Domain.Exceptions
{
    public class DomainException : Exception
    {
        public DomainException() { }
        public DomainException(string message) : base(message) { }
        public DomainException(string message, Exception inner) : base(message, inner) { }
    }

   

    public class CapacityExceededException : DomainException
    {
        public int MaxCapacity { get; }
        public int RequestedCapacity { get; }

        public CapacityExceededException(string message) : base(message) { }

        public CapacityExceededException(string message, int maxCapacity, int requestedCapacity)
            : base(message)
        {
            MaxCapacity = maxCapacity;
            RequestedCapacity = requestedCapacity;
        }
    }
    
}