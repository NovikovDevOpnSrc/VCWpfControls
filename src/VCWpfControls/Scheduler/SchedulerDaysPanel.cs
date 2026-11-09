using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using VCWpfControls.Grid;

namespace VCWpfControls.Scheduler;

/// <summary>
/// The day/work-week/week body of a <see cref="SchedulerView"/>: a pinned time ruler on the
/// left, pinned day headers and an all-day band on top, and one column per day where the
/// engine's lane slots (<see cref="SchedulerEngine.LayoutOverlaps"/>) render as side-by-side
/// blocks (height = time span, lane = horizontal share). The view lays lanes out per covered
/// day, so an occurrence crossing midnight draws one clipped segment per day; segments and
/// blocks outside the visible hour window clip away.
/// </summary>
public sealed class SchedulerDaysPanel : FrameworkElement
{
    private IReadOnlyList<DateTime> _days = Array.Empty<DateTime>();
    private IReadOnlyList<(int DayIndex, SchedulerLaneSlot Slot)> _timedByDay = Array.Empty<(int, SchedulerLaneSlot)>();
    private IReadOnlyList<SchedulerLaneSlot> _allDaySlots = Array.Empty<SchedulerLaneSlot>();
    private int _dayStartHour;
    private int _dayEndHour = 24;
    private double _hourHeight = 40d;
    private double _dayWidth = 150d;
    private double _timeRulerWidth = 52d;
    private double _dayHeaderHeight = 30d;
    private double _allDayRowHeight = 20d;
    private double _horizontalOffset;
    private double _verticalOffset;
    private SchedulerOccurrence? _selected;
    private SchedulerOccurrence? _hover;
    private SchedulerOccurrence? _lastClick;
    private long _lastClickTime;
    private static readonly Brush BlockFill = Frozen(Color.FromArgb(70, 30, 120, 255));
    private static readonly Brush ChipFill = Frozen(Color.FromArgb(90, 30, 120, 255));
    private static readonly Brush TodayTint = Frozen(Color.FromArgb(14, 30, 120, 255));
    private static readonly Brush HalfHourTick = Frozen(Color.FromArgb(90, 160, 160, 160));
    private static readonly Brush PreviewFill = Frozen(Color.FromArgb(40, 30, 120, 255));

    private static Brush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    static SchedulerDaysPanel()
    {
        ClipToBoundsProperty.OverrideMetadata(typeof(SchedulerDaysPanel), new PropertyMetadata(true));
        FocusableProperty.OverrideMetadata(typeof(SchedulerDaysPanel), new FrameworkPropertyMetadata(false));
    }

    /// <summary>The first hour of the visible window (blocks before it clip away).</summary>
    internal int DayStartHour
    {
        get => _dayStartHour;
        set
        {
            _dayStartHour = value;
            UpdateExtent();
        }
    }

    /// <summary>The exclusive last hour of the visible window.</summary>
    internal int DayEndHour
    {
        get => _dayEndHour;
        set
        {
            _dayEndHour = value;
            UpdateExtent();
        }
    }

    /// <summary>The pixel height of one hour.</summary>
    internal double HourHeight
    {
        get => _hourHeight;
        set
        {
            _hourHeight = value;
            UpdateExtent();
        }
    }

    /// <summary>The pixel width of one day column.</summary>
    internal double DayWidth
    {
        get => _dayWidth;
        set
        {
            _dayWidth = value;
            UpdateExtent();
        }
    }

    /// <summary>The width of the pinned time-ruler column.</summary>
    internal double TimeRulerWidth
    {
        get => _timeRulerWidth;
        set
        {
            _timeRulerWidth = value;
            UpdateExtent();
        }
    }

    /// <summary>The height of the pinned day-header row.</summary>
    internal double DayHeaderHeight
    {
        get => _dayHeaderHeight;
        set
        {
            _dayHeaderHeight = value;
            UpdateExtent();
        }
    }

    /// <summary>The height of one all-day band row.</summary>
    internal double AllDayRowHeight
    {
        get => _allDayRowHeight;
        set
        {
            _allDayRowHeight = value;
            UpdateExtent();
        }
    }

