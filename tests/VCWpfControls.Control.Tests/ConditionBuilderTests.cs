using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VCWpfControls.Conditions;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid.Data;
using VCWpfControls.Localization;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The ConditionBuilder control: visual tree editing (fields/operators/typed values,
/// And/Or groups, add/remove) and the text tab with positioned errors.
/// </summary>
public class ConditionBuilderTests
{
    private sealed record Person(string Name, string Dept, decimal Salary, DateTime Hired, bool Active)
    {
        public static Person Of(string name, string dept, decimal salary, int year, bool active = true)
            => new(name, dept, salary, new DateTime(year, 1, 31), active);
    }

    private static ConditionBuilder Create(out Window window, ConditionNode? condition = null)
    {
        var builder = new ConditionBuilder
        {
            Width = 520,
            Height = 320,
            Schema = ConditionSchema.FromType(typeof(Person)),
        };
        if (condition is not null)
        {
            builder.Condition = condition;
        }
        window = TestWindowHost.Show(builder);
        TestWindowHost.Pump();
        return builder;
    }

    private static List<ComboBox> ComboBoxes(ConditionBuilder builder)
        => FindAll<ComboBox>(builder);

    private static List<T> FindAll<T>(DependencyObject root)
        where T : DependencyObject
    {
        var result = new List<T>();
        void Walk(DependencyObject node)
        {
            var children = System.Windows.Media.VisualTreeHelper.GetChildrenCount(node);
            for (var i = 0; i < children; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(node, i);
                if (child is T match)
                {
                    result.Add(match);
                }

                Walk(child);
            }
        }

        Walk(root);
        return result;
    }

    [StaFact]
    public void AutoCreates_AndGroupWithAddButtons()
    {
        var builder = Create(out var window);
        try
        {
            var root = Assert.IsType<ConditionGroup>(builder.Condition);
            Assert.Equal(ConditionGroupOperator.And, root.OperatorKind);
            Assert.Empty(root.Children);

            // Header has the And/Or combo and the two add buttons.
            var buttons = FindAll<Button>(builder);
            Assert.Contains(buttons, b => b.Content as string == GridStrings.ConditionAdd);
            Assert.Contains(buttons, b => b.Content as string == GridStrings.ConditionAddGroup);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void AddCondition_CreatesExpressionRow_WithTypedEditors()
    {
        var builder = Create(out var window);
        try
        {
            FindAll<Button>(builder).First(b => b.Content as string == GridStrings.ConditionAdd).RaiseEvent(
                new RoutedEventArgs(Button.ClickEvent));
            TestWindowHost.Pump();

            var root = (ConditionGroup)builder.Condition!;
            var expression = Assert.Single(root.Children.OfType<ConditionExpression>());
            Assert.Equal(nameof(Person.Name), expression.FieldName); // first schema field
            Assert.IsType<string>(expression.Value);

            // Combos: the group And/Or + the row's field and operator (+ string TextBox).
            Assert.Equal(3, ComboBoxes(builder).Count);
            Assert.Contains(FindAll<TextBox>(builder), t => t.Width == 130);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void BoolField_ShowsCheckBox_DateField_ShowsPicker()
    {
        var condition = new ConditionGroup(ConditionGroupOperator.And)
        {
            Children =
            {
                new ConditionExpression(nameof(Person.Active), GridFilterOperator.Equals, true),
                new ConditionExpression(nameof(Person.Hired), GridFilterOperator.Equals, new DateTime(2020, 1, 31)),
            },
        };
        var builder = Create(out var window, condition);
        try
        {
            Assert.Contains(FindAll<CheckBox>(builder), c => c.IsChecked == true);
            Assert.Contains(FindAll<DatePicker>(builder), p => p.SelectedDate == new DateTime(2020, 1, 31));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Between_ShowsTwoEditors()
    {
        var condition = new ConditionGroup(ConditionGroupOperator.And)
        {
            Children = { new ConditionExpression(nameof(Person.Salary), GridFilterOperator.Between, 100m, 500m) },
        };
        var builder = Create(out var window, condition);
        try
        {
            var editors = FindAll<TextBox>(builder).Where(t => t.Width == 130).ToList();
            Assert.Equal(2, editors.Count);
            Assert.Equal("100", editors[0].Text);
            Assert.Equal("500", editors[1].Text);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void FieldChange_SwitchesOperatorSet_AndResetsValue()
    {
        var condition = new ConditionGroup(ConditionGroupOperator.And)
        {
            Children = { new ConditionExpression(nameof(Person.Name), GridFilterOperator.StartsWith, "x") },
        };
        var builder = Create(out var window, condition);
        try
        {
            var fieldCombo = ComboBoxes(builder)[1]; // [0] is the group And/Or combo
            fieldCombo.SelectedItem = builder.Schema!.Find(nameof(Person.Salary));
            TestWindowHost.Pump();

            var expression = (ConditionExpression)((ConditionGroup)builder.Condition!).Children[0];
            Assert.Equal(nameof(Person.Salary), expression.FieldName);
            Assert.Equal(GridFilterOperator.Equals, expression.OperatorKind); // StartsWith not applicable to numbers
            Assert.Equal(0m, expression.Value);                              // typed default, not the string "x"
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void AndOrCombo_SwitchesGroupOperator()
    {
        var builder = Create(out var window);
        try
        {
            var andOr = ComboBoxes(builder)[0];
            andOr.SelectedIndex = 1; // Or (any)
            TestWindowHost.Pump();
            Assert.Equal(ConditionGroupOperator.Or, ((ConditionGroup)builder.Condition!).OperatorKind);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void RemoveButton_DeletesExpression()
    {
        var condition = new ConditionGroup(ConditionGroupOperator.And)
        {
            Children = { new ConditionExpression(nameof(Person.Name), GridFilterOperator.Equals, "x") },
        };
        var builder = Create(out var window, condition);
        try
        {
            FindAll<Button>(builder).First(b => b.Content as string == "✕").RaiseEvent(
                new RoutedEventArgs(Button.ClickEvent));
            TestWindowHost.Pump();
            Assert.Empty(((ConditionGroup)builder.Condition!).Children);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void AddGroup_CreatesNestedGroup()
    {
        var builder = Create(out var window);
        try
        {
            FindAll<Button>(builder).First(b => b.Content as string == GridStrings.ConditionAddGroup).RaiseEvent(
                new RoutedEventArgs(Button.ClickEvent));
            TestWindowHost.Pump();

            var root = (ConditionGroup)builder.Condition!;
            var nested = Assert.Single(root.Children.OfType<ConditionGroup>());
            Assert.Equal(ConditionGroupOperator.And, nested.OperatorKind);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void TextMode_ParseError_ReportsPosition_KeepsCondition()
    {
        var condition = new ConditionGroup(ConditionGroupOperator.And)
        {
            Children = { new ConditionExpression(nameof(Person.Name), GridFilterOperator.Equals, "x") },
        };
        var builder = Create(out var window, condition);
        try
        {
            builder.IsTextMode = true;
            TestWindowHost.Pump();

            var box = FindAll<TextBox>(builder).First(t => t.Width != 130);
            box.Text = "[Name] ~ 'oops'";
            box.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,
                PresentationSource.FromDependencyObject(box) ?? throw new InvalidOperationException("no source"), 0, Key.Enter)
            {
                RoutedEvent = UIElement.KeyDownEvent,
                Source = box,
            });
            TestWindowHost.Pump();

            Assert.Contains("position", builder.ErrorText, StringComparison.Ordinal);
            Assert.Equal("x", ((ConditionExpression)((ConditionGroup)builder.Condition!).Children[0]).Value); // unchanged
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void TextMode_ValidText_ReplacesTree()
    {
        var builder = Create(out var window);
        try
        {
            builder.IsTextMode = true;
            TestWindowHost.Pump();

            var box = FindAll<TextBox>(builder).First(t => t.Width != 130);
            box.Text = "[Salary] > 1000 Or [Dept] = 'IT'";
            box.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,
                PresentationSource.FromDependencyObject(box) ?? throw new InvalidOperationException("no source"), 0, Key.Enter)
            {
                RoutedEvent = UIElement.KeyDownEvent,
                Source = box,
            });
            TestWindowHost.Pump();

            Assert.Equal(string.Empty, builder.ErrorText);
            var root = Assert.IsType<ConditionGroup>(builder.Condition);
            Assert.Equal(ConditionGroupOperator.Or, root.OperatorKind);
            Assert.Equal(2, root.Children.Count);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Preview_FollowsEdits()
    {
        var condition = new ConditionGroup(ConditionGroupOperator.And)
        {
            Children = { new ConditionExpression(nameof(Person.Salary), GridFilterOperator.GreaterThan, 100) },
        };
        var builder = Create(out var window, condition);
        try
        {
            Assert.Equal("[Salary] > 100", builder.PreviewText);
        }
        finally
        {
            window.Close();
        }
    }
}
