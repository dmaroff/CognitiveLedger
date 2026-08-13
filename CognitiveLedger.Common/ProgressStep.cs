namespace CognitiveLedger.Common;

public class ProgressStep
{
    private readonly int _totalSteps;
    private int _currentStep;
    public decimal PercentageCompleted { get; private set; } = 0;
    public string Text { get; private set; } = string.Empty;

    public ProgressStep(int totalSteps)
    {
        _totalSteps = totalSteps;
        _currentStep = 0;
    }

    public ProgressStep NextStep(string text)
    {
        _currentStep++;
        Text = text;
        PercentageCompleted = (decimal)_currentStep / _totalSteps * 100;
        return this;
    }
}