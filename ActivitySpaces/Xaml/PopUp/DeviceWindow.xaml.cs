using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using ABC.Model.Device;
using ABC.PInvoke;
using ActivitySpaces.Xaml;

namespace ActivitySpaces.PopUp
{
    /// <summary>
    /// Interaction logic for PopUpWindow.xaml
    /// </summary>
    public partial class DeviceWindow
    {
        #region Window Hacks

        private const int GWL_STYLE = -16;
        private const uint WS_SYSMENU = 0x80000;

        protected override void OnSourceInitialized(EventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            User32.SetWindowLongPtr(hwnd, GWL_STYLE, (uint)User32.GetWindowLongPtr(hwnd, GWL_STYLE) & (0xFFFFFFFF ^ WS_SYSMENU));

            base.OnSourceInitialized(e);
        }

        #endregion

        private readonly ActivityBar taskbar;

        public DeviceWindow(ActivityBar bar)
        {
            InitializeComponent();
            Topmost = true;
            ShowInTaskbar = false;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanResize;
            MinHeight = MaxHeight = Height;
            MinWidth = MaxWidth = Width;
            taskbar = bar;
        }

        public void Show(int offset, List<Device> devices)
        {
            if (offset + Width > SystemParameters.PrimaryScreenWidth)
                Left = SystemParameters.PrimaryScreenWidth - Width;
            else
                Left = offset;
            Top = taskbar.Height + 5;
            Show();
            VisualizeDevices(devices);
        }

        private void VisualizeDevices(IEnumerable<Device> devices)
        {
            btnTabletop.Visibility = Visibility.Hidden;
            btnPhone.Visibility = Visibility.Hidden;
            btnLaptop.Visibility = Visibility.Hidden;
            btnTablet.Visibility = Visibility.Hidden;

            foreach (Device dev in devices)
            {
                if (dev.DeviceType == DeviceType.Tabletop)
                    btnTabletop.Visibility = Visibility.Visible;
                else if (dev.DeviceType == DeviceType.SmartPhone)
                    btnPhone.Visibility = Visibility.Visible;
                else if (dev.DeviceType == DeviceType.Laptop)
                    btnLaptop.Visibility = Visibility.Visible;
                else if (dev.DeviceType == DeviceType.Tablet)
                    btnTablet.Visibility = Visibility.Visible;
            }
        }

        private void btnDone_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Visibility = Visibility.Hidden;
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            taskbar.DeleteActivity();
            Hide();
        }
    }
}