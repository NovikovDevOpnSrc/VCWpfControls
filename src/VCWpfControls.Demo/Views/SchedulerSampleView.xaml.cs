using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Demo.ViewModels;
using VCWpfControls.Scheduler;

namespace VCWpfControls.Demo.Views;

/// <summary>Scheduler sample: view switching, navigation and the double-click hook.</summary>
public partial class SchedulerSampleView : UserControl
{
    /// <summary>Creates the page.</summary>
    public SchedulerSampleView()
    {
        InitializeComponent();
        SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, "SchedulerSample");
    }

    private void OnPrev(object sender, RoutedEventArgs e) => Scheduler.Navigate(-1);

    private void OnNext(object sender, RoutedEventArgs e) => Scheduler.Navigate(1);

    private void OnToday(object sender, RoutedEventArgs e) => Scheduler.GoToToday();

    private void OnOccurrenceDoubleClicked(object sender, SchedulerOccurrence occurrence)
        => ((SchedulerSampleViewModel)DataContext).LastAction =
            $"Double-clicked '{occurrence.Subject}' ({occurrence.Start:g} \u2013 {occurrence.End:g})";

    private void OnAppointmentEdited(object sender, SchedulerAppointment appointment)
        => ((SchedulerSampleViewModel)DataContext).LastAction =
            $"Moved/resized '{appointment.Subject}' → {appointment.Start:g} \u2013 {appointment.End:g}";

    private void OnAppointmentCreated(object sender, SchedulerAppointment appointment)
        => ((SchedulerSampleViewModel)DataContext).LastAction =
            $"Created '{appointment.Subject}' {appointment.Start:g} \u2013 {appointment.End:g} (double-click to rename)";

    private void OnAppointmentDeleted(object sender, SchedulerAppointment appointment)
        => ((SchedulerSampleViewModel)DataContext).LastAction =
            $"Deleted '{appointment.Subject}' {appointment.Start:g} \u2013 {appointment.End:g}";
}
