using Eto.Drawing;
using Eto.Forms;
using Velopack;
using Velopack.Exceptions;
using Velopack.Sources;

namespace AxeHax.UI;

/// <summary>
/// Presents a list of patchers that the user can use to patch for their platform.
/// </summary>
public class MainForm : AxeHaxForm
{
    private readonly Label _updateStatusLabel;

    //private readonly UpdateManager _velo = new(new GithubSource("https://github.com/Project-RePSASBR/AxeHax", null, false));
    private UpdateInfo? _updateInfo;

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
        ]);

        layout.Spacing = 5;
        layout.HorizontalContentAlignment = HorizontalAlignment.Stretch;

        this._updateStatusLabel.Text = "Loading updater...";
    }

    protected override void OnLoadComplete(EventArgs e)
    {
        base.OnLoadComplete(e);
        this.UpdateStatus = "Checking for updates...";

        Task.Run(async () =>
        {
            try
            {
                await this.CheckForUpdatesAsync();
            }
            catch (NotInstalledException)
            {
                this.UpdateStatus = "This build of AxeHax cannot be updated.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to check for updates: {ex}", "AxeHax", MessageBoxType.Error);
            }
        });
    }

    private async Task CheckForUpdatesAsync()
    {
        // UpdateInfo? newVersion = await this._velo.CheckForUpdatesAsync();
        UpdateInfo? newVersion = null; // TODO: Velopack
        if (newVersion == null)
        {
            // this.UpdateStatus = $"AxeHax is up to date! You are on v{this._velo.CurrentVersion?.ToNormalizedString() ?? "<unknown>"}.";
            this.UpdateStatus = $"AxeHax is up to date! You are on v<unknown>.";
            return;
        }

        // TODO: Velopack
        //this.UpdateStatus = "Downloading update...";
        //await this._velo.DownloadUpdatesAsync(newVersion);

        //this._updateInfo = newVersion;

        //this.UpdateStatus = $"An update is available. Click here to install AxeHax {newVersion.TargetFullRelease.Version.ToNormalizedString()}!";
        //await Application.Instance.InvokeAsync(() =>
        //{
        //    this._updateStatusLabel.TextColor = Colors.Orange;
        //    this._updateStatusLabel.MouseUp += this.OnClickUpdate;
        //});
    }

    private void OnClickUpdate(object? sender, MouseEventArgs e)
    {
        if (this._updateInfo == null)
            throw new InvalidOperationException("Cannot install an update if an update is not available");

        // TODO: Velopack
        //this.UpdateStatus = "Installing...";

        //Task.Run(() =>
        //{
        //    try
        //    {
        //        this._velo.ApplyUpdatesAndRestart(this._updateInfo);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Failed to install updates: {ex}", "AxeHax", MessageBoxType.Error);
        //    }
        //});
    }
}