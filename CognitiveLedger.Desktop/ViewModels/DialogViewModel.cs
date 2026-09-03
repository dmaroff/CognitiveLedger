using System;
using Avalonia.Media;
using CognitiveLedger.Desktop.Helpers;
using CommunityToolkit.Mvvm.Input;

namespace CognitiveLedger.Desktop.ViewModels;

public partial class DialogViewModel : ViewModelBase
{
    public string WindowTitle { get; }
    public string Message { get; }
    public string IconGlyph { get; }
    public IBrush IconBrush { get; }
    public bool IsOkVisible { get; }
    public bool IsCancelVisible { get; }
    public bool IsAbortVisible { get; }

    public DialogResult Result { get; private set; } = DialogResult.None;

    /// <summary>Raised when a button is clicked, so the view can close itself.</summary>
    public event EventHandler? RequestClose;

    public DialogViewModel()
        : this(string.Empty, string.Empty, DialogIcon.None, DialogButtons.Ok)
    {
    }

    public DialogViewModel(string title, string text, DialogIcon icon, DialogButtons buttons)
    {
        WindowTitle = title;
        Message = text;
        (IconGlyph, IconBrush) = GetIconVisual(icon);
        (IsOkVisible, IsCancelVisible, IsAbortVisible) = GetButtonVisibility(buttons);
    }

    [RelayCommand]
    private void Ok() => Close(DialogResult.Ok);

    [RelayCommand]
    private void Cancel() => Close(DialogResult.Cancel);

    [RelayCommand]
    private void Abort() => Close(DialogResult.Abort);

    private void Close(DialogResult result)
    {
        Result = result;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private static (string Glyph, IBrush Brush) GetIconVisual(DialogIcon icon) => icon switch
    {
        DialogIcon.Information => ("ℹ", Brushes.DodgerBlue),
        DialogIcon.Warning => ("⚠", Brushes.Orange),
        DialogIcon.Error => ("✕", Brushes.Crimson),
        DialogIcon.Question => ("?", Brushes.Gray),
        _ => (string.Empty, Brushes.Transparent)
    };

    private static (bool Ok, bool Cancel, bool Abort) GetButtonVisibility(DialogButtons buttons) => buttons switch
    {
        DialogButtons.Ok => (true, false, false),
        DialogButtons.OkCancel => (true, true, false),
        DialogButtons.OkAbort => (true, false, true),
        DialogButtons.AbortCancel => (false, true, true),
        _ => (true, false, false)
    };
}
