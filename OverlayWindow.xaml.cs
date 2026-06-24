using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WpfColor = System.Windows.Media.Color;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfCursors = System.Windows.Input.Cursors;

namespace DevBar;

public partial class OverlayWindow : Window
{
    public event Action<OverlayWindow>? PillClicked;
    public event Action<OverlayWindow>? RightClicked;

    private bool _isLightTheme;

    public OverlayWindow()
    {
        InitializeComponent();
        Left = -10000;
        Top = -10000;
        ShowServerDown();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;

        var ex = Win32Interop.GetWindowLong(hwnd, Win32Interop.GWL_EXSTYLE);
        ex |= Win32Interop.WS_EX_NOACTIVATE | Win32Interop.WS_EX_TOOLWINDOW;
        Win32Interop.SetWindowLong(hwnd, Win32Interop.GWL_EXSTYLE, ex);

        Win32Interop.SetWindowPos(hwnd, (IntPtr)Win32Interop.HWND_TOPMOST,
            0, 0, 0, 0,
            Win32Interop.SWP_NOMOVE | Win32Interop.SWP_NOSIZE | Win32Interop.SWP_NOACTIVATE);

        int dark = 1;
        Win32Interop.DwmSetWindowAttribute(hwnd, Win32Interop.DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));

        int excludeFromPeek = 1;
        Win32Interop.DwmSetWindowAttribute(hwnd, Win32Interop.DWMWA_EXCLUDED_FROM_PEEK, ref excludeFromPeek, sizeof(int));
    }

    public void ReassertTopmost()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;
        Win32Interop.SetWindowPos(hwnd, (IntPtr)Win32Interop.HWND_TOPMOST,
            0, 0, 0, 0,
            Win32Interop.SWP_NOMOVE | Win32Interop.SWP_NOSIZE | Win32Interop.SWP_NOACTIVATE);
    }

    public void SetTheme(bool isLightTaskbar)
    {
        _isLightTheme = isLightTaskbar;
    }

    private static readonly IReadOnlySet<string> NoChanges = new HashSet<string>();

    public void SetResult(DevBarResult? result) => SetResult(result, NoChanges);

    public void SetResult(DevBarResult? result, IReadOnlySet<string> changedCategories)
    {
        if (result is null)
        {
            ShowServerDown();
            return;
        }

        PillsPanel.Children.Clear();

        var sorted = result.Data
            .Where(kv => kv.Value.Count > 0)
            .OrderBy(kv => result.Metadata.Display.TryGetValue(kv.Key, out var d) ? d.Priority : 99)
            .ThenBy(kv => kv.Key)
            .ToList();

        if (sorted.Count == 0)
        {
            AddMutedPill("—");
            return;
        }

        foreach (var (category, items) in sorted)
        {
            var display = result.Metadata.Display.GetValueOrDefault(category);
            var symbol = display?.Symbol ?? category;
            AddPill(symbol, items.Count, changedCategories.Contains(category));
        }
    }

    public void SetServerDown() => ShowServerDown();

    private void ShowServerDown()
    {
        PillsPanel.Children.Clear();
        AddMutedPill("—");
    }

    private void AddPill(string symbol, int count, bool isNew)
    {
        var pill = BuildPillButton(symbol, count.ToString(), ForegroundBrush(), HoverBrush());
        pill.Click += (_, _) => PillClicked?.Invoke(this);
        if (isNew) AttachJump(pill);
        PillsPanel.Children.Add(pill);
    }

    private void AddMutedPill(string text)
    {
        var pill = BuildPillButton(text, null, MutedBrush(), HoverBrush());
        pill.Click += (_, _) => PillClicked?.Invoke(this);
        PillsPanel.Children.Add(pill);
    }

    private SolidColorBrush ForegroundBrush() => _isLightTheme
        ? new SolidColorBrush(WpfColor.FromRgb(0x1F, 0x1F, 0x1F))
        : new SolidColorBrush(WpfColor.FromRgb(0xFF, 0xFF, 0xFF));

    private SolidColorBrush MutedBrush() => _isLightTheme
        ? new SolidColorBrush(WpfColor.FromArgb(0x80, 0x00, 0x00, 0x00))
        : new SolidColorBrush(WpfColor.FromArgb(0x80, 0xFF, 0xFF, 0xFF));

    private SolidColorBrush HoverBrush() => _isLightTheme
        ? new SolidColorBrush(WpfColor.FromArgb(0x1A, 0x00, 0x00, 0x00))
        : new SolidColorBrush(WpfColor.FromArgb(0x1A, 0xFF, 0xFF, 0xFF));

    private static System.Windows.Controls.Button BuildPillButton(string symbol, string? count, SolidColorBrush fg, SolidColorBrush hover)
    {
        var content = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        content.Children.Add(new TextBlock
        {
            Text = symbol,
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI Emoji"),
            FontSize = 17,
            Foreground = fg,
            VerticalAlignment = VerticalAlignment.Center,
        });
        if (count is not null)
        {
            content.Children.Add(new TextBlock
            {
                Text = count,
                FontSize = 17,
                FontWeight = FontWeights.SemiBold,
                Foreground = fg,
                Margin = new Thickness(3, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            });
        }

        var btn = new System.Windows.Controls.Button
        {
            Content = content,
            Background = WpfBrushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8, 3, 8, 3),
            Margin = new Thickness(1, 0, 1, 0),
            Cursor = WpfCursors.Hand,
            Focusable = false,
        };

        var style = new Style(typeof(System.Windows.Controls.Button));
        style.Setters.Add(new Setter(TemplateProperty, CreatePillTemplate(hover)));
        btn.Style = style;
        return btn;
    }

    private static ControlTemplate CreatePillTemplate(SolidColorBrush hover)
    {
        var template = new ControlTemplate(typeof(System.Windows.Controls.Button));
        var factory = new FrameworkElementFactory(typeof(System.Windows.Controls.Border));
        factory.SetValue(System.Windows.Controls.Border.BackgroundProperty, WpfBrushes.Transparent);
        factory.SetValue(System.Windows.Controls.Border.CornerRadiusProperty, new CornerRadius(4));
        factory.Name = "border";

        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(FrameworkElement.MarginProperty, new Thickness(8, 3, 8, 3));
        content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        content.SetValue(FrameworkElement.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
        factory.AppendChild(content);

        template.VisualTree = factory;

        var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hoverTrigger.Setters.Add(new Setter(System.Windows.Controls.Border.BackgroundProperty, hover, "border"));
        template.Triggers.Add(hoverTrigger);

        return template;
    }

    /// <summary>
    /// Runs a short vertical bounce on a pill to draw the eye when its category
    /// gains a new item. Starts once the element is laid out so the transform has
    /// valid bounds.
    /// </summary>
    private static void AttachJump(FrameworkElement element)
    {
        var transform = new TranslateTransform();
        element.RenderTransform = transform;
        element.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);

        void Start()
        {
            var bounce = new DoubleAnimation
            {
                From = 0,
                To = -4,
                Duration = TimeSpan.FromMilliseconds(160),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(2),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            };
            transform.BeginAnimation(TranslateTransform.YProperty, bounce);
        }

        if (element.IsLoaded) Start();
        else element.Loaded += (_, _) => Start();
    }

    private void OnRootRightClicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        e.Handled = true;
        RightClicked?.Invoke(this);
    }
}
