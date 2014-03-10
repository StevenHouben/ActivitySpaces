using System;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Threading;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;

namespace ActivitySpaces.Xaml
{
    public class ActivityButton : Button
    {
        #region Properties


        private ActivityBar _activityBar;

        public string ActivityId { get; set; }
        Uri _image;

        public Uri Image
        {
            get { return _image; }
            set
            {
                _image = value;
                Invalidate();
                _previousImage = Image;
            }
        }

        string _text;

        public string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                Invalidate();
            }
        }

        RenderMode _renderMode;

        public RenderMode RenderMode
        {
            get { return _renderMode; }
            set
            {
                _renderMode = value;
                Invalidate();
            }

        }

        bool _selected;

        public bool Selected
        {
            get { return _selected; }
            set
            {
                _selected = value;
                Background = _selected ? new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)) : new SolidColorBrush(Colors.Transparent);
                if (!Selected)
                    CalculateColorTracking();
            }
        }

        #endregion


        #region Constructor

        public ActivityButton(ActivityBar activityBar)
        {
            Image = new Uri( "pack://application:,,,/Images/activity.PNG" );
            Text = "Default";
            _activityBar = activityBar;
            
            Initialize();

        }

        void Initialize()
        {
            MaxHeight = 46;

            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            RenderMode = RenderMode.ImageAndText;

            Loaded += ActivityButtonLoaded;
            MouseMove += ActivityButton_MouseMove;
            PreviewMouseMove += ActivityButton_MouseMove;
            SizeChanged += ActivityButton_SizeChanged;
        }

        void ActivityButton_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Invalidate();
        }


        void ActivityButtonLoaded(object sender, RoutedEventArgs e)
        {
            CalculateColorTracking();
        }

        public ActivityButton(Uri img, string text, ActivityBar activityBar)
        {
            Image = img;
            Text = text;
            _activityBar = activityBar;
            Initialize();
        }

        public SavedButton GetSaveableButton()
        {
            return new SavedButton
            {
                Image = Image,
                Text = Text,
                RenderMode = RenderMode,
                Selected = Selected
            };
        }

        #endregion

        #region Private Methods

        void Invalidate()
        {
            Dispatcher.Invoke( DispatcherPriority.Background, new Action( () =>
            {
                Content = null;
                var panel = new StackPanel { Orientation = Orientation.Horizontal };

                switch ( RenderMode )
                {
                    case RenderMode.Image:
                    {
                        var img = new Image { Source = new BitmapImage( Image ), Margin = new Thickness( 5 ) };
                        panel.Children.Add( img );
                    }
                        break;
                    default:
                    {
                        if (_activityBar.BarOrientation != System.Windows.Controls.Orientation.Vertical )
                        {
                            var img = new Image { Source = new BitmapImage(Image), Margin = new Thickness(5) };
                            panel.Children.Add(img);
                            var l = new Label
                            {
                                Foreground = Brushes.White,
                                VerticalAlignment = VerticalAlignment.Center,
                                Content = Text
                            };
                            panel.Children.Add(l);
                        }
                        else
                        {
                            var img = new Image { Source = new BitmapImage(Image), Margin = new Thickness(5) };
                            panel.Children.Add(img);
                        }

                    }
                        break;
                }
                Content = panel;
                if (Image != _previousImage)
                    CalculateColorTracking();
            } ) );
        }

        #endregion


        Uri _previousImage;
        public void CalculateColorTracking()
        {
            var stream = new MemoryStream();
            var frame = BitmapFrame.Create(new BitmapImage(Image));
            var enc = new BmpBitmapEncoder();
            enc.Frames.Add( frame );
            enc.Save( stream );

            using ( var bitmap = new Bitmap( stream ) )
            {
                var tr = 0;
                var tg = 0;
                var tb = 0;

                for (var x = 0; x < bitmap.Width; x++)
                {
                    for (var y = 0; y < bitmap.Height; y++)
                    {
                        var pixel = bitmap.GetPixel(x, y);
                        tr += pixel.R;
                        tg += pixel.G;
                        tb += pixel.B;
                    }
                }

                byte r = (byte)Math.Floor((double)(tr / (bitmap.Height * bitmap.Width)));
                byte g = (byte)Math.Floor((double)(tg / (bitmap.Height * bitmap.Width)));
                byte b = (byte)Math.Floor((double)(tb / (bitmap.Height * bitmap.Width)));

                var radialGradient = new RadialGradientBrush
                    {
                        RadiusX = 1,
                        RadiusY = 1
                    };

                radialGradient.GradientStops.Add(new GradientStop(Color.FromArgb(255, 255, 255, 255), 0));
                radialGradient.GradientStops.Add(new GradientStop(Color.FromArgb(255, r, g, b), 0.6));
                radialGradient.GradientStops.Add(new GradientStop(Color.FromArgb(100, r, g, b), 1));
                Background = radialGradient;
            }

        }
        void ActivityButton_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var b = Background as RadialGradientBrush;

            if (b == null) return;

            Point p;
            if (_activityBar.BarOrientation == System.Windows.Controls.Orientation.Horizontal)
            {
                p = new Point(e.GetPosition(this).X / ActualWidth, 1);
                b.Transform = new TranslateTransform(0, 5);
                b.Center = new Point(0.5, 1);
                b.GradientOrigin = p;
            }
            else
            {
                p = new Point(0.5, e.GetPosition(this).Y / ActualHeight);
                b.GradientOrigin = new Point(0.5, 0.5);
                b.Center = p;
            }

        }
    }

    public enum RenderMode
    {
        Image,
        ImageAndText
    }

    [DataContract]	
    public class SavedButton
    {
        #region Properties
        [DataMember]	
        public Uri Image { get; set; }

        [DataMember]	
        public string Text { get; set; }

        [DataMember]	
        public RenderMode RenderMode { get; set; }

        [DataMember]	
        public bool Selected{ get; set; }

        #endregion
    }
}
