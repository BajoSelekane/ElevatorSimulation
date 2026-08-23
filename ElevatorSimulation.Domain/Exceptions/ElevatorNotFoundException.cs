using System;
using System.Collections.Generic;
using System.Text;

namespace ElevatorSimulation.Domain.Exceptions
{
    public class ElevatorNotFoundException : DomainException
    {
        public int ElevatorId { get; }

        public ElevatorNotFoundException(string message) : base(message) { }

        public ElevatorNotFoundException(string message, int elevatorId)
            : base(message)
        {
            ElevatorId = elevatorId;
        }
    }
}
