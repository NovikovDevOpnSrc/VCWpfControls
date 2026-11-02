using System.Windows.Controls;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.PropertyGrid;
using PropertyGridControl = VCWpfControls.PropertyGrid.PropertyGrid;
using Xunit;

namespace VCWpfControls.Control.Tests;

public class PropertyGridTests
{
    private sealed class Sample
    {
        public string Name { get; set; } = "Alice";

        public int Age { get; set; } = 30;

        public bool Active { get; set; } = true;

        public DayOfWeek Day { get; set; } = DayOfWeek.Friday;
    }

    [StaFact]
    public void SelectedObject_BuildsCategoryRowsWithEditors()
    {
        var propertyGrid = new PropertyGridControl();
        var window = TestWindowHost.Show(propertyGrid);
        propertyGrid.SelectedObject = new Sample();
        TestWindowHost.Pump();

        try
        {
            var host = FindItemsHost(propertyGrid);
            Assert.NotNull(host);
            Assert.True(host!.Children.Count > 0, "at least one category expander expected");

            var expanders = host.Children.OfType<Expander>().ToList();
            Assert.True(expanders.Count > 0);
            var allText = string.Join(" ", expanders.SelectMany(e => FindAllText(e)));
            Assert.Contains("Name", allText);
            Assert.Contains("Age", allText);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Search_FiltersDisplayedProperties()
    {
        var propertyGrid = new PropertyGridControl();
        var window = TestWindowHost.Show(propertyGrid);
        propertyGrid.SelectedObject = new Sample();
        TestWindowHost.Pump();

        try
        {
            var searchBox = (TextBox)propertyGrid.Template.FindName("PART_SearchBox", propertyGrid);
            searchBox.Text = "age";
            TestWindowHost.Pump();

            var allText = CurrentText(propertyGrid);
            Assert.Contains("Age", allText);
            Assert.DoesNotContain("Name", allText);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void PropertyValueChanged_RaisedOnEdit()
    {
        var propertyGrid = new PropertyGridControl();
        var window = TestWindowHost.Show(propertyGrid);
        var sample = new Sample();
        propertyGrid.SelectedObject = sample;
        TestWindowHost.Pump();

        try
        {
            var raised = 0;
            propertyGrid.PropertyValueChanged += (_, _) => raised++;

            var textBox = FindEditor<TextBox>(propertyGrid, item => item.Name == "Name");
            Assert.NotNull(textBox);
            textBox!.Text = "Bob";

            // Text editors commit on Enter and on focus loss.
            textBox.RaiseEvent(new System.Windows.Input.KeyEventArgs(
                System.Windows.Input.Keyboard.PrimaryDevice,
                System.Windows.PresentationSource.FromVisual(textBox)!,
                0,
                System.Windows.Input.Key.Enter)
            {
                RoutedEvent = System.Windows.UIElement.KeyDownEvent,
            });
            TestWindowHost.Pump();

            Assert.True(raised >= 1, "PropertyValueChanged must fire after a committed edit");
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Inpc_SourceChangesRefreshEditors()
    {
        var propertyGrid = new PropertyGridControl();
        var window = TestWindowHost.Show(propertyGrid);
        var sample = new InpcSample();
        propertyGrid.SelectedObject = sample;
        TestWindowHost.Pump();

        try
        {
            var textBox = FindEditor<TextBox>(propertyGrid, item => item.Name == "Title");
            Assert.NotNull(textBox);
            Assert.Equal("before", textBox!.Text);

            sample.Title = "after";
            TestWindowHost.Pump();
            Assert.Equal("after", textBox.Text);
        }
        finally
        {
            window.Close();
        }
    }

    private sealed class InpcSample : System.ComponentModel.INotifyPropertyChanged
    {
        private string _title = "before";

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Title)));
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }

    private static StackPanel? FindItemsHost(PropertyGridControl propertyGrid)
        => propertyGrid.Template.FindName("PART_ItemsHost", propertyGrid) as StackPanel;

    private static string CurrentText(PropertyGridControl propertyGrid)
    {
        var host = FindItemsHost(propertyGrid);
        return host is null ? string.Empty : string.Join(" ", FindAllText(host));
    }

    private static IEnumerable<string> FindAllText(System.Windows.DependencyObject parent)
    {
        var children = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < children; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is TextBlock text)
            {
                yield return text.Text;
            }

            foreach (var nested in FindAllText(child))
            {
                yield return nested;
            }
        }
    }

    private static TEditor? FindEditor<TEditor>(PropertyGridControl propertyGrid, Func<PropertyItem, bool> match)
        where TEditor : System.Windows.FrameworkElement
    {
        var host = FindItemsHost(propertyGrid);
        if (host is null)
        {
            return null;
        }

        foreach (var editor in FindEditorsRecursive(host))
        {
            if (editor.Item is { } item && match(item))
            {
                return editor.Content is TEditor typed ? typed : null;
            }
        }

        return null;
    }

    private static IEnumerable<PropertyGrid.Primitives.PropertyItemEditor> FindEditorsRecursive(System.Windows.DependencyObject parent)
    {
        var children = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < children; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is PropertyGrid.Primitives.PropertyItemEditor editor)
            {
                yield return editor;
            }

            foreach (var nested in FindEditorsRecursive(child))
            {
                yield return nested;
            }
        }
    }
}