    /// <summary>The horizontal scroll offset (the pinned ruler follows it).</summary>
    internal double HorizontalOffset
    {
        get => _horizontalOffset;
        set
        {
            _horizontalOffset = value;
            InvalidateVisual();
        }
    }

    /// <summary>The vertical scroll offset (the pinned header band follows it).</summary>
    internal double VerticalOffset
    {
        get => _verticalOffset;
        set
        {
            _verticalOffset = value;
            InvalidateVisual();
        }
    }

    /// <summary>The day columns (their dates at midnight).</summary>
    internal IReadOnlyList<DateTime> Days
    {
        get => _days;
        set
        {
            _days = value ?? Array.Empty<DateTime>();
            UpdateExtent();
        }
    }

    /// <summary>The timed lane slots, one entry per covered day (the view groups and lays out per day).</summary>
    internal IReadOnlyList<(int DayIndex, SchedulerLaneSlot Slot)> TimedSlotsByDay
    {
        get => _timedByDay;
        set
        {
            _timedByDay = value ?? Array.Empty<(int, SchedulerLaneSlot)>();
            UpdateExtent();
        }
    }

    /// <summary>The lane layout of all-day occurrences (the band rows).</summary>
    internal IReadOnlyList<SchedulerLaneSlot> AllDaySlots
    {
        get => _allDaySlots;
        set
        {
            _allDaySlots = value ?? Array.Empty<SchedulerLaneSlot>();
            UpdateExtent();
        }
    }

    /// <summary>The occurrence outlined as selected (null = none).</summary>
    internal SchedulerOccurrence? SelectedOccurrence
    {
        get => _selected;
        set
        {
            _selected = value;
            InvalidateVisual();
        }
    }

    /// <summary>How many all-day band rows render (extra lanes clip away, max 3).</summary>
    internal int AllDayBandRows
    {
        get
        {
            var lanes = 0;
            foreach (var slot in _allDaySlots)
            {
                lanes = Math.Max(lanes, slot.LaneCount);
            }

            return Math.Min(lanes, 3);
        }
    }

    /// <summary>The y coordinate where the hour grid starts (below the pinned band).</summary>
    internal double ContentTop => _dayHeaderHeight + AllDayBandRows * _allDayRowHeight;

    /// <summary>How many hour rows the visible window spans.</summary>
    internal int VisibleHourCount => Math.Max(1, _dayEndHour - _dayStartHour);

    /// <summary>Where the day column of <paramref name="index"/> starts (x).</summary>
    internal double ColumnX(int index) => _timeRulerWidth + index * _dayWidth;

    /// <summary>The y of a time point inside the visible window (may fall outside the panel).</summary>
    internal double YOf(DateTime time) => YInDay(time, time.Date);

    /// <summary>The y of a time point measured within its own day (24:00 = the day's grid bottom).</summary>
    private double YInDay(DateTime time, DateTime day)
        => ContentTop + (time - day.AddHours(_dayStartHour)).TotalHours * _hourHeight;

    /// <summary>
    /// The layout rectangle of a timed occurrence's segment on one day, clipped to the day
    /// column and the visible hour window; null when nothing remains visible.
    /// </summary>
    internal Rect? BlockOf(SchedulerOccurrence occurrence, DateTime day)
    {
        day = day.Date;
        for (var i = 0; i < _timedByDay.Count; i++)
        {
            var entry = _timedByDay[i];
            if (entry.DayIndex >= _days.Count || !ReferenceEquals(entry.Slot.Occurrence, occurrence) || _days[entry.DayIndex] != day)
            {
                continue;
            }

            var segmentStart = occurrence.Start < day ? day : occurrence.Start;
            var segmentEnd = occurrence.End > day.AddDays(1) ? day.AddDays(1) : occurrence.End;
            var top = Math.Max(YInDay(segmentStart, day), ContentTop);
            var bottom = Math.Min(YInDay(segmentEnd, day), ContentTop + VisibleHourCount * _hourHeight);
            if (bottom <= top)
            {
                return null;
            }

            var laneWidth = _dayWidth / entry.Slot.LaneCount;
            return new Rect(ColumnX(entry.DayIndex) + entry.Slot.Lane * laneWidth + 1, top, laneWidth - 2, bottom - top);
        }

        return null;
    }

