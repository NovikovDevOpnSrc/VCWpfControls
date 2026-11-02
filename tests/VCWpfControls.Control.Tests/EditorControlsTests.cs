using System.Windows;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Editors;
using Xunit;

namespace VCWpfControls.Control.Tests;

public class EditorControlsTests
{
    private sealed class EditorHost : IDisposable
    {
        public NumericUpDown Spinner { get; } = new() { Width = 160 };

        public MaskedTextBox Masked { get; } = new() { Width = 220 };

        public Window Window { get; }

        public EditorHost()
        {
            Window = TestWindowHost.Show(new System.Windows.Controls.StackPanel
            {
                Children = { Spinner, Masked },
            });
            TestWindowHost.Pump();
        }

        public void Dispose() => Window.Close();
    }

    [StaFact]
    public void NumericUpDown_ValueClampedToRange()
    {
        using var host = new EditorHost();
        var spinner = host.Spinner;
        spinner.MinValue = 10;
        spinner.MaxValue = 20;
        spinner.Value = 15;

        spinner.Value = 99;
        Assert.Equal(20m, spinner.Value);

        spinner.Value = 1;
        Assert.Equal(10m, spinner.Value);

        // Bounds themselves re-coerce the current value.
        spinner.Value = 15;
        spinner.MaxValue = 12;
        Assert.Equal(12m, spinner.Value);
    }

    [StaFact]
    public void NumericUpDown_ValueTextFormats()
    {
        using var host = new EditorHost();
        var spinner = host.Spinner
            ;
        spinner.MinValue = 0;
        spinner.MaxValue = 1000;
        spinner.Increment = 0.25m;
        spinner.Value = 12.5m;
        TestWindowHost.Pump();

        // Decimal places derive from the increment (two).
        Assert.Equal("12.50", spinner.ValueText);

        spinner.FormatString = "N1";
        Assert.Equal("12.5", spinner.ValueText);

        spinner.FormatString = null;
        spinner.DecimalPlaces = 0;
        spinner.Value = 13m;
        Assert.Equal("13", spinner.ValueText);
    }

    [StaFact]
    public void NumericUpDown_ValueChangedEventFires()
    {
        using var host = new EditorHost();
        var spinner = host.Spinner;
        spinner.MinValue = 0;
        spinner.MaxValue = 100;

        var raised = 0;
        spinner.ValueChanged += (_, _) => raised++;
        spinner.Value = 42;

        Assert.Equal(1, raised);
        spinner.Value = 42; // no-op
        Assert.Equal(1, raised);
    }

    [StaFact]
    public void NumericUpDown_ReadOnly_IgnoresChanges()
    {
        using var host = new EditorHost();
        var spinner = host.Spinner;
        spinner.MinValue = 0;
        spinner.MaxValue = 100;
        spinner.Value = 10;
        spinner.IsReadOnly = true;

        spinner.ApplyChange(5);
        Assert.Equal(10m, spinner.Value);
    }

    [StaFact]
    public void MaskedTextBox_ShowsMaskedDisplay()
    {
        using var host = new EditorHost();
        var masked = host.Masked;
        masked.Mask = "+7 (000) 000-00-00";
        TestWindowHost.Pump();

        Assert.Equal("+7 (___) ___-__-__", masked.Text);

        masked.Value = "9012345678";
        Assert.Equal("+7 (901) 234-56-78", masked.Text);
        Assert.True(masked.IsMaskComplete);
        Assert.Equal("9012345678", masked.Value);
    }

    [StaFact]
    public void MaskedTextBox_IncompleteValue_KeepsPrompt()
    {
        using var host = new EditorHost();
        var masked = host.Masked;
        masked.Mask = "000-0000";
        masked.Value = "123";

        Assert.Equal("123-____", masked.Text);
        Assert.False(masked.IsMaskComplete);
        Assert.Equal("123", masked.Value);
    }

    [StaFact]
    public void MaskedTextBox_IncludeLiteralsAffectsValue()
    {
        using var host = new EditorHost();
        var masked = host.Masked;
        masked.Mask = "00/00";
        masked.Value = "3112";
        Assert.Equal("3112", masked.Value);

        masked.IncludeLiterals = true;
        Assert.Equal("31/12", masked.Value);
    }
}
