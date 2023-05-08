using System.Globalization;
using System.Windows.Controls;

namespace CloudDriveUI.ValidationRules
{
    class DirectoryExistValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var path = value as string;
            return new ValidationResult(path != null && Directory.Exists(path), "目录不存在");
        }
    }
}
