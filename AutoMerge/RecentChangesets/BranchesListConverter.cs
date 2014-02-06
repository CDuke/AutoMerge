using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace AutoMerge
{
	public class BranchesListConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var branches = value as List<string>;
			if (branches == null || branches.Count == 0)
				return string.Empty;

			if (branches.Count == 1)
			{
				return BranchHelper.GetShortBranchName(branches[0]);
			}
			else
			{
				return "multi";
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}