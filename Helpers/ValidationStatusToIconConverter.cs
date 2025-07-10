using RvtToNavisConverter.Services;
using System;
using System.Globalization;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;

namespace RvtToNavisConverter.Helpers
{
    public class ValidationStatusToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ValidationStatus status)
            {
                return status switch
                {
                    ValidationStatus.Valid => PackIconKind.CheckCircle,
                    ValidationStatus.Invalid => PackIconKind.CloseCircle,
                    ValidationStatus.Warning => PackIconKind.AlertCircle,
                    _ => PackIconKind.HelpCircleOutline,
                };
            }
            return PackIconKind.HelpCircleOutline;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
