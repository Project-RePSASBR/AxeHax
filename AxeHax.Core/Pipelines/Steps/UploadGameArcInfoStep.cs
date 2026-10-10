using System.Reflection;
using AxeHax.Core.Accessors;
using AxeHax.Core.Platform;

namespace AxeHax.Core.Pipelines.Steps;

public class UploadGameArcInfoStep : Step
{
    public UploadGameArcInfoStep(Pipeline pipeline) : base(pipeline)
    { }
    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await PatchAccessor.TryAsync(this, async () =>
        {
            string titleId = this.Game.TitleId;
            string usrDir = $"game/{titleId}/USRDIR/";
            const string arcInfoName = "arc.info";

            string arcInfoPath = Path.Combine(usrDir, arcInfoName);
            
            if (this.Pipeline.Accessor!.FileExists(arcInfoPath))
                this.Pipeline.Accessor.RemoveFile(arcInfoPath);

            this.Progress = 0.5f;

            const string question = "Found custom arc.info next to exe file, upload that instead?";
            if (File.Exists(arcInfoName) && this.Platform.Ask(question) == QuestionResult.Yes)
            {
                await this.Pipeline.Accessor.UploadFileWithProgressAsync(arcInfoName, arcInfoPath, this, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await using Stream? resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(arcInfoName);

                if (resourceStream == null)
                {
                    await this.Fail($"The sprx file for {this.Pipeline.Accessor.GetType().Name} is missing from this build! " +
                                    $"Please tell a developer on Discord/GitHub!");
                    return;
                }

                await this.Pipeline.Accessor.UploadStreamWithProgressAsync(resourceStream, arcInfoPath, this, cancellationToken).ConfigureAwait(false);
            }
        });
    }
}