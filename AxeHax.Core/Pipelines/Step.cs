using AxeHax.Core.Patching;
using AxeHax.Core.Platform;

namespace AxeHax.Core.Pipelines;

public abstract class Step : IAccessesPlatform
{
    protected Pipeline Pipeline { get; }

    public virtual List<StepInput> Inputs { get; } = [];

    protected GameInformation Game => this.Pipeline.GameInformation!;
    protected EncryptionDetails Encryption => this.Pipeline.EncryptionDetails!;

    protected Step(Pipeline pipeline)
    {
        this.Pipeline = pipeline;
    }

    public abstract Task ExecuteAsync(CancellationToken cancellationToken = default);

    public IPlatformInterface Platform => this.Pipeline.Platform;

    public float Progress { get; set; }
    protected bool Failed { get; private set; }

    public Task Fail(string reason)
    {
        this.Failed = true;
        this.Pipeline.Fail(this, reason);
        return Task.CompletedTask;
    }
}