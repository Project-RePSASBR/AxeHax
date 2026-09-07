namespace AxeHax.Core.Pipelines;

public enum PipelineState : byte
{
    NotStarted,
    Running,
    Finished,
    Cancelled,
    Error,
}