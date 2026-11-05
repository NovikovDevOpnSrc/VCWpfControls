using System.Diagnostics;
using System.Globalization;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Xunit;

namespace VCWpfControls.UiTests;

public class CheckboxOverlapProbe
{
    [Fact]
    public void FindOverlappingCheckboxes()
    {
        var repo = FindRepoPath();
        var app = Application.Launch(System.IO.Path.Combine(repo, "src", "VCWpfControls.Demo", "bin", "Release", "net8.0-windows", "VCWpfControls.Demo.exe"));
        try
        {
            using var automation = new UIA3Automation();
            var window = app.GetMainWindow(automation);
            var sb = new System.Text.StringBuilder();

            string NameOf(AutomationElement e)
            {
                try { return e.Properties.Name.Value ?? string.Empty; }
                catch { return string.Empty; }
            }

            var list = window.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.List));
            foreach (var item in list!.FindAllChildren())
            {
                item.Click();
                System.Threading.Thread.Sleep(400);

                var checkboxes = window.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.CheckBox));
                var texts = window.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Text));
                foreach (var cb in checkboxes)
                {
                    var cr = cb.Properties.BoundingRectangle.ValueOrDefault;
                    if (cr.Width == 0 || cr.Height == 0)
                    {
                        continue;
                    }

                    foreach (var tx in texts)
                    {
                        var tr = tx.Properties.BoundingRectangle.ValueOrDefault;
                        if (tr.Width == 0 || tr.Height == 0)
                        {
                            continue;
                        }

                        var ix = Math.Max(cr.X, tr.X);
                        var iy = Math.Max(cr.Y, tr.Y);
                        var ir = Math.Min(cr.Right, tr.Right);
                        var ib = Math.Min(cr.Bottom, tr.Bottom);
                        if (ir - ix > 2 && ib - iy > 2)
                        {
                            sb.AppendLine(string.Create(CultureInfo.InvariantCulture,
                                $"OVERLAP page='{NameOf(item).Split('.').Last()}' checkbox=[{cr.X:F0},{cr.Y:F0} {cr.Width:F0}x{cr.Height:F0}] text='{Trunc(NameOf(tx), 30)}' [{tr.X:F0},{tr.Y:F0} {tr.Width:F0}x{tr.Height:F0}] inter={ir - ix:F0}x{ib - iy:F0}"));
                        }
                    }
                }
            }

            System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "cboverlap.txt"), sb.ToString());
            app.Close();
        }
        finally
        {
            if (!app.HasExited)
            {
                app.Kill();
            }
        }
    }

    private static string FindRepoPath()
    {
        var candidates = new[]
        {
            System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."),
            System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."),
        };

        return candidates.FirstOrDefault(d => System.IO.File.Exists(System.IO.Path.Combine(d, "VCWpfControls.slnx")))
            ?? candidates[0];
    }

    private static string Trunc(string s, int n) => s.Length <= n ? s : s[..n];
}
