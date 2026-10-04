using AxeHax.Core.Pipelines.Steps;

namespace AxeHax.Core.Pipelines.Psasbr;

public class PsasbrRPCS3NoPsarcEncryptPatchPipeline : Pipeline
{
    public override string Id => "psasbr-rpcs3-patch";
    public override string Name => "PSASBR RPCS3 Patch";

    protected override Type SetupAccessorStepType => typeof(SetupEmulatorAccessorStep);
    public override bool ReplacesEboot => true;
    public override bool UsesPsarcEncryption => false;

    public override string? ShorthandUrlId => "rpcs3";

    public override IEnumerable<string> GameNameFilters => ["PlayStation® All-Stars Battle Royale", "All-Stars Battle Royale", "psasbr"];

    protected override List<Type> StepTypes =>
    [
        // Info gathering stage
        typeof(ValidateGameStep),
        typeof(DownloadParamSfoStep),
        typeof(DownloadGameEbootStep),
        typeof(ReadEbootContentIdStep),
        typeof(DownloadGameLicenseStep),

        // Decryption and patch stage
        typeof(PrepareSceToolStep),
        typeof(DecryptGameEbootStep),
        typeof(PrepareEbootPatchCreatorAndVerifyStep),
        typeof(ApplyPatchToEbootStep),
        typeof(ApplySprxPatchToEbootStep),

        // Encryption and upload stage
        typeof(FakeEncryptGameEbootStep),
        typeof(PrintInfoForEncryptedGameEbootStep),
        typeof(BackupGameEbootBeforeReplaceStep),
        typeof(UploadCircleButtonSprxStep),
        typeof(UploadGameEbootStep),
    ];
}