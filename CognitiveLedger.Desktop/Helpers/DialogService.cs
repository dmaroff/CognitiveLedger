using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CognitiveLedger.Desktop.ViewModels;
using CognitiveLedger.Desktop.Views;

namespace CognitiveLedger.Desktop.Helpers;

/// <summary>
/// Shows the generic dialog. View models don't hold a reference to a Window, so this
/// resolves the owner itself the same way MainViewModel does for the file picker.
/// </summary>
public static class DialogService
{
    public static async Task<DialogResult> ShowAsync(
        string title,
        string text,
        DialogIcon icon = DialogIcon.None,
        DialogButtons buttons = DialogButtons.Ok)
    {
        var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner is null)
        {
            return DialogResult.None;
        }

        var viewModel = new DialogViewModel(title, text, icon, buttons);
        var view = new DialogView { DataContext = viewModel };

        await view.ShowDialog(owner);

        return viewModel.Result;
    }
}
