using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace VCWpfControls.Grid.Data;

/// <summary>
/// The observable row collection exposed as the grid's view. Supports replacing the whole
/// content with a single <see cref="NotifyCollectionChangedAction.Reset"/> notification
/// (cheap for the items control) while still raising granular Add/Remove notifications for
/// incremental source changes. UI-framework free.
/// </summary>
public sealed class GridRowCollection : IList<GridRowInfo>, IReadOnlyList<GridRowInfo>, IList, INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly List<GridRowInfo> _items = new();

    /// <inheritdoc />
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Number of rows.</summary>
    public int Count => _items.Count;

    bool ICollection<GridRowInfo>.IsReadOnly => false;

    bool IList.IsFixedSize => false;

    bool IList.IsReadOnly => false;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => ((ICollection)_items).SyncRoot;

    /// <summary>Replaces the whole content and raises a single Reset notification.</summary>
    public void ReplaceAll(IEnumerable<GridRowInfo> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        _items.Clear();
        _items.AddRange(rows);
        OnReset();
    }

    /// <inheritdoc />
    public void Insert(int index, GridRowInfo item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _items.Insert(index, item);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
    }

    /// <summary>Removes the row at the index and raises a Remove notification.</summary>
    public void RemoveAt(int index)
    {
        var item = _items[index];
        _items.RemoveAt(index);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
    }

    /// <inheritdoc />
    public GridRowInfo this[int index]
    {
        get => _items[index];
        set => throw new NotSupportedException("Row replacement is not supported; rebuild the view instead.");
    }

    /// <inheritdoc />
    public void Add(GridRowInfo item) => Insert(_items.Count, item);

    /// <inheritdoc />
    public void Clear() => ReplaceAll(Array.Empty<GridRowInfo>());

    /// <inheritdoc />
    public bool Contains(GridRowInfo item) => _items.Contains(item);

    /// <inheritdoc />
    public void CopyTo(GridRowInfo[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

    /// <inheritdoc />
    public IEnumerator<GridRowInfo> GetEnumerator() => _items.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public int IndexOf(GridRowInfo item) => _items.IndexOf(item);

    /// <inheritdoc />
    public bool Remove(GridRowInfo item)
    {
        var index = _items.IndexOf(item);
        if (index < 0)
        {
            return false;
        }

        RemoveAt(index);
        return true;
    }

    private void OnReset()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    private void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        CollectionChanged?.Invoke(this, args);
    }

    object? IList.this[int index]
    {
        get => _items[index];
        set => throw new NotSupportedException("Row replacement is not supported; rebuild the view instead.");
    }

    int IList.Add(object? value)
    {
        Add((GridRowInfo)value!);
        return Count - 1;
    }

    bool IList.Contains(object? value) => value is GridRowInfo row && Contains(row);

    int IList.IndexOf(object? value) => value is GridRowInfo row ? IndexOf(row) : -1;

    void IList.Insert(int index, object? value) => Insert(index, (GridRowInfo)value!);

    void IList.Remove(object? value)
    {
        if (value is GridRowInfo row)
        {
            Remove(row);
        }
    }

    void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
}
