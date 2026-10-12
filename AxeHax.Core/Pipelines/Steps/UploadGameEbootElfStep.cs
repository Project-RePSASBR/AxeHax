using AxeHax.Core.Accessors;

namespace AxeHax.Core.Pipelines.Steps;

public class UploadGameEbootElfStep : Step
{
    public UploadGameEbootElfStep(Pipeline pipeline) : base(pipeline)
    {
    }

    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        PatchAccessor.Try(this, async () =>
        {
            string titleId = this.Game.TitleId;
            string usrDir = $"game/{titleId}/USRDIR/";

            string eboot = Path.Combine(usrDir, "EBOOT.elf");

            if (this.Pipeline.Accessor!.FileExists(eboot))
                this.Pipeline.Accessor.RemoveFile(eboot);

            this.Progress = 0.5f;

            await this.Pipeline.Accessor.UploadFileWithProgressAsync(
                this.Game.DecryptedEbootPath!, eboot, this, cancellationToken);
        });

        return Task.CompletedTask;
    }
}