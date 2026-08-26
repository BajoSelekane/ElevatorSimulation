using ElevatorSimulation.Domain.Exceptions;
using FluentAssertions;

namespace ElevatorSimulation.Domain.Tests
{
    [Trait("Category", "Unit")]
    public class ExceptionTests
    {
        [Fact]
        public void DomainException_ShouldExposeMessageAndInnerException()
        {
            var inner = new InvalidOperationException("inner");
            var ex = new DomainException("failed", inner);

            ex.Message.Should().Be("failed");
            ex.InnerException.Should().Be(inner);
            new DomainException().Message.Should().NotBeNull();
        }

        [Fact]
        public void ElevatorNotFoundException_ShouldStoreElevatorId()
        {
            var named = new ElevatorNotFoundException("missing", 7);

            named.ElevatorId.Should().Be(7);
            named.Message.Should().Be("missing");
            new ElevatorNotFoundException().Message.Should().Contain("Elevator");
            new ElevatorNotFoundException("gone").Message.Should().Be("gone");
        }

        [Fact]
        public void InvalidFloorException_ShouldStoreFloor()
        {
            var ex = new InvalidFloorException("bad floor", 99);

            ex.InvalidFloor.Should().Be(99);
            new InvalidFloorException("bad").Message.Should().Be("bad");
        }

        [Fact]
        public void CapacityExceededException_ShouldStoreCapacities()
        {
            var ex = new CapacityExceededException("full", 10, 12);

            ex.MaxCapacity.Should().Be(10);
            ex.RequestedCapacity.Should().Be(12);
            new CapacityExceededException("full").Message.Should().Be("full");
        }

        [Fact]
        public void PassengerNotFoundException_ShouldStorePassengerId()
        {
            var ex = new PassengerNotFoundException("missing", 42);

            ex.PassengerId.Should().Be(42);
            new PassengerNotFoundException("missing").Message.Should().Be("missing");
        }

        [Fact]
        public void RemainingDomainExceptions_ShouldCarryMessage()
        {
            new SimulatorStateException("bad state").Message.Should().Be("bad state");
            new InvalidOperationDomainException("not allowed").Message.Should().Be("not allowed");
        }
    }
}