    /// <summary>
    /// The on-screen rectangle of an all-day slot's chip (spans its days, inside the pinned
    /// band — its y includes the current vertical offset); null when the days are outside the range.
    /// </summary>
    internal Rect? AllDayChipOf(SchedulerOccurrence occurrence)
    {
        var startIndex = IndexOfDay(occurrence.Start.Date);
        if (startIndex < 0)
        {
            return null;
        }

        var lastCovered = occurrence.End == occurrence.End.Date ? occurrence.End.AddDays(-1).Date : occurrence.End.Date;
        var endIndex = IndexOfDay(lastCovered);
        if (endIndex < 0)
        {
            endIndex = startIndex;
        }

        foreach (var slot in _allDaySlots)
        {
            if (ReferenceEquals(slot.Occurrence, occurrence))
            {
                return new Rect(
                    ColumnX(startIndex) + 2,
                    _verticalOffset + _dayHeaderHeight + slot.Lane * _allDayRowHeight + 2,
                    (endIndex - startIndex + 1) * _dayWidth - 4,
                    _allDayRowHeight - 4);
            }
        }

        return null;
    }

    /// <summary>The occurrence whose block or chip contains the point (layout coordinates), if any.</summary>
    internal SchedulerOccurrence? HitTest(Point point)
    {
        // The pinned all-day band overlays the hour content — it wins the hit test first.
        if (point.Y >= _verticalOffset + _dayHeaderHeight
            && point.Y < _verticalOffset + _dayHeaderHeight + AllDayBandRows * _allDayRowHeight)
        {
            foreach (var slot in _allDaySlots)
            {
                if (slot.Lane < AllDayBandRows && AllDayChipOf(slot.Occurrence) is { } chip && chip.Contains(point))
                {
                    return slot.Occurrence;
                }
            }

            return null;
        }

        if (point.Y < ContentTop || point.X < _timeRulerWidth)
        {
            return null;
        }

        for (var i = 0; i < _timedByDay.Count; i++)
        {
            var entry = _timedByDay[i];
            if (entry.DayIndex < _days.Count
                && BlockOf(entry.Slot.Occurrence, _days[entry.DayIndex]) is { } block
                && block.Contains(point))
            {
                return entry.Slot.Occurrence;
            }
        }

        return null;
    }

    /// <summary>Simulates a click: selects the occurrence, detects a double-click (test/interaction hook).</summary>
    internal void ClickOccurrence(SchedulerOccurrence occurrence)
    {
        var now = Environment.TickCount64;
        if (ReferenceEquals(_lastClick, occurrence) && now - _lastClickTime < 500)
        {
            OccurrenceDoubleClicked?.Invoke(this, occurrence);
            _lastClick = null;
        }
        else
        {
            _lastClick = occurrence;
        }

        _lastClickTime = now;
        _selected = occurrence;
        OccurrenceSelected?.Invoke(this, occurrence);
        InvalidateVisual();
    }

    /// <summary>Raised when the user double-clicks an occurrence (editor integration).</summary>
    public event EventHandler<SchedulerOccurrence>? OccurrenceDoubleClicked;

    /// <summary>Raised after a click selected an occurrence (the view mirrors it into its DP).</summary>
    internal event EventHandler<SchedulerOccurrence>? OccurrenceSelected;

    /// <summary>Raised after a drag/resize committed (dates already on the appointment).</summary>
    public event EventHandler<SchedulerAppointment>? AppointmentEdited;

    /// <summary>Raised when the user dragged out an empty range to create an appointment.</summary>
    public event EventHandler<(DateTime Start, DateTime End)>? RangeSelected;

    /// <summary>What the pointer is dragging.</summary>
    public enum DragMode
    {
        /// <summary>Not dragging.</summary>
        None = 0,

        /// <summary>Moving the whole occurrence.</summary>
        Move = 1,

