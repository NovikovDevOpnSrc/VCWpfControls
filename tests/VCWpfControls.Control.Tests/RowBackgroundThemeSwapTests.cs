using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The theme-swap stale-zebra bug: rows COPIED the grid's brush values at prepare time —
/// when the palette dictionary swapped, the grid's DynamicResource re-resolved but nothing
/// re-propagated to already-realized rows, and the odd (zebra) rows kept the old theme's
/// background (DarkBlue stripes after switching to Light). Rows now BIND to the grid's
/// RowBackground / AlternatingRowBackground (PriorityBinding keeps the odd-row
/// AlternatingRowBackground ?? RowBackground fallback), so every DP re-resolve reaches
/// every realized row.
/// </summary>
public sealed class RowBackgroundThemeSwapTests
{
    private static GridControl MakeGrid(int rows = 30)
    {
        var grid = new GridControl
        {
            Width = 700,
            Height = 240,
            AutoGenerateColumns = true,
            RowHeight = 26,
        };
        grid.ItemsSource = new ObservableCollection<TestModels.Employee>(TestModels.CreateEmployees(rows));
        return grid;
    }

    [StaFact]
    public void BrushSwap_ReachesEveryRealizedRow()
    {
        var grid = MakeGrid();
        var navy = new SolidColorBrush(Color.FromRgb(0x20, 0x30, 0x4A));
        var light = new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xFA));
        grid.AlternatingRowBackground = navy;
        grid.RowBackground = new SolidColorBrush(Colors.White);

        var window = TestWindowHost.Show(grid);
        TestWindowHost.Pump();
        try
        {
            // The zebra rows carry the navy brush.
            var rows = GridRowsOf(grid);
            Assert.Contains(rows, r => ReferenceEquals(r.Background, navy));

            // The theme swap: the grid's DP re-resolves (here simulated directly).
            grid.AlternatingRowBackground = light;
            TestWindowHost.Pump();

            foreach (var row in rows)
            {
                Assert.False(ReferenceEquals(row.Background, navy),
                    "a realized row still carries the previous palette's zebra brush after the swap");
            }

            Assert.Contains(rows, r => ReferenceEquals(r.Background, light));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void DictionarySwap_NoRowKeepsTheOldPalette()
    {
        // The real flow: the page sets AlternatingRowBackground through a DynamicResource
        // token; ThemeManager swaps the palette dictionary in the app-level resources.
        var grid = MakeGrid();
        grid.SetResourceReference(GridControl.AlternatingRowBackgroundProperty, GridResources.AlternatingRowBackgroundKey);

        var host = new System.Windows.Controls.StackPanel();
        host.Children.Add(grid);
        var darkBlue = new ResourceDictionary
        {
            Source = new System.Uri("pack://application:,,,/VCWpfControls;component/Themes/DarkBlue.xaml"),
        };
        host.Resources.MergedDictionaries.Add(darkBlue);

        var window = TestWindowHost.Show(host);
        TestWindowHost.Pump();
        try
        {
            var rows = GridRowsOf(grid);
            var navy = Assert.IsType<SolidColorBrush>(darkBlue[GridResources.AlternatingRowBackgroundKey]);
            Assert.Contains(rows, r => r.Background is SolidColorBrush navyRow && navyRow.Color == navy.Color);

            // Swap the palette (what ThemeManager.Replace does at the app level).
            var light = new ResourceDictionary
            {
                Source = new System.Uri("pack://application:,,,/VCWpfControls;component/Themes/Light.xaml"),
            };
            host.Resources.MergedDictionaries.Remove(darkBlue);
            host.Resources.MergedDictionaries.Add(light);
            TestWindowHost.Pump();
            TestWindowHost.Pump();

            var lightZebra = Assert.IsType<SolidColorBrush>(light[GridResources.AlternatingRowBackgroundKey]);
            foreach (var row in rows)
            {
                if (row.Background is SolidColorBrush brush)
                {
                    Assert.NotEqual(navy.Color, brush.Color); // no stale DarkBlue stripes
                }
            }

            Assert.Contains(rows, r => r.Background is SolidColorBrush zebra && zebra.Color == lightZebra.Color);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void OddRows_FallBackToRowBackground_WhenAlternatingIsNull()
    {
        var grid = MakeGrid(10);
        var rowBrush = new SolidColorBrush(Colors.Beige);
        grid.RowBackground = rowBrush; // Alternating stays null — the ?? fallback path

        var window = TestWindowHost.Show(grid);
        TestWindowHost.Pump();
        try
        {
            foreach (var row in GridRowsOf(grid))
            {
                Assert.True(ReferenceEquals(row.Background, rowBrush) || row.Background is null,
                    "with no AlternatingRowBackground every row must follow RowBackground");
            }
        }
        finally
        {
            window.Close();
        }
    }

    private static System.Collections.Generic.List<Grid.Primitives.GridRow> GridRowsOf(GridControl grid)
    {
        var result = new System.Collections.Generic.List<Grid.Primitives.GridRow>();
        void Walk(System.Windows.DependencyObject d)
        {
            var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(d);
            for (var i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(d, i);
                if (child is Grid.Primitives.GridRow row)
                {
                    result.Add(row);
                }

                Walk(child);
            }
        }

        Walk(grid);
        return result;
    }
}
