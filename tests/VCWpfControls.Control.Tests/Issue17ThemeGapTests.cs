using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Themes;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// Issue #17 (pilot consumer, dark theme): stock links stayed system blue on the dark
/// surface (~1-2:1) because no palette themed Hyperlink, and ToggleButton had no implicit
/// style in ANY palette (the reporter suspected a Light/Dark parity gap — the sets were
/// actually equal; both styles were simply missing everywhere). Fixed with implicit
/// styles in all palettes + a PARITY guard: the implicit style TYPE SET must be identical
/// across Light/Dark/HighContrast.
/// </summary>
public class Issue17ThemeGapTests
{
    private static ResourceDictionary Load(string name)
    {
        _ = new GridControl(); // pack:// registration
        return new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/VCWpfControls;component/Themes/{name}.xaml"),
        };
    }

    [StaFact]
    public void ThemedPalettes_DefineTheSameImplicitStyleSet()
    {
        var byTheme = new Dictionary<string, HashSet<Type>>();
        foreach (var name in new[] { "Light", "Dark", "HighContrast" })
        {
            var set = new HashSet<Type>();
            foreach (var key in Load(name).Keys)
            {
                if (key is Type targetType)
                {
                    set.Add(targetType); // implicit styles are keyed by their TargetType
                }
            }

            byTheme[name] = set;
        }

        Assert.NotEmpty(byTheme["Light"]);
        void AssertSame(string a, string b)
        {
            var onlyA = string.Join(",", byTheme[a].Except(byTheme[b]).Select(t => t.Name).Order());
            var onlyB = string.Join(",", byTheme[b].Except(byTheme[a]).Select(t => t.Name).Order());
            Assert.True(onlyA.Length == 0 && onlyB.Length == 0,
                $"implicit style sets differ: only in {a}: [{onlyA}]; only in {b}: [{onlyB}]");
        }

        AssertSame("Light", "Dark");
        AssertSame("Light", "HighContrast");
    }

    [StaFact]
    public void Hyperlink_FollowsTheDarkPalette()
    {
        var host = new StackPanel();
        host.Resources.MergedDictionaries.Add(Load("Dark"));
        var link = new Hyperlink(new Run("docs"));
        var text = new TextBlock();
        text.Inlines.Add(link);
        host.Children.Add(text);

        var window = TestWindowHost.Show(host);
        TestWindowHost.Pump();
        try
        {
            var foreground = Assert.IsType<SolidColorBrush>(link.Foreground);
            var accentText = Assert.IsType<SolidColorBrush>(Load("Dark")[GridResources.AccentTextKey]);
            Assert.Equal(accentText.Color, foreground.Color); // #77B7F7 on #292929 = 6.86:1
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ToggleButton_DarkChromeFollowsTheTheme()
    {
        var host = new StackPanel();
        host.Resources.MergedDictionaries.Add(Load("Dark"));
        var toggle = new System.Windows.Controls.Primitives.ToggleButton
        {
            Content = "Toggle me",
            Width = 120,
        };
        host.Children.Add(toggle);

        var window = TestWindowHost.Show(host);
        TestWindowHost.Pump();
        try
        {
            Assert.True(toggle.ActualHeight >= 32, $"toggle {toggle.ActualHeight:F0}px < 32");

            var width = (int)Math.Ceiling(host.ActualWidth);
            var height = (int)Math.Ceiling(host.ActualHeight);
            var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            var visual = new System.Windows.Media.DrawingVisual();
            using (var ctx = visual.RenderOpen())
            {
                ctx.DrawRectangle(new VisualBrush(host), null, new Rect(0, 0, width, height));
            }

            bitmap.Render(visual);
            var pixels = new byte[width * height * 4];
            bitmap.CopyPixels(pixels, width * 4, 0);
            var lumas = new List<int>();
            var tl = toggle.TranslatePoint(new Point(0, 0), host);
            for (var y = (int)tl.Y + 4; y < tl.Y + toggle.ActualHeight - 4; y++)
            {
                for (var x = (int)tl.X + 4; x < tl.X + toggle.ActualWidth - 4; x++)
                {
                    var i = (y * width + x) * 4;
                    if (pixels[i + 3] < 200)
                    {
                        continue;
                    }

                    lumas.Add((pixels[i] * 11 + pixels[i + 1] * 59 + pixels[i + 2] * 30) / 100);
                }
            }

            Assert.NotEmpty(lumas);
            lumas.Sort();
            var median = lumas[lumas.Count / 2];
            Assert.True(median < 150, $"ToggleButton still light chrome: median luma {median}");
        }
        finally
        {
            window.Close();
        }
    }
}
