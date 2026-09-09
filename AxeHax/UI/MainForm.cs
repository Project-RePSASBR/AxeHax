using Eto.Drawing;
using Eto.Forms;
using AxeHax.Core.Pipelines;
using AxeHax.Core.Pipelines.Psasbr;

namespace AxeHax.UI;

/// <summary>
/// Presents a list of patchers that the user can use to patch for their platform.
/// </summary>
public class MainForm : AxeHaxForm
{
    private readonly Label _updateStatusLabel;

    private string UpdateStatus
    {
        get => this._updateStatusLabel.Text;
        set
        {
            Application.Instance.Invoke(() =>
            {
                this._updateStatusLabel.Text = value;
            });
        }
    }

    public MainForm() : base(string.Empty, new Size(450, -1))
    {
        StackLayout layout;
        this.Content = layout = new StackLayout
        // ReSharper disable once RedundantExplicitParamsArrayCreation
        ([
            new Label { Text = "Welcome to AxeHax! Please pick a patching method to continue." },
            this._updateStatusLabel = new Label(),
            new Label { Text = "PlayStation All-Stars Battle Royale:" },
            this.PipelineButton<PsasbrPS3PatchPipeline>("Patch PSASBR for PS3"),
            this.PipelineButton<PsasbrRPCS3PatchPipeline>("Patch PSASBR for RPCS3"),

            new Label { Text = "Advanced (for experts):" },
            this.PipelineButton<ElfToElfPatchPipeline>(".elf->.elf Patch"),

            #if DEBUG
            new Label { Text = "Debugging options:" },
            this.PipelineButton<ExamplePipeline>("Example Pipeline"),
            #endif
        ]);

        layout.Spacing = 5;
        layout.HorizontalContentAlignment = HorizontalAlignment.Stretch;

        this._updateStatusLabel.Text = "Loading updater...";
    }

    private Button PipelineButton<TPipeline>(string name) where TPipeline : Pipeline, new()
    {
        return new Button((_, _) => this.ShowChild<PipelineForm<TPipeline>>()) { Text = name };
    }
}