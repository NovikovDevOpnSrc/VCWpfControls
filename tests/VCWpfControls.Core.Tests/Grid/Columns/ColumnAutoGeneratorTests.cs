using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using VCWpfControls.Grid.Columns;
using Xunit;

namespace VCWpfControls.Core.Tests.Grid.Columns;

public class ColumnAutoGeneratorTests
{
    private sealed class Model
    {
        [Display(Name = "Full name", Order = 2)]
        public string? Name { get; set; }

        [Display(Order = 1)]
        public int Age { get; set; }

        public DateTime Born { get; set; }

        public bool Active { get; set; }

        public DayOfWeek Day { get; set; }

        public decimal? Salary { get; set; }

        [Browsable(false)]
        public string? Internal { get; set; }

        [ReadOnly(true)]
        public string? Computed { get; set; }

        public string this[int index] => string.Empty;
    }

    [Fact]
    public void GenerateColumns_MapsClrTypesToColumnTypes()
    {
        var columns = ColumnAutoGenerator.GenerateColumns(typeof(Model)).ToList();

        var byName = columns.ToDictionary(c => c.FieldName);
        Assert.IsType<GridTextColumn>(byName["Name"]);
        Assert.IsType<GridNumericColumn>(byName["Age"]);
        Assert.IsType<GridDateColumn>(byName["Born"]);
        Assert.IsType<GridCheckBoxColumn>(byName["Active"]);
        Assert.IsType<GridComboBoxColumn>(byName["Day"]);
        Assert.IsType<GridNumericColumn>(byName["Salary"]);
        Assert.IsType<GridTextColumn>(byName["Computed"]);
    }

    [Fact]
    public void GenerateColumns_HonorsDisplayOrder()
    {
        var columns = ColumnAutoGenerator.GenerateColumns(typeof(Model)).ToList();

        Assert.Equal("Age", columns[0].FieldName);
        Assert.Equal("Name", columns[1].FieldName);
    }

    [Fact]
    public void GenerateColumns_UsesDisplayAttributeNameAsHeader()
    {
        var columns = ColumnAutoGenerator.GenerateColumns(typeof(Model)).ToList();

        Assert.Equal("Full name", columns.First(c => c.FieldName == "Name").Header);
    }

    [Fact]
    public void GenerateColumns_SkipsNonBrowsableAndIndexers()
    {
        var columns = ColumnAutoGenerator.GenerateColumns(typeof(Model)).ToList();

        Assert.DoesNotContain(columns, c => c.FieldName == "Internal");
        Assert.Equal(7, columns.Count);
    }

    [Fact]
    public void GenerateColumns_AppliesReadOnlyAttribute()
    {
        var columns = ColumnAutoGenerator.GenerateColumns(typeof(Model)).ToList();

        Assert.True(columns.First(c => c.FieldName == "Computed").ReadOnly);
        Assert.False(columns.First(c => c.FieldName == "Age").ReadOnly);
    }

    [Fact]
    public void GenerateColumns_EnumColumnGetsEnumItemsSource()
    {
        var columns = ColumnAutoGenerator.GenerateColumns(typeof(Model)).ToList();
        var combo = (GridComboBoxColumn)columns.First(c => c.FieldName == "Day");

        Assert.NotNull(combo.ItemsSource);
        Assert.Equal(Enum.GetValues(typeof(DayOfWeek)), combo.ItemsSource);
    }
}
