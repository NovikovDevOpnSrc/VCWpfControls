using System.Windows;
using System.Windows.Controls;

namespace VCWpfControls.Wizard;

/// <summary>
/// One page of a <see cref="Wizard"/>: arbitrary content plus a header and the
/// <see cref="CanLeave"/> gate that blocks forward navigation (e.g. while invalid).
/// </summary>
public class WizardPage : ContentControl
{
    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header), typeof(object), typeof(WizardPage), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="CanLeave"/> dependency property.</summary>
    public static readonly DependencyProperty CanLeaveProperty = DependencyProperty.Register(
        nameof(CanLeave), typeof(bool), typeof(WizardPage),
        new PropertyMetadata(true, OnCanLeaveChanged));

    static WizardPage()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(WizardPage),
            new FrameworkPropertyMetadata(typeof(WizardPage)));
        FocusableProperty.OverrideMetadata(typeof(WizardPage),
            new FrameworkPropertyMetadata(false));
    }

    /// <summary>The page caption shown by the wizard template.</summary>
    public object Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>
    /// Whether forward navigation off this page is allowed — bind to your validation
    /// state; the wizard disables Next/Finish and GoNext ignores clicks while false.
    /// </summary>
    public bool CanLeave
    {
        get => (bool)GetValue(CanLeaveProperty);
        set => SetValue(CanLeaveProperty, value);
    }

    private static void OnCanLeaveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // The owning wizard refreshes its button states through the logical parent.
        if (((WizardPage)d).Parent is Wizard wizard)
        {
            wizard.RefreshNavigationForPageChange();
        }
    }

    internal void NotifyCanLeaveChanged() => OnCanLeaveChanged(this, default);
}
