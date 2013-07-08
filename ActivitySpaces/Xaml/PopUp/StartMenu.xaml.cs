using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Image = System.Windows.Controls.Image;

namespace ActivitySpaces.Xaml.PopUp
{
    public partial class StartMenu : Window
    {
        #region Members

        private readonly ActivityBar _taskbar;

        #endregion

        #region Constructor

        public StartMenu(ActivityBar tBar)
        {
            InitializeComponent();

            ShowInTaskbar = false;

            _taskbar = tBar;

            MinHeight = MaxHeight = Height;
            MinWidth = MaxWidth = Width;

            Top = _taskbar.Height + 5;
            Left = 4;
        }

        #endregion

        #region Methods

        public Image WFormsImageToWPFImage(System.Drawing.Image Old_School_Image)
        {
            var ms = new MemoryStream();
            Old_School_Image.Save(ms, ImageFormat.Png);

            var bImg = new BitmapImage();
            bImg.BeginInit();
            bImg.StreamSource = new MemoryStream(ms.ToArray());
            bImg.EndInit();
            var WPFImage = new Image();
            WPFImage.Source = bImg;
            return WPFImage;
            ;
        }

        #endregion

        #region Events

        private void btnAddActivity_Click(object sender, RoutedEventArgs e)
        {
            _taskbar.AddEmptyActivity();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            _taskbar.ExitApplication();
        }

        #endregion
    }
}