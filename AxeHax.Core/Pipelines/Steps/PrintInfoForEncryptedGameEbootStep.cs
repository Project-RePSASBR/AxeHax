using SCEToolSharp;

namespace AxeHax.Core.Pipelines.Steps;

public class PrintInfoForEncryptedGameEbootStep : Step
{
    public PrintInfoForEncryptedGameEbootStep(Pipeline pipeline) : base(pipeline)
    {}

    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        LibSceToolSharp.PrintInfos(this.Game.EncryptedEbootPath);
        return Task.CompletedTask;
    }
}