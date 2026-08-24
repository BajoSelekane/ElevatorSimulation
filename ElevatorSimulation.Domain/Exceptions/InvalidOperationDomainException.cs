using System;
using System.Collections.Generic;
using System.Text;

namespace ElevatorSimulation.Domain.Exceptions
{
    public class InvalidOperationDomainException : DomainException
    {
        public InvalidOperationDomainException(string message) : base(message) { }
    }
}
