using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using VCWpfControls.Grid.Data;

namespace VCWpfControls.Grid;

/// <summary>
/// The items view the <see cref="GridControl"/> presents to its ItemsControl: the data
/// engine's rows plus (optionally) the add-row placeholder appended at the end. The
/// placeholder must NOT live inside the engine's own view — the engine's incremental
/// index bookkeeping counts data rows only, and a sentinel inside it desynchronizes the
/// WPF collection view ("added item does not appear at index" from ListCollectionView).
/// Indices of engine rows are identical here because the placeholder is strictly last.
/// </summary>
internal sealed class GridItemsView : IList<GridRowInfo>, IReadOnlyList<GridRowInfo>, IList, INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly GridRowCollection _inner;
    private GridDataRow? _placeholder;

    /// <inheritdoc />
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    public GridItemsView(GridRowCollection inner)
    {
        _inner = inner;
        _inner.CollectionChanged += OnInnerChanged;
    }

    /// <summary>The add-row placeholder row, when shown.</summary>
    public GridRowInfo? Placeholder => _placeholder;

    /// <summary>Shows or hides the placeholder at the end of the view.</summary>
    public void SetPlaceholder(GridDataRow? placeholder)
    {
        if (ReferenceEquals(_placeholder, placeholder))
        {
            return;
        }

        if (_placeholder is not null)
        {
            var old = _placeholder;
            _placeholder = null;
            Changed(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, old, _inner.Count));
        }

        if (placeholder is not null)
        {
            _placeholder = placeholder;
            Changed(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, placeholder, _inner.Count));
        }
    }

    private void OnInnerChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // The engine never sees the placeholder, so its indices carry over 1:1.
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
            case NotifyCollectionChangedAction.Remove:
            case NotifyCollectionChangedAction.Move:
            case NotifyCollectionChangedAction.Replace:
                Changed(e);
                break;
            default:
                Changed(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                break;
        }
    }

    private void Changed(NotifyCollectionChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        CollectionChanged?.Invoke(this, e);
    }

    /// <inheritdoc />
    public int Count => _inner.Count + (_placeholder is null ? 0 : 1);

    /// <inheritdoc />
    public GridRowInfo this[int index]
    {
        get
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return index < _inner.Count ? _inner[index] : _placeholder!;
        }
    }

    /// <inheritdoc />
    public IEnumerator<GridRowInfo> GetEnumerator()
    {
        foreach (var row in _inner)
        {
            yield return row;
        }

        if (_placeholder is not null)
        {
            yield return _placeholder;
        }
    }

    /// <inheritdoc />
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    // Read-only plumbing: mutation goes through the engine (or SetPlaceholder).

    bool ICollection<GridRowInfo>.IsReadOnly => true;

    bool IList.IsReadOnly => true;

    bool IList.IsFixedSize => false;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    GridRowInfo IList<GridRowInfo>.this[int index]
    {
        get => this[index];
        set => throw new NotSupportedException();
    }

    object? IList.this[int index]
    {
        get => this[index];
        set => throw new NotSupportedException();
    }

    void ICollection<GridRowInfo>.Add(GridRowInfo item) => throw new NotSupportedException();

    int IList.Add(object? value) => throw new NotSupportedException();

    void IList<GridRowInfo>.Insert(int index, GridRowInfo item) => throw new NotSupportedException();

    void IList.Insert(int index, object? value) => throw new NotSupportedException();

    bool ICollection<GridRowInfo>.Remove(GridRowInfo item) => throw new NotSupportedException();

    void IList.Remove(object? value) => throw new NotSupportedException();

    void IList<GridRowInfo>.RemoveAt(int index) => throw new NotSupportedException();

    void IList.RemoveAt(int index) => throw new NotSupportedException();

    void ICollection<GridRowInfo>.Clear() => throw new NotSupportedException();

    void IList.Clear() => throw new NotSupportedException();

    void ICollection<GridRowInfo>.CopyTo(GridRowInfo[] array, int arrayIndex)
    {
        foreach (var row in this)
        {
            array[arrayIndex++] = row;
        }
    }

    void ICollection.CopyTo(Array array, int index)
    {
        foreach (var row in this)
        {
            array.SetValue(row, index++);
        }
    }

    bool IList.Contains(object? value) => value is GridRowInfo row && Contains(row);

    int IList.IndexOf(object? value) => value is GridRowInfo row ? IndexOf(row) : -1;

    /// <inheritdoc />
    public bool Contains(GridRowInfo item) => IndexOf(item) >= 0;

    int IList<GridRowInfo>.IndexOf(GridRowInfo item) => IndexOf(item);

    /// <inheritdoc />
    public int IndexOf(GridRowInfo item)
    {
        for (var i = 0; i < Count; i++)
        {
            if (ReferenceEquals(this[i], item))
            {
                return i;
            }
        }

        return -1;
    }
}
