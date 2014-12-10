using System;
using System.Globalization;
using System.Windows.Data;

namespace AutoMerge
{
    public class MergeOptionToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                var mergeOption = (MergeOption)value;
                switch (mergeOption)
                {
                    case MergeOption.KeepTarget:
                        return "Keep target (discard)";
                    case MergeOption.ManualResolveConflict:
                        return "Manual";
                    case MergeOption.OverwriteTarget:
                        return "Overwrite target";
                }
            }

            return "Unknown mode";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
