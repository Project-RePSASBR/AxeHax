using AxeHax.Core.Accessors;

namespace AxeHax.Core.Pipelines.Steps;

public class BackupPatchPsarcBeforeReplaceStep : Step
{
    public BackupPatchPsarcBeforeReplaceStep(Pipeline pipeline) : base(pipeline)
    { }

    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        PatchAccessor.Try(this, () =>
        {
            string titleId = this.Game.TitleId;
            string usrDir = $"game/{titleId}/USRDIR";

            string psarc = Path.Combine(usrDir, "patch.psarc");
            string backup = Path.Combine(usrDir, "patch.psarc.orig");

            if (this.Pipeline.Accessor?.FileExists(backup) ?? false)
            {
                State.Logger.LogInfo(
                    InfoRetrieval,
                    "{0} has already been backed up to {1}. Skipping backup.",
                    psarc,
                    backup
                );
                this.Progress = 1.0f;
            }
            else
            {
                this.Progress = 0.5f;
                this.Pipeline.Accessor.MoveFile(psarc, backup);
            }
        });

        return Task.CompletedTask;
    }
}