using System;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CognitiveLedger.AI.Identifiers;
using CognitiveLedger.AI.Services.Ollama;
using CognitiveLedger.Desktop.Helpers;
using CognitiveLedger.Parser.PDF;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable All

namespace CognitiveLedger.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly OllamaService? _ollamaService;
    private readonly IServiceProvider? _serviceProvider;
    //private readonly IPdfTextExtractor? _pdfTextExtractor;
    private readonly IBankIdentifier? _bankIdentifier;
    private readonly IStatementAnalyzerFactory? _statementAnalyzerFactory;

    public IServiceProvider ServiceProvider => _serviceProvider ?? throw new InvalidOperationException("Service provider is not initialized.");
    //public IPdfTextExtractor PdfTextExtractor => _pdfTextExtractor ?? throw new InvalidOperationException("PDF text extractor is not initialized.");
    public IBankIdentifier BankIdentifier => _bankIdentifier ?? throw new InvalidOperationException("Bank identifier is not initialized.");
    public IStatementAnalyzerFactory StatementAnalyzerFactory => _statementAnalyzerFactory ?? throw new InvalidOperationException("Statement analyzer factory is not initialized.");
    
    [ObservableProperty]
    public partial string Greeting { get; set; } = "Welcome to Avalonia!";

    [ObservableProperty]
    public partial bool IsAiInstalled { get; set; }

    [ObservableProperty]
    public partial bool IsAiReady { get; set; }

    private static readonly string[] item = ["*.pdf"];

    [ObservableProperty]
    public partial OllamaDownloadViewModel? DownloadViewModel { get; set; }

    public MainViewModel()
    {
    }

    public MainViewModel(
        OllamaService ollamaService,
        IServiceProvider serviceProvider,
        //IPdfTextExtractor pdfTextExtractor,
        IBankIdentifier bankIdentifier,
        IStatementAnalyzerFactory statementAnalyzerFactory)
    {
        _ollamaService = ollamaService;
        _serviceProvider = serviceProvider;
        //_pdfTextExtractor = pdfTextExtractor;
        _bankIdentifier = bankIdentifier;
        _statementAnalyzerFactory = statementAnalyzerFactory;
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
    
    [RelayCommand]
    private async Task ReadStatement()
    {
        var topLevel = (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (topLevel is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select a statement",
            AllowMultiple = false,
            FileTypeFilter = [new("PDF files") { Patterns = item }]
        });
        
        if (files[0] is null)
            return;

        await using var stream = await files[0].OpenReadAsync();
        //var text = PdfTextExtractor.ExtractText(stream);

        // var response = await BankIdentifier.IdentifyBankAsync(text);
        // if (response.Confidence.HasFlag(BankIdentificationConfidence.Medium | BankIdentificationConfidence.High))
        // {
        //     await ShowUnknownBankIdentifcationDialog();
        //     return;
        // }

        // var statementAnalyzer = StatementAnalyzerFactory.GetAnalyzer(response.BankName.ToString());
        // await statementAnalyzer.AnalyzeStatement(text);
    }

    private async Task ShowUnknownBankIdentifcationDialog()
    {
        var result = await DialogService.ShowAsync(
            "Unrecognized Statement",
            "Could not determine which bank issued this statement. Try another file?",
            DialogIcon.Warning,
            DialogButtons.Ok);
    }
}