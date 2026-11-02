using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace VCWpfControls.Docking;

/// <summary>
/// Adorner highlighting the drop zone under the cursor while a pane tab is dragged.
/// Kept as an adorner so the manager template needs no extra parts.
/// </summary>
public sealed class DockDropAdorner : Adorner
{
    private readonly DockZone _zone;
    private readonly Brush _fill;
    private readonly Brush _border;

    /// <summary>Creates the adorner for the highlighted zone.</summary>
    public DockDropAdorner(UIElement adornedElement, DockZone zone, Brush fill, Brush border)
        : base(adornedElement)
    {
        _zone = zone;
        _fill = fill;
        _border = border;
        IsHitTestVisible = false;
    }

    /// <inheritdoc />
    protected override void OnRender(DrawingContext drawingContext)
    {
        var size = AdornedElement.RenderSize;
        if (size.Width <= 0 || size.Height <= 0)
        {
            return;
        }

        var rect = _zone switch
        {
            DockZone.Left => new Rect(0, 0, size.Width * 0.25, size.Height),
            DockZone.Right => new Rect(size.Width * 0.75, 0, size.Width * 0.25, size.Height),
            DockZone.Top => new Rect(0, 0, size.Width, size.Height * 0.25),
            DockZone.Bottom => new Rect(0, size.Height * 0.75, size.Width, size.Height * 0.25),
            _ => new Rect(size.Width * 0.25, size.Height * 0.25, size.Width * 0.5, size.Height * 0.5),
        };

        drawingContext.DrawRectangle(_fill, new Pen(_border, 2), rect);
    }
}
