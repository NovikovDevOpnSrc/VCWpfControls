using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Primitives;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The blank-header-launch guard, round 2. The user reproduced on a second page and
/// reported the heal asymmetry: HOVER does nothing (only the border background changes),
/// a CLICK heals — a click sorts, and the sort pipeline re-runs EnsureHeaders, a full
/// re-parent of the cached header controls. That makes the caption's
/// <c>{Binding Column.Header, RelativeSource=TemplatedParent}</c> the suspect: a
/// templated-parent path binding that fails to (re)attach at launch stays empty until the
/// next re-parent. The caption is now RELAYED IN CODE (the Caption DP + TemplateBinding)
/// — no binding-expression lifecycle at all — and the band is force-repainted at Loaded
/// and on DpiChanged. These tests pin the code-side delivery path.
/// </summary>
public sealed class HeaderRenderRefreshTests
{
    private static GridControl MakeGrid()
    {
        var grid = new GridControl { Width = 700, Height = 260, AutoGenerateColumns = true };
        grid.ItemsSource = new ObservableCollection<TestModels.Employee>(TestModels.CreateEmployees(4));
        return grid;
    }

    private static ContentPresenter? HeaderContentOf(GridColumnHeader header)
    {
        var count = VisualTreeHelper.GetChildrenCount(header);
        for (var i = 0; i < count; i++)
        {
            if (VisualTreeHelper.GetChild(header, i) is FrameworkElement root
                && FindByName(root, "HeaderContent") is { } presenter)
            {
                return presenter;
            }
        }

        return null;
    }

    private static ContentPresenter? FindByName(FrameworkElement element, string name)
    {
        if (element.Name == name && element is ContentPresenter presenter)
        {
            return presenter;
        }

        var count = VisualTreeHelper.GetChildrenCount(element);
        for (var i = 0; i < count; i++)
        {
            if (VisualTreeHelper.GetChild(element, i) is FrameworkElement child
                && FindByName(child, name) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    [StaFact]
    public void Loaded_ForceRefreshesHeaderBand_ExactlyOnce()
    {
        var grid = MakeGrid();
        Assert.Equal(0, grid.HeaderRenderRefreshCount); // nothing before the first frame

        var window = TestWindowHost.Show(grid);
        TestWindowHost.Pump();
        try
        {
            // Loaded fired AFTER the first Render pass: the band has been repainted once.
            Assert.Equal(1, grid.HeaderRenderRefreshCount);

            // Exactly once: further pumping must not refresh again.
            TestWindowHost.Pump();
            System.Threading.Thread.Sleep(50);
            TestWindowHost.Pump();
            Assert.Equal(1, grid.HeaderRenderRefreshCount);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void RefreshHeaderRender_TouchesEveryMaterializedHeader()
    {
        var grid = MakeGrid();
        var window = TestWindowHost.Show(grid);
        TestWindowHost.Pump();
        try
        {
            // The manual path works too (used by the Loaded hook; assertible by repeat calls).
            grid.RefreshHeaderRender();
            Assert.Equal(2, grid.HeaderRenderRefreshCount);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Caption_IsDeliveredByCode_NotByTemplatedParentBinding()
    {
        var grid = MakeGrid();
        var window = TestWindowHost.Show(grid);
        TestWindowHost.Pump();
        try
        {
            var presenter = grid.HeadersPresenter!;
            foreach (var header in presenter.Children.OfType<GridColumnHeader>())
            {
                // The DP carries the column's Header verbatim (object relay, not a binding).
                Assert.Equal(header.Column!.Header, header.Caption);

                // And the template actually displays it: the HeaderContent presenter's
                // Content (a TemplateBinding target) is the caption, and its realized
                // TextBlock shows the text.
                var content = HeaderContentOf(header);
                Assert.NotNull(content);
                Assert.Equal(header.Column.Header, content!.Content);
                if (VisualTreeHelper.GetChild(content, 0) is TextBlock text)
                {
                    Assert.Equal(header.Column.Header?.ToString(), text.Text);
                }
            }
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Caption_FollowsRuntimeHeaderChanges()
    {
        var grid = MakeGrid();
        var window = TestWindowHost.Show(grid);
        TestWindowHost.Pump();
        try
        {
            var header = grid.HeadersPresenter!.Children.OfType<GridColumnHeader>().First(h => h.Column!.FieldName == "Name");
            Assert.Equal("Name", header.Caption);

            grid.Columns.First(c => c.FieldName == "Name").Header = "Renamed";
            TestWindowHost.Pump();
            Assert.Equal("Renamed", header.Caption);

            var content = HeaderContentOf(header);
            Assert.NotNull(content);
            Assert.Equal("Renamed", content!.Content);
        }
        finally
        {
            window.Close();
        }
    }
}
