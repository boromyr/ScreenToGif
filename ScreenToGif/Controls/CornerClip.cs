using System.Windows;
using System.Windows.Media;

namespace ScreenToGif.Controls;

/// <summary>
/// Clips an element using a rounded rectangle, so that its content follows the given corner radius.
/// </summary>
public static class CornerClip
{
    public static readonly DependencyProperty RadiusProperty = DependencyProperty.RegisterAttached("Radius", typeof(double), typeof(CornerClip), new PropertyMetadata(0d, Radius_PropertyChanged));

    public static double GetRadius(DependencyObject obj) => (double)obj.GetValue(RadiusProperty);

    public static void SetRadius(DependencyObject obj, double value) => obj.SetValue(RadiusProperty, value);

    private static void Radius_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            return;

        element.SizeChanged -= Element_SizeChanged;

        if ((double)e.NewValue > 0)
        {
            element.SizeChanged += Element_SizeChanged;
            UpdateClip(element);
            return;
        }

        element.ClearValue(UIElement.ClipProperty);
    }

    private static void Element_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateClip((FrameworkElement)sender);
    }

    private static void UpdateClip(FrameworkElement element)
    {
        var radius = GetRadius(element);
        var clip = new RectangleGeometry(new Rect(0, 0, element.ActualWidth, element.ActualHeight), radius, radius);
        clip.Freeze();

        element.Clip = clip;
    }
}
