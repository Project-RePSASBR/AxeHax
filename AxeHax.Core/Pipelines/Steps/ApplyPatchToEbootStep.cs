namespace AxeHax.Core.Pipelines.Steps;

public class ApplyPatchToEbootStep : Step
{
    public ApplyPatchToEbootStep(Pipeline pipeline) : base(pipeline)
    {}

    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        this.Pipeline.Patcher!.Patch();
        this.Pipeline.Patcher!.Dispose();
        return Task.CompletedTask;
    }
}
