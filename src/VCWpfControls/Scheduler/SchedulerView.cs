using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace VCWpfControls.Scheduler;

/// <summary>
/// A calendar/scheduler control on the headless <see cref="SchedulerEngine"/>: day,
/// work-week, week and month views of an appointment list (plain or recurring), with a
/// time ruler, side-by-side lanes for overlapping occurrences, an all-day band, a today
/// highlight and a now line. Navigate by changing <see cref="AnchorDate"/> (or
/// <see cref="Navigate"/>/<see cref="GoToToday"/>); double-click an occurrence to open an
/// editor (<see cref="OccurrenceDoubleClicked"/>).
/// </summary>
public class SchedulerView : Control
{
    /// <summary>Identifies the <see cref="Appointments"/> dependency property.</summary>
    public static readonly DependencyProperty AppointmentsProperty = DependencyProperty.Register(
        nameof(Appointments), typeof(ObservableCollection<SchedulerAppointment>), typeof(SchedulerView),
        new PropertyMetadata(null, OnAppointmentsChanged));

    /// <summary>Identifies the <see cref="ViewType"/> dependency property.</summary>
    public static readonly DependencyProperty ViewTypeProperty = DependencyProperty.Register(
        nameof(ViewType), typeof(SchedulerViewType), typeof(SchedulerView),
        new PropertyMetadata(SchedulerViewType.WorkWeek, OnLayoutPropertyChanged));

    /// <summary>Identifies the <see cref="AnchorDate"/> dependency property.</summary>
    public static readonly DependencyProperty AnchorDateProperty = DependencyProperty.Register(
        nameof(AnchorDate), typeof(DateTime), typeof(SchedulerView),
        new PropertyMetadata(default(DateTime), OnLayoutPropertyChanged));

    /// <summary>Identifies the <see cref="DayStartHour"/> dependency property.</summary>
    public static readonly DependencyProperty DayStartHourProperty = DependencyProperty.Register(
        nameof(DayStartHour), typeof(int), typeof(SchedulerView),
        new PropertyMetadata(0, OnLayoutPropertyChanged));

    /// <summary>Identifies the <see cref="DayEndHour"/> dependency property.</summary>
    public static readonly DependencyProperty DayEndHourProperty = DependencyProperty.Register(
        nameof(DayEndHour), typeof(int), typeof(SchedulerView),
        new PropertyMetadata(24, OnLayoutPropertyChanged));

    /// <summary>Identifies the <see cref="DayWidth"/> dependency property.</summary>
    public static readonly DependencyProperty DayWidthProperty = DependencyProperty.Register(
        nameof(DayWidth), typeof(double), typeof(SchedulerView),
        new PropertyMetadata(150d, OnLayoutPropertyChanged));

    /// <summary>Identifies the <see cref="HourHeight"/> dependency property.</summary>
    public static readonly DependencyProperty HourHeightProperty = DependencyProperty.Register(
        nameof(HourHeight), typeof(double), typeof(SchedulerView),
        new PropertyMetadata(40d, OnLayoutPropertyChanged));

