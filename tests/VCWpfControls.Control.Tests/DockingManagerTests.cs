using System.Windows;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Docking;
using Xunit;

namespace VCWpfControls.Control.Tests;

public class DockingManagerTests
{
    private static DockingManager CreateManager(Window window, params DockPane[] panes)
    {
        var manager = new DockingManager { Width = 800, Height = 500 };
        window.Content = manager;
        foreach (var pane in panes)
        {
            manager.Panes.Add(pane);
        }

        TestWindowHost.Pump();
        return manager;
    }

    private static DockPane Pane(string header, DockZone zone, string? name = null)
        => new()
        {
            Header = header,
            Name = name ?? string.Empty,
            Zone = zone,
            Content = new System.Windows.Controls.TextBlock { Text = header },
        };

    [StaFact]
    public void Panes_LandInTheirZones()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var center = Pane("Editor", DockZone.Center);
            var properties = Pane("Properties", DockZone.Right);
            var toolbox = Pane("Toolbox", DockZone.Left);
            var output = Pane("Output", DockZone.Bottom);
            var manager = CreateManager(window, center, properties, toolbox, output);

            Assert.Contains(center, manager.CenterHostForTest().Panes);
            Assert.Contains(properties, manager.RightHostForTest().Panes);
            Assert.Contains(toolbox, manager.LeftHostForTest().Panes);
            Assert.Contains(output, manager.BottomHostForTest().Panes);
            Assert.True(manager.LeftHostForTest().HasVisiblePanes);
            Assert.True(manager.BottomHostForTest().HasVisiblePanes);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Close_HidesPane_AndShowRestoresIt()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var pane = Pane("Explorer", DockZone.Left);
            var manager = CreateManager(window, pane);

            var closed = 0;
            manager.PaneClosed += (_, _) => closed++;

            pane.Close();
            TestWindowHost.Pump();

            Assert.True(pane.IsHidden);
            Assert.Equal(1, closed);
            Assert.False(manager.LeftHostForTest().HasVisiblePanes);
            Assert.DoesNotContain(pane, manager.LeftHostForTest().Panes);

            manager.ShowPane(pane);
            TestWindowHost.Pump();
            Assert.False(pane.IsHidden);
            Assert.True(manager.LeftHostForTest().HasVisiblePanes);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ZoneChange_MovesPaneBetweenZones()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var pane = Pane("Tasks", DockZone.Right);
            var manager = CreateManager(window, pane);

            pane.Zone = DockZone.Bottom;
            TestWindowHost.Pump();

            Assert.DoesNotContain(pane, manager.RightHostForTest().Panes);
            Assert.Contains(pane, manager.BottomHostForTest().Panes);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ActivePane_FollowsZoneSelection()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var first = Pane("Document 1", DockZone.Center);
            var second = Pane("Document 2", DockZone.Center);
            var manager = CreateManager(window, first, second);

            // The most recently docked pane becomes active (like a newly opened tab).
            Assert.Equal(second, manager.ActivePane);

            manager.CenterHostForTest().Select(first);
            TestWindowHost.Pump();
            Assert.Equal(first, manager.ActivePane);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Layout_RoundTripRestoresZonesAndSizes()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var left = Pane("Toolbox", DockZone.Left, "toolbox");
            left.DockWidth = new GridLength(240);
            var right = Pane("Properties", DockZone.Right, "properties");
            var center = Pane("Editor", DockZone.Center, "editor");
            var manager = CreateManager(window, left, right, center);

            var xml = manager.SaveLayout();
            Assert.Contains("toolbox", xml);
            Assert.Contains("properties", xml);

            // Move panes around, then restore.
            left.Zone = DockZone.Bottom;
            right.Close();
            TestWindowHost.Pump();

            manager.LoadLayout(xml);
            TestWindowHost.Pump();

            Assert.Equal(DockZone.Left, left.Zone);
            Assert.False(right.IsHidden);
            Assert.Contains(left, manager.LeftHostForTest().Panes);
            Assert.Contains(right, manager.RightHostForTest().Panes);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void NonClosablePane_HasNoCloseEffect()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var pane = Pane("Pinned", DockZone.Top);
            pane.IsClosable = false;
            var manager = CreateManager(window, pane);

            pane.Close();
            Assert.False(pane.IsHidden);
            Assert.True(manager.TopHostForTest().HasVisiblePanes);
        }
        finally
        {
            window.Close();
        }
    }
}
