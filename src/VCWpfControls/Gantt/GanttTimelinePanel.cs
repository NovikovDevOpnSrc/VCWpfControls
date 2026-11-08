using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VCWpfControls.Grid;

namespace VCWpfControls.Gantt;

/// <summary>
/// The right half of a <see cref="GanttView"/>: a timescale header plus one bar row per
/// task row of the companion tree grid (row heights stay in lock-step). Draws bars with a
/// completion fill, milestone diamonds, dependency arrows, a today line and hover/selected
/// states; raises <see cref="BarDoubleClicked"/> for editor integration.
/// </summary>
public sealed class GanttTimelinePanel : FrameworkElement
{
    /// <summary>Identifies the tasks source.</summary>
    public static readonly DependencyProperty TasksProperty = DependencyProperty.Register(
        nameof(Tasks), typeof(IReadOnlyList<GanttTask>), typeof(GanttTimelinePanel),
        new PropertyMetadata(Array.Empty<GanttTask>(), OnLayoutChanged));

    /// <summary>Identifies the dependencies source.</summary>
    public static readonly DependencyProperty DependenciesProperty = DependencyProperty.Register(
        nameof(Dependencies), typeof(IReadOnlyList<GanttDependency>), typeof(GanttTimelinePanel),
        new PropertyMetadata(Array.Empty<GanttDependency>(), OnLayoutChanged));

