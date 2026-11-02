using VCWpfControls.Docking;
using Xunit;

namespace VCWpfControls.Core.Tests.Docking;

public class DockLayoutSerializerTests
{
    [Fact]
    public void RoundTrip_PreservesPaneLayouts()
    {
        var model = new DockLayoutModel();
        model.Panes.Add(new DockPaneLayout
        {
            Name = "properties",
            Zone = DockZone.Right,
            Width = 300,
            IsHidden = false,
        });
        model.Panes.Add(new DockPaneLayout
        {
            Name = "toolbox",
            Zone = DockZone.Left,
            Width = 220.5,
            IsHidden = true,
        });
        model.Panes.Add(new DockPaneLayout
        {
            Name = "output",
            Zone = DockZone.Bottom,
            Height = 140,
        });
        model.Panes.Add(new DockPaneLayout
        {
            Name = "floating",
            Zone = DockZone.Center,
            IsFloating = true,
            FloatingLeft = 100.5,
            FloatingTop = 80,
            FloatingWidth = 420,
            FloatingHeight = 320,
        });

        var xml = DockLayoutSerializer.ToXml(model);
        var restored = DockLayoutSerializer.FromXml(xml);

        Assert.Equal(4, restored.Panes.Count);

        Assert.Equal("properties", restored.Panes[0].Name);
        Assert.Equal(DockZone.Right, restored.Panes[0].Zone);
        Assert.Equal(300d, restored.Panes[0].Width);
        Assert.False(restored.Panes[0].IsHidden);

        Assert.Equal("toolbox", restored.Panes[1].Name);
        Assert.Equal(DockZone.Left, restored.Panes[1].Zone);
        Assert.Equal(220.5d, restored.Panes[1].Width);
        Assert.True(restored.Panes[1].IsHidden);

        Assert.Equal("output", restored.Panes[2].Name);
        Assert.Equal(DockZone.Bottom, restored.Panes[2].Zone);
        Assert.Equal(140d, restored.Panes[2].Height);

        Assert.True(restored.Panes[3].IsFloating);
        Assert.Equal(100.5d, restored.Panes[3].FloatingLeft);
        Assert.Equal(320d, restored.Panes[3].FloatingHeight);
    }

    [Fact]
    public void FromXml_ToleratesUnknownZonesAndBadNumbers()
    {
        const string xml = """
            <DockLayout>
              <Pane name="a" zone="Nowhere" width="wide" height="" hidden="1" />
              <Pane name="b" zone="Top" width="100" height="50" hidden="0" />
            </DockLayout>
            """;

        var model = DockLayoutSerializer.FromXml(xml);

        Assert.Equal(2, model.Panes.Count);
        Assert.Equal(DockZone.Center, model.Panes[0].Zone); // unknown zone falls back
        Assert.Equal(0d, model.Panes[0].Width);
        Assert.True(model.Panes[0].IsHidden);
        Assert.Equal(DockZone.Top, model.Panes[1].Zone);
    }

    [Fact]
    public void FromXml_EmptyInput_Throws()
    {
        Assert.Throws<ArgumentException>(() => DockLayoutSerializer.FromXml(" "));
    }
}
