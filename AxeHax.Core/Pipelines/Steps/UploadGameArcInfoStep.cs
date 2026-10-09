using System.Reflection;
using AxeHax.Core.Accessors;
using AxeHax.Core.Platform;

namespace AxeHax.Core.Pipelines.Steps;

public class UploadGameArcInfoStep : Step
{
    public UploadGameArcInfoStep(Pipeline pipeline) : base(pipeline)
    { }
    public override float Progress { get; protected set; }
    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await PatchAccessor.TryAsync(this, async () =>
        {
            string titleId = this.Game.TitleId;
            string usrDir = $"game/{titleId}/USRDIR";
            const string arcInfoName = "arc.info";
            string arcInfoPath = usrDir + arcInfoName;
            
            if (this.Pipeline.Accessor!.FileExists(arcInfoPath))
                this.Pipeline.Accessor.RemoveFile(arcInfoPath);

            this.Progress = 0.5f;

            const string question = "Found custom arc.info next to exe file, upload that instead?";
            if (File.Exists(arcInfoName) && this.Platform.Ask(question) == QuestionResult.Yes)
            {
                this.Pipeline.Accessor.UploadFile(arcInfoName, arcInfoPath);
            }
            else
            {
                await using Stream writeStream = this.Pipeline.Accessor.OpenWrite(arcInfoPath);
                await using Stream? readStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(arcInfoName);
                if (readStream == null)
                {
                    await this.Fail($"The arc.info file for {this.Pipeline.Accessor.GetType().Name} is missing from this build! " +
                                    $"Please tell a developer on Discord/GitHub!");
                    return;
                }

                await readStream.CopyToAsync(writeStream, cancellationToken);
                await writeStream.FlushAsync(cancellationToken);
            }
        });
    }
}