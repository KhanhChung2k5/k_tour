using System.Globalization;

namespace HeriStepAI.Mobile.Converters;

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
        return isSelected 
            ? Application.Current?.Resources["White"] ?? Colors.White
            : Color.FromArgb("#FFFFFF33"); // Semi-transparent white
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
        return isSelected 
            ? Application.Current?.Resources["Primary"] ?? Colors.Orange
            : Application.Current?.Resources["White"] ?? Colors.White;
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
        return isSelected 
            ? Application.Current?.Resources["White"] ?? Colors.White
            : Color.FromArgb("#FFFFFF33"); // Semi-transparent white
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
        return isSelected 
            ? Application.Current?.Resources["Primary"] ?? Colors.Orange
            : Application.Current?.Resources["White"] ?? Colors.White;
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
        return isSelected 
            ? Application.Current?.Resources["Primary"] ?? Colors.Orange
            : Colors.Transparent;
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
        return isSelected 
            ? Application.Current?.Resources["White"] ?? Colors.White
            : Application.Current?.Resources["TextSecondary"] ?? Colors.Gray;
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
        return isTrue 
            ? Application.Current?.Resources["SecondaryLight"] ?? Colors.LightGreen
            : Application.Current?.Resources["Error"] ?? Colors.Red;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts bool to status text (Bật/Tắt)
/// </summary>
public class BoolToStatusConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b && b ? "Bật" : "Tắt";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
