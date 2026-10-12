using Eto.Drawing;
using Eto.Forms;
using System.Runtime.InteropServices;
using AxeHax.Core;

namespace AxeHax.UI;

/// <summary>
/// Helper abstract class of <see cref="Form"/> to help with titles and apply styling.
/// </summary>
public abstract class AxeHaxForm : Form
{
    protected AxeHaxForm(string subtitle, Size size, bool padBottom = true)
    {
        UpdateSubtitle(subtitle);

        this.ClientSize = size;
        // this.AutoSize = true;
        this.Padding = new Padding(10, 10, 10, padBottom ? 10 : 0);

        try
        {
            this.Icon = Icon.FromResource("AllStarsMatchmaker.ico");
        }
        catch (Exception ex)
        {
            // Not very important, so these should just be warnings, not errors
            State.Logger.LogWarning(LogType.AxeHaxForm, $"Unhandled exception while loading AxeHax.ico in {this.GetType()}: {ex}");

            // attempt to fall back to png icon
            try
            {
                this.Icon = Icon.FromResource("AllStarsMatchmaker.png");
            }
            catch (Exception ex2)
            {
                State.Logger.LogWarning(LogType.AxeHaxForm, $"Unhandled exception while loading AxeHax.png in {this.GetType()}: {ex2}");
            }
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Menu = new AxeHaxMenuBar();
    }

    /// <summary>
    /// Shows a child <see cref="AxeHaxForm"/>.
    /// </summary>
    /// <param name="close">Should the parent window be closed after the child is shown? Defaults to true.</param>
    /// <typeparam name="TForm">The child <see cref="AxeHaxForm"/> to show.</typeparam>
    protected void ShowChild<TForm>(bool close = true) where TForm : AxeHaxForm, new()
    {
        TForm form = new();
        form.Show();

        State.Logger.LogDebug(OSIntegration, $"Showing child form {form.GetType().Name} '{form.Title}'");

        if (close)
        {
            Application.Instance.MainForm = form;
            this.Close();
        }
    }

    protected void UpdateSubtitle(string subtitle)
    {
        this.Title = "AxeHax";
        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            this.Title += " - " + subtitle;
        }
    }

    private class AxeHaxMenuBar : MenuBar
    {
        public AxeHaxMenuBar()
        {
            this.Style = "MenuBar";
            this.IncludeSystemItems = MenuBarSystemItems.All;
        }
    }
}