using VCWpfControls.Conditions;
using VCWpfControls.Grid.Data;
using VCWpfControls.Localization;
using Xunit;

namespace VCWpfControls.Core.Tests.Conditions;

/// <summary>
/// Condition editor stage A (UI-free core): the model, schema inference, evaluation
/// (delegating to the grid filter semantics) and the text language round-trip.
/// </summary>
public class ConditionCoreTests
{
    private sealed record Person(string Name, string Dept, decimal Salary, DateTime Hired, bool Active)
    {
        public static Person Of(string name, string dept, decimal salary, int year, bool active = true)
            => new(name, dept, salary, new DateTime(year, 1, 31), active);
    }

    private static readonly Person Alice = Person.Of("Alice", "IT", 900m, 2020);
    private static readonly Person Bob = Person.Of("Bob", "HR", 1200m, 2018, active: false);

    // --- Schema ---

    [Fact]
    public void Schema_FromType_IncludesSupportedTypes_HonorsDisplayAttribute()
    {
        var schema = ConditionSchema.FromType(typeof(Person));
        Assert.Contains(schema.Fields, f => f.Name == nameof(Person.Name));
        Assert.Contains(schema.Fields, f => f.Name == nameof(Person.Salary));
        Assert.Contains(schema.Fields, f => f.Name == nameof(Person.Hired));
        Assert.Contains(schema.Fields, f => f.Name == nameof(Person.Active));
        Assert.Equal(5, schema.Fields.Count); // incl. Dept
        Assert.NotNull(schema.Find("salary")); // case-insensitive lookup
    }

    private sealed record Shift(string Site, System.DayOfWeek Day)
    {
    }

    [Fact]
    public void Schema_FromType_EnumField_CarriesSuggestions()
    {
        var schema = ConditionSchema.FromType(typeof(Shift));
        Assert.Equal(2, schema.Fields.Count);
        var day = schema.Find(nameof(Shift.Day));
        Assert.NotNull(day);
        Assert.NotNull(day!.Suggestions);
    }

    // --- Evaluator ---

    [Fact]
    public void Evaluate_AndOrGroups()
    {
        // (Salary > 1000 And Dept = 'HR') Or Name = 'Alice'
        var tree = new ConditionGroup(ConditionGroupOperator.Or)
        {
            Children =
            {
                new ConditionGroup(ConditionGroupOperator.And)
                {
                    Children =
                    {
                        new ConditionExpression(nameof(Person.Salary), GridFilterOperator.GreaterThan, 1000m),
                        new ConditionExpression(nameof(Person.Dept), GridFilterOperator.Equals, "HR"),
                    },
                },
                new ConditionExpression(nameof(Person.Name), GridFilterOperator.Equals, "Alice"),
            },
        };

        Assert.True(ConditionEvaluator.Matches(Alice, tree));
        Assert.True(ConditionEvaluator.Matches(Bob, tree));
        Assert.False(ConditionEvaluator.Matches(Person.Of("Carol", "Sales", 500m, 2021), tree));

        // And requires ALL: one true child must not be enough (catches And/Or mixups).
        var andTree = new ConditionGroup(ConditionGroupOperator.And)
        {
            Children =
            {
                new ConditionExpression(nameof(Person.Salary), GridFilterOperator.GreaterThan, 500m),
                new ConditionExpression(nameof(Person.Dept), GridFilterOperator.Equals, "IT"),
            },
        };
        Assert.True(ConditionEvaluator.Matches(Alice, andTree));          // both hold
        Assert.False(ConditionEvaluator.Matches(Person.Of("Dave", "Sales", 900m, 2021), andTree)); // salary holds, dept does not
    }

    [Fact]
    public void Evaluate_OperatorSemantics_MatchTheGridEngine()
    {
        Assert.True(ConditionEvaluator.Matches(Alice, new ConditionExpression(nameof(Person.Salary), GridFilterOperator.Between, 500m, 1000m)));
        Assert.False(ConditionEvaluator.Matches(Alice, new ConditionExpression(nameof(Person.Name), GridFilterOperator.StartsWith, "bob")));
        Assert.True(ConditionEvaluator.Matches(Bob, new ConditionExpression(nameof(Person.Active), GridFilterOperator.Equals, false)));
        Assert.True(ConditionEvaluator.Matches(Alice, new ConditionExpression(nameof(Person.Dept), GridFilterOperator.In, new List<object> { "IT", "HR" })));
        Assert.False(ConditionEvaluator.Matches(Alice, new ConditionExpression(nameof(Person.Name), GridFilterOperator.IsNull)));
    }

    [Fact]
    public void Evaluate_EmptyGroups_VacuousTruth()
    {
        Assert.True(ConditionEvaluator.Matches(Alice, new ConditionGroup(ConditionGroupOperator.And)));
        Assert.False(ConditionEvaluator.Matches(Alice, new ConditionGroup(ConditionGroupOperator.Or)));
        Assert.True(ConditionEvaluator.Matches(Alice, null));
    }

    [Fact]
    public void Evaluate_WhereAndMatchesAll()
    {
        var people = new[] { Alice, Bob };
        var wellPaid = new ConditionExpression(nameof(Person.Salary), GridFilterOperator.GreaterThanOrEqual, 1000m);
        Assert.Single(ConditionEvaluator.Where(people, wellPaid));
        Assert.False(ConditionEvaluator.MatchesAll(people, wellPaid));
        Assert.True(ConditionEvaluator.MatchesAll(people, null));
    }

