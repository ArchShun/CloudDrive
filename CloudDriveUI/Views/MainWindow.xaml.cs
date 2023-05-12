using CloudDriveUI.Core.Interfaces;
using MaterialDesignThemes.Wpf;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace CloudDriveUI.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        NotifyIcon notifyIcon;
        public MainWindow(ISnackbarMessage snackbarMessageQueue)
        {
            InitializeComponent();
            
            this.MainSnackbar.MessageQueue = (SnackbarMessageQueue?)snackbarMessageQueue;

            this.StateChanged += Window_StateChanged;
            this.notifyIcon = new NotifyIcon();
            this.notifyIcon.Text = "CloudDrive";//鼠标移入图标后显示的名称
            this.notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
            this.notifyIcon.Visible = true;
            notifyIcon.MouseDoubleClick += new MouseEventHandler((o, e) =>
            {
                if (e.Button == MouseButtons.Left) Show(o, e);
            });
        }

        /// <summary>
        /// 窗体状态改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_StateChanged(object? sender, EventArgs e)
        {
            //判断是否选择的是最小化按钮
            if (WindowState == WindowState.Minimized)
            {
                //隐藏任务栏区图标
                ShowInTaskbar = false;
                //图标显示在托盘区
                notifyIcon.Visible = true;
            }
        }


        /// <summary>
        /// 显示窗体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Show(object sender, EventArgs e)
        {
            this.ShowInTaskbar = true;
            this.WindowState = WindowState.Normal;
            this.Activate();
        }        

       
    }    
}
