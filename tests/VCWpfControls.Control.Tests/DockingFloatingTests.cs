using System.Windows;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Docking;
using Xunit;

namespace VCWpfControls.Control.Tests;

public class DockingFloatingTests
{
    private static DockingManager CreateManager(Window window, out DockPane pane)
    {
        var manager = new DockingManager { Width = 700, Height = 400 };
        window.Content = manager;
        pane = new DockPane
        {
            Header = "Explorer",
            Name = "explorer",
            Zone = DockZone.Left,
            Content = new System.Windows.Controls.TextBlock { Text = "content" },
        };
        manager.Panes.Add(pane);
        TestWindowHost.Pump();
        return manager;
    }

    [StaFact]
    public void FloatPane_OpensWindowWithPane_AndDockPaneBackRestoresZone()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var manager = CreateManager(window, out var pane);
            Assert.Contains(pane, manager.LeftHostForTest().Panes);

            manager.FloatPane(pane);
            TestWindowHost.Pump();

            Assert.True(pane.IsFloating);
            Assert.DoesNotContain(pane, manager.LeftHostForTest().Panes);
            Assert.Single(manager.FloatingWindowsForTest());
            var floating = manager.FloatingWindowsForTest()[0];
            Assert.Same(pane, floating.Content);

            manager.DockPaneBack(pane);
            TestWindowHost.Pump();

            Assert.False(pane.IsFloating);
            Assert.Empty(manager.FloatingWindowsForTest());
            Assert.Contains(pane, manager.LeftHostForTest().Panes);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ClosingFloatingWindow_DocksPaneBack()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var manager = CreateManager(window, out var pane);
            manager.FloatPane(pane);
            TestWindowHost.Pump();

            var floating = manager.FloatingWindowsForTest()[0];
            floating.Close();
            TestWindowHost.Pump();

            Assert.False(pane.IsFloating);
            Assert.Contains(pane, manager.LeftHostForTest().Panes);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Layout_RoundTripKeepsFloatingState()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var manager = CreateManager(window, out var pane);
            manager.FloatPane(pane);
            TestWindowHost.Pump();

            var xml = manager.SaveLayout();
            Assert.Contains("floating=\"1\"", xml);

            manager.DockPaneBack(pane);
            TestWindowHost.Pump();
            Assert.False(pane.IsFloating);

            manager.LoadLayout(xml);
            TestWindowHost.Pump();
            Assert.True(pane.IsFloating);
            Assert.Single(manager.FloatingWindowsForTest());
        }
        finally
        {
            foreach (var floating in managerWindowsOf(window))
            {
                floating.Close();
            }

            window.Close();
        }

        static System.Collections.Generic.List<Window> managerWindowsOf(Window window)
        {
            var result = new System.Collections.Generic.List<Window>();
            if (window.Content is DockingManager manager)
            {
                result.AddRange(manager.FloatingWindowsForTest());
            }

            return result;
        }
    }
}
