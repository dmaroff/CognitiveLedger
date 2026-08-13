using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CognitiveLedger.Desktop.ViewModels;

namespace CognitiveLedger.Desktop.Views;

public partial class OllamaDownloadView : Window
{
    // Avalonia can't selectively hide just the OS title bar's close button, so instead we
    // cancel every close attempt unless it was triggered by our own code (Cancel button or
    // the download completing) via CloseInternal().
    private bool _allowClose;

    public OllamaDownloadView()
    {
        InitializeComponent();
        Opened += OnOpened;
        Closing += OnClosing;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is not OllamaDownloadViewModel viewModel)
        {
            return;
        }

        viewModel.Completed += (_, _) => CloseInternal();
        await viewModel.StartAsync();
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => CloseInternal();

    private void CloseInternal()
    {
        _allowClose = true;
        Close();
    }
}
