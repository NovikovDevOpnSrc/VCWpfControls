using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// Issue #16: template cells with interactive content (a star Button with Focusable=False,
/// kept out of the Tab chain) had NO keyboard channel — Space did nothing. Space on the
/// current template cell now activates the first Invoke/Toggle child through its automation
/// peer (the same channel assistive tech uses).
/// </summary>
public class Issue16TemplateCellKeyboardTests
{
    private sealed class Row
    {
        public string Name { get; set; } = "A";
    }

    private static GridControl CreateGrid(out Window window, out List<string> clicks)
    {
        clicks = new List<string>();
        var clickSink = clicks; // captured by the factory handler (out-params can't be)
        var grid = new GridControl { Width = 400, Height = 200 };
        grid.Columns.Add(new GridTextColumn { FieldName = "Name", Width = new GridColumnWidth(150, GridUnitType.Pixel) });

        // The issue's scenario: a non-focusable button inside CellTemplate (out of the Tab
        // chain; 500 stars must not flood keyboard navigation).
        var template = new DataTemplate();
        var factory = new FrameworkElementFactory(typeof(Button));
        factory.SetValue(Button.ContentProperty, "★");
        factory.SetValue(Button.FocusableProperty, false);
        factory.AddHandler(Button.ClickEvent, new RoutedEventHandler((_, _) => clickSink.Add("star")));
        template.VisualTree = factory;
        grid.Columns.Add(new GridTemplateColumn
        {
            FieldName = "Name",
            Header = "Fav",
            CellTemplate = template,
            Width = new GridColumnWidth(80, GridUnitType.Pixel),
        });
        grid.ItemsSource = new[] { new Row(), new Row() };
        window = TestWindowHost.Show(grid);
        TestWindowHost.Pump();
        return grid;
    }

    [StaFact]
    public void Space_ActivatesTemplateCellButton()
    {
        var grid = CreateGrid(out var window, out var clicks);
        try
        {
            // Current cell = the template column of the first row.
            var templateColumn = grid.Columns[1];
            grid.SetCurrentCellForTest(((Grid.Data.GridDataRow)grid.DataEngine.View[0]).Item, templateColumn);
            TestWindowHost.Pump();

            SendKeyDown(grid, Key.Space);
            TestWindowHost.Pump();

            Assert.Single(clicks); // the star button received the activation
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Space_OnPlainCell_DoesNothing()
    {
        var grid = CreateGrid(out var window, out var clicks);
        try
        {
            grid.SetCurrentCellForTest(((Grid.Data.GridDataRow)grid.DataEngine.View[0]).Item, grid.Columns[0]); // text column
            TestWindowHost.Pump();

            SendKeyDown(grid, Key.Space);
            TestWindowHost.Pump();

            Assert.Empty(clicks); // nothing activated, key not consumed meaninglessly
        }
        finally
        {
            window.Close();
        }
    }

    private static void SendKeyDown(UIElement target, Key key)
    {
        var source = PresentationSource.FromVisual(target);
        Assert.NotNull(source);
        target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source!, 0, key)
        {
            RoutedEvent = UIElement.KeyDownEvent,
            Source = target,
        });
    }
}
