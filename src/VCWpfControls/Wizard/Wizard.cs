using System.Collections.ObjectModel;
using System.Windows.Markup;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VCWpfControls.Wizard;

/// <summary>Event arguments of <see cref="Wizard.PageChanging"/>.</summary>
public sealed class WizardPageChangingEventArgs : RoutedEventArgs
{
    internal WizardPageChangingEventArgs(WizardPage from, WizardPage to)
    {
        From = from;
        To = to;
    }

    /// <summary>The page being left.</summary>
    public WizardPage From { get; }

    /// <summary>The page about to be shown.</summary>
    public WizardPage To { get; }

    /// <summary>Set to <see langword="true"/> to keep the user on <see cref="From"/>.</summary>
    public bool Cancel { get; set; }
}

/// <summary>
/// A paged wizard: a sequence of <see cref="WizardPage"/>s with Back/Next/Finish/Cancel
/// buttons and cancelable page-changing events. Pages are content; the wizard template
/// hosts them plus the navigation bar.
/// </summary>
[ContentProperty(nameof(Pages))]
[TemplatePart(Name = PartBack, Type = typeof(Button))]
[TemplatePart(Name = PartNext, Type = typeof(Button))]
[TemplatePart(Name = PartCancel, Type = typeof(Button))]
public class Wizard : Control
{
    private const string PartBack = "PART_BackButton";
    private const string PartNext = "PART_NextButton";
    private const string PartCancel = "PART_CancelButton";

    /// <summary>Identifies the <see cref="CurrentPageIndex"/> dependency property.</summary>
    public static readonly DependencyProperty CurrentPageIndexProperty = DependencyProperty.Register(
        nameof(CurrentPageIndex), typeof(int), typeof(Wizard),
        new PropertyMetadata(0, OnCurrentPageIndexChanged, CoerceIndex));

    private Button? _backButton;
    private bool _restoringIndex;
    private Button? _nextButton;
    private Button? _cancelButton;

