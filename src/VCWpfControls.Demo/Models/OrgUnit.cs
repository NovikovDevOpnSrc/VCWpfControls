namespace VCWpfControls.Demo.Models;

/// <summary>Hierarchical sample model for the TreeListView demo.</summary>
public class OrgUnit
{
    /// <summary>Creates a unit.</summary>
    public OrgUnit(string name, int headcount, string lead)
    {
        Name = name;
        Headcount = headcount;
        Lead = lead;
    }

    /// <summary>Unit name.</summary>
    public string Name { get; set; }

    /// <summary>People in the unit (direct).</summary>
    public int Headcount { get; set; }

    /// <summary>Unit lead.</summary>
    public string Lead { get; set; }

    /// <summary>Child units.</summary>
    public List<OrgUnit> Children { get; } = new();
}
