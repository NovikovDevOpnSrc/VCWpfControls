using System.Windows;

namespace VCWpfControls.Demo.Services;

/// <summary>
/// The canonical data-context proxy (a Freezable lives in page Resources and inherits the
/// page's DataContext even though grid columns are DependencyObjects outside the visual
/// tree — a RelativeSource AncestorType binding on a column can never resolve there).
/// Usage: ItemsSource="{Binding Departments, Source={StaticResource PageProxy}}".
/// </summary>
public sealed class BindingProxy : Freezable
{
    /// <summary>Identifies the <see cref="Data"/> dependency property.</summary>
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(object), typeof(BindingProxy), new PropertyMetadata(null));

    /// <summary>The forwarded page DataContext (or any subtree value).</summary>
    public object? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    /// <inheritdoc />
    protected override Freezable CreateInstanceCore() => new BindingProxy();
}
