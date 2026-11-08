using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Docking;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// Nested splitter groups in one docking zone (<see cref="DockPane.SplitGroup"/>): panes of
/// one group tab together; another group stacks below/next to it with an independent
/// splitter; closing the last pane of a group removes its host; layout round-trips.
/// </summary>
public class DockSplitGroupTests
{
    private static DockingManager Create(out Window window)
    {
        var manager = new DockingManager { Width = 640, Height = 420 };
        var tools = new DockPane { Header = "Tools", Zone = DockZone.Left, Content = new TextBlock { Text = "tools" } };
        var explorer = new DockPane { Header = "Explorer", Zone = DockZone.Left, Content = new TextBlock { Text = "explorer" } };
        var output = new DockPane { Header = "Output", Zone = DockZone.Left, SplitGroup = 1, Content = new TextBlock { Text = "output" } };
        var errors = new DockPane { Header = "Errors", Zone = DockZone.Left, SplitGroup = 1, Content = new TextBlock { Text = "errors" } };
        manager.Panes.Add(tools);
        manager.Panes.Add(explorer);
        manager.Panes.Add(output);
        manager.Panes.Add(errors);
        window = TestWindowHost.Show(manager);
        TestWindowHost.Pump();
        return manager;
    }

    [StaFact]
    public void SameGroup_TabsTogether_NewGroup_GetsOwnHostWithSplitter()
    {
        var manager = Create(out var window);
        try
        {
            var group = manager.LeftGroupForTest();
            Assert.NotNull(group);
            Assert.Equal(2, group!.Hosts.Count); // primary + split group 1

            // Group 0: Tools + Explorer tabbed; group 1: Output + Errors tabbed.
            Assert.Contains("Tools", manager.HostCaptionsForTest(0));
            Assert.Contains("Explorer", manager.HostCaptionsForTest(0));
            Assert.Contains("Output", manager.HostCaptionsForTest(1));
            Assert.Contains("Errors", manager.HostCaptionsForTest(1));
            Assert.DoesNotContain("Output", manager.HostCaptionsForTest(0));

            // A splitter separates the two hosts.
            Assert.Equal(1, CountSplitters(group));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ClosingLastPaneOfGroup_RemovesItsHost()
    {
        var manager = Create(out var window);
        try
        {
            var group = manager.LeftGroupForTest()!;
            Assert.Equal(2, group.Hosts.Count);

            var output = manager.Panes.First(p => (p.Header as string) == "Output");
            var errors = manager.Panes.First(p => (p.Header as string) == "Errors");
            output.IsHidden = true;
            TestWindowHost.Pump();
            Assert.Equal(2, group.Hosts.Count); // Errors still keeps group 1 alive

            errors.IsHidden = true;
            TestWindowHost.Pump();
            Assert.Single(group.Hosts); // group host pruned
            Assert.Equal(0, CountSplitters(group));

            // The primary group keeps its panes.
            Assert.True(manager.HasVisiblePanesForTest(DockZone.Left));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ZoneVisibility_SurvivesWhenOnlyNestedHostHasPanes()
    {
        var manager = Create(out var window);
        try
        {
            // Hide every group-0 pane: the zone must stay visible via group 1.
            manager.Panes.First(p => (p.Header as string) == "Tools").IsHidden = true;
            manager.Panes.First(p => (p.Header as string) == "Explorer").IsHidden = true;
            TestWindowHost.Pump();
            Assert.True(manager.HasVisiblePanesForTest(DockZone.Left));

            // And hiding the rest collapses the zone.
            manager.Panes.First(p => (p.Header as string) == "Output").IsHidden = true;
            manager.Panes.First(p => (p.Header as string) == "Errors").IsHidden = true;
            TestWindowHost.Pump();
            Assert.False(manager.HasVisiblePanesForTest(DockZone.Left));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void LayoutXml_RoundTripsSplitGroup()
    {
        var manager = Create(out var window);
        try
        {
            var xml = manager.SaveLayout();
            Assert.Contains("sg=\"1\"", xml);

            // Reset and reload: the split structure returns.
            manager.Panes.First(p => (p.Header as string) == "Output").SplitGroup = 0;
            manager.Panes.First(p => (p.Header as string) == "Errors").SplitGroup = 0;
            TestWindowHost.Pump();
            Assert.Single(manager.LeftGroupForTest()!.Hosts);

            manager.LoadLayout(xml);
            TestWindowHost.Pump();
            Assert.Equal(2, manager.LeftGroupForTest()!.Hosts.Count);
        }
        finally
        {
            window.Close();
        }
    }

    private static int CountSplitters(DockZoneGroup group)
        => DescendantsOf(group).OfType<GridSplitter>().Count();

    private static IEnumerable<DependencyObject> DescendantsOf(DependencyObject root)
    {
        var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (var nested in DescendantsOf(child))
            {
                yield return nested;
            }
        }
    }
}