    /// <summary>Identifies the <see cref="SelectedOccurrence"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedOccurrenceProperty = DependencyProperty.Register(
        nameof(SelectedOccurrence), typeof(SchedulerOccurrence), typeof(SchedulerView),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedOccurrenceChanged));

    private ScrollViewer? _daysScroll;
    private SchedulerDaysPanel? _daysPanel;
    private SchedulerMonthPanel? _monthPanel;

    static SchedulerView()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SchedulerView), new FrameworkPropertyMetadata(typeof(SchedulerView)));
    }

    /// <summary>Creates the view.</summary>
    public SchedulerView()
    {
        Appointments = new ObservableCollection<SchedulerAppointment>();
    }

    /// <summary>The appointments (plain or recurring; mutations refresh the view).</summary>
    public ObservableCollection<SchedulerAppointment>? Appointments
    {
        get => (ObservableCollection<SchedulerAppointment>?)GetValue(AppointmentsProperty);
        set => SetValue(AppointmentsProperty, value);
    }

    /// <summary>The shown view granularity.</summary>
    public SchedulerViewType ViewType
    {
        get => (SchedulerViewType)GetValue(ViewTypeProperty);
        set => SetValue(ViewTypeProperty, value);
    }

    /// <summary>
    /// The date the view is anchored to (Day = that day; WorkWeek/Week = its week;
    /// Month = its month). The default value anchors to today.
    /// </summary>
    public DateTime AnchorDate
    {
        get => (DateTime)GetValue(AnchorDateProperty);
        set => SetValue(AnchorDateProperty, value);
    }

    /// <summary>The first visible hour of day/work-week/week views (default 0).</summary>
    public int DayStartHour
    {
        get => (int)GetValue(DayStartHourProperty);
        set => SetValue(DayStartHourProperty, value);
    }

    /// <summary>The exclusive last visible hour of day/work-week/week views (default 24).</summary>
    public int DayEndHour
    {
        get => (int)GetValue(DayEndHourProperty);
        set => SetValue(DayEndHourProperty, value);
    }

    /// <summary>The pixel width of one day column (day/work-week/week views).</summary>
    public double DayWidth
    {
        get => (double)GetValue(DayWidthProperty);
        set => SetValue(DayWidthProperty, value);
    }

    /// <summary>The pixel height of one hour (day/work-week/week views).</summary>
    public double HourHeight
    {
        get => (double)GetValue(HourHeightProperty);
        set => SetValue(HourHeightProperty, value);
    }

    /// <summary>The occurrence clicked last (bindable two-way; null = none).</summary>
    public SchedulerOccurrence? SelectedOccurrence
    {
        get => (SchedulerOccurrence?)GetValue(SelectedOccurrenceProperty);
        set => SetValue(SelectedOccurrenceProperty, value);
    }

    /// <summary>Raised when the user double-clicks an occurrence (open an editor here).</summary>
    public event EventHandler<SchedulerOccurrence>? OccurrenceDoubleClicked;

    /// <summary>The effective anchor (an unset anchor falls back to today).</summary>
    internal DateTime EffectiveAnchor => AnchorDate == default(DateTime) ? DateTime.Today : AnchorDate;

    /// <summary>The date range the current view shows (end exclusive).</summary>
    internal (DateTime Start, DateTime End) CurrentRange
        => SchedulerEngine.ViewRange(ViewType, EffectiveAnchor);

    /// <summary>The day columns of day/work-week/week views (empty in the month view).</summary>
    internal IReadOnlyList<DateTime> CurrentDays
        => ViewType == SchedulerViewType.Month || _daysPanel is null ? Array.Empty<DateTime>() : _daysPanel.Days;

    /// <summary>Every occurrence overlapping the current range (the instances the panels hold).</summary>
    internal IReadOnlyList<SchedulerOccurrence> VisibleOccurrences { get; private set; } = Array.Empty<SchedulerOccurrence>();

    /// <summary>The day/body panel (test hook).</summary>
    internal SchedulerDaysPanel? DaysPanel => _daysPanel;

    /// <summary>The month panel (test hook).</summary>
    internal SchedulerMonthPanel? MonthPanel => _monthPanel;

    /// <summary>Moves the anchor by whole view steps (days/weeks/months; negative goes back).</summary>
    public void Navigate(int stepCount)
    {
        AnchorDate = ViewType switch
        {
            SchedulerViewType.Day => EffectiveAnchor.AddDays(stepCount),
            SchedulerViewType.Month => EffectiveAnchor.AddMonths(stepCount),
            _ => EffectiveAnchor.AddDays(7 * stepCount),
        };
    }

    /// <summary>Anchors the view back to today.</summary>
    public void GoToToday() => AnchorDate = DateTime.Today;

    /// <summary>Rebuilds the current view from <see cref="Appointments"/> (called automatically on changes).</summary>
    public void Rebuild()
    {
        if (_daysScroll is not null)
        {
            var isMonth = ViewType == SchedulerViewType.Month;
            _daysScroll.Visibility = isMonth ? Visibility.Collapsed : Visibility.Visible;
            _monthPanel?.SetValue(VisibilityProperty, isMonth ? Visibility.Visible : Visibility.Collapsed);
        }

        if (ViewType == SchedulerViewType.Month)
        {
            RebuildMonth();
            return;
        }

        if (_daysPanel is not null)
        {
            var (start, end) = CurrentRange;
            var days = new List<DateTime>();
            for (var day = start; day < end; day = day.AddDays(1))
            {
                days.Add(day);
            }

            var occurrences = SchedulerEngine.Expand(Appointments ?? new ObservableCollection<SchedulerAppointment>(), start, end);
            var timed = new List<SchedulerOccurrence>();
            var allDay = new List<SchedulerOccurrence>();
            foreach (var occurrence in occurrences)
            {
                if (occurrence.Appointment.IsAllDay)
                {
                    allDay.Add(occurrence);
                }
                else
                {
                    timed.Add(occurrence);
                }
            }

            // Lanes are laid out per day: overnight occurrences join every day they cover.
            var timedByDay = new List<(int DayIndex, SchedulerLaneSlot Slot)>();
            for (var i = 0; i < days.Count; i++)
            {
                var dayStart = days[i];
                var dayEnd = dayStart.AddDays(1);
                var covering = timed.Where(o => o.Start < dayEnd && o.End > dayStart).ToList();
                foreach (var slot in SchedulerEngine.LayoutOverlaps(covering))
                {
                    timedByDay.Add((i, slot));
                }
            }

            _daysPanel.Days = days;
            _daysPanel.TimedSlotsByDay = timedByDay;
            _daysPanel.AllDaySlots = SchedulerEngine.LayoutOverlaps(allDay);
            _daysPanel.DayStartHour = Math.Clamp(DayStartHour, 0, 23);
            _daysPanel.DayEndHour = Math.Clamp(DayEndHour, Math.Clamp(DayStartHour, 0, 23) + 1, 24);
            _daysPanel.DayWidth = Math.Max(DayWidth, 20);
            _daysPanel.HourHeight = Math.Max(HourHeight, 8);
            _daysPanel.SelectedOccurrence = SelectedOccurrence;
            VisibleOccurrences = occurrences;
        }
    }

    private void RebuildMonth()
    {
        if (_monthPanel is null)
        {
            return;
        }

        var days = SchedulerEngine.MonthGrid(EffectiveAnchor);
        var range = MonthRange();
        var occurrences = SchedulerEngine.Expand(Appointments ?? new ObservableCollection<SchedulerAppointment>(), range.Start, range.End);
        _monthPanel.Days = days;
        _monthPanel.Occurrences = occurrences;
        _monthPanel.MonthAnchor = EffectiveAnchor;
        _monthPanel.SelectedOccurrence = SelectedOccurrence;
        VisibleOccurrences = occurrences;
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _daysScroll = GetTemplateChild("PART_DaysScroll") as ScrollViewer;
        _daysPanel = GetTemplateChild("PART_Days") as SchedulerDaysPanel;
        _monthPanel = GetTemplateChild("PART_Month") as SchedulerMonthPanel;

        if (_daysScroll is not null && _daysPanel is not null)
        {
            _daysScroll.ScrollChanged += (_, e) =>
            {
                if (_daysPanel is not null)
                {
                    _daysPanel.HorizontalOffset = e.HorizontalOffset;
                    _daysPanel.VerticalOffset = e.VerticalOffset;
                }
            };
        }

        if (_daysPanel is not null)
        {
            _daysPanel.OccurrenceSelected += OnPanelOccurrenceSelected;
            _daysPanel.OccurrenceDoubleClicked += OnPanelDoubleClicked;
        }

        if (_monthPanel is not null)
        {
            _monthPanel.OccurrenceSelected += OnPanelOccurrenceSelected;
            _monthPanel.OccurrenceDoubleClicked += OnPanelDoubleClicked;
            _monthPanel.SetValue(VisibilityProperty, Visibility.Collapsed);
        }

        Rebuild();
    }

    private void OnPanelOccurrenceSelected(object? sender, SchedulerOccurrence occurrence)
        => SelectedOccurrence = occurrence;

    private void OnPanelDoubleClicked(object? sender, SchedulerOccurrence occurrence)
        => OccurrenceDoubleClicked?.Invoke(this, occurrence);

    private (DateTime Start, DateTime End) MonthRange()
    {
        var grid = SchedulerEngine.MonthGrid(EffectiveAnchor);
        return (grid[0], grid[grid.Count - 1].AddDays(1));
    }

    private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((SchedulerView)d).Rebuild();

    private static void OnSelectedOccurrenceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (SchedulerView)d;
        if (view._daysPanel is not null)
        {
            view._daysPanel.SelectedOccurrence = view.SelectedOccurrence;
        }

        if (view._monthPanel is not null)
        {
            view._monthPanel.SelectedOccurrence = view.SelectedOccurrence;
        }
    }

    private static void OnAppointmentsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (SchedulerView)d;
        if (e.OldValue is ObservableCollection<SchedulerAppointment> old)
        {
            old.CollectionChanged -= view.OnAppointmentsCollectionChanged;
        }

        if (e.NewValue is ObservableCollection<SchedulerAppointment> current)
        {
            current.CollectionChanged += view.OnAppointmentsCollectionChanged;
        }

        view.Rebuild();
    }

    private void OnAppointmentsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();
}
