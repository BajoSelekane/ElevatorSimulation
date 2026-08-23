namespace ElevatorSimulation.Domain.Enums
{
    public enum ElevatorStatus
    {
        Stationary,
        Moving,
        DoorsOpen,
        DoorsClosing,
        OutOfService
    }

    public enum ElevatorDirection
    {
        Idle,
        Up,
        Down
    }

    public enum ElevatorType
    {
        Standard,
        HighSpeed,
        Freight,
        Glass
    }
}