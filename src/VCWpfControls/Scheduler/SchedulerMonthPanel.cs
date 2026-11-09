using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using VCWpfControls.Grid;

namespace VCWpfControls.Scheduler;

/// <summary>
/// The month body of a <see cref="SchedulerView"/>: the engine's six-week
/// <see cref="SchedulerEngine.MonthGrid"/> as a 7×6 grid of day cells filling the available
/// size, with weekday headers, today highlighted, and per-day chips for every occurrence
/// (an occurrence spanning days chips on each of them). Shows at most
/// <see cref="ChipsPerCell"/> chips per cell plus a "+N more" overflow row.
/// </summary>
public sealed class SchedulerMonthPanel : FrameworkElement
{
    private IReadOnlyList<DateTime> _days = Array.Empty<DateTime>();
    private IReadOnlyList<SchedulerOccurrence> _occurrences = Array.Empty<SchedulerOccurrence>();
    private Dictionary<DateTime, List<SchedulerOccurrence>> _byDay = new();
    private DateTime _monthAnchor = DateTime.Today;
    private SchedulerOccurrence? _selected;
    private SchedulerOccurrence? _hover;
    private SchedulerOccurrence? _lastClick;
    private long _lastClickTime;

    /// <summary>How many chips fit one cell before the "+N more" row.</summary>
    public const int ChipsPerCell = 2;

    private static readonly Brush ChipFill = Frozen(70, 30, 120, 255);
    private static readonly Brush TodayTint = Frozen(16, 30, 120, 255);
    private static readonly Brush NeighborDayDim = Frozen(90, 255, 255, 255);

