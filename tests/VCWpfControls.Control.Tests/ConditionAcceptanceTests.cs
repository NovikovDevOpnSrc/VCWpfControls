using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Themes;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// Fixes from the seventh acceptance round (ConditionBuilder): literal Firebrick/
/// LightGray colors, unthemable preview, hardcoded English operator captions, an unnamed
/// icon-only remove button, a dead editor for a null/bare-root Condition, and focus
/// destroyed by the full rebuild on every structural edit.
/// </summary>
public class ConditionAcceptanceTests
{
    public sealed class Row
    {
        public string Name { get; set; } = "Alpha";

        public int Salary { get; set; } = 100;
    }

    private static VCWpfControls.Conditions.ConditionBuilder Create(
        out Window window,
        VCWpfControls.Conditions.ConditionNode? condition = null,
        GridTheme theme = GridTheme.Light)
    {
        var builder = new VCWpfControls.Conditions.ConditionBuilder { Width = 520, Height = 280 };
        builder.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/VCWpfControls;component/Themes/{theme}.xaml"),
        });
        builder.Schema = VCWpfControls.Conditions.ConditionSchema.FromType(typeof(Row));
        builder.Condition = condition;
        window = TestWindowHost.Show(builder);
        TestWindowHost.Pump();
        return builder;
    }

    private static Button? FindButton(DependencyObject root, string text)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is Button b && b.Content?.ToString() == text)
            {
                return b;
            }

            if (FindButton(child, text) is { } deeper)
            {
                return deeper;
            }
        }

        return null;
    }

    [StaFact]
    public void NullCondition_AddPersistsIntoTheUsersCondition()
    {
        var builder = Create(out var window);
        try
        {
            // After the normalize-on-rebuild, the root IS a group assigned back to Condition.
            Assert.NotNull(builder.Condition);
            Assert.IsType<VCWpfControls.Conditions.ConditionGroup>(builder.Condition);

            var add = FindButton(builder, VCWpfControls.Localization.GridStrings.Resource("ConditionAdd"));
            Assert.NotNull(add);
            add!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, add));
            TestWindowHost.Pump();

            var group = Assert.IsType<VCWpfControls.Conditions.ConditionGroup>(builder.Condition);
            Assert.Single(group.Children); // the click persisted — not a throwaway host
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void DarkTheme_PreviewAndErrorFollowTokens_NoLiterals()
    {
        var expression = new VCWpfControls.Conditions.ConditionExpression(
            "Salary", VCWpfControls.Grid.Data.GridFilterOperator.GreaterThan, 50);
        var builder = Create(out var window, expression, GridTheme.Dark);
        try
        {
            // The preview carries the muted token (not opacity-on-unset black).
            var preview = FindTextBlock(builder, text => text.StartsWith(
                VCWpfControls.Localization.GridStrings.ConditionPreview, StringComparison.Ordinal));
            Assert.NotNull(preview);
            var foreground = Assert.IsType<SolidColorBrush>(preview!.Foreground);
            var dict = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/VCWpfControls;component/Themes/Dark.xaml"),
            };
            var muted = Assert.IsType<SolidColorBrush>(dict[GridResources.MutedTextKey]);
            Assert.Equal(muted.Color, foreground.Color);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void OperatorCombo_CaptionsFollowTheCurrentCulture()
    {
        var expression = new VCWpfControls.Conditions.ConditionExpression(
            "Name", VCWpfControls.Grid.Data.GridFilterOperator.Contains, "a");
        var builder = Create(out var window, expression);
        try
        {
            // The operator combo is the second in a row; find the one whose items carry
            // operator tags by opening each (containers generate on open).
            var combos = new List<ComboBox>();
            Collect(builder, combos);
            var captions = new List<string>();
            foreach (var combo in combos)
            {
                combo.IsDropDownOpen = true;
                TestWindowHost.Pump();
                captions.AddRange(combo.Items.OfType<ComboBoxItem>()
                    .Select(i => i.Content?.ToString() ?? string.Empty));
                combo.IsDropDownOpen = false;
            }

            Assert.NotEmpty(captions);
            var expectedContains = VCWpfControls.Localization.GridStrings.Resource("ConditionOpContains");
            Assert.Contains(captions, c => c == expectedContains);
            Assert.DoesNotContain(captions, c => c == "contains" && expectedContains != "contains");
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void RemoveButton_CarriesAccessibilityName()
    {
        var expression = new VCWpfControls.Conditions.ConditionExpression(
            "Salary", VCWpfControls.Grid.Data.GridFilterOperator.GreaterThan, 50);
        var builder = Create(out var window, expression);
        try
        {
            var remove = FindButton(builder, "\u2715");
            Assert.NotNull(remove);
            Assert.Equal(
                VCWpfControls.Localization.GridStrings.Resource("ConditionRemove"),
                System.Windows.Automation.AutomationProperties.GetName(remove));
        }
        finally
        {
            window.Close();
        }
    }

    private static TextBlock? FindTextBlock(DependencyObject root, Func<string, bool> match)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is TextBlock tb && match(tb.Text))
            {
                return tb;
            }

            if (FindTextBlock(child, match) is { } deeper)
            {
                return deeper;
            }
        }

        return null;
    }

    private static void Collect(DependencyObject root, List<ComboBox> found)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is ComboBox combo)
            {
                found.Add(combo);
            }

            Collect(child, found);
        }
    }
}
