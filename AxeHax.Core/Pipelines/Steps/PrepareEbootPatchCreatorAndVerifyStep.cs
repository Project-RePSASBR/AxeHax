using AxeHax.Core;
using AxeHax.Core.Patching;
using AxeHax.Core.Pipelines;
using AxeHax.Core.Verification;
using AxeHax.Core.Patching;
using AxeHax.Core.Verification;

namespace AxeHax.Core.Pipelines.Steps;

public class PrepareEbootPatchCreatorAndVerifyStep : Step
{
    public PrepareEbootPatchCreatorAndVerifyStep(Pipeline pipeline) : base(pipeline)
    { }

    public override List<StepInput> Inputs =>
    [
        CommonStepInputs.RPCS3Folder,
    ];

    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        EbootPatcher patcher = new(File.Open(this.Game.DecryptedEbootPath!, FileMode.Open, FileAccess.ReadWrite), !this.Pipeline.UsesPsarcEncryption);
        patcher.GenerateRpcs3Patch = true;
        patcher.Rpcs3PatchFolder = Path.GetFullPath(Path.Combine(this.Pipeline.Inputs["hdd0-path"], "..", "patches"));
        patcher.TitleId = this.Game.TitleId;
        patcher.GameName = this.Game.Name;
        patcher.GameVersion = this.Game.Version;

        State.Logger.LogDebug(RPCS3, $"RPCS3 patches folder: {patcher.Rpcs3PatchFolder}");

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