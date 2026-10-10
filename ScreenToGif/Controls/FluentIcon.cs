using System;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace ScreenToGif.Controls;

/// <summary>
/// Creates an icon brush (same shape as the Vector.* resources) from a Segoe Fluent Icons glyph.
/// Usage: {n:FluentIcon E74E} or {n:FluentIcon EA3B, Brush=Fluent.Record}.
/// </summary>
[MarkupExtensionReturnType(typeof(DrawingBrush))]
public class FluentIcon : MarkupExtension
{
    private const double Size = 64;
    private static readonly Typeface IconTypeface = new(new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

    public FluentIcon()
    { }

    public FluentIcon(string glyph)
    {
        Glyph = glyph;
    }

    /// <summary>
    /// Hexadecimal code point of the glyph, such as E74E.
    /// </summary>
    [ConstructorArgument("glyph")]
    public string Glyph { get; set; }

    /// <summary>
    /// Resource key of the brush used to paint the glyph.
    /// </summary>
    public string Brush { get; set; } = "Fluent.Text.Primary";

    public override object ProvideValue(IServiceProvider serviceProvider) => Create(Glyph, Brush);

    public static DrawingBrush Create(string glyph, string brushKey)
    {
        var brush = Application.Current?.TryFindResource(brushKey) as Brush ?? Brushes.Gray;
        var text = char.ConvertFromUtf32(int.Parse(glyph, NumberStyles.HexNumber));
        var formatted = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, IconTypeface, Size, brush, 1d);

        //Centers the glyph inside a fixed square, so that all icons share the same scale.
        var geometry = formatted.BuildGeometry(new Point());
        var bounds = geometry.Bounds;

        if (!bounds.IsEmpty)
            geometry.Transform = new TranslateTransform(Size / 2 - (bounds.Left + bounds.Width / 2), Size / 2 - (bounds.Top + bounds.Height / 2));

        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, Size, Size))));
        group.Children.Add(new GeometryDrawing(brush, null, geometry));

        var drawingBrush = new DrawingBrush(group) { Stretch = Stretch.Uniform };

        if (drawingBrush.CanFreeze)
            drawingBrush.Freeze();

        return drawingBrush;
    }
}
