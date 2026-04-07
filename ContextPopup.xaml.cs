using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfColor = System.Windows.Media.Color;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfCursors = System.Windows.Input.Cursors;

namespace DevBar;

public partial class ContextPopup : Window
{
    public event Action? PreferencesClicked;
    public event Action? RefreshClicked;
    public event Action? QuitClicked;

    public ContextPopup()
    {
        InitializeComponent();
        AddItem("\u2699  Preferences", () => PreferencesClicked?.Invoke());
        AddItem("\u21BB  Refresh", () => RefreshClicked?.Invoke());
        AddSeparator();
        AddItem("\u2716  Quit", () => QuitClicked?.Invoke());
    }

    private void AddItem(string label, Action action)
    {
        var btn = new System.Windows.Controls.Button
        {
            Content = label,
            Background = WpfBrushes.Transparent,
            Foreground = new SolidColorBrush(WpfColor.FromRgb(0xCC, 0xCC, 0xCC)),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(12, 7, 12, 7),
            HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left,
            Cursor = WpfCursors.Hand,
            FontSize = 13,
        };
        btn.Click += (_, _) => { Close(); action(); };

        var style = new Style(typeof(System.Windows.Controls.Button));
        style.Setters.Add(new Setter(TemplateProperty, CreateMenuItemTemplate()));
        btn.Style = style;

        MenuPanel.Children.Add(btn);
    }

    private void AddSeparator()
    {
        MenuPanel.Children.Add(new Separator
        {
            Background = new SolidColorBrush(WpfColor.FromRgb(0x3F, 0x3F, 0x46)),
            Margin = new Thickness(8, 2, 8, 2),
            Height = 1,
        });
    }

    private static ControlTemplate CreateMenuItemTemplate()
    {
        var template = new ControlTemplate(typeof(System.Windows.Controls.Button));
        var factory = new FrameworkElementFactory(typeof(System.Windows.Controls.Border));
        factory.SetValue(System.Windows.Controls.Border.BackgroundProperty, WpfBrushes.Transparent);
        factory.SetValue(System.Windows.Controls.Border.CornerRadiusProperty, new CornerRadius(4));
        factory.Name = "border";

        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(FrameworkElement.MarginProperty, new Thickness(12, 7, 12, 7));
        content.SetValue(FrameworkElement.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Left);
        factory.AppendChild(content);

        template.VisualTree = factory;

        var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hoverTrigger.Setters.Add(new Setter(System.Windows.Controls.Border.BackgroundProperty,
            new SolidColorBrush(WpfColor.FromRgb(0x3E, 0x3E, 0x42)), "border"));
        template.Triggers.Add(hoverTrigger);

        return template;
    }
}
