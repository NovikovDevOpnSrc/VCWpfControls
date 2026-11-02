using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using VCWpfControls.PropertyGrid;
using Xunit;

namespace VCWpfControls.Core.Tests.PropertyGrid;

public class PropertyItemBuilderTests
{
    private sealed class Address
    {
        [Display(Name = "City name")]
        public string City { get; set; } = "Berlin";

        public int Zip { get; set; } = 10115;
    }

    private sealed class Model
    {
        [Display(Name = "Full name", GroupName = "Identity", Order = 1, Description = "Who is this")]
        public string? Name { get; set; }

        [Display(Order = 2)]
        [Category("Main")]
        public int Age { get; set; }

        [Category("Extra")]
        public bool Active { get; set; }

        public DayOfWeek Day { get; set; }

        public DateTime Born { get; set; }

        public decimal Salary { get; set; }

        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }

        public Address? Address { get; set; }

        [Browsable(false)]
        public string? Hidden { get; set; }

        [ReadOnly(true)]
        public string? Computed { get; set; }

        public List<int> Numbers { get; set; } = new();
    }

    private static List<PropertyItem> Build() => PropertyItemBuilder.Build(new Model());

    [Fact]
    public void Build_MapsEditorKindsByType()
    {
        var items = Build();
        var byName = items.ToDictionary(i => i.Name);

        Assert.Equal(PropertyItemEditorKind.Text, byName["Name"].EditorKind);
        Assert.Equal(PropertyItemEditorKind.Numeric, byName["Age"].EditorKind);
        Assert.Equal(PropertyItemEditorKind.Boolean, byName["Active"].EditorKind);
        Assert.Equal(PropertyItemEditorKind.Enum, byName["Day"].EditorKind);
        Assert.Equal(PropertyItemEditorKind.Date, byName["Born"].EditorKind);
        Assert.Equal(PropertyItemEditorKind.Numeric, byName["Salary"].EditorKind);
        Assert.Equal(PropertyItemEditorKind.MultiLineText, byName["Notes"].EditorKind);
        Assert.Equal(PropertyItemEditorKind.ReadOnly, byName["Numbers"].EditorKind);
    }

    [Fact]
    public void Build_HonorsDisplayAndCategoryAttributes()
    {
        var items = Build();
        var name = items.First(i => i.Name == "Name");

        Assert.Equal("Full name", name.DisplayName);
        Assert.Equal("Identity", name.Category);
        Assert.Equal("Who is this", name.Description);
        Assert.Equal(1, name.SortOrder);
        Assert.Equal("Main", items.First(i => i.Name == "Age").Category);
    }

    [Fact]
    public void Build_SkipsNonBrowsable_AndAppliesReadOnly()
    {
        var items = Build();

        Assert.DoesNotContain(items, i => i.Name == "Hidden");
        Assert.True(items.First(i => i.Name == "Computed").IsReadOnly);
        Assert.False(items.First(i => i.Name == "Age").IsReadOnly);
    }

    [Fact]
    public void Build_ComplexObjectsAreExpandable()
    {
        var items = Build();

        var address = items.First(i => i.Name == "Address");
        Assert.True(address.IsExpandable);
        Assert.False(items.First(i => i.Name == "Notes").IsExpandable);

        var model = new Model { Address = new Address() };
        var addressItem = PropertyItemBuilder.Build(model).First(i => i.Name == "Address");
        var children = PropertyItemBuilder.BuildChildren(addressItem)!;
        Assert.Contains(children, c => c.Name == "City");
    }

    [Fact]
    public void Value_GetSetRoundTrip()
    {
        var model = new Model { Name = "before" };
        var item = PropertyItemBuilder.Build(model).First(i => i.Name == "Name");

        Assert.Equal("before", item.GetValue());
        item.SetValue("after");
        Assert.Equal("after", model.Name);
    }

    [Fact]
    public void GetValueText_CollectionsShowCount()
    {
        var model = new Model { Numbers = new List<int> { 1, 2, 3 } };
        var item = PropertyItemBuilder.Build(model).First(i => i.Name == "Numbers");

        Assert.Contains("3", item.GetValueText());
    }
}
