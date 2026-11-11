using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Scheduler;
using VCWpfControls.Themes;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The scheduler polish round: the keyboard surface (arrows walk the visible occurrences,
/// Enter activates, Delete removes, T anchors to today, Esc clears), the now-line geometry
/// (window/day guards + y mapping) and the theme-token rendering — the month chips drew
/// BLACK text regardless of the palette (invisible on the dark/HC event fill) and the day
/// panels carried literal brushes; everything resolves through GridResources tokens now.
/// </summary>
public class SchedulerPolishTests
{
    private static readonly DateTime Nov9 = new(2026, 11, 9); // Monday
    private static readonly DateTime Nov10 = new(2026, 11, 10);

    private static SchedulerView CreateDays(out Window window, params SchedulerAppointment[] appointments)
    {
        var view = new SchedulerView
        {
            Width = 800,
            Height = 420,
            AnchorDate = Nov9,
            Appointments = new ObservableCollection<SchedulerAppointment>(appointments),
        };
        window = TestWindowHost.Show(view);
        TestWindowHost.Pump();
        return view;
    }

    private static void SendKeyDown(UIElement target, Key key)
    {
        var source = PresentationSource.FromVisual(target);
        Assert.NotNull(source);
        target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source!, 0, key)
        {
            RoutedEvent = UIElement.KeyDownEvent,
            Source = target,
        });
    }

    [StaFact]
    public void Arrows_WalkSelectionByStartTime()
    {
        var view = CreateDays(out var window,
            new SchedulerAppointment("a", "Late", Nov9.AddHours(13), Nov9.AddHours(14)),
            new SchedulerAppointment("b", "First", Nov9.AddHours(9), Nov9.AddHours(10)),
            new SchedulerAppointment("c", "Mid", Nov9.AddHours(11), Nov9.AddHours(12)));
        try
        {
            Assert.Null(view.SelectedOccurrence);

            SendKeyDown(view, Key.Down);
            Assert.Equal("First", view.SelectedOccurrence!.Subject); // nothing selected -> the earliest

            SendKeyDown(view, Key.Down);
            Assert.Equal("Mid", view.SelectedOccurrence!.Subject);

            SendKeyDown(view, Key.Right);
            Assert.Equal("Late", view.SelectedOccurrence!.Subject);

            SendKeyDown(view, Key.Up);
            Assert.Equal("Mid", view.SelectedOccurrence!.Subject);

            SendKeyDown(view, Key.Left);
            Assert.Equal("First", view.SelectedOccurrence!.Subject);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Arrows_DoNotWrapAtTheEnds()
    {
        var view = CreateDays(out var window,
            new SchedulerAppointment("a", "Only", Nov9.AddHours(9), Nov9.AddHours(10)));
        try
        {
            SendKeyDown(view, Key.Up); // nothing selected -> the last (= the only one)
            Assert.Equal("Only", view.SelectedOccurrence!.Subject);

            SendKeyDown(view, Key.Up); // already first — stays
            Assert.Equal("Only", view.SelectedOccurrence!.Subject);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Enter_ActivatesTheSelectedOccurrence()
    {
        var view = CreateDays(out var window,
            new SchedulerAppointment("a", "Meeting", Nov9.AddHours(9), Nov9.AddHours(10)));
        try
        {
            SchedulerOccurrence? activated = null;
            view.OccurrenceDoubleClicked += (_, occurrence) => activated = occurrence;

            SendKeyDown(view, Key.Down);
            SendKeyDown(view, Key.Enter);

            Assert.NotNull(activated);
            Assert.Equal("Meeting", activated!.Subject);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Delete_RemovesTheAppointmentAndClearsSelection()
    {
        var appointment = new SchedulerAppointment("a", "Doomed", Nov9.AddHours(9), Nov9.AddHours(10));
        var view = CreateDays(out var window, appointment,
            new SchedulerAppointment("b", "Stays", Nov9.AddHours(11), Nov9.AddHours(12)));
        try
        {
            var deleted = new List<SchedulerAppointment>();
            view.AppointmentDeleted += (_, removed) => deleted.Add(removed);

            SendKeyDown(view, Key.Down); // select "Doomed"
            SendKeyDown(view, Key.Delete);

            var remaining = Assert.Single(view.Appointments!);
            Assert.Equal("Stays", remaining.Subject);
            Assert.Null(view.SelectedOccurrence);
            Assert.Same(appointment, Assert.Single(deleted));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Delete_WithoutSelection_ChangesNothing()
    {
        var view = CreateDays(out var window,
            new SchedulerAppointment("a", "Meeting", Nov9.AddHours(9), Nov9.AddHours(10)));
        try
        {
            var deleted = 0;
            view.AppointmentDeleted += (_, _) => deleted++;

            SendKeyDown(view, Key.Delete);

            Assert.Single(view.Appointments!);
            Assert.Equal(0, deleted);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void T_AnchorsToToday_AndEscapeClearsSelection()
    {
        var view = CreateDays(out var window,
            new SchedulerAppointment("a", "Meeting", Nov9.AddHours(9), Nov9.AddHours(10)));
        try
        {
            SendKeyDown(view, Key.Down);
            Assert.NotNull(view.SelectedOccurrence);

            SendKeyDown(view, Key.Escape);
            Assert.Null(view.SelectedOccurrence);

            SendKeyDown(view, Key.T);
            Assert.Equal(DateTime.Today, view.AnchorDate);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void NowLine_MapsTimeToY()
    {
        var view = CreateDays(out var window,
            new SchedulerAppointment("a", "Meeting", Nov9.AddHours(9), Nov9.AddHours(10)));
        try
        {
            var panel = view.DaysPanel!;
            var now = Nov9.AddHours(10).AddMinutes(30); // Monday 10:30 — inside the anchored work week
            panel.NowForTest = () => now;

            var line = panel.NowLineRect();
            Assert.False(line.IsEmpty);
            Assert.Equal(panel.ContentTop + 10.5 * panel.HourHeight, line.Y, 1);
            Assert.Equal(panel.TimeRulerWidth, line.X, 1);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void NowLine_HiddenOutsideTheHourWindowAndForeignDays()
    {
        var view = CreateDays(out var window,
            new SchedulerAppointment("a", "Meeting", Nov9.AddHours(9), Nov9.AddHours(10)));
        view.DayStartHour = 8;
        view.DayEndHour = 18;
        TestWindowHost.Pump();
        try
        {
            var panel = view.DaysPanel!;

            panel.NowForTest = () => Nov9.AddHours(19); // after the visible window
            Assert.True(panel.NowLineRect().IsEmpty);

            panel.NowForTest = () => Nov9.AddHours(7); // before the visible window
            Assert.True(panel.NowLineRect().IsEmpty);

            panel.NowForTest = () => new DateTime(2026, 11, 14, 10, 0, 0); // Saturday — not in the work week
            Assert.True(panel.NowLineRect().IsEmpty);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void SchedulerTokens_FollowSystemColorsInHighContrast()
    {
        _ = new GridControl(); // registers pack:// in STA hosts without an Application
        var dictionary = new ResourceDictionary
        {
            Source = new System.Uri("pack://application:,,,/VCWpfControls;component/Themes/HighContrast.xaml"),
        };

        var eventFill = Assert.IsType<SolidColorBrush>(dictionary[GridResources.EventFillKey]);
        Assert.Equal(SystemColors.WindowColor, eventFill.Color); // flat: structure rides on the frame lines

        var nowLine = Assert.IsType<SolidColorBrush>(dictionary[GridResources.NowLineKey]);
        Assert.Equal(SystemColors.HotTrackColor, nowLine.Color);
    }

    [StaFact]
    public void MonthChipText_IsReadableInDarkTheme()
    {
        var view = new SchedulerView
        {
            Width = 700,
            Height = 480,
            ViewType = SchedulerViewType.Month,
            AnchorDate = new DateTime(2026, 11, 1),
            Appointments = new ObservableCollection<SchedulerAppointment>
            {
                new("a", "Dark theme chip", Nov10.AddHours(9), Nov10.AddHours(10)),
            },
        };
        ThemeManager.ApplyTo(view.Resources, GridTheme.Dark, null);
        var window = TestWindowHost.Show(view);
        TestWindowHost.Pump();
        try
        {
            var panel = view.MonthPanel!;
            var occurrence = view.VisibleOccurrences.Single(o => o.Subject == "Dark theme chip");
            var chip = panel.ChipOf(occurrence, Nov10);
            Assert.NotNull(chip);

            // Offscreen render through a VisualBrush (direct RenderTargetBitmap.Render of an
            // element yields an empty raster in this host — see ColumnSeparatorTests).
            var width = (int)Math.Ceiling(panel.RenderSize.Width);
            var height = (int)Math.Ceiling(panel.RenderSize.Height);
            Assert.True(width > 0 && height > 0, $"month panel not laid out: {width}x{height}");
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            var visual = new DrawingVisual();
            using (var ctx = visual.RenderOpen())
            {
                ctx.DrawRectangle(new VisualBrush(panel), null, new Rect(0, 0, width, height));
            }

            bitmap.Render(visual);
            var pixels = new byte[width * height * 4];
            bitmap.CopyPixels(pixels, width * 4, 0);

            // The chip text must be near-white in the dark theme (TextBrush #FFFFFF): with the
            // old hardcoded Brushes.Black the brightest chip content was the blue fill itself
            // (luma ~140); white glyph cores are >200.
            var maxLuma = 0;
            for (var y = (int)chip!.Value.Y; y < chip.Value.Bottom; y++)
            {
                for (var x = (int)chip.Value.X; x < chip.Value.Right; x++)
                {
                    var i = (y * width + x) * 4; // Pbgra: B,G,R,A
                    var luma = (pixels[i] * 11 + pixels[i + 1] * 59 + pixels[i + 2] * 30) / 100;
                    maxLuma = Math.Max(maxLuma, luma);
                }
            }

            Assert.True(maxLuma > 200, $"chip text unreadable in dark theme: brightest luma {maxLuma}");
        }
        finally
        {
            window.Close();
        }
    }
}
