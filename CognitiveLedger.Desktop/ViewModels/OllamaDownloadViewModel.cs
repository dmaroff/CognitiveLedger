using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CognitiveLedger.AI.Services.Ollama;
using CognitiveLedger.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

// ReSharper disable All

namespace CognitiveLedger.Desktop.ViewModels;

public partial class OllamaDownloadViewModel : ViewModelBase
{
    private readonly OllamaService? _ollamaService;
    private readonly CancellationTokenSource _cts = new();

    [ObservableProperty]
    public partial decimal PercentageCompleted { get; set; }

    [ObservableProperty]
    public partial string StatusText { get; set; } = "Preparing...";

    [ObservableProperty]
    public partial bool IsRunning { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }
    
    [ObservableProperty]
    public partial bool IsCancelled { get; set; }
    

    /// <summary>Raised once setup finishes successfully, so the view can close itself.</summary>
    public event EventHandler? Completed;

    public OllamaDownloadViewModel()
    {
    }

    public OllamaDownloadViewModel(OllamaService ollamaService)
    {
        _ollamaService = ollamaService;
    }

    /// <summary>
    /// Kicks off the download/install/start flow. Intended to be called by the view once
    /// it's displayed (e.g. from its Opened event), not from the constructor, since it does
    /// real work (network calls, process start) rather than just preparing state.
    /// </summary>
    public async Task StartAsync()
    {
        if (_ollamaService is null || IsRunning)
        {
            return;
        }

        IsRunning = true;
        ErrorMessage = null;
        IsCancelled = false;
        try
        {
            await _ollamaService.DownloadAndStartAsync(ProgressNextStep, _cts.Token);
            Completed?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            // User cancelled via the Cancel command; the view closes itself independently.
            IsCancelled = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsRunning = false;
        }
    }

    [RelayCommand]
    private void Cancel() => _cts.Cancel();

    partial void OnErrorMessageChanged(string? value) => HasError = !string.IsNullOrEmpty(value);
    
    private void ProgressNextStep(ProgressStep step) => Dispatcher.UIThread.Post(() =>
    {
        PercentageCompleted = step.PercentageCompleted;
        StatusText = step.Text;
    });
}
