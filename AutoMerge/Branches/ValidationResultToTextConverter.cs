using System;
using System.Globalization;
using System.Windows.Data;

namespace AutoMerge
{
	public class ValidationResultToTextConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var validationResult = (BranchValidationResult) value;

			if (validationResult == BranchValidationResult.AlreadyMerged)
				return "Changeset already merged";
			if (validationResult == BranchValidationResult.BranchNotMapped)
				return "Branch not mapped";
			if (validationResult == BranchValidationResult.ItemHasLocalChanges)
				return "Some files have local changes. Commit or undo it";
			if (validationResult == BranchValidationResult.Undefined)
				return "Unknown error";

			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}