        /// <summary>Resizing the start (top) edge.</summary>
        ResizeStart = 2,

        /// <summary>Resizing the end (bottom) edge.</summary>
        ResizeEnd = 3,

        /// <summary>Dragging out a new-appointment range on empty space.</summary>
        Create = 4,
    }

    private DragMode _dragMode = DragMode.None;
    private SchedulerOccurrence? _dragOccurrence;
    private Point _dragOrigin;
    private DateTime _createAnchor;
    private int _snapMinutes = 15;

    /// <summary>The drag/resize time quantum in minutes (1 = minute-precision).</summary>
    internal int SnapMinutes
    {
        get => _snapMinutes;
        set
        {
            _snapMinutes = Math.Max(1, value);
            InvalidateVisual();
        }
    }

    /// <summary>
    /// Test seam for synthesized mouse events (which carry no position of their own):
    /// when set, pointer queries resolve to this instead of the real device position.
    /// </summary>
    internal Func<Point>? NextPositionForTest { get; set; }

    private Point PositionOf(MouseEventArgs e) => NextPositionForTest is { } next ? next() : e.GetPosition(this);

    /// <summary>
    /// Applies a whole move to a non-recurring occurrence's master (drag-move path; test
    /// hook): the time delta is snapped to <see cref="SnapMinutes"/>; all-day occurrences
    /// shift by days only. Recurring series are not editable (no-op).
    /// </summary>
    internal void MoveOccurrence(SchedulerOccurrence occurrence, int deltaDays, TimeSpan deltaTime)
    {
        var appointment = occurrence.Appointment;
        if (appointment.Recurrence is not null)
        {
            return;
        }

        if (appointment.IsAllDay)
        {
            appointment.Start = appointment.Start.AddDays(deltaDays);
            appointment.End = appointment.End.AddDays(deltaDays);
        }
        else
        {
            var shift = TimeSpan.FromDays(deltaDays) + Snap(deltaTime);
            appointment.Start += shift;
            appointment.End += shift;
        }

        AppointmentEdited?.Invoke(this, appointment);
        InvalidateVisual();
    }

    /// <summary>
    /// Moves the start edge of a non-recurring occurrence's master (drag-resize path; test
    /// hook): snapped, clamped to keep at least one snap of duration. Recurring/all-day = no-op.
    /// </summary>
    internal void ResizeOccurrenceStart(SchedulerOccurrence occurrence, TimeSpan deltaTime)
    {
        var appointment = occurrence.Appointment;
        if (appointment.Recurrence is not null || appointment.IsAllDay)
        {
            return;
        }

        var newStart = appointment.Start + Snap(deltaTime);
        if (newStart > appointment.End.AddMinutes(-_snapMinutes))
        {
            newStart = appointment.End.AddMinutes(-_snapMinutes);
        }

        appointment.Start = newStart;
        AppointmentEdited?.Invoke(this, appointment);
        InvalidateVisual();
    }

    /// <summary>
    /// Moves the end edge of a non-recurring occurrence's master (drag-resize path; test
    /// hook): snapped, clamped to keep at least one snap of duration. Recurring/all-day = no-op.
    /// </summary>
    internal void ResizeOccurrenceEnd(SchedulerOccurrence occurrence, TimeSpan deltaTime)
    {
        var appointment = occurrence.Appointment;
        if (appointment.Recurrence is not null || appointment.IsAllDay)
        {
            return;
        }

        var newEnd = appointment.End + Snap(deltaTime);
        if (newEnd < appointment.Start.AddMinutes(_snapMinutes))
        {
            newEnd = appointment.Start.AddMinutes(_snapMinutes);
        }

        appointment.End = newEnd;
        AppointmentEdited?.Invoke(this, appointment);
        InvalidateVisual();
    }

    /// <summary>
    /// Requests a new appointment for a dragged-out range (empty-space drag path; test
    /// hook): the range is ordered and stretched to at least one snap. The VIEW creates the
    /// appointment and adds it to its collection.
    /// </summary>
    internal void CreateFromRange(DateTime start, DateTime end)
    {
        if (end < start)
        {
            (start, end) = (end, start);
        }

        if (end == start)
        {
            end = start.AddMinutes(_snapMinutes);
        }

        RangeSelected?.Invoke(this, (start, end));
    }

