using System.Xml.Linq;

namespace VCWpfControls.Docking;

/// <summary>The docking side of a pane.</summary>
public enum DockZone
{
    /// <summary>Left edge, full height, resizable splitter to the center.</summary>
    Left = 0,

    /// <summary>Top edge, full width, resizable splitter to the center.</summary>
    Top = 1,

    /// <summary>Right edge, full height, resizable splitter to the center.</summary>
    Right = 2,

    /// <summary>Bottom edge, full width, resizable splitter to the center.</summary>
    Bottom = 3,

    /// <summary>Center area, tabbed with the other center panes.</summary>
    Center = 4,
}

/// <summary>UI-free layout snapshot of one pane (for persistence).</summary>
public sealed class DockPaneLayout
{
    /// <summary>Pane identity: its name, falling back to the header text.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The docked zone.</summary>
    public DockZone Zone { get; set; } = DockZone.Center;

    /// <summary>Zone width in pixels (left/right zones).</summary>
    public double Width { get; set; }

    /// <summary>Zone height in pixels (top/bottom zones).</summary>
    public double Height { get; set; }

    /// <summary>Whether the pane is closed/hidden.</summary>
    public bool IsHidden { get; set; }

    /// <summary>Whether the pane is collapsed to an edge tab (auto-hide).</summary>
    public bool IsAutoHidden { get; set; }

    /// <summary>The splitter group of the pane within its zone (0 = primary).</summary>
    public int SplitGroup { get; set; }

    /// <summary>Whether the pane lives in a floating window.</summary>
    public bool IsFloating { get; set; }

    /// <summary>Floating window left edge (screen coordinates).</summary>
    public double FloatingLeft { get; set; }

    /// <summary>Floating window top edge (screen coordinates).</summary>
    public double FloatingTop { get; set; }

    /// <summary>Floating window width.</summary>
    public double FloatingWidth { get; set; }

    /// <summary>Floating window height.</summary>
    public double FloatingHeight { get; set; }
}

/// <summary>UI-free layout snapshot of a whole docking layout.</summary>
public sealed class DockLayoutModel
{
    /// <summary>All pane snapshots.</summary>
    public List<DockPaneLayout> Panes { get; } = new();
}

/// <summary>
/// Serializes <see cref="DockLayoutModel"/> to a compact XML string and back. UI-free and
/// directly unit-testable.
/// </summary>
public static class DockLayoutSerializer
{
    /// <summary>Produces the XML representation of the layout.</summary>
    public static string ToXml(DockLayoutModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var root = new XElement("DockLayout");
        foreach (var pane in model.Panes)
        {
            root.Add(new XElement(
                "Pane",
                new XAttribute("name", pane.Name ?? string.Empty),
                new XAttribute("zone", pane.Zone.ToString()),
                new XAttribute("width", pane.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new XAttribute("height", pane.Height.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new XAttribute("hidden", pane.IsHidden ? "1" : "0"),
                new XAttribute("ah", pane.IsAutoHidden ? "1" : "0"),
                new XAttribute("sg", pane.SplitGroup.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new XAttribute("floating", pane.IsFloating ? "1" : "0"),
                new XAttribute("fx", pane.FloatingLeft.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new XAttribute("fy", pane.FloatingTop.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new XAttribute("fw", pane.FloatingWidth.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new XAttribute("fh", pane.FloatingHeight.ToString(System.Globalization.CultureInfo.InvariantCulture))));
        }

        return root.ToString(SaveOptions.DisableFormatting);
    }

    /// <summary>Parses an XML layout; invalid entries are skipped.</summary>
    public static DockLayoutModel FromXml(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            throw new ArgumentException("Layout XML must not be empty.", nameof(xml));
        }

        var root = XElement.Parse(xml);
        var model = new DockLayoutModel();

        foreach (var element in root.Elements("Pane"))
        {
            var name = (string?)element.Attribute("name") ?? string.Empty;
            if (!Enum.TryParse((string?)element.Attribute("zone"), out DockZone zone))
            {
                zone = DockZone.Center;
            }

            model.Panes.Add(new DockPaneLayout
            {
                Name = name,
                Zone = zone,
                Width = ParseDouble(element.Attribute("width")),
                Height = ParseDouble(element.Attribute("height")),
                IsHidden = (string?)element.Attribute("hidden") == "1",
                IsAutoHidden = (string?)element.Attribute("ah") == "1",
                SplitGroup = int.TryParse((string?)element.Attribute("sg"), out var splitGroup) ? splitGroup : 0,
                IsFloating = (string?)element.Attribute("floating") == "1",
                FloatingLeft = ReadDouble(element, "fx"),
                FloatingTop = ReadDouble(element, "fy"),
                FloatingWidth = ReadDouble(element, "fw"),
                FloatingHeight = ReadDouble(element, "fh"),
            });
        }

        return model;
    }

    private static double ReadDouble(XElement element, string attributeName)
        => double.TryParse((string?)element.Attribute(attributeName), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;

    private static double ParseDouble(XAttribute? attribute)
        => attribute is not null
            && double.TryParse(attribute.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
}