    private static Brush Frozen(byte alpha, byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromArgb(alpha, r, g, b));
        brush.Freeze();
        return brush;
    }

    static SchedulerMonthPanel()
    {
        ClipToBoundsProperty.OverrideMetadata(typeof(SchedulerMonthPanel), new PropertyMetadata(true));
        FocusableProperty.OverrideMetadata(typeof(SchedulerMonthPanel), new FrameworkPropertyMetadata(false));
    }

    /// <summary>The 42 grid days (culture week order, starting at the first full week).</summary>
    internal IReadOnlyList<DateTime> Days
    {
        get => _days;
        set
        {
            _days = value ?? Array.Empty<DateTime>();
            InvalidateVisual();
        }
    }

    /// <summary>Every occurrence overlapping the grid range (chips on each covered day).</summary>
    internal IReadOnlyList<SchedulerOccurrence> Occurrences
    {
        get => _occurrences;
        set
        {
            _occurrences = value ?? Array.Empty<SchedulerOccurrence>();
            _byDay = new Dictionary<DateTime, List<SchedulerOccurrence>>();
            foreach (var occurrence in _occurrences)
            {
                for (var day = occurrence.Start.Date; day < occurrence.End; day = day.AddDays(1))
                {
                    if (!_byDay.TryGetValue(day, out var list))
                    {
                        _byDay[day] = list = new List<SchedulerOccurrence>();
                    }

                    list.Add(occurrence);
                }
            }

            InvalidateVisual();
        }
    }

    /// <summary>The month being shown (days of other months render dimmed).</summary>
    internal DateTime MonthAnchor
    {
        get => _monthAnchor;
        set
        {
            _monthAnchor = value;
            InvalidateVisual();
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

    /// <summary>The height of the weekday header row.</summary>
    internal double WeekdayHeaderHeight { get; set; } = 26d;

    /// <summary>The chip row height inside a cell.</summary>
    internal double ChipHeight { get; set; } = 15d;

    /// <summary>The width of one of the 7 columns at the current size.</summary>
    internal double ColumnWidth => RenderSize.Width / 7d;

    /// <summary>The height of one of the 6 day rows at the current size.</summary>
    internal double RowHeight => Math.Max((RenderSize.Height - WeekdayHeaderHeight) / 6d, 1d);

    /// <summary>The cell rectangle of a grid day (null when the day is not in the grid).</summary>
    internal Rect? CellOf(DateTime day)
    {
        var index = IndexOfDay(day.Date);
        if (index < 0)
        {
            return null;
        }

        var column = index % 7;
        var row = index / 7;
        return new Rect(column * ColumnWidth, WeekdayHeaderHeight + row * RowHeight, ColumnWidth, RowHeight);
    }

    /// <summary>The chip rectangle of an occurrence on one covered day (null when the chip is in the overflow row).</summary>
    internal Rect? ChipOf(SchedulerOccurrence occurrence, DateTime day)
    {
        if (CellOf(day) is not { } cell || !_byDay.TryGetValue(day.Date, out var list))
        {
            return null;
        }

        var index = list.IndexOf(occurrence);
        if (index < 0 || index >= ChipsPerCell)
        {
            return null; // in the "+N more" overflow
        }

        return new Rect(cell.X + 2, cell.Y + 16 + index * ChipHeight, cell.Width - 4, ChipHeight - 1);
    }

    /// <summary>The occurrence whose chip contains the point, if any (overflow chips are not clickable).</summary>
    internal SchedulerOccurrence? HitTest(Point point)
    {
        if (point.Y < WeekdayHeaderHeight)
        {
            return null;
        }

        foreach (var pair in _byDay)
        {
            foreach (var occurrence in pair.Value)
            {
                if (ChipOf(occurrence, pair.Key) is { } chip && chip.Contains(point))
                {
                    return occurrence;
                }
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
            if (_days[i] == date)
            {
                return i;
            }
        }

        return -1;
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
            ToolTip = $"{hovered.Subject}: {hovered.Start:d} \u2013 {hovered.End:d}";
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
        if (_days.Count == 0)
        {
            return;
        }

        var culture = CultureInfo.CurrentCulture;
        var accent = TryFindResource(GridResources.AccentKey) as Brush ?? Brushes.DodgerBlue;
        var surface = TryFindResource(GridResources.SurfaceBackgroundKey) as Brush ?? Brushes.White;
        var border = TryFindResource(GridResources.HeaderBorderKey) as Brush ?? Brushes.LightGray;
        var gridLine = TryFindResource(GridResources.GridLineKey) as Brush ?? Brushes.Gainsboro;
        var textBrush = TryFindResource(GridResources.TextBrushKey) as Brush ?? Brushes.Black;
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var captionTypeface = new Typeface(SystemFonts.MessageFontFamily, FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
        var textTypeface = new Typeface(SystemFonts.MessageFontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        drawingContext.DrawRectangle(surface, null, new Rect(0, 0, RenderSize.Width, RenderSize.Height));

        // Weekday header (order matches the grid, which starts at the culture's first day).
        for (var i = 0; i < 7 && i < _days.Count; i++)
        {
            var name = culture.DateTimeFormat.GetAbbreviatedDayName(_days[i].DayOfWeek);
            drawingContext.DrawText(new FormattedText(
                name, culture, FlowDirection.LeftToRight, captionTypeface, 11, textBrush, dpi),
                new Point(i * ColumnWidth + 6, (WeekdayHeaderHeight - 15) / 2));
        }

        drawingContext.DrawLine(new Pen(border, 1), new Point(0, WeekdayHeaderHeight), new Point(RenderSize.Width, WeekdayHeaderHeight));

        // Day cells.
        var cellPen = new Pen(gridLine, 1);
        for (var i = 0; i < _days.Count; i++)
        {
            if (CellOf(_days[i]) is not { } cell)
            {
                continue;
            }

            var day = _days[i];
            var isToday = day == DateTime.Today;
            var inMonth = day.Month == _monthAnchor.Month && day.Year == _monthAnchor.Year;

            if (isToday)
            {
                drawingContext.DrawRectangle(TodayTint, null, cell);
            }

            drawingContext.DrawRectangle(null, cellPen, cell);

            var dayText = new FormattedText(
                day.Day.ToString(culture), culture, FlowDirection.LeftToRight, captionTypeface, 11,
                isToday ? accent : textBrush, dpi)
            {
                MaxTextWidth = Math.Max(ColumnWidth - 8, 1),
            };
            drawingContext.DrawText(dayText, new Point(cell.Right - dayText.Width - 4, cell.Top + 2));
            if (!inMonth)
            {
                // Dim the day number of neighbor-month cells.
                drawingContext.DrawRectangle(NeighborDayDim, null, new Rect(cell.X, cell.Y, cell.Width, 16));
            }

            if (!_byDay.TryGetValue(day, out var list))
            {
                continue;
            }

            for (var c = 0; c < list.Count && c < ChipsPerCell; c++)
            {
                if (ChipOf(list[c], day) is not { } chip)
                {
                    continue;
                }

                var isSelected = ReferenceEquals(list[c], _selected);
                drawingContext.DrawRoundedRectangle(ChipFill,
                    new Pen(isSelected ? accent : Brushes.Gray, isSelected ? 1.6 : 0.6), chip, 2, 2);
                DrawChipText(drawingContext, list[c].Subject, chip, Brushes.Black, textTypeface, dpi);
            }

            if (list.Count > ChipsPerCell)
            {
                var more = "+" + (list.Count - ChipsPerCell).ToString(culture);
                var moreX = ChipOf(list[0], day) is { } first ? first.X + 4 : cell.X + 4;
                drawingContext.DrawText(new FormattedText(
                    more, culture, FlowDirection.LeftToRight, textTypeface, 10, textBrush, dpi),
                    new Point(moreX, cell.Y + 16 + ChipsPerCell * ChipHeight));
            }
        }
    }

    private static void DrawChipText(DrawingContext drawingContext, string text, Rect chip, Brush brush, Typeface typeface, double dpi)
    {
        if (chip.Height < 10 || string.IsNullOrEmpty(text))
        {
            return;
        }

        var formatted = new FormattedText(
            text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 9, brush, dpi)
        {
            MaxTextWidth = Math.Max(chip.Width - 6, 1),
            MaxTextHeight = Math.Max(chip.Height - 2, 1),
        };
        drawingContext.DrawText(formatted, new Point(chip.X + 3, chip.Y + (chip.Height - formatted.Height) / 2));
    }
}
