using VCWpfControls.Editors;
using Xunit;

namespace VCWpfControls.Core.Tests.Editors;

public class InputMaskTests
{
    [Fact]
    public void DisplayString_ShowsLiteralsAndPrompts()
    {
        var mask = new InputMask("+7 (000) 000-00-00");
        Assert.Equal("+7 (___) ___-__-__", mask.ToDisplayString());
    }

    [Fact]
    public void Insert_FillsSlotsAndSkipsLiterals()
    {
        var mask = new InputMask("+7 (000) 000-00-00");
        var caret = mask.Insert("9012345678", 0);

        Assert.Equal("+7 (901) 234-56-78", mask.ToDisplayString());
        Assert.Equal(mask.Length, caret);
        Assert.True(mask.IsComplete);
        Assert.Equal("9012345678", mask.ToValueString());
    }

    [Fact]
    public void Insert_RejectsWrongCharacters()
    {
        var mask = new InputMask("000");
        Assert.Equal(-1, mask.InsertChar('a', 0));
        Assert.Equal(0, mask.Insert(string.Empty, 0)); // empty input: caret unchanged
        Assert.Equal(1, mask.InsertChar('5', 0));
    }

    [Fact]
    public void Insert_MultiCharacter_StopsOnFirstRejected()
    {
        var mask = new InputMask("000");
        Assert.Equal(-1, mask.Insert("12x", 0));
        // The accepted prefix stays applied.
        Assert.Equal("12_", mask.ToDisplayString());
    }

    [Fact]
    public void LetterSlots_AcceptOnlyLetters()
    {
        var mask = new InputMask("LL-00");
        Assert.Equal(-1, mask.InsertChar('1', 0));
        Assert.Equal(1, mask.InsertChar('a', 0));
        Assert.False(mask.IsComplete);
        // 'B' fills the second letter slot; the caret jumps past the '-' literal to the digit.
        Assert.Equal(3, mask.InsertChar('B', 1));
        Assert.False(mask.IsComplete);
        mask.Insert("42", 3);
        Assert.True(mask.IsComplete);
    }

    [Fact]
    public void Backspace_ClearsPreviousEditableSlot()
    {
        var mask = new InputMask("000-0000");
        mask.Insert("1234567", 0);
        Assert.Equal("123-4567", mask.ToDisplayString());

        var caret = mask.Backspace(mask.Length);
        Assert.Equal(7, caret);
        Assert.Equal("123-456_", mask.ToDisplayString());
        Assert.False(mask.IsComplete);
    }

    [Fact]
    public void Delete_ClearsCurrentSlot_KeepingCaret()
    {
        var mask = new InputMask("000");
        mask.Insert("123", 0);
        var caret = mask.Delete(1);
        Assert.Equal(1, caret);
        Assert.Equal("1_3", mask.ToDisplayString());
    }

    [Fact]
    public void EscapedCharacters_AreLiterals()
    {
        // Verbatim "\\0\\0" = backslash, slot, backslash, slot (escaped backslash literals).
        var mask = new InputMask(@"\\0\\0");
        Assert.Equal(4, mask.Length);
        Assert.Equal(@"\_\_", mask.ToDisplayString());
        Assert.Equal(3, mask.InsertChar('9', 0)); // caret lands on the last editable slot
        Assert.Equal(@"\9\_", mask.ToDisplayString());
    }

    [Fact]
    public void ValueString_CanIncludeLiterals()
    {
        var mask = new InputMask("00/00");
        mask.Insert("3112", 0);
        Assert.Equal("3112", mask.ToValueString());
        Assert.Equal("31/12", mask.ToValueString(includeLiterals: true));
    }

    [Fact]
    public void SetValue_FillsEditableSlotsOnly()
    {
        var mask = new InputMask("(000)");
        mask.SetValue("42");
        Assert.Equal("(42_)", mask.ToDisplayString());
        Assert.Equal("42", mask.ToValueString());
    }
}

public class NumericSpinnerLogicTests
{
    [Fact]
    public void Clamp_LimitsToRange()
    {
        Assert.Equal(5m, NumericSpinnerLogic.Clamp(3m, 5m, 10m));
        Assert.Equal(10m, NumericSpinnerLogic.Clamp(42m, 5m, 10m));
        Assert.Equal(7m, NumericSpinnerLogic.Clamp(7m, 5m, 10m));
    }

    [Fact]
    public void Parse_AcceptsCultureAndInvariantNumbers()
    {
        Assert.Equal(12.5m, NumericSpinnerLogic.Parse("12.5", System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(-3m, NumericSpinnerLogic.Parse("-3"));
        Assert.Null(NumericSpinnerLogic.Parse("abc"));
        Assert.Null(NumericSpinnerLogic.Parse("  "));
    }

    [Fact]
    public void Format_UsesPlacesOrFormatString()
    {
        var invariant = System.Globalization.CultureInfo.InvariantCulture;
        Assert.Equal("12.50", NumericSpinnerLogic.Format(12.5m, 2, null, invariant));
        Assert.Equal("1,200.00", NumericSpinnerLogic.Format(1200m, 2, "N2", invariant));
    }

    [Fact]
    public void DeriveDecimalPlaces_FromIncrement()
    {
        Assert.Equal(2, NumericSpinnerLogic.DeriveDecimalPlaces(0.01m, -1));
        Assert.Equal(0, NumericSpinnerLogic.DeriveDecimalPlaces(1m, -1));
        Assert.Equal(3, NumericSpinnerLogic.DeriveDecimalPlaces(1m, 3));
    }
}
