using System;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using NetVanguard.Core.Models;

namespace NetVanguard.App.Converters
{
    public class ActionToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is FirewallAction action)
            {
                // Fluent UI Icons: CheckMark (Allow) or Cancel (Block)
                return action == FirewallAction.Allow ? "\uE73E" : "\uE711";
            }
            return "\uE711";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class ActionToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is FirewallAction action)
            {
                // Allow = Green, Block = Red
                return action == FirewallAction.Allow 
                    ? new SolidColorBrush(Colors.MediumSeaGreen) 
                    : new SolidColorBrush(Colors.IndianRed);
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
