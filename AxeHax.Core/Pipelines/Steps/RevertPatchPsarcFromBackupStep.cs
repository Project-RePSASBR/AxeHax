using AxeHax.Core.Accessors;

namespace AxeHax.Core.Pipelines.Steps;

public class RevertPatchPsarcFromBackupStep : Step
{
    public RevertPatchPsarcFromBackupStep(Pipeline pipeline) : base(pipeline)
    { }

    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        PatchAccessor.Try(this, () =>
        {
            string titleId = this.Game.TitleId;
            string usrDir = $"game/{titleId}/USRDIR";

            string psarc = Path.Combine(usrDir, "patch.psarc");
            string backup = Path.Combine(usrDir, "patch.psarc.orig");

            if (!this.Pipeline.Accessor!.FileExists(backup))
            {
                this.Fail("The original backup couldn't be found. Is your game unpatched?");
                return;
            }

            this.Progress = 0.25f;

            if (this.Pipeline.Accessor!.FileExists(psarc))
            {
                this.Progress = 0.50f;
                this.Pipeline.Accessor.RemoveFile(psarc);
            }

            this.Progress = 0.75f;
            this.Pipeline.Accessor.MoveFile(backup, psarc);
        });

        this.Progress = 1.0f;
        return Task.CompletedTask;
    }
}