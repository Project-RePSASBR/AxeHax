using AxeHax.Core.Patching;
using AxeHax.Core.Verification;

namespace AxeHax.Core.Pipelines.Steps;

public class PrepareEbootPatcherAndVerifyStep : Step
{
    public PrepareEbootPatcherAndVerifyStep(Pipeline pipeline) : base(pipeline)
    {}

    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        EbootPatcher patcher = new(File.Open(this.Game.DecryptedEbootPath!, FileMode.Open, FileAccess.ReadWrite));

        this.Pipeline.Patcher = patcher;

        List<Message> messages = patcher.Verify();
        foreach (Message message in messages)
        {
            State.Logger.LogInfo(Patcher, message.ToString());
        }

        if (messages.Any(m => m.Level == MessageLevel.Error))
        {
            return this.Fail("There were errors while verifying the patch details against the EBOOT. Check the log for more information.");
        }
        
        return Task.CompletedTask;
    }
}