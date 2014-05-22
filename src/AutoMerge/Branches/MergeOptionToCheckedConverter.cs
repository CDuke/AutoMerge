using System;
using System.Globalization;
using System.Windows.Data;

namespace AutoMerge
{
	public class MergeOptionToCheckedConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (MergeOption) value == (MergeOption) parameter;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return parameter;
		}
	}
}