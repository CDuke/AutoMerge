using System;
using System.Globalization;
using System.Windows.Data;

namespace AutoMerge
{
	public class BranchNameConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var fullBranchName = (value is string) ? (string)value : String.Empty;
			return BranchHelper.GetShortBranchName(fullBranchName);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}