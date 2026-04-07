using System.Windows;
using System.Windows.Controls;
using WpfColor = System.Windows.Media.Color;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfCursors = System.Windows.Input.Cursors;
using System.Windows.Media;

namespace DevBar;

public partial class PopupWindow : Window
{
    public event Action<string>? ItemClicked;

    public PopupWindow(DevBarResult result)
    {
        InitializeComponent();

        int itemCount = 0;
        var sortedCategories = result.Data
            .Where(kv => kv.Value.Count > 0)
            .OrderBy(kv =>
                result.Metadata.Display.TryGetValue(kv.Key, out var d) ? d.Priority : 99)
            .ThenBy(kv => kv.Key);

        foreach (var (category, items) in sortedCategories)
        {
            var display = result.Metadata.Display.GetValueOrDefault(category);
            var title = display?.Title ?? category;

            var header = new TextBlock
            {
                Text = title,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(0xCC, 0xCC, 0xCC)),
                Margin = new Thickness(4, itemCount > 0 ? 10 : 2, 4, 4),
            };
            ItemsPanel.Children.Add(header);

            foreach (var item in items)
            {
                var btn = new System.Windows.Controls.Button
                {
                    Content = item.Title,
                    Tag = item.Url,
                    Background = WpfBrushes.Transparent,
                    Foreground = new SolidColorBrush(WpfColor.FromRgb(0x58, 0x9D, 0xF6)),
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(8, 5, 8, 5),
                    HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left,
                    Cursor = WpfCursors.Hand,
                    FontSize = 12.5,
                };
                btn.Click += (_, _) => ItemClicked?.Invoke(item.Url);

                var style = new Style(typeof(System.Windows.Controls.Button));
                style.Setters.Add(new Setter(TemplateProperty, CreateButtonTemplate()));
                btn.Style = style;

                ItemsPanel.Children.Add(btn);
                itemCount++;
            }
        }

        if (itemCount == 0)
        {
            ItemsPanel.Children.Add(new TextBlock
            {
                Text = "All clear",
                FontSize = 13,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(0x4C, 0xAF, 0x50)),
                Margin = new Thickness(4),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            });
        }

    }

    private static ControlTemplate CreateButtonTemplate()
    {
        var template = new ControlTemplate(typeof(System.Windows.Controls.Button));
        var factory = new FrameworkElementFactory(typeof(System.Windows.Controls.Border));
        factory.SetValue(System.Windows.Controls.Border.BackgroundProperty, WpfBrushes.Transparent);
        factory.SetValue(System.Windows.Controls.Border.CornerRadiusProperty, new CornerRadius(4));
        factory.Name = "border";

        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(FrameworkElement.MarginProperty, new Thickness(8, 5, 8, 5));
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
