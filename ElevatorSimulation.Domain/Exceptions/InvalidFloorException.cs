using System;
using System.Collections.Generic;
using System.Text;

namespace ElevatorSimulation.Domain.Exceptions
{
    public class InvalidFloorException : DomainException
    {
        public int InvalidFloor { get; }

        public InvalidFloorException(string message) : base(message) { }

        public InvalidFloorException(string message, int invalidFloor)
            : base(message)
        {
            InvalidFloor = invalidFloor;
        }
    }
}
