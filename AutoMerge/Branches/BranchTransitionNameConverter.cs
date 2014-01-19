using System;
using System.Globalization;
using System.Windows.Data;

namespace AutoMerge
{
	public class BranchTransitionNameConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var sourceBranchName = values[0] as string;
			var targetBranchName = values[1] as string;

			return string.Format("{0} -> {1}", ExtractShortName(sourceBranchName), ExtractShortName(targetBranchName));
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		private static string ExtractShortName(string fullBranchName)
		{
			if (string.IsNullOrWhiteSpace(fullBranchName))
				return string.Empty;

			var pos = fullBranchName.LastIndexOf('/');
			var name = fullBranchName.Substring(pos + 1);
			return name;
		}
	}
}