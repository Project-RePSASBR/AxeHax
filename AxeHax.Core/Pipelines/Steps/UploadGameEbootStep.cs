using AxeHax.Core.Accessors;

namespace AxeHax.Core.Pipelines.Steps;

public class UploadGameEbootStep : Step
{
    public UploadGameEbootStep(Pipeline pipeline) : base(pipeline)
    {
    }

    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        PatchAccessor.Try(this, async () =>
        {
            string titleId = this.Game.TitleId;
            string usrDir = $"game/{titleId}/USRDIR/";
        
            string eboot = Path.Combine(usrDir, "EBOOT.BIN");
            
            if (this.Pipeline.Accessor!.FileExists(eboot))
                this.Pipeline.Accessor.RemoveFile(eboot);

            this.Progress = 0.5f;

            await this.Pipeline.Accessor.UploadFileWithProgressAsync(
                this.Game.EncryptedEbootPath!, eboot, this, cancellationToken);
        });

        return Task.CompletedTask;
    }
}