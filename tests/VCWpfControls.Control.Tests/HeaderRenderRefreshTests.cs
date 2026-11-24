using System.Collections.ObjectModel;
using System.Windows;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The blank-header-launch guard: a user report showed empty header captions with
/// theme-fallback colors right after demo launch — the visual tree was provably correct
/// (the caption binding, generation, re-entry and theme-swap paths all check out), so the
/// surviving frame was a stale launch raster that nothing ever invalidated again (the
/// band's layout does not change after load). The grid now force-repaints the whole header
/// subtree once, after the first Render pass (Loaded fires below Render priority).
/// </summary>
public sealed class HeaderRenderRefreshTests
{
    [StaFact]
    public void Loaded_ForceRefreshesHeaderBand_ExactlyOnce()
    {
        var grid = new GridControl
        {
            Width = 700,
            Height = 260,
            AutoGenerateColumns = true,
        };
        grid.ItemsSource = new ObservableCollection<TestModels.Employee>(TestModels.CreateEmployees(4));
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
        var grid = new GridControl
        {
            Width = 700,
            Height = 260,
            AutoGenerateColumns = true,
        };
        grid.ItemsSource = new ObservableCollection<TestModels.Employee>(TestModels.CreateEmployees(4));
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
}
