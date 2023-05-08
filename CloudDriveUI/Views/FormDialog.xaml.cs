using CloudDriveUI.Models;
using System.Windows.Controls;

namespace CloudDriveUI.Views
{
    /// <summary>
    /// ListDialog.xaml 的交互逻辑
    /// </summary>
    public partial class FormDialog : UserControl
    {
        public FormDialog(List<FormItem> items)
        {
            InitializeComponent();
            DataContext = items;
            //var copy = new ObservableCollection<FormItem>(items.Select(e => new FormItem(e)).ToList());

        }


        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var flag = true;
            foreach (var itm in Fields.Items)
            {
                if (itm is FormItem tmp)
                    flag &= tmp.Validated.Invoke(tmp.Value);
                if (!flag) break;
            }
            Submit.IsEnabled = flag;
        }

    }
}
