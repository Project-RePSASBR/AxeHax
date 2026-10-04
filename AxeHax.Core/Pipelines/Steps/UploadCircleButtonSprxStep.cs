using System.Reflection;
using AxeHax.Core.Accessors;
using AxeHax.Core.Platform;

namespace AxeHax.Core.Pipelines.Steps;

public class UploadCircleButtonSprxStep : Step
{
    public UploadCircleButtonSprxStep(Pipeline pipeline) : base(pipeline)
    { }

    public override float Progress { get; protected set; }
    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await PatchAccessor.TryAsync(this, async () =>
        {
            const string pluginsFolder = "plugins/";
            const string sprxName = "CircleButton.sprx";
            const string sprxPath = pluginsFolder + sprxName;

            this.Pipeline.Accessor!.CreateDirectoryIfNotExists(pluginsFolder);

            if (this.Pipeline.Accessor.FileExists(sprxPath))
                this.Pipeline.Accessor.RemoveFile(sprxPath);

            this.Progress = 0.5f;

            const string question = "Found custom CircleButton.sprx next to exe file, upload that instead?";

            if (File.Exists(sprxName) && this.Platform.Ask(question) == QuestionResult.Yes)
            {
                this.Pipeline.Accessor.UploadFile(sprxName, sprxPath);
            }
            else
            {
                await using Stream writeStream = this.Pipeline.Accessor.OpenWrite(sprxPath);
                await using Stream? readStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(sprxName);

                if (readStream == null)
                {
                    await this.Fail($"The sprx file for {this.Pipeline.Accessor.GetType().Name} is missing from this build! " +
                                    $"Please tell a developer on Discord/GitHub!");

                    return;
                }

                await readStream.CopyToAsync(writeStream, cancellationToken);
                await writeStream.FlushAsync(cancellationToken);
            }
        });
    }
}