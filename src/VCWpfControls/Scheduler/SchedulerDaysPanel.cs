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

    private int IndexOfDay(DateTime date)
    {
        for (var i = 0; i < _days.Count; i++)
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

    /// <inheritdoc />
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (HitTest(e.GetPosition(this)) is { } occurrence)
        {
            ClickOccurrence(occurrence);
        }

        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var occurrence = HitTest(e.GetPosition(this));
        if (!ReferenceEquals(occurrence, _hover))
        {
            _hover = occurrence;
            InvalidateVisual();
        }

        if (occurrence is { } hovered)
        {
            ToolTip = $"{hovered.Subject}: {hovered.Start:t} \u2013 {hovered.End:t}";
        }
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

            DrawBlock(drawingContext, rect, entry.Slot.Occurrence, accent, textBrush, textTypeface, dpi);
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
