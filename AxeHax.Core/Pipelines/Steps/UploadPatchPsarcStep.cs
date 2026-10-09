using System.Reflection;
using AxeHax.Core.Accessors;
using AxeHax.Core.Platform;

namespace AxeHax.Core.Pipelines.Steps;

public class UploadPatchPsarcStep : Step
{
    public UploadPatchPsarcStep(Pipeline pipeline) : base(pipeline)
    { }
    public override float Progress { get; protected set; }
    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await PatchAccessor.TryAsync(this, async () =>
        {
            string titleId = this.Game.TitleId;
            string usrDir = $"game/{titleId}/USRDIR";
            const string psarcName = "patch.psarc";
            string psarcPath = usrDir + psarcName;

            if (this.Pipeline.Accessor!.FileExists(psarcPath))
                this.Pipeline.Accessor.RemoveFile(psarcPath);

            this.Progress = 0.5f;

            if (File.Exists(psarcName))
            {
                this.Pipeline.Accessor.UploadFile(psarcName, psarcPath);
            }
            else
            {
                await this.Fail($"The patch.psarc file for {this.Game.TitleId} was not found next to the AxeHax executable! " +
                                $"Contact a developer on Discord/GitHub on how to recover this file.");
                return;
            }
        });
    }
}