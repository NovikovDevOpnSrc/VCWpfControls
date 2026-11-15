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
[TemplatePart(Name = PartStepText, Type = typeof(TextBlock))]
public class Wizard : Control
{
    private const string PartBack = "PART_BackButton";
    private const string PartNext = "PART_NextButton";
    private const string PartCancel = "PART_CancelButton";
    private const string PartStepText = "PART_StepText";

    /// <summary>Identifies the <see cref="CurrentPageIndex"/> dependency property.</summary>
    public static readonly DependencyProperty CurrentPageIndexProperty = DependencyProperty.Register(
        nameof(CurrentPageIndex), typeof(int), typeof(Wizard),
        new PropertyMetadata(0, OnCurrentPageIndexChanged, CoerceIndex));

    /// <summary>Identifies the read-only <see cref="CurrentPage"/> dependency property (the template binds it; a CLR property would never notify).</summary>
    private static readonly DependencyPropertyKey CurrentPagePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(CurrentPage), typeof(WizardPage), typeof(Wizard), new PropertyMetadata(null));

    /// <summary>Identifies the read-only <see cref="CurrentPage"/> dependency property.</summary>
    public static readonly DependencyProperty CurrentPageProperty = CurrentPagePropertyKey.DependencyProperty;

    /// <summary>Identifies the read-only <see cref="HasPages"/> dependency property (drives the template's empty state).</summary>
    private static readonly DependencyPropertyKey HasPagesPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HasPages), typeof(bool), typeof(Wizard), new PropertyMetadata(false));

    /// <summary>Identifies the read-only <see cref="HasPages"/> dependency property.</summary>
    public static readonly DependencyProperty HasPagesProperty = HasPagesPropertyKey.DependencyProperty;

    private Button? _backButton;
    private bool _restoringIndex;
    private Button? _nextButton;
    private Button? _cancelButton;
    private TextBlock? _stepText;

    static Wizard()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Wizard),
            new FrameworkPropertyMetadata(typeof(Wizard)));
    }

    private static readonly System.ComponentModel.DependencyPropertyDescriptor CanLeaveDescriptor =
        System.ComponentModel.DependencyPropertyDescriptor.FromProperty(WizardPage.CanLeaveProperty, typeof(WizardPage));

    /// <summary>Creates the wizard.</summary>
    public Wizard()
    {
        Pages.CollectionChanged += OnPagesChanged;
    }

    private void OnPagesChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // Track CanLeave of EVERY page directly — relying on the page's logical Parent
        // misses code-built pages (Parent == null) and any non-standard hosting.
        if (e.OldItems is not null)
        {
            foreach (WizardPage page in e.OldItems)
            {
                CanLeaveDescriptor.RemoveValueChanged(page, OnAnyPageCanLeaveChanged);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (WizardPage page in e.NewItems)
            {
                CanLeaveDescriptor.AddValueChanged(page, OnAnyPageCanLeaveChanged);
            }
        }

        CoerceAndRefresh();
    }

    private void OnAnyPageCanLeaveChanged(object? sender, EventArgs e) => RefreshNavigation();

    /// <summary>The wizard pages, in order.</summary>
    public ObservableCollection<WizardPage> Pages { get; } = new();

    /// <summary>Zero-based index of the shown page; coerced into <c>[0, Pages.Count-1]</c>.</summary>
    public int CurrentPageIndex
    {
        get => (int)GetValue(CurrentPageIndexProperty);
        set => SetValue(CurrentPageIndexProperty, value);
    }

    /// <summary>The currently shown page (first page by default; null with no pages).</summary>
    public WizardPage? CurrentPage => (WizardPage?)GetValue(CurrentPageProperty);

    /// <summary>Whether the wizard has any pages (the template's empty state follows it).</summary>
    public bool HasPages => (bool)GetValue(HasPagesProperty);

    private WizardPage? ComputeCurrentPage()
        => Pages.Count > 0 ? Pages[Math.Clamp(CurrentPageIndex, 0, Pages.Count - 1)] : null;

    /// <summary>Whether the user can go back from the current page.</summary>
    public bool CanGoBack => CurrentPageIndex > 0;

    /// <summary>Whether forward navigation is possible (not on the last page and the page allows leaving).</summary>
    public bool CanGoNext => Pages.Count > 0
        && CurrentPageIndex < Pages.Count - 1
        && (ComputeCurrentPage()?.CanLeave ?? false);

    /// <summary>Whether the current page is the last one and may be finished.</summary>
    public bool CanFinish => Pages.Count > 0
        && CurrentPageIndex == Pages.Count - 1
        && (ComputeCurrentPage()?.CanLeave ?? false);

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
        _stepText = GetTemplateChild(PartStepText) as TextBlock;

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
        var to = wizard.ComputeCurrentPage();
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

    /// <summary>
    /// The keyboard surface of the dialog: Enter confirms (Next/Finish), Esc cancels —
    /// the Windows wizard convention the guidelines demand. Keys a focused child already
    /// consumed (a multi-line TextBox's Enter) never reach this class handler.
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
        {
            Cancel();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && (CanGoNext || CanFinish))
        {
            GoNext();
            e.Handled = true;
        }
    }

    /// <summary>Updates button states, the Next/Finish caption, the header/step texts and the empty state after any change.</summary>
    private void RefreshNavigation()
    {
        // The template binds these read-only DPs; a CLR CurrentPage property never
        // notifies, and bindings on TemplatedParent paths without notifications go stale.
        SetValue(CurrentPagePropertyKey, ComputeCurrentPage());
        SetValue(HasPagesPropertyKey, Pages.Count > 0);

        if (_stepText is not null)
        {
            _stepText.Text = Pages.Count == 0
                ? string.Empty
                : Localization.GridStrings.WizardStepFormat(CurrentPageIndex + 1, Pages.Count);
        }

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

        // The buttons carry commands whose CanExecuteChanged rides
        // CommandManager.RequerySuggested (fired by USER INPUT only). A programmatic
        // CanLeave change (a binding, a UIA Toggle) raises no input — without this
        // invalidation IsEnabledCore keeps the stale disabled verdict and overrides
        // the direct IsEnabled sets above.
        System.Windows.Input.CommandManager.InvalidateRequerySuggested();
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
