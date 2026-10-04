using AxeHax.Core.Pipelines.Steps;

namespace AxeHax.Core.Pipelines.Psasbr;

public class PsasbrPS3NoPsarcEncryptPatchPipeline : Pipeline
{
    public override string Id => "psasbr-ps3-patch";
    public override string Name => "PSASBR PS3 Patch";

    protected override Type SetupAccessorStepType => typeof(SetupPS3AccessorStep);
    public override bool ReplacesEboot => true;

    public override string? ShorthandUrlId => "ps3";

    public override IEnumerable<string> GameNameFilters => ["PlayStation® All-Stars Battle Royale", "All-Stars Battle Royale", "psasbr"];

    protected override List<Type> StepTypes =>
    [
        // Info gathering stage
        typeof(ValidateGameStep),
        typeof(DownloadParamSfoStep),
        typeof(DownloadGameEbootStep),
        typeof(ReadEbootContentIdStep),
        typeof(DownloadGameLicenseStep),
        typeof(GetConsoleIdpsStep),

        // Decryption and patch stage
        typeof(PrepareSceToolStep),
        typeof(DecryptGameEbootStep),
        typeof(PrepareEbootPatcherAndVerifyStep),
        typeof(ApplyPatchToEbootStep),
        typeof(ApplySprxPatchToEbootStep),

        // Encryption and upload stage
        typeof(EncryptGameEbootStep),
        typeof(BackupGameEbootBeforeReplaceStep),
        typeof(UploadCircleButtonSprxStep),
        typeof(UploadGameEbootStep),
    ];
}