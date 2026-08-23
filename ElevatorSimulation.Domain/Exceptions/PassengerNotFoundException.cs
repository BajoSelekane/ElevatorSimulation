using System;
using System.Collections.Generic;
using System.Text;

namespace ElevatorSimulation.Domain.Exceptions
{
    public class PassengerNotFoundException : DomainException
    {
        public int PassengerId { get; }

        public PassengerNotFoundException(string message) : base(message) { }

        public PassengerNotFoundException(string message, int passengerId)
            : base(message)
        {
            PassengerId = passengerId;
        }
    }
}
