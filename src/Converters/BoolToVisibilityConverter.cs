using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Colinhas.Converters;

/// <summary>
/// Converts a bool to Visibility. Set <see cref="Invert"/> to true to flip it.
/// WinUI 3 doesn't ship a built-in BoolToVisibilityConverter.
/// </summary>
public sealed partial class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var flag = value is bool b && b;
        if (Invert) flag = !flag;
        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
