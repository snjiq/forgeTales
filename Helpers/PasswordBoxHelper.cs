using System.Windows;
using System.Windows.Controls;

namespace ForgeTales
{
    public static class PasswordBoxHelper
    {
        public static readonly DependencyProperty IsPasswordRevealedProperty =
            DependencyProperty.RegisterAttached("IsPasswordRevealed", typeof(bool), typeof(PasswordBoxHelper),
            new PropertyMetadata(false));

        public static bool GetIsPasswordRevealed(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsPasswordRevealedProperty);
        }

        public static void SetIsPasswordRevealed(DependencyObject obj, bool value)
        {
            obj.SetValue(IsPasswordRevealedProperty, value);
        }
    }
}