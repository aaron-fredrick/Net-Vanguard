using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Diagnostics;

namespace NetVanguard.App.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToAccentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            try
            {
                bool isActive = value is bool b && b;
                
                // Use standard WinUI 3 theme keys which are more reliable
                string resourceKey = isActive ? "AccentFillColorDefaultBrush" : "ControlFillColorDefaultBrush";
                
                if (Application.Current != null && Application.Current.Resources.ContainsKey(resourceKey))
                {
                    return Application.Current.Resources[resourceKey];
                }

                // Fallback to coded brushes if resources are missing
                return isActive 
                    ? new SolidColorBrush(Colors.RoyalBlue) 
                    : new SolidColorBrush(Colors.Transparent);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in BoolToAccentConverter: {ex.Message}");
                return new SolidColorBrush(Colors.Transparent);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
