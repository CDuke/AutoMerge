using System;
using System.Globalization;
using System.Windows.Data;

namespace AutoMerge
{
	public class ValidationResultToEnableConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var validationResult = (BranchValidationResult) value;

			return validationResult == BranchValidationResult.Success;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}