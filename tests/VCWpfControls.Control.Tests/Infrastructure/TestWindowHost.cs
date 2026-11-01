using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace VCWpfControls.Control.Tests.Infrastructure;

/// <summary>Helpers for control tests: hosting, layout pumping and bitmap snapshots.</summary>
public static class TestWindowHost
{
    /// <summary>Hosts the element in a window, shows it and pumps layout synchronously.</summary>
    public static Window Show(FrameworkElement element, double width = 900, double height = 500)
    {
        var window = new Window
        {
            Width = width,
            Height = height,
            WindowStyle = WindowStyle.ToolWindow,
            ShowInTaskbar = false,
            ShowActivated = false,
            Content = element,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -32000,
            Top = -32000,
        };
        window.Show();
        Pump();
        return window;
    }

    /// <summary>Processes all pending dispatcher operations at Render priority and below.</summary>
    public static void Pump()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
            new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
        // A second pass lets data-bound items generate containers.
        frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Loaded,
            new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }
}