    // --- Text language ---

    [Fact]
    public void Parse_SimpleOperators()
    {
        var node = (ConditionExpression)ConditionParser.Parse("[Salary] > 1000");
        Assert.Equal(nameof(Person.Salary), node.FieldName);
        Assert.Equal(GridFilterOperator.GreaterThan, node.OperatorKind);
        Assert.Equal(1000, node.Value);
    }

    [Fact]
    public void Parse_AndBindsTighterThanOr()
    {
        // a Or b And c  =>  Or(a, And(b, c))
        var root = (ConditionGroup)ConditionParser.Parse("[Name] = 'x' Or [Dept] = 'y' And [Salary] > 1");
        Assert.Equal(ConditionGroupOperator.Or, root.OperatorKind);
        Assert.Equal(2, root.Children.Count);
        var andGroup = (ConditionGroup)root.Children[1];
        Assert.Equal(ConditionGroupOperator.And, andGroup.OperatorKind);
        Assert.Equal(2, andGroup.Children.Count);
    }

    [Fact]
    public void Parse_ParenthesesMakeGroups_RoundTrips()
    {
        var text = "[Salary] > 500 And ([Dept] = 'IT' Or [Active] = false)";
        var root = (ConditionGroup)ConditionParser.Parse(text);
        Assert.Equal(ConditionGroupOperator.And, root.OperatorKind);

        var formatted = ConditionParser.Format(root);
        Assert.Equal(text, formatted); // canonical round-trip

        // And the reparsed tree evaluates the same.
        var reparsed = (ConditionGroup)ConditionParser.Parse(formatted);
        Assert.True(ConditionEvaluator.Matches(Alice, reparsed));
        Assert.True(ConditionEvaluator.Matches(Bob, reparsed)); // 1200 > 500 And (HR Or inactive)
        Assert.False(ConditionEvaluator.Matches(Person.Of("Carol", "Sales", 400m, 2021), reparsed));
    }

    [Fact]
    public void Parse_BetweenCommaPair_AndInList()
    {
        var between = (ConditionExpression)ConditionParser.Parse("[Salary] Between 500, 1000");
        Assert.Equal(GridFilterOperator.Between, between.OperatorKind);
        Assert.Equal(500, between.Value);
        Assert.Equal(1000, between.Value2);

        var inList = (ConditionExpression)ConditionParser.Parse("[Dept] In ('IT', 'HR', 'Sales')");
        Assert.Equal(GridFilterOperator.In, inList.OperatorKind);
        var list = Assert.IsType<List<object>>(inList.Value);
        Assert.Equal(3, list.Count);
    }

    [Fact]
    public void Parse_Literals_StringsNumbersBoolsNullsDates_EscapedQuotes()
    {
        Assert.Equal("O'Brien", ((ConditionExpression)ConditionParser.Parse("[Name] = 'O''Brien'")).Value);
        Assert.Equal(true, ((ConditionExpression)ConditionParser.Parse("[Active] = true")).Value);
        Assert.Null(((ConditionExpression)ConditionParser.Parse("[Name] IsNull")).Value);
        var dateExpr = Assert.IsType<ConditionExpression>(ConditionParser.Parse("[Hired] = #2020-01-31#"));
        Assert.Equal(new DateTime(2020, 1, 31), dateExpr.Value);
        Assert.Equal(42, ((ConditionExpression)ConditionParser.Parse("[Salary] = 42")).Value);
    }

    [Theory]
    [InlineData("[Name")]
    [InlineData("[Name] ~ 'x'")]
    [InlineData("[Name] = unquoted")]
    [InlineData("[Salary] Between 1")]
    [InlineData("[Dept] In ('IT'")]
    [InlineData("[Name] = 'x' Extra")]
    [InlineData("")]
    [InlineData("([Name] = 'x'")]
    public void Parse_Errors_CarryPosition(string text)
    {
        var exception = Assert.Throws<ConditionParseException>(() => ConditionParser.Parse(text));
        Assert.True(exception.Position >= 0);
        Assert.Contains("position", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_RoundTrips_NestedTree()
    {
        var tree = new ConditionGroup(ConditionGroupOperator.Or)
        {
            Children =
            {
                new ConditionExpression(nameof(Person.Name), GridFilterOperator.StartsWith, "A"),
                new ConditionGroup(ConditionGroupOperator.And)
                {
                    Children =
                    {
                        new ConditionExpression(nameof(Person.Dept), GridFilterOperator.In, new List<object> { "IT", "HR" }),
                        new ConditionExpression(nameof(Person.Hired), GridFilterOperator.LessThan, new DateTime(2019, 6, 1)),
                    },
                },
            },
        };

        var text = ConditionParser.Format(tree);
        var reparsed = ConditionParser.Parse(text);
        Assert.Equal(text, ConditionParser.Format(reparsed)); // stable format
        Assert.True(ConditionEvaluator.Matches(Alice, reparsed));
    }

    [Fact]
    public void KeywordMatching_CaseInsensitive_AndNotPartOfIdentifiers()
    {
        var root = (ConditionGroup)ConditionParser.Parse("[Name] = 'x' and [Dept] = 'y' or [Salary] > 0");
        Assert.Equal(ConditionGroupOperator.Or, root.OperatorKind);
    }
}
