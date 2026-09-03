using System;
using Avalonia.Controls;
using CognitiveLedger.Desktop.ViewModels;

namespace CognitiveLedger.Desktop.Views;

public partial class DialogView : Window
{
    public DialogView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is DialogViewModel viewModel)
        {
            viewModel.RequestClose += (_, _) => Close();
        }
    }
}
