using AxeHax.Core.Platform;

namespace AxeHax.Core.Pipelines.Steps;

public class OutputElfStep : Step
{
    public OutputElfStep(Pipeline pipeline) : base(pipeline)
    { }

    public override List<StepInput> Inputs =>
    [
        CommonStepInputs.ElfOutput,
    ];

    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        string elfOutput = this.Inputs.First().GetValueFromPipeline(this.Pipeline);
        if (File.Exists(elfOutput) && this.Platform.Ask("The output EBOOT already exists. Would you like to replace it?") == QuestionResult.No)
        {
            return Task.CompletedTask;
        }

        File.Copy(this.Game.DecryptedEbootPath!, Path.GetFullPath(elfOutput));

        return Task.CompletedTask;
    }
}