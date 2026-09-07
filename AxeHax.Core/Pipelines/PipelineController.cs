using AxeHax.Core.Platform;

namespace AxeHax.Core.Pipelines;

/// <summary>
/// A set of common utilities to assist in controlling a pipeline from a UI.
/// </summary>
public sealed class PipelineController : IAccessesPlatform
{
    private readonly Pipeline _pipeline;
    private readonly Action<Action> _uiThread;
    
    private CancellationTokenSource? _cts;
    
    public IPlatformInterface Platform => this._pipeline.Platform;

    public PipelineController(Pipeline pipeline, Action<Action> uiThread)
    {
        this._pipeline = pipeline;
        this._uiThread = uiThread;
    }

    public bool PipelineRunning => this._cts != null;

    public bool EnableControls => this._pipeline.State != PipelineState.Running;
    
    public string MainButtonText => this._pipeline.State switch
    {
        PipelineState.NotStarted => "Patch!",
        PipelineState.Running => "Patching... (click to cancel)",
        PipelineState.Finished => "Complete!",
        PipelineState.Cancelled => "Patch!",
        PipelineState.Error => "Retry (patch failed!)",
        _ => throw new ArgumentOutOfRangeException(),
    };

    public void CancelPipeline()
    {
        this._cts?.Cancel();
    }

    public void MainButtonClick()
    {
        if (this._pipeline.State == PipelineState.Running)
        {
            this.CancelPipeline();
            return;
        }

        this._cts = new CancellationTokenSource();
        
        if (this._pipeline.State is PipelineState.Cancelled or PipelineState.Error or PipelineState.Finished)
        {
            this._pipeline.Reset();
        }
        
        Task.Run(async () =>
        {
            try
            {
                this.Platform.PrepareThread();
                await this._pipeline.ExecuteAsync(this._cts?.Token ?? CancellationToken.None);
            }
            catch (Exception ex)
            {
                this.Platform.ErrorPrompt($"Unhandled error while running pipeline {this._pipeline.Name}: {ex}");
            }
            finally
            {
                this.Platform.PrepareStopThread();
            }
        }, this._cts?.Token ?? CancellationToken.None);
    }
}