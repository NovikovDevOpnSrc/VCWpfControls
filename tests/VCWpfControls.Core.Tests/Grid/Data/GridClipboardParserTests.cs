using System.Globalization;
using VCWpfControls.Grid.Data;
using Xunit;

namespace VCWpfControls.Core.Tests.Grid.Data;

public class GridClipboardParserTests
{
    [Fact]
    public void ParseTsv_SplitsRowsAndCells_DropsEmptyLines()
    {
        var rows = GridClipboard.ParseTsv("a\tb\tc\r\nd\te\n\nf\r\n");
        Assert.Equal(3, rows.Count);
        Assert.Equal(new[] { "a", "b", "c" }, rows[0]);
        Assert.Equal(new[] { "d", "e" }, rows[1]);
        Assert.Equal(new[] { "f" }, rows[2]);
    }

    [Fact]
    public void ParseTsv_EmptyInput_NoRows()
    {
        Assert.Empty(GridClipboard.ParseTsv(string.Empty));
        Assert.Empty(GridClipboard.ParseTsv(null!));
    }

    [Theory]
    [InlineData("42", typeof(int), 42)]
    [InlineData("::double::", typeof(double), 42.5d)]
    [InlineData("true", typeof(bool), true)]
    [InlineData("2020-05-26", typeof(DateTime), "2020-05-26T00:00:00")]
    public void TryConvert_ParsesCultureAware(string text, Type type, object expected)
    {
        if (text == "::double::")
        {
            // Render the expected value with the CURRENT culture: the parser is culture-aware.
            text = string.Format(CultureInfo.CurrentCulture, "{0}", (double)expected);
        }

        Assert.True(GridClipboard.TryConvert(text, type, out var value));
        Assert.Equal(Convert.ChangeType(expected, Nullable.GetUnderlyingType(type) ?? type, CultureInfo.InvariantCulture), value);
    }

    [Fact]
    public void TryConvert_NullableInt_AndNullText()
    {
        Assert.True(GridClipboard.TryConvert("7", typeof(int?), out var seven));
        Assert.Equal(7, seven);
        Assert.True(GridClipboard.TryConvert(null, typeof(int?), out var nul));
        Assert.Null(nul);
    }

    [Fact]
    public void TryConvert_GuidAndEnum()
    {
        Assert.True(GridClipboard.TryConvert("{7b9e6f1c-2f4e-4c9f-9bd1-3d5f0a1b2c3d}", typeof(Guid), out var guid));
        Assert.IsType<Guid>(guid);
        Assert.True(GridClipboard.TryConvert("Enter", typeof(ConsoleKey), out var key));
        Assert.Equal(ConsoleKey.Enter, key);
    }

    [Theory]
    [InlineData("abc", typeof(int))]
    [InlineData("not-a-date", typeof(DateTime))]
    public void TryConvert_BadInput_ReturnsFalse(string text, Type type)
    {
        Assert.False(GridClipboard.TryConvert(text, type, out _));
    }

    [Fact]
    public void TryConvert_EmptyText_NonNullableValueTypes_GetDefault()
    {
        // A value-type property cannot receive null: an empty cell yields default(0/false).
        Assert.True(GridClipboard.TryConvert(null, typeof(int), out var zero));
        Assert.Equal(0, zero);
    }
}
