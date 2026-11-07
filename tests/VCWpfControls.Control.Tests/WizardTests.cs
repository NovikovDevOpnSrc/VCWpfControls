using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Wizard;
using WizardControl = VCWpfControls.Wizard.Wizard;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// Wizard navigation: Next/Back flow, page validation via <c>CanLeave</c>, cancelable
/// PageChanging, Finished on the last page, Cancelled, and button states.
/// </summary>
public class WizardTests
{
    private static WizardControl Create(out Window window, int pageCount = 3)
    {
        var wizard = new WizardControl { Width = 420, Height = 300 };
        for (var i = 0; i < pageCount; i++)
        {
            wizard.Pages.Add(new WizardPage
            {
                Header = $"Page {i + 1}",
                Content = new TextBlock { Text = $"content {i + 1}" },
            });
        }

        window = TestWindowHost.Show(wizard);
        TestWindowHost.Pump();
        return wizard;
    }

    [StaFact]
    public void NextBack_NavigatePages_AndButtonStatesFollow()
    {
        var wizard = Create(out var window);
        try
        {
            Assert.Equal(0, wizard.CurrentPageIndex);
            Assert.False(wizard.CanGoBack);
            Assert.True(wizard.CanGoNext);
            Assert.False(wizard.CanFinish);

            wizard.GoNext();
            Assert.Equal(1, wizard.CurrentPageIndex);
            Assert.True(wizard.CanGoBack);

            wizard.GoBack();
            Assert.Equal(0, wizard.CurrentPageIndex);

            // Buttons reflect the states.
            var back = (Button)wizard.Template.FindName("PART_BackButton", wizard)!;
            Assert.False(back.IsEnabled);
            var next = (Button)wizard.Template.FindName("PART_NextButton", wizard)!;
            Assert.True(next.IsEnabled);
            Assert.Equal(VCWpfControls.Localization.GridStrings.WizardNext, next.Content);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void LastPage_NextRaisesFinished_IndexUnchanged()
    {
        var wizard = Create(out var window);
        var finished = 0;
        wizard.Finished += (_, _) => finished++;
        try
        {
            wizard.GoNext();
            wizard.GoNext();
            Assert.Equal(2, wizard.CurrentPageIndex);
            Assert.True(wizard.CanFinish);
            Assert.False(wizard.CanGoNext);

            var next = (Button)wizard.Template.FindName("PART_NextButton", wizard)!;
            Assert.Equal(VCWpfControls.Localization.GridStrings.WizardFinish, next.Content);

            wizard.GoNext(); // finish
            Assert.Equal(1, finished);
            Assert.Equal(2, wizard.CurrentPageIndex); // stays on the last page
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void CanLeaveFalse_BlocksForward()
    {
        var wizard = Create(out var window);
        try
        {
            wizard.Pages[0].CanLeave = false;
            TestWindowHost.Pump();
            Assert.False(wizard.CanGoNext);
            wizard.GoNext();
            Assert.Equal(0, wizard.CurrentPageIndex);

            wizard.Pages[0].CanLeave = true; // unblocks
            TestWindowHost.Pump();
            Assert.True(wizard.CanGoNext);
            wizard.GoNext();
            Assert.Equal(1, wizard.CurrentPageIndex);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void PageChanging_Cancel_KeepsPage()
    {
        var wizard = Create(out var window);
        var changing = 0;
        wizard.PageChanging += (_, e) =>
        {
            changing++;
            if (e.To == wizard.Pages[2])
            {
                e.Cancel = true;
            }
        };
        try
        {
            wizard.GoNext();
            Assert.Equal(1, changing);
            Assert.Equal(1, wizard.CurrentPageIndex);

            wizard.GoNext(); // blocked
            Assert.Equal(2, changing);
            Assert.Equal(1, wizard.CurrentPageIndex);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void PageChanged_FiresOnNavigation()
    {
        var wizard = Create(out var window);
        var changed = 0;
        wizard.PageChanged += (_, _) => changed++;
        try
        {
            wizard.GoNext();
            Assert.Equal(1, changed);
            wizard.GoBack();
            Assert.Equal(2, changed);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Cancel_RaisesCancelledEvent()
    {
        var wizard = Create(out var window);
        var cancelled = 0;
        wizard.Cancelled += (_, _) => cancelled++;
        try
        {
            wizard.Cancel();
            Assert.Equal(1, cancelled);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void IndexCoercion_ClampsToPages()
    {
        var wizard = Create(out var window);
        try
        {
            wizard.CurrentPageIndex = 99;
            Assert.Equal(2, wizard.CurrentPageIndex);
            wizard.CurrentPageIndex = -5;
            Assert.Equal(0, wizard.CurrentPageIndex);
        }
        finally
        {
            window.Close();
        }
    }
}
