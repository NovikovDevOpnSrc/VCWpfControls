using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The binding-error guard (browser-verification idea, WPF edition): after a scenario
/// pumps, <see cref="BindingErrorScanner"/> walks the realized tree and reports every
/// binding whose status is a hard failure. The library's own templates and column
/// bindings must stay clean — a silent binding failure is a real bug class here (the
/// Department editor once rendered empty because a path missed the <c>Data.</c> prefix).
/// </summary>
public sealed class BindingErrorGuardTests
{
    [StaFact]
    public void BrokenBinding_IsDetected()
    {
        var bound = new TextBox { DataContext = "seed" }; // a real source LACKING the path
        bound.SetBinding(TextBox.TextProperty, new System.Windows.Data.Binding("NoSuchProperty.Seed"));
        var working = new TextBox { DataContext = "seed" };
        working.SetBinding(TextBox.TextProperty, new System.Windows.Data.Binding("Length")
        {
            Mode = System.Windows.Data.BindingMode.OneWay,
        });
        var panel = new StackPanel();
        panel.Children.Add(bound);
        panel.Children.Add(working);

        var window = TestWindowHost.Show(panel);
        TestWindowHost.Pump();
        try
        {
            Assert.Equal("4", working.Text); // the control: evaluation machinery ran

            var failures = BindingErrorScanner.FailuresUnder(panel);
            Assert.Contains(failures, f => f.Contains("NoSuchProperty", StringComparison.Ordinal));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void RealisticGrid_NoBindingFailures()
    {
        var grid = new GridControl
        {
            Width = 800,
            Height = 300,
            AutoGenerateColumns = true,
            RowHeight = 26,
            FrozenColumnCount = 1,
        };
        grid.ItemsSource = new ObservableCollection<TestModels.Employee>(TestModels.CreateEmployees(12));

        var window = TestWindowHost.Show(grid);
        TestWindowHost.Pump();
        TestWindowHost.Pump();
        try
        {
            Assert.Empty(BindingErrorScanner.FailuresUnder(grid));
        }
        finally
        {
            window.Close();
        }
    }
}
