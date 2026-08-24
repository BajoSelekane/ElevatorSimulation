using System;
using System.Collections.Generic;
using System.Text;

namespace ElevatorSimulation.Domain.Exceptions
{
    public class SimulatorStateException : DomainException
    {
        public SimulatorStateException(string message) : base(message) { }
    }
}
