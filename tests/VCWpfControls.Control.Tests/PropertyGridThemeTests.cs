using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VCWpfControls.Control.Tests.Infrastructure;
using Xunit;
using PropertyGridControl = VCWpfControls.PropertyGrid.PropertyGrid;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// Regression (user report): the PropertyGrid's code-created texts (category headers,
/// property names, expandable value texts) rendered BLACK in the dark theme — Foreground
/// inheritance from the control does not reach them. Every created TextBlock now binds to
/// the TextBrush palette token explicitly.
/// </summary>
public class PropertyGridThemeTests
{
    private sealed class Sample
    {
        public string Name { get; set; } = "Alpha";
        public int Size { get; set; } = 10;
    }

    [StaFact]
    public void AllTexts_FollowTheThemeTextBrush()
    {
        var grid = new PropertyGridControl();
        grid.Resources.MergedDictionaries.Add(
            new ResourceDictionary { Source = new System.Uri("pack://application:,,,/VCWpfControls;component/Themes/Dark.xaml") });
        grid.SelectedObject = new Sample();
        var window = TestWindowHost.Show(grid);
        try
        {
            var host = (StackPanel)grid.Template.FindName("PART_ItemsHost", grid)!;
            var texts = new List<TextBlock>();
            Collect(host, texts);
            Assert.True(texts.Count >= 3, $"expected the sample texts, got {texts.Count}");
            foreach (var text in texts)
            {
                var foreground = Assert.IsType<SolidColorBrush>(text.Foreground);
                Assert.True(foreground.Color.R > 200, $"text '{text.Text}' renders dark in dark theme (#{foreground.Color})");
            }
        }
        finally
        {
            window.Close();
        }
    }

    private static void Collect(System.Windows.DependencyObject node, List<TextBlock> sink)
    {
        var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(node);
        for (var i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(node, i);
            if (child is TextBlock text)
            {
                sink.Add(text);
            }

            Collect(child, sink);
        }
    }
}
