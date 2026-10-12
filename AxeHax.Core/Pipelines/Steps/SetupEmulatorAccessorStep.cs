using AxeHax.Core.Accessors;

namespace AxeHax.Core.Pipelines.Steps;

public class SetupEmulatorAccessorStep : Step
{
    public SetupEmulatorAccessorStep(Pipeline pipeline) : base(pipeline)
    { }


    public override List<StepInput> Inputs { get; } = [
        CommonStepInputs.RPCS3Folder,
    ];

    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        string path = this.Inputs[0].GetValueFromPipeline(this.Pipeline);

        if (path.StartsWith("search-ms:"))
            return this.Fail(
                "The path you entered is a Windows search link. " +
                "Please use the real file path. " +
                "(it usually starts with C:\\ or some other drive letter)");

        State.Logger.LogDebug(RPCS3, $"Using RPCS3 path {path}");
        this.Pipeline.Accessor = new EmulatorPatchAccessor(path);

        return Task.CompletedTask;
    }
}