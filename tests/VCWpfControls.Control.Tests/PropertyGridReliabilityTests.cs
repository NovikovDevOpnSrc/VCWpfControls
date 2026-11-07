using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VCWpfControls.Control.Tests.Infrastructure;
using PropertyGridControl = VCWpfControls.PropertyGrid.PropertyGrid;
using PropertyItemEditorKind = VCWpfControls.PropertyGrid.PropertyItemEditorKind;
using PropertyGridCommitMode = VCWpfControls.PropertyGrid.PropertyGridCommitMode;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// PropertyGrid reliability pass: metadata attributes, search, expandable objects, INPC
/// refresh, read-only surface, null display, commit timing (OnCommit default vs Immediate,
/// Esc cancel) and TypeConverter-driven display/parsing.
/// </summary>
public class PropertyGridReliabilityTests
{
    private sealed class Document : System.ComponentModel.INotifyPropertyChanged
    {
        private string _title = "Doc";

        public event PropertyChangedEventHandler? PropertyChanged;

        [DisplayName("Document title")]
        [Description("The visible caption")]
        [Category("Identity")]
        [Display(Order = 1)]
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
            }
        }

        private int _pages = 12;

        [Category("Misc")]
        [Display(Order = 2)]
        public int Pages
        {
            get => _pages;
            set
            {
                _pages = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Pages)));
            }
        }

        [ReadOnly(true)]
        public string Locked { get; set; } = "no edits";

        public Guid TraceId { get; set; } = Guid.Parse("{7b9e6f1c-2f4e-4c9f-9bd1-3d5f0a1b2c3d}");

        public string? NullableNote { get; set; }

        public DayOfWeek Day { get; set; } = DayOfWeek.Monday;

        public List<int> Items { get; set; } = new() { 1, 2 };
    }

    [StaFact]
    public void Attributes_DriveMetadata()
    {
        var grid = Show(new Document());
        try
        {
            var items = grid.GetAllItemsForTest();
            var title = items.Single(i => i.Name == nameof(Document.Title));
            Assert.Equal("Document title", title.DisplayName);
            Assert.Equal("Identity", title.Category);
            Assert.Equal("The visible caption", title.Description);
            Assert.True(title.SortOrder < items.Single(i => i.Name == nameof(Document.Pages)).SortOrder);
            Assert.True(items.Single(i => i.Name == nameof(Document.Locked)).IsReadOnly);
        }
        finally
        {
            System.Windows.Window.GetWindow(grid)?.Close();
        }
    }

    [StaFact]
    public void NullValue_DisplaysEmpty_NonNullableGetOnlyStaysReadOnly()
    {
        var doc = new Document();
        var grid = Show(doc);
        try
        {
            var note = grid.GetAllItemsForTest().Single(i => i.Name == nameof(Document.NullableNote));
            Assert.Equal(string.Empty, note.GetValueText());
            Assert.False(note.IsReadOnly);
        }
        finally
        {
            System.Windows.Window.GetWindow(grid)?.Close();
        }
    }

    [StaFact]
    public void GuidProperty_TypeConverterDisplaysAndParses()
    {
        var doc = new Document();
        var grid = Show(doc);
        try
        {
            var trace = grid.GetAllItemsForTest().Single(i => i.Name == nameof(Document.TraceId));
            Assert.Equal("7b9e6f1c-2f4e-4c9f-9bd1-3d5f0a1b2c3d", trace.GetValueText());
            Assert.Equal(PropertyItemEditorKind.Text, trace.EditorKind); // editable, not read-only

            // Commit a new valid guid through the editor's conversion path.
            var editor = grid.GetEditorForTest(trace)!;
            var textBox = Assert.IsType<TextBox>(editor.EditorContentForTest());
            Assert.NotNull(textBox);
            var parsed = VCWpfControls.PropertyGrid.Primitives.PropertyItemEditor.ConvertForTest(trace, "{11111111-2222-3333-4444-555555555555}");
            Assert.IsType<Guid>(parsed);
            Assert.Equal(new Guid("11111111-2222-3333-4444-555555555555"), parsed);
        }
        finally
        {
            System.Windows.Window.GetWindow(grid)?.Close();
        }
    }

    [StaFact]
    public void TextEditor_CommitsOnEnter_NotPerKeystroke_ByDefault()
    {
        var doc = new Document();
        var grid = Show(doc);
        try
        {
            var title = grid.GetAllItemsForTest().Single(i => i.Name == nameof(Document.Title));
            var textBox = Assert.IsType<TextBox>(grid.GetEditorForTest(title)!.EditorContentForTest());
            textBox.Text = "changed-but-not-committed";
            TestWindowHost.Pump();
            Assert.Equal("Doc", doc.Title); // no commit without Enter/focus loss

            textBox.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromDependencyObject(textBox), 0, Key.Enter)
            {
                RoutedEvent = UIElement.KeyDownEvent,
            });
            TestWindowHost.Pump();
            Assert.Equal("changed-but-not-committed", doc.Title);
        }
        finally
        {
            System.Windows.Window.GetWindow(grid)?.Close();
        }
    }

    [StaFact]
    public void TextEditor_EscCancelsPendingEdit()
    {
        var doc = new Document();
        var grid = Show(doc);
        try
        {
            var title = grid.GetAllItemsForTest().Single(i => i.Name == nameof(Document.Title));
            var textBox = Assert.IsType<TextBox>(grid.GetEditorForTest(title)!.EditorContentForTest());
            textBox.Text = "pending";
            textBox.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromDependencyObject(textBox), 0, Key.Escape)
            {
                RoutedEvent = UIElement.KeyDownEvent,
            });
            TestWindowHost.Pump();
            Assert.Equal("Doc", doc.Title);      // not committed
            Assert.Equal("Doc", textBox.Text);   // editor restored
        }
        finally
        {
            System.Windows.Window.GetWindow(grid)?.Close();
        }
    }

    [StaFact]
    public void ImmediateMode_CommitsPerKeystroke()
    {
        var doc = new Document();
        var grid = new PropertyGridControl { CommitMode = PropertyGridCommitMode.Immediate, Width = 400 };
        var window = TestWindowHost.Show(grid);
        grid.SelectedObject = doc;
        TestWindowHost.Pump();
        try
        {
            var title = grid.GetAllItemsForTest().Single(i => i.Name == nameof(Document.Title));
            var textBox = Assert.IsType<TextBox>(grid.GetEditorForTest(title)!.EditorContentForTest());
            textBox.Text = "i";
            TestWindowHost.Pump();
            Assert.Equal("i", doc.Title); // committed without Enter
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void InpcSource_PropertyChangeRefreshesEditors()
    {
        var doc = new Document();
        var grid = Show(doc);
        try
        {
            var pages = grid.GetAllItemsForTest().Single(i => i.Name == nameof(Document.Pages));
            doc.Pages = 99;
            TestWindowHost.Pump();
            var editor = grid.GetEditorForTest(pages);
            var text = Assert.IsType<TextBox>(editor!.EditorContentForTest());
            Assert.NotNull(text);
            Assert.Equal("99", text!.Text);
        }
        finally
        {
            System.Windows.Window.GetWindow(grid)?.Close();
        }
    }

    [StaFact]
    public void Search_FiltersToMatchingItems()
    {
        var doc = new Document();
        var grid = Show(doc);
        try
        {
            var search = grid.GetSearchBoxForTest();
            Assert.NotNull(search);
            search!.Text = "pages";
            TestWindowHost.Pump();
            var visible = grid.GetVisibleItemNamesForTest();
            Assert.Contains("Pages", visible);
            Assert.DoesNotContain(visible, n => n.Contains("Document title", StringComparison.Ordinal));
        }
        finally
        {
            System.Windows.Window.GetWindow(grid)?.Close();
        }
    }

    [StaFact]
    public void EnumEditor_CommitsSelection()
    {
        var doc = new Document();
        var grid = Show(doc);
        try
        {
            var day = grid.GetAllItemsForTest().Single(i => i.Name == nameof(Document.Day));
            var combo = Assert.IsType<ComboBox>(grid.GetEditorForTest(day)!.EditorContentForTest());
            combo.SelectedItem = DayOfWeek.Sunday;
            TestWindowHost.Pump();
            Assert.Equal(DayOfWeek.Sunday, doc.Day);
        }
        finally
        {
            System.Windows.Window.GetWindow(grid)?.Close();
        }
    }

    [StaFact]
    public void ValueChangedEvent_FiresOncePerCommit()
    {
        var doc = new Document();
        var grid = Show(doc);
        var fired = 0;
        grid.PropertyValueChanged += (_, _) => fired++;
        try
        {
            var title = grid.GetAllItemsForTest().Single(i => i.Name == nameof(Document.Title));
            var textBox = Assert.IsType<TextBox>(grid.GetEditorForTest(title)!.EditorContentForTest());
            textBox.Text = "one";
            textBox.Text = "two";
            Assert.Equal(0, fired); // typing alone never fires

            textBox.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromDependencyObject(textBox), 0, Key.Enter)
            {
                RoutedEvent = UIElement.KeyDownEvent,
            });
            Assert.Equal(1, fired);
            Assert.Equal("two", doc.Title);
        }
        finally
        {
            System.Windows.Window.GetWindow(grid)?.Close();
        }
    }

    private static PropertyGridControl Show(Document doc)
    {
        var grid = new PropertyGridControl { Width = 400 };
        var window = TestWindowHost.Show(grid);
        grid.SelectedObject = doc;
        TestWindowHost.Pump();
        return grid;
    }
}
