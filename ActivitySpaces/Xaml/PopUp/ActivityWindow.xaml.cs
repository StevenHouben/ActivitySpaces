using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using NooSphere.Model;
using ABC.PInvoke;
using Point = System.Windows.Point;

namespace ActivitySpaces.Xaml.PopUp
{
    /// <summary>
    /// Interaction logic for PopUpWindow.xaml
    /// </summary>
    public partial class ActivityWindow : Window
    {
        #region Window Hacks

        private const int GWL_STYLE = -16;
        private const uint WS_SYSMENU = 0x80000;

        protected override void OnSourceInitialized(EventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            User32.SetWindowLongPtr(hwnd, GWL_STYLE, (uint)Whathecode.Interop.User32.GetWindowLongPtr(hwnd, GWL_STYLE) & (0xFFFFFFFF ^ WS_SYSMENU));

            base.OnSourceInitialized(e);
        }

        #endregion

        private readonly ActivityBar _taskbar;
        private Activity _activity;

        public ActivityWindow(ActivityBar bar)
        {
            InitializeComponent();
            Topmost = true;
            ShowInTaskbar = false;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanResize;
            MinHeight = MaxHeight = Height;
            MinWidth = MaxWidth = Width;
            _taskbar = bar;
        }
  
        public void Show(Activity act, Point location, ActivityButton btn)
        {
            Dispatcher.Invoke((() =>
                {
                    chkRendering.IsChecked = btn.RenderMode != RenderMode.Image;
                    _activity = act;
                    Left = location.X;
                    Top = location.Y;

                    txtActivity.Text = act.Name;
                    Visibility = Visibility.Visible;
                    if (act.Meta.Data != null)
                    {
                        image.Source = File.Exists(act.Meta.Data) ? new BitmapImage(new Uri((String)act.Meta.Data)) : new BitmapImage(new Uri("pack://application:,,,/Images/activity.PNG"));
                    }
                    else
                    {
                        image.Source = new BitmapImage(new Uri("pack://application:,,,/Images/activity.PNG"));
                    }
                    Show();
            }));
        }

        private void btnDone_Click(object sender, RoutedEventArgs e)
        {
            _activity.Name = txtActivity.Text;
            _taskbar.EditActivity(_activity,Convert.ToBoolean(chkRendering.IsChecked),ImagePath);
            Hide();
        }
        private void BtnChangePic_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    FileName = "Document",
                    DefaultExt = ".png",
                    Filter = "PNG Images (.png)|*.png",
                    RestoreDirectory = true,
                    InitialDirectory = "c:\\icons"
                };

            // Show open file dialog box 
            var result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result != true) return;
            // Open document 
            _activity.Meta.Data= dlg.FileName;
            if (File.Exists(dlg.FileName))
            {
                image.Source = new BitmapImage(new Uri(dlg.FileName));
                ImagePath = dlg.FileName;
            }
        }

        public string ImagePath { get; private set; }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Visibility = Visibility.Hidden;
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            _taskbar.DeleteActivity();
            Hide();
        }
    }
}