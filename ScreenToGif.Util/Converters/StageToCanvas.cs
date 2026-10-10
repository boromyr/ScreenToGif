using ScreenToGif.Domain.Enums;
using System.Globalization;
using System.Windows.Data;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;

namespace ScreenToGif.Util.Converters;

public class StageToCanvas : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not RecorderStages stage)
            return (Brush)Application.Current.FindResource("Vector.Record");

        switch (stage)
        {
            case RecorderStages.Stopped:
                return (Brush)Application.Current.FindResource("Vector.Record");
            case RecorderStages.Recording:
                return (Brush)Application.Current.FindResource("Vector.Pause");
            case RecorderStages.Paused:
                return (Brush)Application.Current.FindResource("Vector.Record");
#pragma warning disable CS0612 //Snapping is obsolete, but still used by the webcam recorder.
            case RecorderStages.Snapping:
#pragma warning restore CS0612
                return (Brush)Application.Current.FindResource("Vector.Camera.Add");
        }

        return (Brush)Application.Current.FindResource("Vector.Record");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value;
    }
}