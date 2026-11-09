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

    /// <summary>The scroll viewport height (rows outside [offset, offset+viewport) don't render).</summary>
    internal double ViewportHeight
    {
        get => _viewportHeight;
        set
        {
            _viewportHeight = value;
            InvalidateVisual();
        }
    }

    private double _viewportHeight;

    /// <summary>The first row index the last render pass drew (test hook).</summary>
    internal int FirstRenderedRow { get; private set; }

    /// <summary>The last row index the last render pass drew (test hook).</summary>
    internal int LastRenderedRow { get; private set; }

    /// <summary>The visible row window of the current viewport (culling band).</summary>
    private (int First, int Last) VisibleRowRange
    {
        get
        {
            if (Tasks.Count == 0)
            {
                return (0, -1);
            }

            var viewport = _viewportHeight > 0 ? _viewportHeight : RenderSize.Height;
            var top = VerticalSyncOffset;
            var bottom = top + viewport + RowHeight; // one row of grace on both ends
            var first = Math.Max(0, (int)Math.Floor((top - HeaderHeight - RowHeight) / RowHeight));
            var last = Math.Min(Tasks.Count - 1, (int)Math.Ceiling((bottom - HeaderHeight) / RowHeight));
            return (first, Math.Max(first, last));
        }
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
        => new(TotalWidth, HeaderHeight + Tasks.Count * RowHeight + (_showLegend ? LegendHeight : 0));

    /// <summary>Identifies the <see cref="DayWidth"/> dependency property.</summary>
    public static readonly DependencyProperty DayWidthProperty = DependencyProperty.Register(
        nameof(DayWidth), typeof(double), typeof(GanttTimelinePanel),
        new PropertyMetadata(28d, OnLayoutChanged));

    private List<GanttTimescaleCell> _cells = new();
    private List<GanttTimescaleCell> _majorCells = new();
    private HashSet<string> _parentIds = new();
    private List<DateTime> _nonWorkingDays = new();
    private bool _panning;
    private double _lastPanX;
    private DateTime _rangeStart = DateTime.Today;
    private DateTime _rangeEnd = DateTime.Today;
    private int _hoverIndex = -1;
    private static readonly System.Windows.Media.Brush NonWorkingTint = Frozen(56, 128, 128, 128);
    private static readonly System.Windows.Media.Brush BaselineFill = Frozen(110, 105, 105, 105);

    private static System.Windows.Media.Brush Frozen(byte alpha, byte r, byte g, byte b)
    {
        var brush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(alpha, r, g, b));
        brush.Freeze();
        return brush;
    }

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

    /// <summary>The working-time calendar (weekends/holidays tint under the grid; null = none).</summary>
    public GanttCalendar? Calendar
    {
        get => _calendar;
        set
        {
            _calendar = value;
            RebuildRange();
            InvalidateVisual();
        }
    }

    private GanttCalendar? _calendar;

    /// <summary>The non-working days of the current range (test hook).</summary>
    public IReadOnlyList<DateTime> NonWorkingDays => _nonWorkingDays;

    /// <summary>The MAJOR (upper) timescale row cells (test hook).</summary>
    public IReadOnlyList<GanttTimescaleCell> MajorCells => _majorCells;

    /// <summary>Whether the task is a parent (renders as an Oracle summary bar; test hook).</summary>
    internal bool IsSummaryTask(GanttTask task) => _parentIds.Contains(task.Id);

    /// <summary>
    /// Applies a discrete zoom step (wheel path; test hook): positive zooms OUT
    /// (Day→Week→Month), negative zooms IN. Raises <see cref="ZoomStepRequested"/> for the
    /// owning view (the zoom unit is its dependency property).
    /// </summary>
    internal void RequestZoom(int steps) => ZoomStepRequested?.Invoke(this, steps);

    /// <summary>Pans the chart horizontally by a pixel increment (drag-pan path; test hook).</summary>
    internal void PanBy(double pixelIncrement) => PanRequested?.Invoke(this, pixelIncrement);

    /// <summary>The resize/move/pan cursor for a chart point (hover path; test hook).</summary>
    internal System.Windows.Input.Cursor? CursorForZone(Point point)
    {
        var (index, mode) = TaskZoneAt(point);
        return mode switch
        {
            DragMode.ResizeStart or DragMode.ResizeFinish => System.Windows.Input.Cursors.SizeWE,
            DragMode.Move => System.Windows.Input.Cursors.SizeAll,
            _ when index < 0 && point.Y >= HeaderHeight => System.Windows.Input.Cursors.Hand,
            _ => null,
        };
    }

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

    /// <summary>Whether baseline bars render under the main bars (when a task carries a baseline).</summary>
    public bool ShowBaseline
    {
        get => _showBaseline;
        set
        {
            _showBaseline = value;
            InvalidateVisual();
        }
    }

    private bool _showBaseline = true;

    /// <summary>
    /// The thin baseline bar of a task (test hook): spanning the baseline dates at the top
    /// of the row band; null when the task has no baseline or baselines are hidden.
    /// </summary>
    internal Rect? BaselineBarOf(GanttTask task)
    {
        if (!_showBaseline || task.BaselineStart is not { } start || task.BaselineFinish is not { } finish)
        {
            return null;
        }

        var index = IndexOfReference(task);
        if (index < 0)
        {
            return null;
        }

        var width = Math.Max(DayWidth * 0.5, (finish.Date - start.Date).TotalDays * DayWidth);
        return new Rect(XOf(start) + 1, HeaderHeight + index * RowHeight + 1, width, 5);
    }

    /// <summary>Raised when the user double-clicks a task bar.</summary>
    public event EventHandler<GanttTask>? BarDoubleClicked;

    /// <summary>Raised after the user moved or resized a bar (task dates already updated).</summary>
    public event EventHandler<GanttTask>? TaskEdited;

    /// <summary>Raised after the user drew a link from one bar to another.</summary>
    public event EventHandler<(GanttTask Predecessor, GanttTask Successor, GanttLinkType Type)>? LinkCreated;

    /// <summary>Raised when the wheel (or <see cref="RequestZoom"/>) asks to change the zoom unit.</summary>
    public event EventHandler<int>? ZoomStepRequested;

    /// <summary>Raised while the user pans the chart by dragging empty space (<see cref="PanBy"/>).</summary>
    public event EventHandler<double>? PanRequested;

    /// <summary>Whether the legend strip renders pinned to the bottom of the viewport.</summary>
    public bool ShowLegend
    {
        get => _showLegend;
        set
        {
            _showLegend = value;
            InvalidateMeasure(); // the extent includes the legend strip
            InvalidateVisual();
        }
    }

    private bool _showLegend = true;

    /// <summary>The legend strip height.</summary>
    public double LegendHeight { get; set; } = 22d;

    /// <summary>Whether task names render as labels after the bars (Oracle: default off).</summary>
    public bool ShowBarLabels
    {
        get => _showBarLabels;
        set
        {
            _showBarLabels = value;
            InvalidateVisual();
        }
    }

    private bool _showBarLabels;

    /// <summary>What the pointer is dragging.</summary>
    public enum DragMode
    {
        /// <summary>Not dragging.</summary>
        None = 0,

        /// <summary>Moving the whole bar.</summary>
        Move = 1,

        /// <summary>Resizing the start edge.</summary>
        ResizeStart = 2,

        /// <summary>Resizing the finish edge.</summary>
        ResizeFinish = 3,

        /// <summary>Drawing a dependency link from the finish anchor.</summary>
        LinkFromFinish = 4,

        /// <summary>Drawing a dependency link from the start anchor.</summary>
        LinkFromStart = 5,
    }

    private DragMode _dragMode = DragMode.None;
    private GanttTask? _dragTask;
    private Point _dragOrigin;
    private DateTime _dragStartOrigin;
    private DateTime _dragFinishOrigin;
    private Point _linkCurrent;

    /// <summary>Applies a whole-day move to a task (drag-move path; test hook).</summary>
    internal void MoveTask(GanttTask task, int deltaDays)
    {
        if (deltaDays == 0 || task.IsMilestone)
        {
            return;
        }

        task.Start = task.Start.AddDays(deltaDays);
        task.Finish = task.Finish.AddDays(deltaDays);
        TaskEdited?.Invoke(this, task);
        InvalidateVisual();
    }

    /// <summary>Changes the task duration by moving the start edge (test hook).</summary>
    internal void ResizeTaskStart(GanttTask task, int deltaDays)
    {
        if (deltaDays == 0 || task.IsMilestone)
        {
            return;
        }

        var newStart = task.Start.AddDays(deltaDays);
        if (newStart >= task.Finish.Date)
        {
            return; // keep at least one day
        }

        task.Start = newStart;
        TaskEdited?.Invoke(this, task);
        InvalidateVisual();
    }

    /// <summary>Changes the task duration by moving the finish edge (test hook).</summary>
    internal void ResizeTaskFinish(GanttTask task, int deltaDays)
    {
        if (deltaDays == 0)
        {
            return;
        }

        var newFinish = task.Finish.AddDays(deltaDays);
        if (newFinish <= task.Start.Date)
        {
            return;
        }

        task.Finish = newFinish;
        TaskEdited?.Invoke(this, task);
        InvalidateVisual();
    }

    /// <summary>Creates a link between two tasks (drag-link path; test hook).</summary>
    internal void CreateLink(GanttTask predecessor, GanttTask successor, GanttLinkType type)
        => LinkCreated?.Invoke(this, (predecessor, successor, type));

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
        _majorCells = GanttEngine.BuildMajorTimescale(_rangeStart, _rangeEnd.AddDays(-1), Unit, CultureInfo.CurrentCulture);
        _parentIds = new HashSet<string>(Tasks.Where(t => t.ParentId is not null).Select(t => t.ParentId!), StringComparer.Ordinal);
        _nonWorkingDays = new List<DateTime>();
        if (_calendar is { } calendar)
        {
            for (var day = _rangeStart; day < _rangeEnd; day = day.AddDays(1))
            {
                if (!calendar.IsWorkingDay(day))
                {
                    _nonWorkingDays.Add(day);
                }
            }
        }

        InvalidateMeasure(); // the extent follows the task count and the range
    }

    /// <summary>The bottom edge of the row area (the legend strip is below it).</summary>
    private double RowsBottom => HeaderHeight + Tasks.Count * RowHeight;

    /// <summary>The y of the pinned legend strip (clamped to the content bottom).</summary>
    private double LegendY
    {
        get
        {
            var viewport = _viewportHeight > 0 ? _viewportHeight : RenderSize.Height;
            return Math.Min(VerticalSyncOffset + viewport - LegendHeight, Height - LegendHeight);
        }
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

        // Two-level header (Oracle/Fiori): the MAJOR row (months/years) above the MINOR row
        // (the zoom unit cells), a separator between them, minor ticks down the whole chart.
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var majorRowBottom = HeaderHeight - 14;
        drawingContext.DrawRectangle(surface, new Pen(border, 1), new Rect(0, 0, RenderSize.Width, HeaderHeight));

        var typeface = new Typeface(SystemFonts.MessageFontFamily, FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
        foreach (var cell in _majorCells)
        {
            var x = XOf(cell.Start);
            var w = (cell.End - cell.Start).TotalDays * DayWidth;
            if (x + w < 0 || x > RenderSize.Width)
            {
                continue;
            }

            drawingContext.DrawText(new FormattedText(
                cell.Caption, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, textBrush, dpi)
            {
                MaxTextWidth = Math.Max(w - 6, 1),
            }, new Point(x + 4, 1));
            drawingContext.DrawLine(new Pen(border, 1), new Point(x, 0), new Point(x, majorRowBottom));
        }

        drawingContext.DrawLine(new Pen(border, 1), new Point(0, majorRowBottom), new Point(RenderSize.Width, majorRowBottom));

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
                10,
                textBrush,
                dpi), new Point(x + 3, majorRowBottom + 2));
            drawingContext.DrawLine(new Pen(border, 1), new Point(x, majorRowBottom), new Point(x, RenderSize.Height));
        }

        drawingContext.DrawLine(new Pen(border, 1), new Point(0, HeaderHeight), new Point(RenderSize.Width, HeaderHeight));

        // Today line.
        var todayX = XOf(DateTime.Today);
        if (todayX >= 0 && todayX <= RenderSize.Width)
        {
            drawingContext.DrawLine(new Pen(Brushes.OrangeRed, 1) { DashStyle = DashStyles.Dash }, new Point(todayX, 0), new Point(todayX, RenderSize.Height));
        }

        // Row separators (only the visible band — culling).
        var (firstRow, lastRow) = VisibleRowRange;
        FirstRenderedRow = firstRow;
        LastRenderedRow = lastRow;
        for (var i = firstRow; i <= lastRow + 1; i++)
        {
            var y = HeaderHeight + i * RowHeight;
            drawingContext.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(128, 160, 160, 160)), 0.5), new Point(0, y), new Point(RenderSize.Width, y));
        }

        // Non-working day tint (weekends/holidays under the grid, beneath the bars).
        foreach (var day in _nonWorkingDays)
        {
            var x = XOf(day);
            if (x + DayWidth >= 0 && x <= RenderSize.Width)
            {
                drawingContext.DrawRectangle(NonWorkingTint, null, new Rect(x, HeaderHeight, DayWidth, RenderSize.Height - HeaderHeight));
            }
        }

        // Baseline bars (the thin planned band under the actual bar; visible band only).
        if (_showBaseline)
        {
            for (var i = firstRow; i <= lastRow; i++)
            {
                if (BaselineBarOf(Tasks[i]) is not { } baseline)
                {
                    continue;
                }

                drawingContext.DrawRectangle(BaselineFill, new Pen(Brushes.DimGray, 0.6), baseline);
            }
        }

        // Bars / milestones (visible band only — culling).
        for (var i = firstRow; i <= lastRow; i++)
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
            var fill = task.BarBrush ?? Brushes.LightGray;

            if (_parentIds.Contains(task.Id))
            {
                // Oracle SUMMARY bar: a thin band with end caps carrying the rollup
                // completion of all descendants (not the parent's own number).
                var centerY = rect.Y + rect.Height / 2;
                var band = new Rect(rect.X, centerY - 3, rect.Width, 6);
                var capHeight = 12;
                drawingContext.DrawRectangle(fill, new Pen(critical ? accent : Brushes.Gray, critical ? 1.6 : 0.8), band);
                drawingContext.DrawRectangle(fill, null, new Rect(rect.X, centerY - capHeight / 2, 2.5, capHeight));
                drawingContext.DrawRectangle(fill, null, new Rect(rect.Right - 2.5, centerY - capHeight / 2, 2.5, capHeight));

                var summaryCompletion = GanttEngine.SummaryCompletion(task, Tasks);
                var summaryWidth = rect.Width * Math.Clamp(summaryCompletion, 0, 100) / 100.0;
                if (summaryWidth > 0)
                {
                    drawingContext.DrawRectangle(accent, null, new Rect(rect.X, band.Y, Math.Min(summaryWidth, rect.Width), band.Height));
                }

                if (i == _hoverIndex)
                {
                    drawingContext.DrawRectangle(null, new Pen(accent, 1.5), rect);
                }

                continue;
            }

            drawingContext.DrawRectangle(fill, new Pen(critical ? accent : Brushes.Gray, critical ? 1.6 : 0.8), rect);

            var completionWidth = rect.Width * Math.Clamp(task.Completion, 0, 100) / 100.0;
            if (completionWidth > 0)
            {
                drawingContext.DrawRectangle(accent, null, new Rect(rect.X, rect.Y, Math.Min(completionWidth, rect.Width), rect.Height));
            }

            if (i == _hoverIndex)
            {
                drawingContext.DrawRectangle(null, new Pen(accent, 1.5), rect);
            }

            // Oracle taskbar label: after the bar, clipped to the chart width, toggleable.
            if (ShowBarLabels)
            {
                var label = new FormattedText(
                    task.Name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, textBrush, dpi)
                {
                    MaxTextWidth = Math.Max(RenderSize.Width - rect.Right - 8, 1),
                };
                drawingContext.DrawText(label, new Point(rect.Right + 4, rect.Y + (rect.Height - label.Height) / 2));
            }
        }

        // Rubber band while the user draws a link.
        if (_dragMode is DragMode.LinkFromFinish or DragMode.LinkFromStart && _dragTask is { } linkSource)
        {
            var sourceIndex = IndexOfReference(linkSource);
            if (sourceIndex >= 0)
            {
                var rect = BarRect(linkSource, sourceIndex);
                var from = new Point(rect.Right, rect.Y + rect.Height / 2);
                drawingContext.DrawLine(new Pen(accent, 1) { DashStyle = DashStyles.Dash }, from, _linkCurrent);
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

        // Legend strip pinned to the viewport bottom (Oracle information/legend panel,
        // compressed to one row): sample glyph + localized caption per chart shape.
        if (_showLegend)
        {
            var legendY = LegendY;
            drawingContext.DrawRectangle(surface, new Pen(border, 1), new Rect(0, legendY, RenderSize.Width, LegendHeight));
            var captionTypeface = new Typeface(SystemFonts.MessageFontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            var x = 8d;
            foreach (var item in ComputeLegendItems())
            {
                var glyphWidth = item.Key switch
                {
                    "milestone" => 10,
                    "baseline" => 14,
                    "nonworking" => 10,
                    _ => 16,
                };
                DrawLegendGlyph(drawingContext, item.Key, x, legendY + LegendHeight / 2, glyphWidth, accent);
                var caption = new FormattedText(
                    item.Caption, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, captionTypeface, 10, textBrush, dpi)
                {
                    MaxTextWidth = Math.Max(RenderSize.Width - x - glyphWidth - 8, 1),
                };
                drawingContext.DrawText(caption, new Point(x + glyphWidth + 4, legendY + (LegendHeight - caption.Height) / 2));
                x += glyphWidth + 8 + caption.Width + 16;
                if (x > RenderSize.Width - 60)
                {
                    break; // clipped gracefully
                }
            }
        }
    }

    /// <summary>The legend entries the current chart state shows (render + test hook).</summary>
    internal IReadOnlyList<(string Key, string Caption)> ComputeLegendItems()
    {
        var items = new List<(string, string)>
        {
            ("task", Localization.GridStrings.GanttLegendTask),
        };

        if (Tasks.Any(t => _parentIds.Contains(t.Id)))
        {
            items.Add(("summary", Localization.GridStrings.GanttLegendSummary));
        }

        if (Tasks.Any(t => t.IsMilestone))
        {
            items.Add(("milestone", Localization.GridStrings.GanttLegendMilestone));
        }

        if (CriticalTaskIds.Count > 0)
        {
            items.Add(("critical", Localization.GridStrings.GanttLegendCritical));
        }

        if (_showBaseline && Tasks.Any(t => t.BaselineStart is not null))
        {
            items.Add(("baseline", Localization.GridStrings.GanttLegendBaseline));
        }

        if (_calendar is not null)
        {
            items.Add(("nonworking", Localization.GridStrings.GanttLegendNonWorking));
        }

        return items;
    }

    private static void DrawLegendGlyph(DrawingContext drawingContext, string key, double x, double centerY, double width, Brush accent)
    {
        switch (key)
        {
            case "milestone":
                drawingContext.DrawGeometry(Brushes.DimGray, new Pen(Brushes.Black, 0.8), CreateDiamond(new Point(x + width / 2, centerY), 5));
                break;
            case "critical":
                drawingContext.DrawRectangle(null, new Pen(accent, 1.6), new Rect(x, centerY - 4, width, 8));
                break;
            case "baseline":
                drawingContext.DrawRectangle(BaselineFill, new Pen(Brushes.DimGray, 0.6), new Rect(x, centerY - 2, width, 4));
                break;
            case "nonworking":
                drawingContext.DrawRectangle(NonWorkingTint, new Pen(Brushes.DimGray, 0.6), new Rect(x, centerY - 4, width, 8));
                break;
            case "summary":
                drawingContext.DrawRectangle(Brushes.LightGray, null, new Rect(x, centerY - 3, width, 6));
                drawingContext.DrawRectangle(Brushes.LightGray, null, new Rect(x, centerY - 6, 2, 12));
                drawingContext.DrawRectangle(Brushes.LightGray, null, new Rect(x + width - 2, centerY - 6, 2, 12));
                break;
            default:
                drawingContext.DrawRectangle(Brushes.LightGray, new Pen(Brushes.Gray, 0.8), new Rect(x, centerY - 4, width, 8));
                drawingContext.DrawRectangle(accent, null, new Rect(x, centerY - 4, width * 0.55, 8));
                break;
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
        var point = e.GetPosition(this);
        var (index, mode) = TaskZoneAt(point);
        DetectDoubleClick(index);
        if (index >= 0 && mode != DragMode.None)
        {
            _dragTask = Tasks[index];
            _dragMode = mode;
            _dragOrigin = point;
            _dragStartOrigin = _dragTask.Start;
            _dragFinishOrigin = _dragTask.Finish;
            _linkCurrent = _dragOrigin;
            CaptureMouse();
        }
        else if (point.Y >= HeaderHeight && point.Y < RowsBottom)
        {
            // Fiori/Oracle pan: dragging empty chart space scrolls the timeline.
            _panning = true;
            _lastPanX = point.X;
            CaptureMouse();
        }

        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        RequestZoom(e.Delta > 0 ? -1 : 1); // wheel up = zoom in (towards Day)
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var point = e.GetPosition(this);

        if (_panning && IsMouseCaptured)
        {
            // Incremental deltas: dragging left moves the content left (scroll right).
            PanBy(_lastPanX - point.X);
            _lastPanX = point.X;
            return;
        }

        var hover = TaskAt(point);
        if (hover != _hoverIndex && _dragMode == DragMode.None)
        {
            _hoverIndex = hover;
            InvalidateVisual();
        }

        if (_dragMode == DragMode.None)
        {
            Cursor = CursorForZone(point); // Fiori: resize/move cursor feedback over zones
        }

        if (hover >= 0 && _dragMode == DragMode.None)
        {
            var hovered = Tasks[hover];
            ToolTip = $"{hovered.Name}: {hovered.Start:d} \u2013 {hovered.Finish:d} ({hovered.Completion}%)";
            if (hovered.BaselineStart is { } baselineStart && hovered.BaselineFinish is { } baselineFinish)
            {
                var variance = (hovered.Start.Date - baselineStart.Date).Days;
                ToolTip += $" | base {baselineStart:d} \u2013 {baselineFinish:d} ({variance:+#;-#;0}d)";
            }
        }

        if (_dragMode != DragMode.None && _dragTask is { } task && IsMouseCaptured)
        {
            var delta = DayDeltaSinceOrigin();
            switch (_dragMode)
            {
                case DragMode.Move:
                    var applied = (int)((task.Start - _dragStartOrigin).TotalDays);
                    MoveTask(task, delta - applied);
                    break;
                case DragMode.ResizeStart:
                    var appliedStart = (int)((task.Start - _dragStartOrigin).TotalDays);
                    ResizeTaskStart(task, delta - appliedStart);
                    break;
                case DragMode.ResizeFinish:
                    var appliedFinish = (int)((task.Finish - _dragFinishOrigin).TotalDays);
                    ResizeTaskFinish(task, delta - appliedFinish);
                    break;
                default:
                    _linkCurrent = e.GetPosition(this); // rubber band while drawing a link
                    InvalidateVisual();
                    break;
            }
        }
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_dragMode is DragMode.LinkFromFinish or DragMode.LinkFromStart && _dragTask is { } source)
        {
            var (targetIndex, _) = TaskZoneAt(e.GetPosition(this));
            if (targetIndex >= 0 && !ReferenceEquals(Tasks[targetIndex], source))
            {
                CreateLink(source, Tasks[targetIndex], GanttLinkType.FinishToStart);
            }
        }

        _dragMode = DragMode.None;
        _dragTask = null;
        _panning = false;
        ReleaseMouseCapture();
        InvalidateVisual();
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
        => TaskZoneAt(point).Index;

    private (int Index, DragMode Mode) TaskZoneAt(Point point)
    {
        if (point.Y < HeaderHeight)
        {
            return (-1, DragMode.None);
        }

        var index = (int)((point.Y - HeaderHeight) / RowHeight);
        if (index < 0 || index >= Tasks.Count)
        {
            return (-1, DragMode.None);
        }

        var rect = BarRect(Tasks[index], index);
        if (point.X < rect.Left - 3 || point.X > rect.Right + 3)
        {
            return (-1, DragMode.None);
        }

        // Edges resize; Alt+edge draws a link; the middle moves.
        if (point.X <= rect.Left + 4)
        {
            return (index, DragMode.ResizeStart);
        }

        if (point.X >= rect.Right - 4)
        {
            return Keyboard.Modifiers == ModifierKeys.Alt
                ? (index, DragMode.LinkFromFinish)
                : (index, DragMode.ResizeFinish);
        }

        return (index, DragMode.Move);
    }

    private int DayDeltaSinceOrigin()
        => (int)Math.Round((Mouse.GetPosition(this).X - _dragOrigin.X) / Math.Max(DayWidth, 1));

    private int IndexOfReference(GanttTask task)
    {
        for (var i = 0; i < Tasks.Count; i++)
        {
            if (ReferenceEquals(Tasks[i], task))
            {
                return i;
            }
        }

        return -1;
    }
}
