using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace NetVanguard.App.Helpers
{
    public static class CursorExtensions
    {
        public static readonly DependencyProperty CursorProperty =
            DependencyProperty.RegisterAttached(
                "Cursor",
                typeof(InputSystemCursorShape),
                typeof(CursorExtensions),
                new PropertyMetadata(InputSystemCursorShape.Arrow, OnCursorChanged));

        public static void SetCursor(UIElement element, InputSystemCursorShape value)
        {
            element.SetValue(CursorProperty, value);
        }

        public static InputSystemCursorShape GetCursor(UIElement element)
        {
            return (InputSystemCursorShape)element.GetValue(CursorProperty);
        }

        private static readonly System.Reflection.PropertyInfo _protectedCursorProp = 
            typeof(UIElement).GetProperty("ProtectedCursor", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

        private static void OnCursorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element && e.NewValue is InputSystemCursorShape shape)
            {
                // In WinUI 3, ProtectedCursor is protected. We use reflection to set it from this attached property.
                _protectedCursorProp?.SetValue(element, InputSystemCursor.Create(shape));
            }
        }
    }
}
