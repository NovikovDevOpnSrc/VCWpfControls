using System.Collections.ObjectModel;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Data;
using Xunit;
using static VCWpfControls.Control.Tests.TestModels;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// Stress churn: rapid ObservableCollection mutation under an active sort + group, and a
/// rapid settings storm (sort/filter/group toggling) — the view must converge to the
/// correct row set every time (the issue-#5 regression class, now under load).
/// </summary>
public class StressChurnTests
{
    private static ObservableCollection<Employee> Create(int count)
    {
        var list = new ObservableCollection<Employee>();
        for (var i = 0; i < count; i++)
        {
            list.Add(new Employee { Id = i, Name = $"Emp {i % 37:000}", Department = $"D{i % 5}", Salary = 100m + (i % 13) * 10 });
        }

        return list;
    }

    [StaFact]
    public void MutationStorm_UnderGroupingAndSort_ConvergesToTruth()
    {
        var source = Create(150);
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = source;
        grid.SortBy("Salary", GridSortDirection.Ascending);
        grid.GroupDescriptions.Add(new GridGroupDescription("Department"));
        TestWindowHost.Pump();
        try
        {
            var random = new Random(20261109);
            for (var wave = 0; wave < 30; wave++)
            {
                // Remove a random slice, add fresh items back.
                for (var r = 0; r < 5 && source.Count > 10; r++)
                {
                    source.RemoveAt(random.Next(source.Count));
                }

                for (var a = 0; a < 5; a++)
                {
                    source.Add(new Employee { Id = 1000 + wave * 10 + a, Name = $"New {wave}-{a}", Department = $"D{random.Next(5)}", Salary = 100m + random.Next(130) });
                }

                TestWindowHost.Pump();
            }

            TestWindowHost.Pump();

            // The view must hold exactly the live items (no ghosts, no losses).
            var viewItems = grid.DataEngine.View.OfType<GridDataRow>().Select(r => r.Item).ToList();
            Assert.Equal(source.Count, viewItems.Count);
            Assert.Equal(
                source.Select(e => e.Id).OrderBy(id => id),
                viewItems.Select(e => ((Employee)e).Id).OrderBy(id => id));

            // Every grouped department's item count matches the source truth.
            var truthByDept = source.GroupBy(e => e.Department).ToDictionary(g => g.Key, g => g.Count());
            var gridByDept = grid.DataEngine.DataRows
                .Select(r => ((Employee)r.Item).Department)
                .GroupBy(d => d)
                .ToDictionary(g => g.Key, g => g.Count());
            Assert.Equal(truthByDept.Count, gridByDept.Count);
            foreach (var pair in truthByDept)
            {
                Assert.Equal(pair.Value, gridByDept[pair.Key]);
            }
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void SettingsStorm_SortFilterGroupToggleChurn_GridStaysAlive()
    {
        var grid = new GridControl { ItemsSource = Create(80) };
        var window = TestWindowHost.Show(grid);
        TestWindowHost.Pump();
        try
        {
            for (var wave = 0; wave < 20; wave++)
            {
                grid.SortBy(wave % 2 == 0 ? "Name" : "Salary", GridSortDirection.Ascending);
                if (wave % 3 == 0)
                {
                    grid.FilterDescriptions.Clear();
                    grid.FilterDescriptions.Add(new GridFilterDescription("Salary", GridFilterOperator.GreaterThan, 110m + wave));
                }

                if (wave % 2 == 0)
                {
                    grid.GroupDescriptions.Add(new GridGroupDescription("Department"));
                }
                else if (grid.GroupDescriptions.Count > 0)
                {
                    grid.GroupDescriptions.RemoveAt(0);
                }

                TestWindowHost.Pump();
                Assert.True(grid.DataEngine.View.Count > 0);
            }
        }
        finally
        {
            window.Close();
        }
    }
}
