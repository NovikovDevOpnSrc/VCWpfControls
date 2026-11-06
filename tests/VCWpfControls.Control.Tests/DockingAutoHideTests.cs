using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Docking;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// Auto-hide of <see cref="DockingManager"/>: an edge pane collapses to a strip tab, the
/// flyout shows it on demand, the pin docks it back, center panes ignore auto-hide, and
/// the layout XML round-trips the flag.
/// </summary>
public class DockingAutoHideTests
{
    private static (DockingManager Manager, DockPane Left, DockPane Center, Window Window) Create()
    {
        var manager = new DockingManager { Width = 600, Height = 400 };
        var left = new DockPane { Header = "Tools", Zone = DockZone.Left, Content = new TextBlock { Text = "tools content" } };
        var center = new DockPane { Header = "Doc", Zone = DockZone.Center, Content = new TextBlock { Text = "doc content" } };
        manager.Panes.Add(left);
        manager.Panes.Add(center);
        var window = TestWindowHost.Show(manager);
        TestWindowHost.Pump();
        return (manager, left, center, window);
    }

    [StaFact]
    public void EdgePane_AutoHidden_LeavesZoneAndShowsTab()
    {
        var (manager, left, _, window) = Create();
        try
        {
            left.IsAutoHidden = true;
            TestWindowHost.Pump();

            Assert.Single(manager.AutoHiddenLeftPanes);
            Assert.Same(left, manager.AutoHiddenLeftPanes[0]);
            Assert.False(manager.HasVisiblePanesForTest(DockZone.Left)); // zone collapsed
            Assert.True(manager.HasAutoHideTabForTest(left));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Flyout_ShowsPane_PinDocksBack()
    {
        var (manager, left, _, window) = Create();
        try
        {
            left.IsAutoHidden = true;
            TestWindowHost.Pump();

            manager.OpenAutoHideFlyout(left);
            TestWindowHost.Pump();
            Assert.True(manager.IsAutoHideFlyoutOpen);
            Assert.Same(left, manager.FlyoutPane);

            left.IsAutoHidden = false; // what the flyout pin button does
            TestWindowHost.Pump();
            Assert.False(manager.IsAutoHideFlyoutOpen);
            Assert.Null(manager.FlyoutPane);
            Assert.Empty(manager.AutoHiddenLeftPanes);
            Assert.True(manager.HasVisiblePanesForTest(DockZone.Left)); // back in the zone
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void CenterPane_IgnoresAutoHide()
    {
        var (manager, _, center, window) = Create();
        try
        {
            center.IsAutoHidden = true;
            TestWindowHost.Pump();

            Assert.False(center.IsAutoHidden); // reverted
            Assert.True(manager.HasVisiblePanesForTest(DockZone.Center));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void HiddenPane_CanAutoHide_AndShowPane_ReturnsItDocked()
    {
        var (manager, left, _, window) = Create();
        try
        {
            left.IsAutoHidden = true;
            left.IsHidden = true;
            TestWindowHost.Pump();
            Assert.Empty(manager.AutoHiddenLeftPanes); // hidden wins

            manager.ShowPane(left);
            TestWindowHost.Pump();
            Assert.Single(manager.AutoHiddenLeftPanes); // still auto-hidden after show

            left.IsAutoHidden = false;
            TestWindowHost.Pump();
            Assert.True(manager.HasVisiblePanesForTest(DockZone.Left));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void LayoutXml_RoundTripsAutoHide()
    {
        var (manager, left, _, window) = Create();
        try
        {
            left.IsAutoHidden = true;
            TestWindowHost.Pump();

            var xml = manager.SaveLayout();
            Assert.Contains("ah=\"1\"", xml);

            left.IsAutoHidden = false;
            TestWindowHost.Pump();
            manager.LoadLayout(xml);
            TestWindowHost.Pump();

            Assert.True(left.IsAutoHidden);
            Assert.Single(manager.AutoHiddenLeftPanes);
        }
        finally
        {
            window.Close();
        }
    }
}