    /// <summary>Identifies the <see cref="Unit"/> dependency property.</summary>
    public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
        nameof(Unit), typeof(GanttTimescaleUnit), typeof(GanttTimelinePanel),
        new PropertyMetadata(GanttTimescaleUnit.Day, OnLayoutChanged));

    /// <summary>Identifies the <see cref="RowHeight"/> dependency property.</summary>
    public static readonly DependencyProperty RowHeightProperty = DependencyProperty.Register(
        nameof(RowHeight), typeof(double), typeof(GanttTimelinePanel),
        new PropertyMetadata(26d, OnLayoutChanged));

    /// <summary>Identifies the <see cref="HeaderHeight"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderHeightProperty = DependencyProperty.Register(
        nameof(HeaderHeight), typeof(double), typeof(GanttTimelinePanel),
        new PropertyMetadata(28d, OnLayoutChanged));

    /// <summary>Identifies the <see cref="HorizontalOffset"/> dependency property.</summary>
    public static readonly DependencyProperty HorizontalOffsetProperty = DependencyProperty.Register(
        nameof(HorizontalOffset), typeof(double), typeof(GanttTimelinePanel),
        new PropertyMetadata(0d, OnLayoutChanged));

    /// <summary>Identifies the <see cref="CriticalTaskIds"/> dependency property.</summary>
    public static readonly DependencyProperty CriticalTaskIdsProperty = DependencyProperty.Register(
        nameof(CriticalTaskIds), typeof(IReadOnlyList<string>), typeof(GanttTimelinePanel),
        new PropertyMetadata(Array.Empty<string>(), OnLayoutChanged));

    /// <summary>Identifies the <see cref="VerticalSyncOffset"/> dependency property.</summary>
    public static readonly DependencyProperty VerticalSyncOffsetProperty = DependencyProperty.Register(
        nameof(VerticalSyncOffset), typeof(double), typeof(GanttTimelinePanel),
        new PropertyMetadata(0d, OnLayoutChanged));

    /// <summary>The vertical scroll offset mirrored from the tree grid (lock-step rows).</summary>
    public double VerticalSyncOffset
    {
        get => (double)GetValue(VerticalSyncOffsetProperty);
        set => SetValue(VerticalSyncOffsetProperty, value);
    }

    /// <summary>Identifies the <see cref="DayWidth"/> dependency property.</summary>
    public static readonly DependencyProperty DayWidthProperty = DependencyProperty.Register(
        nameof(DayWidth), typeof(double), typeof(GanttTimelinePanel),
        new PropertyMetadata(28d, OnLayoutChanged));

    private List<GanttTimescaleCell> _cells = new();
    private DateTime _rangeStart = DateTime.Today;
    private DateTime _rangeEnd = DateTime.Today;
    private int _hoverIndex = -1;

    static GanttTimelinePanel()
    {
        ClipToBoundsProperty.OverrideMetadata(typeof(GanttTimelinePanel), new PropertyMetadata(true));
        FocusableProperty.OverrideMetadata(typeof(GanttTimelinePanel), new FrameworkPropertyMetadata(false));
    }

    /// <summary>The visible tasks, in the SAME order as the tree grid rows.</summary>
    public IReadOnlyList<GanttTask> Tasks
    {
        get => (IReadOnlyList<GanttTask>)GetValue(TasksProperty);
        set => SetValue(TasksProperty, value);
    }

    /// <summary>The dependency arrows to draw.</summary>
    public IReadOnlyList<GanttDependency> Dependencies
    {
        get => (IReadOnlyList<GanttDependency>)GetValue(DependenciesProperty);
        set => SetValue(DependenciesProperty, value);
    }

    /// <summary>The timescale zoom unit.</summary>
    public GanttTimescaleUnit Unit
    {
        get => (GanttTimescaleUnit)GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    /// <summary>The row height — MUST match the tree grid rows (lock-step).</summary>
    public double RowHeight
    {
        get => (double)GetValue(RowHeightProperty);
        set => SetValue(RowHeightProperty, value);
    }

    /// <summary>The timescale header height.</summary>
    public double HeaderHeight
    {
        get => (double)GetValue(HeaderHeightProperty);
        set => SetValue(HeaderHeightProperty, value);
    }

    /// <summary>The horizontal scroll offset (pixels).</summary>
    public double HorizontalOffset
    {
        get => (double)GetValue(HorizontalOffsetProperty);
        set => SetValue(HorizontalOffsetProperty, value);
    }

    /// <summary>Ids of critical-path tasks (their bars render accent-outlined).</summary>
    public IReadOnlyList<string> CriticalTaskIds
    {
        get => (IReadOnlyList<string>)GetValue(CriticalTaskIdsProperty);
        set => SetValue(CriticalTaskIdsProperty, value);
    }

    /// <summary>Width of one day in pixels (drives the total extent).</summary>
    public double DayWidth
    {
        get => (double)GetValue(DayWidthProperty);
        set => SetValue(DayWidthProperty, value);
    }

    /// <summary>The timescale cells of the current range (test hook).</summary>
    public IReadOnlyList<GanttTimescaleCell> Cells => _cells;

    /// <summary>The rendered bar of a task (test hook).</summary>
    internal Rect? BarOf(GanttTask task)
    {
        for (var i = 0; i < Tasks.Count; i++)
        {
            if (ReferenceEquals(Tasks[i], task))
            {
                return BarRect(task, i);
            }
        }

        return null;
    }

    /// <summary>Raised when the user double-clicks a task bar.</summary>
    public event EventHandler<GanttTask>? BarDoubleClicked;

    private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var panel = (GanttTimelinePanel)d;
        panel.RebuildRange();
        panel.InvalidateVisual();
    }

    /// <summary>Recomputes the date range and the timescale cells.</summary>
    private void RebuildRange()
    {
        var tasks = Tasks;
        if (tasks.Count == 0)
        {
            _rangeStart = DateTime.Today.AddDays(-1);
            _rangeEnd = DateTime.Today.AddDays(30);
        }
        else
        {
            _rangeStart = tasks.Min(t => t.Start.Date);
            _rangeEnd = tasks.Max(t => t.Finish.Date).AddDays(1);
        }

        _cells = GanttEngine.BuildTimescale(_rangeStart, _rangeEnd.AddDays(-1), Unit, CultureInfo.CurrentCulture);
        Width = TotalWidth;
    }

    private double TotalWidth => Math.Max((_rangeEnd - _rangeStart).TotalDays * DayWidth, 100);

    private double XOf(DateTime date) => (date.Date - _rangeStart).TotalDays * DayWidth - HorizontalOffset;

    private Rect BarRect(GanttTask task, int index)
    {
        var x = XOf(task.Start);
        var width = Math.Max(task.IsMilestone ? DayWidth * 0.9 : DayWidth * 0.62, (task.Finish.Date - task.Start.Date).TotalDays * DayWidth);
        if (!task.IsMilestone)
        {
            width = Math.Max((task.Finish.Date - task.Start.Date).TotalDays * DayWidth - 2, DayWidth * 0.4);
        }

        var y = HeaderHeight + index * RowHeight;
        var height = Math.Min(RowHeight - 8, 18);
        return new Rect(x + 1, y + (RowHeight - height) / 2, width, height);
    }

    /// <inheritdoc />
    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var accent = TryFindResource(GridResources.AccentKey) as Brush ?? Brushes.DodgerBlue;
        var surface = TryFindResource(GridResources.SurfaceBackgroundKey) as Brush ?? Brushes.White;
        var border = TryFindResource(GridResources.HeaderBorderKey) as Brush ?? Brushes.LightGray;
        var textBrush = TryFindResource(GridResources.TextBrushKey) as Brush ?? Brushes.Black;
        var criticalSet = new HashSet<string>(CriticalTaskIds);

        drawingContext.DrawRectangle(surface, null, new Rect(0, 0, RenderSize.Width, RenderSize.Height));

        // Header background + separator.
        drawingContext.DrawRectangle(surface, new Pen(border, 1), new Rect(0, 0, RenderSize.Width, HeaderHeight));

        // Timescale cells.
        var typeface = new Typeface(SystemFonts.MessageFontFamily, FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
        foreach (var cell in _cells)
        {
            var x = XOf(cell.Start);
            var w = (cell.End - cell.Start).TotalDays * DayWidth;
            if (x + w < 0 || x > RenderSize.Width)
            {
                continue;
            }

            drawingContext.DrawText(new FormattedText(
                cell.Caption,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                11,
                textBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip), new Point(x + 4, 6));
            drawingContext.DrawLine(new Pen(border, 1), new Point(x, 0), new Point(x, RenderSize.Height));
        }

        drawingContext.DrawLine(new Pen(border, 1), new Point(0, HeaderHeight), new Point(RenderSize.Width, HeaderHeight));

        // Today line.
        var todayX = XOf(DateTime.Today);
        if (todayX >= 0 && todayX <= RenderSize.Width)
        {
            drawingContext.DrawLine(new Pen(Brushes.OrangeRed, 1) { DashStyle = DashStyles.Dash }, new Point(todayX, 0), new Point(todayX, RenderSize.Height));
        }

        // Row separators.
        var rowCount = Tasks.Count;
        for (var i = 0; i <= rowCount; i++)
        {
            var y = HeaderHeight + i * RowHeight;
            drawingContext.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(128, 160, 160, 160)), 0.5), new Point(0, y), new Point(RenderSize.Width, y));
        }

        // Bars / milestones.
        for (var i = 0; i < Tasks.Count; i++)
        {
            var task = Tasks[i];
            var rect = BarRect(task, i);
            if (rect.Right < 0 || rect.Left > RenderSize.Width)
            {
                continue;
            }

            if (task.IsMilestone)
            {
                var center = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
                var size = 7;
                var diamond = CreateDiamond(center, size);
                drawingContext.DrawGeometry(i == _hoverIndex ? accent : Brushes.DimGray, new Pen(Brushes.Black, 0.8), diamond);
                continue;
            }

            var critical = criticalSet.Contains(task.Id);
            drawingContext.DrawRectangle(Brushes.LightGray, new Pen(critical ? accent : Brushes.Gray, critical ? 1.6 : 0.8), rect);

            var completionWidth = rect.Width * Math.Clamp(task.Completion, 0, 100) / 100.0;
            if (completionWidth > 0)
            {
                drawingContext.DrawRectangle(accent, null, new Rect(rect.X, rect.Y, Math.Min(completionWidth, rect.Width), rect.Height));
            }

            if (i == _hoverIndex)
            {
                drawingContext.DrawRectangle(null, new Pen(accent, 1.5), rect);
            }
        }

        // Dependency arrows (drawn after bars so they stay visible).
        foreach (var link in Dependencies)
        {
            DrawLink(drawingContext, link, Brushes.Gray);
        }

        foreach (var link in Dependencies)
        {
            if (criticalSet.Contains(link.SuccessorId) && criticalSet.Contains(link.PredecessorId))
            {
                DrawLink(drawingContext, link, accent);
            }
        }
    }

    private static StreamGeometry CreateDiamond(Point center, double size)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(center.X, center.Y - size), false, true);
            ctx.LineTo(new Point(center.X + size, center.Y), false, false);
            ctx.LineTo(new Point(center.X, center.Y + size), false, false);
            ctx.LineTo(new Point(center.X - size, center.Y), false, false);
        }

        geometry.Freeze();
        return geometry;
    }

    private void DrawLink(DrawingContext dc, GanttDependency link, Brush brush)
    {
        var predecessor = Tasks.FirstOrDefault(t => t.Id == link.PredecessorId);
        var successor = Tasks.FirstOrDefault(t => t.Id == link.SuccessorId);
        if (predecessor is null || successor is null)
        {
            return;
        }

        var from = AnchorOf(predecessor, link.Type is GanttLinkType.FinishToStart or GanttLinkType.FinishToFinish, isSource: true);
        var to = AnchorOf(successor, link.Type is GanttLinkType.FinishToStart or GanttLinkType.StartToFinish, isSource: false);
        if (from is null || to is null)
        {
            return;
        }

        var pen = new Pen(brush, 1);
        const double arm = 6;
        var start = from!.Value;
        var end = to!.Value;
        var midX = end.X - arm;
        dc.DrawLine(pen, start, new Point(midX, start.Y));
        dc.DrawLine(pen, new Point(midX, start.Y), new Point(midX, end.Y));
        dc.DrawLine(pen, new Point(midX, end.Y), end);
        DrawArrowHead(dc, brush, end, pointingRight: end.X > midX);
    }

    private Point? AnchorOf(GanttTask task, bool finishSide, bool isSource)
    {
        int index;
        for (index = 0; index < Tasks.Count; index++)
        {
            if (ReferenceEquals(Tasks[index], task))
            {
                break;
            }
        }

        if (index >= Tasks.Count)
        {
            return null;
        }

        var rect = BarRect(task, index);
        // Predecessor anchors stick out of the FINISH side; successors out of the START side.
        var x = finishSide ? rect.Right : rect.Left;
        return new Point(x, rect.Y + rect.Height / 2);
    }

    private static void DrawArrowHead(DrawingContext dc, Brush brush, Point tip, bool pointingRight)
    {
        var size = 4;
        var direction = pointingRight ? 1 : -1;
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(tip, false, true);
            ctx.LineTo(new Point(tip.X - direction * size, tip.Y - size / 2.0), false, false);
            ctx.LineTo(new Point(tip.X - direction * size, tip.Y + size / 2.0), false, false);
        }

        geometry.Freeze();
        dc.DrawGeometry(brush, null, geometry);
    }

    /// <inheritdoc />
    protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        => new PointHitTestResult(this, hitTestParameters.HitPoint);

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var point = e.GetPosition(this);
        var index = TaskAt(point);
        if (index != _hoverIndex)
        {
            _hoverIndex = index;
            InvalidateVisual();
        }

        if (index >= 0)
        {
            var task = Tasks[index];
            ToolTip = $"{task.Name}: {task.Start:d} \u2013 {task.Finish:d} ({task.Completion}%)";
        }
    }

    /// <inheritdoc />
    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverIndex = -1;
        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();
        DetectDoubleClick(TaskAt(e.GetPosition(this)));
        e.Handled = true;
    }

    /// <summary>
    /// Wire-up point: FrameworkElement has neither OnMouseDoubleClick nor the event — a
    /// plain double-click detector in the left-button-down path instead.
    /// </summary>
    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        _lastClickTime = Environment.TickCount64;
    }

    private long _lastClickTime;
    private int _lastClickIndex = -1;

    private void DetectDoubleClick(int index)
    {
        var now = Environment.TickCount64;
        if (index == _lastClickIndex && now - _lastClickTime < 500 && index >= 0)
        {
            BarDoubleClicked?.Invoke(this, Tasks[index]);
            _lastClickIndex = -1;
        }
        else
        {
            _lastClickIndex = index;
        }

        _lastClickTime = now;
    }

    private int TaskAt(Point point)
    {
        if (point.Y < HeaderHeight)
        {
            return -1;
        }

        var index = (int)((point.Y - HeaderHeight) / RowHeight);
        if (index < 0 || index >= Tasks.Count)
        {
            return -1;
        }

        var rect = BarRect(Tasks[index], index);
        return point.X >= rect.Left - 3 && point.X <= rect.Right + 3 ? index : -1;
    }
}
