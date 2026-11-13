using System.Windows.Controls;

namespace VCWpfControls.Demo.Views;

/// <summary>Editors sample: masked inputs and numeric spinners.</summary>
public partial class EditorsSampleView : UserControl
{
    /// <summary>Creates the view.</summary>
    public EditorsSampleView()
    {
        InitializeComponent();
        this.SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, "EditorsSample");
        NameComplete.ItemsSource = new[]
        {
            "Alice Johnson", "Albert Wagner", "Amelia Clarke", "Boris Novak",
            "Clara Oswald", "Dmitri Ivanov", "Elena Petrova", "Frederik Zimen",
        };
    }
}
