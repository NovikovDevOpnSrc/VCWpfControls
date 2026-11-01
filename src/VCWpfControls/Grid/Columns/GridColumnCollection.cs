using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace VCWpfControls.Grid.Columns;

/// <summary>
/// An observable collection of grid columns. The owning grid is notified whenever columns
/// are added, removed or replaced.
/// </summary>
public sealed class GridColumnCollection : ObservableCollection<GridColumnBase>
{
    /// <summary>Notifies about structural changes (add/remove/replace/clear/reset).</summary>
    internal event EventHandler<NotifyCollectionChangedEventArgs>? StructureChanged;

    /// <inheritdoc />
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnCollectionChanged(e);
        StructureChanged?.Invoke(this, e);
    }
}