    /// <summary>Snaps a time delta to whole <see cref="SnapMinutes"/> increments.</summary>
    internal TimeSpan Snap(TimeSpan value)
        => TimeSpan.FromMinutes(Math.Round(value.TotalMinutes / _snapMinutes, MidpointRounding.AwayFromZero) * _snapMinutes);

    private int IndexOfDay(DateTime date)
    {        for (var i = 0; i < _days.Count; i++)
        {
            if (_days[i] == date.Date)
            {
                return i;
            }
        }

        return -1;
    }

    private void UpdateExtent()
    {
        Width = Math.Max(_timeRulerWidth + _days.Count * _dayWidth, 100);
        Height = ContentTop + VisibleHourCount * _hourHeight;
        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        => new PointHitTestResult(this, hitTestParameters.HitPoint);

    /// <summary>The occurrence under the point and what dragging it would do (band chips move; block edges resize; empty space creates).</summary>
    private (SchedulerOccurrence? Occurrence, DragMode Mode) ZoneAt(Point point)
    {
        // The pinned all-day band: chips are movable by whole days.
        if (point.Y >= _verticalOffset + _dayHeaderHeight
            && point.Y < _verticalOffset + _dayHeaderHeight + AllDayBandRows * _allDayRowHeight)
        {
            foreach (var slot in _allDaySlots)
            {
                if (slot.Lane < AllDayBandRows && AllDayChipOf(slot.Occurrence) is { } chip && chip.Contains(point))
                {
                    return (slot.Occurrence, DragMode.Move);
                }
            }

            return (null, DragMode.None);
        }

        if (point.Y < ContentTop || point.X < _timeRulerWidth)
        {
            return (null, DragMode.None);
        }

        for (var i = 0; i < _timedByDay.Count; i++)
        {
            var entry = _timedByDay[i];
            if (entry.DayIndex >= _days.Count
                || BlockOf(entry.Slot.Occurrence, _days[entry.DayIndex]) is not { } block
                || !block.Contains(point))
            {
                continue;
            }

            if (point.Y <= block.Top + 4)
            {
                return (entry.Slot.Occurrence, DragMode.ResizeStart);
            }

            if (point.Y >= block.Bottom - 4)
            {
                return (entry.Slot.Occurrence, DragMode.ResizeEnd);
            }

            return (entry.Slot.Occurrence, DragMode.Move);
        }

        return (null, DragMode.Create);
    }

    /// <summary>The snapped date+time at a point, clamped to the day columns and the hour window.</summary>
    private DateTime TimeAt(Point point)
    {
        if (_days.Count == 0)
        {
            return DateTime.Today;
        }

        var dayIndex = (int)Math.Clamp((point.X - _timeRulerWidth) / _dayWidth, 0, _days.Count - 1);
        var hours = Math.Clamp((point.Y - ContentTop) / _hourHeight + _dayStartHour, _dayStartHour, _dayEndHour);
        return SnapToGrid(_days[dayIndex].AddHours(hours));
    }

    private DateTime SnapToGrid(DateTime time)
        => time.Date + Snap(time - time.Date);

    /// <summary>The block/chip rect of the dragged occurrence ghost-shifted to the pointer (the drop preview).</summary>
    private Rect? PreviewRectOf(SchedulerOccurrence occurrence, Point point)
    {
        if (occurrence.Appointment.IsAllDay)
        {
            return AllDayChipOf(occurrence) is { } chip
                ? new Rect(chip.X + DayDeltaOf(point) * _dayWidth, chip.Y, chip.Width, chip.Height)
                : null;
        }

        for (var i = 0; i < _timedByDay.Count; i++)
        {
            var entry = _timedByDay[i];
            if (!ReferenceEquals(entry.Slot.Occurrence, occurrence)
                || entry.DayIndex >= _days.Count
                || BlockOf(occurrence, _days[entry.DayIndex]) is not { } block)
            {
                continue;
            }

            var dy = Snap(TimeSpan.FromHours((point.Y - _dragOrigin.Y) / _hourHeight)).TotalHours * _hourHeight;
            return _dragMode switch
            {
                DragMode.Move => new Rect(block.X + DayDeltaOf(point) * _dayWidth, block.Y + dy, block.Width, block.Height),
                DragMode.ResizeStart => new Rect(block.X, block.Y + dy, block.Width, Math.Max(block.Height - dy, 2)),
                DragMode.ResizeEnd => new Rect(block.X, block.Y, block.Width, Math.Max(block.Height + dy, 2)),
                _ => block,
            };
        }

        return null;
    }

    private int DayDeltaOf(Point point)
        => (int)Math.Round((point.X - _dragOrigin.X) / _dayWidth, MidpointRounding.AwayFromZero);

    /// <inheritdoc />
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        var point = PositionOf(e);
        var (occurrence, mode) = ZoneAt(point);
        if (occurrence is not null)
        {
            ClickOccurrence(occurrence);
            if (occurrence.Appointment.Recurrence is null && mode != DragMode.None)
            {
                _dragOccurrence = occurrence;
                _dragMode = mode;
                _dragOrigin = point;
                CaptureMouse();
            }
        }
        else if (mode == DragMode.Create)
        {
            _dragMode = DragMode.Create;
            _dragOrigin = point;
            _createAnchor = TimeAt(point);
            CaptureMouse();
        }

        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var point = PositionOf(e);
        if (_dragMode == DragMode.None)
        {
            var occurrence = HitTest(point);
            var hoverChanged = !ReferenceEquals(occurrence, _hover);
            _hover = occurrence;

            var (_, mode) = ZoneAt(point);
            Cursor = mode switch
            {
                DragMode.ResizeStart or DragMode.ResizeEnd => Cursors.SizeNS,
                DragMode.Move => Cursors.SizeAll,
                DragMode.Create => Cursors.Cross,
                _ => null,
            };

            if (occurrence is { } hovered)
            {
                ToolTip = $"{hovered.Subject}: {hovered.Start:t} \u2013 {hovered.End:t}";
            }

            if (hoverChanged)
            {
                InvalidateVisual();
            }

            return;
        }

        InvalidateVisual(); // the drag/create preview follows the pointer
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        var point = PositionOf(e);
        switch (_dragMode)
        {
            case DragMode.Move when _dragOccurrence is { } dragged:
            {
                var delta = dragged.Appointment.IsAllDay
                    ? TimeSpan.Zero
                    : TimeSpan.FromHours((point.Y - _dragOrigin.Y) / _hourHeight);
                MoveOccurrence(dragged, DayDeltaOf(point), delta);
                break;
            }

            case DragMode.ResizeStart when _dragOccurrence is { } dragged:
                ResizeOccurrenceStart(dragged, TimeSpan.FromHours((point.Y - _dragOrigin.Y) / _hourHeight));
                break;

            case DragMode.ResizeEnd when _dragOccurrence is { } dragged:
                ResizeOccurrenceEnd(dragged, TimeSpan.FromHours((point.Y - _dragOrigin.Y) / _hourHeight));
                break;

            case DragMode.Create:
                CreateFromRange(_createAnchor, TimeAt(point));
                break;
        }

        _dragMode = DragMode.None;
        _dragOccurrence = null;
        ReleaseMouseCapture();
        InvalidateVisual();
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _hover = null;
        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var culture = CultureInfo.CurrentCulture;
        var accent = TryFindResource(GridResources.AccentKey) as Brush ?? Brushes.DodgerBlue;
        var surface = TryFindResource(GridResources.SurfaceBackgroundKey) as Brush ?? Brushes.White;
        var headerBackground = TryFindResource(GridResources.HeaderBackgroundKey) as Brush ?? Brushes.WhiteSmoke;
        var border = TryFindResource(GridResources.HeaderBorderKey) as Brush ?? Brushes.LightGray;
        var gridLine = TryFindResource(GridResources.GridLineKey) as Brush ?? Brushes.Gainsboro;
        var textBrush = TryFindResource(GridResources.TextBrushKey) as Brush ?? Brushes.Black;
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var captionTypeface = new Typeface(SystemFonts.MessageFontFamily, FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
        var textTypeface = new Typeface(SystemFonts.MessageFontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        drawingContext.DrawRectangle(surface, null, new Rect(0, 0, Width, Height));

        // --- Scrolling content (layout coordinates). ---
        var todayIndex = IndexOfDay(DateTime.Today);
        if (todayIndex >= 0)
        {
            drawingContext.DrawRectangle(TodayTint, null, new Rect(ColumnX(todayIndex), 0, _dayWidth, Height));
        }

        // Hour lines + day column separators.
        var hourPen = new Pen(gridLine, 1);
        for (var h = 0; h <= VisibleHourCount; h++)
        {
            var y = ContentTop + h * _hourHeight;
            drawingContext.DrawLine(hourPen, new Point(0, y), new Point(Width, y));
        }

        for (var i = 0; i <= _days.Count; i++)
        {
            drawingContext.DrawLine(hourPen, new Point(ColumnX(i), 0), new Point(ColumnX(i), Height));
        }

        // Half-hour ticks inside the ruler strip (lighter, short).
        var halfPen = new Pen(HalfHourTick, 0.5);
        for (var h = 0; h < VisibleHourCount; h++)
        {
            var y = ContentTop + h * _hourHeight + _hourHeight / 2;
            drawingContext.DrawLine(halfPen, new Point(0, y), new Point(_timeRulerWidth, y));
        }

        // Timed blocks.
        for (var i = 0; i < _timedByDay.Count; i++)
        {
            var entry = _timedByDay[i];
            if (entry.DayIndex >= _days.Count
                || BlockOf(entry.Slot.Occurrence, _days[entry.DayIndex]) is not { } rect)
            {
                continue;
            }

            if (_dragMode is DragMode.Move or DragMode.ResizeStart or DragMode.ResizeEnd
                && ReferenceEquals(entry.Slot.Occurrence, _dragOccurrence))
            {
                continue; // the drag preview below replaces it while dragging
            }

            DrawBlock(drawingContext, rect, entry.Slot.Occurrence, accent, textBrush, textTypeface, dpi);
        }

        // Drag preview: the occurrence's ghost at the snapped drop position.
        if (_dragOccurrence is { } dragged
            && _dragMode is DragMode.Move or DragMode.ResizeStart or DragMode.ResizeEnd
            && PreviewRectOf(dragged, Mouse.GetPosition(this)) is { } ghost)
        {
            drawingContext.DrawRoundedRectangle(PreviewFill, new Pen(accent, 1.2), ghost, 3, 3);
            DrawBlockText(drawingContext, dragged.Subject, ghost, textBrush, textTypeface, dpi);
        }

        // Create preview: the dragged-out range in the anchor day's column.
        if (_dragMode == DragMode.Create)
        {
            var current = TimeAt(Mouse.GetPosition(this));
            var start = _createAnchor <= current ? _createAnchor : current;
            var end = _createAnchor <= current ? current : _createAnchor;
            var day = _createAnchor.Date;
            var dayIndex = IndexOfDay(day);
            if (dayIndex >= 0)
            {
                var top = Math.Max(YInDay(start, day), ContentTop);
                var bottom = Math.Min(YInDay(end, day), ContentTop + VisibleHourCount * _hourHeight);
                if (bottom > top)
                {
                    drawingContext.DrawRoundedRectangle(PreviewFill, new Pen(accent, 1), new Rect(ColumnX(dayIndex) + 1, top, _dayWidth - 2, bottom - top), 3, 3);
                }
            }
        }

        // Now line (only when today's column is visible and inside the hour window).
        if (todayIndex >= 0 && DateTime.Now.Hour >= _dayStartHour && DateTime.Now.Hour < _dayEndHour)
        {
            var nowY = YOf(DateTime.Now);
            drawingContext.DrawLine(new Pen(Brushes.OrangeRed, 1) { DashStyle = DashStyles.Dash },
                new Point(_timeRulerWidth, nowY), new Point(Width, nowY));
        }

        // --- Pinned time ruler (x follows the horizontal scroll). ---
        var rulerX = _horizontalOffset;
        drawingContext.DrawRectangle(headerBackground, null, new Rect(rulerX, 0, _timeRulerWidth, Height));
        for (var h = 0; h < VisibleHourCount; h++)
        {
            var label = (_dayStartHour + h).ToString("00", CultureInfo.InvariantCulture) + ":00";
            drawingContext.DrawText(new FormattedText(
                label, culture, FlowDirection.LeftToRight, textTypeface, 10, textBrush, dpi),
                new Point(rulerX + 6, ContentTop + h * _hourHeight + 2));
        }

        drawingContext.DrawLine(new Pen(border, 1), new Point(rulerX + _timeRulerWidth, 0), new Point(rulerX + _timeRulerWidth, Height));

        // --- Pinned header band + all-day chips (y follows the vertical scroll). ---
        var bandTop = _verticalOffset;
        var bandHeight = _dayHeaderHeight + AllDayBandRows * _allDayRowHeight;
        drawingContext.DrawRectangle(headerBackground, null, new Rect(0, bandTop, Width, bandHeight));

        for (var i = 0; i < _days.Count; i++)
        {
            var day = _days[i];
            var isToday = i == todayIndex;
            var caption = culture.DateTimeFormat.GetAbbreviatedDayName(day.DayOfWeek) + " " + day.Day.ToString(culture);
            var captionText = new FormattedText(
                caption, culture, FlowDirection.LeftToRight, captionTypeface, 11,
                isToday ? accent : textBrush, dpi)
            {
                MaxTextWidth = _dayWidth - 8,
            };
            drawingContext.DrawText(captionText, new Point(ColumnX(i) + 4, bandTop + (_dayHeaderHeight - captionText.Height) / 2));
            if (isToday)
            {
                drawingContext.DrawRectangle(accent, null, new Rect(ColumnX(i) + 2, bandTop + _dayHeaderHeight - 2, _dayWidth - 4, 2));
            }
        }

        foreach (var slot in _allDaySlots)
        {
            if (slot.Lane >= AllDayBandRows || AllDayChipOf(slot.Occurrence) is not { } chip)
            {
                continue; // rows beyond the band clip away
            }

            var isSelected = ReferenceEquals(slot.Occurrence, _selected);
            drawingContext.DrawRoundedRectangle(ChipFill,
                new Pen(isSelected ? accent : Brushes.Gray, isSelected ? 1.6 : 0.8), chip, 3, 3);
            DrawBlockText(drawingContext, slot.Occurrence.Subject, chip, textBrush, textTypeface, dpi);
        }

        drawingContext.DrawRectangle(null, new Pen(border, 1), new Rect(0, bandTop, Width, bandHeight));
    }

    private void DrawBlock(DrawingContext drawingContext, Rect rect, SchedulerOccurrence occurrence, Brush accent, Brush textBrush, Typeface typeface, double dpi)
    {
        var isSelected = ReferenceEquals(occurrence, _selected);
        var isHover = ReferenceEquals(occurrence, _hover);
        drawingContext.DrawRoundedRectangle(BlockFill,
            new Pen(isSelected || isHover ? accent : Brushes.Gray, isSelected ? 1.6 : 0.8), rect, 3, 3);
        DrawBlockText(drawingContext, occurrence.Subject, rect, textBrush, typeface, dpi);
    }

    private static void DrawBlockText(DrawingContext drawingContext, string text, Rect rect, Brush brush, Typeface typeface, double dpi)
    {
        if (rect.Height < 12 || string.IsNullOrEmpty(text))
        {
            return;
        }

        var formatted = new FormattedText(
            text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, brush, dpi)
        {
            MaxTextWidth = Math.Max(rect.Width - 8, 1),
            MaxTextHeight = Math.Max(rect.Height - 4, 1),
        };
        drawingContext.DrawText(formatted, new Point(rect.X + 4, rect.Y + Math.Max(2, (rect.Height - formatted.Height) / 2)));
    }
}
