using System;
using System.Text;
using System.Windows.Data;

namespace AutoMerge
{
	/// <summary>
	/// Changeset comment converter class.
	/// </summary>
	public class ChangesetCommentConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var comment = (value is string) ? (string)value : String.Empty;
			var sb = new StringBuilder(comment);
			sb.Replace('\r', ' ');
			sb.Replace('\n', ' ');
			sb.Replace('\t', ' ');

			if (sb.Length > 64)
			{
				sb.Remove(61, sb.Length - 61);
				sb.Append("...");
			}

			return sb.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return null;
		}
	}
}