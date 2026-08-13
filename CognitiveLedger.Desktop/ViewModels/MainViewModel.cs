using System;
using Avalonia.Threading;
using CognitiveLedger.AI.Services.Ollama;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable All

namespace CognitiveLedger.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly OllamaService? _ollamaService;
    private readonly IServiceProvider? _serviceProvider;

    [ObservableProperty]
    public partial string Greeting { get; set; } = "Welcome to Avalonia!";

    [ObservableProperty]
    public partial bool IsAiInstalled { get; set; }

    [ObservableProperty]
    public partial bool IsAiReady { get; set; }

    [ObservableProperty]
    public partial OllamaDownloadViewModel? DownloadViewModel { get; set; }

    public MainViewModel()
    {
    }

    public MainViewModel(OllamaService ollamaService, IServiceProvider serviceProvider)
    {
        _ollamaService = ollamaService;
        _serviceProvider = serviceProvider;
        _ollamaService.StateChanged += (_, _) => Dispatcher.UIThread.Post(RefreshState);
        RefreshState();
    }

    private void RefreshState()
    {
        IsAiInstalled = _ollamaService?.IsInstalled ?? false;
        IsAiReady = _ollamaService?.IsRunning ?? false;
    }

    [RelayCommand]
    private void InstallAi()
    {
        if (_serviceProvider is null || DownloadViewModel is not null)
        {
            return;
        }

        // Just shows the view - OllamaDownloadView triggers the actual download/install/start
        // itself from its Loaded event, once it's really on screen.
        DownloadViewModel = _serviceProvider.GetRequiredService<OllamaDownloadViewModel>();
    }
}
