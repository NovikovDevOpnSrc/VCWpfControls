using VCWpfControls.Grid.Data;
using Xunit;

namespace VCWpfControls.Core.Tests.Grid.Data;

public class PropertyAccessorTests
{
    private sealed class Address
    {
        public string? City { get; set; }
    }

    private sealed class Person
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public Address? Address { get; set; }
    }

    [Fact]
    public void GetValue_SimpleProperty_ReturnsValue()
    {
        var accessor = PropertyAccessor.GetOrCreate(typeof(Person), "Name");
        var person = new Person { Name = "Alice" };

        Assert.Equal("Alice", accessor.GetValue(person));
    }

    [Fact]
    public void GetValue_NestedPath_ReturnsLeafValue()
    {
        var accessor = PropertyAccessor.GetOrCreate(typeof(Person), "Address.City");
        var person = new Person { Address = new Address { City = "Berlin" } };

        Assert.Equal("Berlin", accessor.GetValue(person));
    }

    [Fact]
    public void GetValue_NullIntermediate_ReturnsNull()
    {
        var accessor = PropertyAccessor.GetOrCreate(typeof(Person), "Address.City");

        Assert.Null(accessor.GetValue(new Person()));
    }

    [Fact]
    public void GetValue_NullTarget_ReturnsNull()
    {
        var accessor = PropertyAccessor.GetOrCreate(typeof(Person), "Name");

        Assert.Null(accessor.GetValue(null));
    }

    [Fact]
    public void InvalidPath_IsFlaggedAndReturnsNull()
    {
        var accessor = PropertyAccessor.GetOrCreate(typeof(Person), "Nope.Nope");

        Assert.False(accessor.IsValid);
        Assert.Null(accessor.GetValue(new Person()));
    }

    [Fact]
    public void ValueType_ReflectsLastSegment()
    {
        var accessor = PropertyAccessor.GetOrCreate(typeof(Person), "Address.City");

        Assert.Equal(typeof(string), accessor.ValueType);
    }

    [Fact]
    public void GetOrCreate_CachesInstances()
    {
        var a = PropertyAccessor.GetOrCreate(typeof(Person), "Name");
        var b = PropertyAccessor.GetOrCreate(typeof(Person), "Name");

        Assert.Same(a, b);
    }

    [Fact]
    public void GetValue_CaseInsensitiveFallback_ResolvesProperty()
    {
        var accessor = PropertyAccessor.GetOrCreate(typeof(Person), "name");
        var person = new Person { Name = "Bob" };

        Assert.Equal("Bob", accessor.GetValue(person));
    }
}
