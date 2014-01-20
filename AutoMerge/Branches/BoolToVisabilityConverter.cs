using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AutoMerge
{
	public class BoolToVisabilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (bool)value ? Visibility.Visible : Visibility.Hidden;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var visibility = (Visibility) value;

			return visibility == Visibility.Visible;
		}
	}
}