    static Wizard()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Wizard),
            new FrameworkPropertyMetadata(typeof(Wizard)));
    }

    /// <summary>Creates the wizard.</summary>
    public Wizard()
    {
        Pages.CollectionChanged += (_, _) => CoerceAndRefresh();
    }

    /// <summary>The wizard pages, in order.</summary>
    public ObservableCollection<WizardPage> Pages { get; } = new();

    /// <summary>Zero-based index of the shown page; coerced into <c>[0, Pages.Count-1]</c>.</summary>
    public int CurrentPageIndex
    {
        get => (int)GetValue(CurrentPageIndexProperty);
        set => SetValue(CurrentPageIndexProperty, value);
    }

    /// <summary>The currently shown page (first page by default; null before load with no pages).</summary>
    public WizardPage? CurrentPage => Pages.Count > 0 ? Pages[Math.Clamp(CurrentPageIndex, 0, Pages.Count - 1)] : null;

    /// <summary>Whether the user can go back from the current page.</summary>
    public bool CanGoBack => CurrentPageIndex > 0;

    /// <summary>Whether forward navigation is possible (not on the last page and the page allows leaving).</summary>
    public bool CanGoNext => Pages.Count > 0
        && CurrentPageIndex < Pages.Count - 1
        && (CurrentPage?.CanLeave ?? false);

    /// <summary>Whether the current page is the last one and may be finished.</summary>
    public bool CanFinish => Pages.Count > 0
        && CurrentPageIndex == Pages.Count - 1
        && (CurrentPage?.CanLeave ?? false);

    /// <summary>Raised before leaving a page — cancel to keep the user on it.</summary>
    public event EventHandler<WizardPageChangingEventArgs>? PageChanging
    {
        add => AddHandler(PageChangingEvent, value);
        remove => RemoveHandler(PageChangingEvent, value);
    }

    /// <summary>Identifies the <see cref="PageChanging"/> routed event.</summary>
    public static readonly RoutedEvent PageChangingEvent = EventManager.RegisterRoutedEvent(
        nameof(PageChanging), RoutingStrategy.Direct,
        typeof(EventHandler<WizardPageChangingEventArgs>), typeof(Wizard));

    /// <summary>Raised after a new page became current.</summary>
    public event RoutedEventHandler? PageChanged
    {
        add => AddHandler(PageChangedEvent, value);
        remove => RemoveHandler(PageChangedEvent, value);
    }

    /// <summary>Identifies the <see cref="PageChanged"/> routed event.</summary>
    public static readonly RoutedEvent PageChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(PageChanged), RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(Wizard));

    /// <summary>Raised when the user clicks Finish on the last page.</summary>
    public event RoutedEventHandler? Finished
    {
        add => AddHandler(FinishedEvent, value);
        remove => RemoveHandler(FinishedEvent, value);
    }

    /// <summary>Identifies the <see cref="Finished"/> routed event.</summary>
    public static readonly RoutedEvent FinishedEvent = EventManager.RegisterRoutedEvent(
        nameof(Finished), RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(Wizard));

    /// <summary>Raised when the user clicks Cancel.</summary>
    public event RoutedEventHandler? Cancelled
    {
        add => AddHandler(CancelledEvent, value);
        remove => RemoveHandler(CancelledEvent, value);
    }

    /// <summary>Identifies the <see cref="Cancelled"/> routed event.</summary>
    public static readonly RoutedEvent CancelledEvent = EventManager.RegisterRoutedEvent(
        nameof(Cancelled), RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(Wizard));

    /// <summary>Moves to the previous page when possible.</summary>
    public void GoBack() => CurrentPageIndex = Math.Max(0, CurrentPageIndex - 1);

    /// <summary>
    /// Moves to the next page when possible; on the last page raises <see cref="Finished"/>
    /// instead. Both raise <see cref="PageChanging"/> first (cancelable).
    /// </summary>
    public void GoNext()
    {
        if (!CanGoNext && !CanFinish)
        {
            return;
        }

        if (CanFinish)
        {
            RaiseEvent(new RoutedEventArgs(FinishedEvent, this));
            return;
        }

        CurrentPageIndex += 1;
    }

    /// <summary>Raises <see cref="Cancelled"/> (the application decides what to do).</summary>
    public void Cancel()
        => RaiseEvent(new RoutedEventArgs(CancelledEvent, this));

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _backButton = GetTemplateChild(PartBack) as Button;
        _nextButton = GetTemplateChild(PartNext) as Button;
        _cancelButton = GetTemplateChild(PartCancel) as Button;

        if (_backButton is not null)
        {
            _backButton.Command = new RelayCommand(() => CanGoBack, GoBack);
        }

        if (_nextButton is not null)
        {
            _nextButton.Command = new RelayCommand(() => CanGoNext || CanFinish, GoNext);
        }

        if (_cancelButton is not null)
        {
            _cancelButton.Command = new RelayCommand(() => true, Cancel);
        }

        RefreshNavigation();
    }

    private static object CoerceIndex(DependencyObject d, object baseValue)
        => ((Wizard)d).Pages.Count == 0
            ? 0
            : Math.Clamp((int)baseValue, 0, ((Wizard)d).Pages.Count - 1);

    private static void OnCurrentPageIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var wizard = (Wizard)d;
        if (wizard._restoringIndex)
        {
            wizard.RefreshNavigation();
            return;
        }

        var from = wizard.Pages.Count > 0 ? wizard.Pages[Math.Clamp((int)e.OldValue, 0, wizard.Pages.Count - 1)] : null;
        var to = wizard.CurrentPage;
        if (from is null || to is null || ReferenceEquals(from, to))
        {
            wizard.RefreshNavigation();
            return;
        }

        var args = new WizardPageChangingEventArgs(from, to)
        {
            RoutedEvent = PageChangingEvent,
        };
        wizard.RaiseEvent(args);
        if (args.Cancel)
        {
            // Restore synchronously: the index must be back before GoNext returns. The
            // guard prevents the restore from raising PageChanging a second time.
            wizard._restoringIndex = true;
            try
            {
                wizard.SetCurrentValue(CurrentPageIndexProperty, e.OldValue);
            }
            finally
            {
                wizard._restoringIndex = false;
            }

            return;
        }

        wizard.RefreshNavigation();
        wizard.RaiseEvent(new RoutedEventArgs(PageChangedEvent, wizard));
    }

    /// <summary>Updates button states and the Next/Finish caption after any change.</summary>
    private void RefreshNavigation()
    {
        if (_backButton is not null)
        {
            _backButton.IsEnabled = CanGoBack;
        }

        if (_nextButton is not null)
        {
            _nextButton.IsEnabled = CanGoNext || CanFinish;
            _nextButton.Content = CanFinish ? Localization.GridStrings.WizardFinish : Localization.GridStrings.WizardNext;
        }

        RaiseEvent(new RoutedEventArgs(NextButtonStateChangedEvent, this));
    }

    /// <summary>Raised after navigation states changed (bind button states in custom templates).</summary>
    public event RoutedEventHandler? NextButtonStateChanged
    {
        add => AddHandler(NextButtonStateChangedEvent, value);
        remove => RemoveHandler(NextButtonStateChangedEvent, value);
    }

    /// <summary>Identifies the <see cref="NextButtonStateChanged"/> routed event.</summary>
    public static readonly RoutedEvent NextButtonStateChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(NextButtonStateChanged), RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(Wizard));

    /// <summary>Called by a page whose CanLeave changed (refresh button states).</summary>
    internal void RefreshNavigationForPageChange() => RefreshNavigation();

    private void CoerceAndRefresh()
    {
        CoerceValue(CurrentPageIndexProperty);
        RefreshNavigation();
    }

    private sealed class RelayCommand : ICommand
    {
        private readonly Func<bool> _canExecute;
        private readonly Action _execute;

        public RelayCommand(Func<bool> canExecute, Action execute)
        {
            _canExecute = canExecute;
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute();

        public void Execute(object? parameter) => _execute();
    }
}
