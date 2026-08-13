using System.ComponentModel;
using Avalonia.Controls;
using CognitiveLedger.Desktop.ViewModels;

namespace CognitiveLedger.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.PropertyChanged += OnMainViewModelPropertyChanged;
        }
    }

    private async void OnMainViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainViewModel.DownloadViewModel))
        {
            return;
        }

        if (sender is not MainViewModel { DownloadViewModel: { } downloadViewModel } viewModel)
        {
            return;
        }

        var dialog = new OllamaDownloadView { DataContext = downloadViewModel };
        await dialog.ShowDialog(this);
        viewModel.DownloadViewModel = null;
    }
}
