using AxeHax.Core.Accessors;

namespace AxeHax.Core.Pipelines.Steps;

public class BackupGameEbootBeforeReplaceStep : Step
{
    public BackupGameEbootBeforeReplaceStep(Pipeline pipeline) : base(pipeline)
    { }

    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        PatchAccessor.Try(this, () =>
        {
            string titleId = this.Game.TitleId;
            string usrDir = $"game/{titleId}/USRDIR";

            string eboot = Path.Combine(usrDir, "EBOOT.BIN");
            string backup = Path.Combine(usrDir, "EBOOT.BIN.ORIG");

            if (this.Pipeline.Accessor?.FileExists(backup) ?? false)
            {
                State.Logger.LogInfo(
                    InfoRetrieval,
                    "{0} has already been backed up to {1}. Skipping backup.",
                    eboot,
                    backup
                );
                this.Progress = 1.0f;
            }
            else
            {
                this.Progress = 0.5f;
                this.Pipeline.Accessor.MoveFile(eboot, backup);
            }
        });

        return Task.CompletedTask;
    }
}