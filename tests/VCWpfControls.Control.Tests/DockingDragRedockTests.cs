using System.Windows;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Docking;
using Xunit;

namespace VCWpfControls.Control.Tests;

public class DockingDragRedockTests
{
    [StaFact]
    public void DragTabToZone_MovesPane()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var manager = new DockingManager { Width = 800, Height = 600 };
            window.Content = manager;
            var pane = new DockPane
            {
                Header = "P",
                Name = "p",
                Zone = DockZone.Center,
                Content = new System.Windows.Controls.TextBlock { Text = "x" },
            };
            manager.Panes.Add(pane);
            TestWindowHost.Pump();

            // Simulate the drag: start, move over the right edge, drop.
            manager.StartPaneDrag(pane);
            manager.CompletePaneDragAtForTest(new Point(780, 300));
            TestWindowHost.Pump();

            Assert.Equal(DockZone.Right, pane.Zone);
            Assert.Contains(pane, manager.RightHostForTest().Panes);

            // Drop in the middle moves it back to the center zone.
            manager.StartPaneDrag(pane);
            manager.CompletePaneDragAtForTest(new Point(400, 300));
            TestWindowHost.Pump();
            Assert.Equal(DockZone.Center, pane.Zone);
            Assert.Contains(pane, manager.CenterHostForTest().Panes);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ZoneAt_EdgesAndCenter()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var manager = new DockingManager { Width = 800, Height = 600 };
            window.Content = manager;
            TestWindowHost.Pump();

            Assert.Equal(DockZone.Left, manager.ZoneAtForTest(new Point(10, 300)));
            Assert.Equal(DockZone.Right, manager.ZoneAtForTest(new Point(790, 300)));
            Assert.Equal(DockZone.Top, manager.ZoneAtForTest(new Point(400, 10)));
            Assert.Equal(DockZone.Bottom, manager.ZoneAtForTest(new Point(400, 590)));
            Assert.Equal(DockZone.Center, manager.ZoneAtForTest(new Point(400, 300)));
        }
        finally
        {
            window.Close();
        }
    }
}
