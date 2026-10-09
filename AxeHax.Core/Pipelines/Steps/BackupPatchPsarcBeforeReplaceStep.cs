using AxeHax.Core.Accessors;

namespace AxeHax.Core.Pipelines.Steps;

public class BackupPatchPsarcBeforeReplaceStep : Step
{
    public BackupPatchPsarcBeforeReplaceStep(Pipeline pipeline) : base(pipeline)
    { }

    public override float Progress { get; protected set; }
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
                this.Progress = 0.5f;
                this.Pipeline.Accessor.CopyFile(psarc, backup);
            }
        });

        return Task.CompletedTask;
    }
}