using System.Collections.ObjectModel;
using System.Windows.Media;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Themes;
using Xunit;
using static VCWpfControls.Control.Tests.TestModels;

namespace VCWpfControls.Control.Tests;

public class GridUxTests
{
    [StaFact]
    public void EmptyState_ShownWhenNoRows_HiddenWhenRowsAppear()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        var data = new ObservableCollection<Employee>();
        grid.ItemsSource = data;
        TestWindowHost.Pump();

        try
        {
            var empty = grid.Template.FindName("PART_EmptyState", grid) as System.Windows.Controls.TextBlock;
            Assert.NotNull(empty);
            Assert.Equal(System.Windows.Visibility.Visible, empty!.Visibility);
            Assert.Equal(Localization.GridStrings.NoData, empty.Text);

            data.Add(new Employee { Id = 1, Name = "A" });
            TestWindowHost.Pump();
            Assert.Equal(System.Windows.Visibility.Collapsed, empty.Visibility);

            data.Remove(data[0]);
            TestWindowHost.Pump();
            Assert.Equal(System.Windows.Visibility.Visible, empty.Visibility);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Theme_SwitchesTextBrush_DarkThemeReadable()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees(3);
        TestWindowHost.Pump();

        try
        {
            var lightForeground = (SolidColorBrush)grid.Foreground;
            GridThemeManager.Apply(grid, GridTheme.Dark);
            TestWindowHost.Pump();

            var darkForeground = (SolidColorBrush)grid.Foreground;
            Assert.NotEqual(lightForeground.Color, darkForeground.Color);

            // Dark theme text must be light: each channel noticeably above 128.
            Assert.True(darkForeground.Color.R > 128 && darkForeground.Color.G > 128 && darkForeground.Color.B > 128,
                $"dark text color {darkForeground.Color} must be light for readability");
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void DefaultTypography_IsFluentBody14()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        try
        {
            Assert.Equal(14d, grid.FontSize);
        }
        finally
        {
            window.Close();
        }
    }
}
