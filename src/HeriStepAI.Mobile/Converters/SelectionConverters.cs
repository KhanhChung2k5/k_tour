using System.Globalization;

namespace HeriStepAI.Mobile.Converters;

// Dùng màu trực tiếp để tránh KeyNotFoundException khi ResourceDictionary chưa load
internal static class SafeColors
{
    public static readonly Color Primary = Color.FromArgb("#E07B4C");
    public static readonly Color White = Colors.White;
    public static readonly Color TextSecondary = Color.FromArgb("#757575");
    public static readonly Color SecondaryLight = Color.FromArgb("#81C784");
    public static readonly Color Error = Color.FromArgb("#F44336");
}

/// <summary>
/// Converts selected gender to background color
/// </summary>
public class GenderColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var selectedGender = value as string ?? "male";
        var buttonGender = parameter as string ?? "male";
        
        var isSelected = selectedGender == buttonGender;
        return isSelected ? SafeColors.White : Color.FromArgb("#FFFFFF33");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts selected gender to text color
/// </summary>
public class GenderTextColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var selectedGender = value as string ?? "male";
        var buttonGender = parameter as string ?? "male";
        
        var isSelected = selectedGender == buttonGender;
        return isSelected ? SafeColors.Primary : SafeColors.White;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts selected region to background color
/// </summary>
public class RegionColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var selectedRegion = value as string ?? "central";
        var buttonRegion = parameter as string ?? "central";
        
        var isSelected = selectedRegion == buttonRegion;
        return isSelected ? SafeColors.White : Color.FromArgb("#FFFFFF33");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts selected region to text color
/// </summary>
public class RegionTextColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var selectedRegion = value as string ?? "central";
        var buttonRegion = parameter as string ?? "central";
        
        var isSelected = selectedRegion == buttonRegion;
        return isSelected ? SafeColors.Primary : SafeColors.White;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts selected category to background color
/// </summary>
public class CategoryColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var selectedCategory = value is int i ? i : 0;
        var buttonCategory = int.TryParse(parameter?.ToString(), out var p) ? p : 0;
        
        var isSelected = selectedCategory == buttonCategory;
        return isSelected ? SafeColors.Primary : Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts selected category to text color
/// </summary>
public class CategoryTextColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var selectedCategory = value is int i ? i : 0;
        var buttonCategory = int.TryParse(parameter?.ToString(), out var p) ? p : 0;
        
        var isSelected = selectedCategory == buttonCategory;
        return isSelected ? SafeColors.White : SafeColors.TextSecondary;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Inverts a boolean value
/// </summary>
public class InvertBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts bool to status color (green/red)
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isTrue = value is bool b && b;
        return isTrue ? SafeColors.SecondaryLight : SafeColors.Error;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts bool to localized status text (On/Off)
/// </summary>
public class BoolToStatusConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isOn = value is bool b && b;
        try
        {
            var loc = IPlatformApplication.Current!.Services.GetRequiredService<Services.ILocalizationService>();
            return isOn ? loc.GetString("On") : loc.GetString("Off");
        }
        catch
        {
            return isOn ? "ON" : "OFF";
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
