using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ChatClient.Converters // သင့် Project ရဲ့ Namespace အတိုင်း ပြောင်းပေးပါ
{
    public class StatusColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isOnline && isOnline)
            {
                return Brushes.Green; // Online ဆိုရင် အစိမ်း
            }
            return Brushes.Gray; // Offline ဆိုရင် မီးခိုးရောင်
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}