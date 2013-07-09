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

        RenderMode _internalRenderMode = RenderMode.Image;
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
                //Background = _selected ? new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)) : new SolidColorBrush(Colors.Transparent);
            }
        }

        #endregion


        #region Constructor

        public ActivityButton()
        {
            Image = new Uri( "pack://application:,,,/Images/activity.PNG" );
            Text = "Default";
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

        bool _renderAsImage = false;
        void ActivityButton_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _renderAsImage = Width < 100;
            Invalidate();
        }


        void ActivityButtonLoaded(object sender, RoutedEventArgs e)
        {
            CalculateColorTracking();
        }

        public ActivityButton( Uri img, string text )
        {
            Image = img;
            Text = text;
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
                        if ( !_renderAsImage )
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

                //var brush = new LinearGradientBrush {EndPoint = new Point(0.5, 1.0), StartPoint = new Point(0.5, 0.0)};


                //brush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(0xFF, r, g, b), 0.00));
                //brush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(0x00, r, g, b), 1.00));

                var radialGradient = new RadialGradientBrush
                    {
                        GradientOrigin = new Point(0.5, 0.5),
                        Center = new Point(0.5, 1),
                        RadiusX = 0.9,
                        RadiusY = 0.9,
                        SpreadMethod = GradientSpreadMethod.Pad
                    };
                //radialGradient.GradientStops.Add(new GradientStop(Colors.White, 0.00));
                //radialGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(255, r, g, b), 1.00));
                //radialGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(100, r, g, b), 2.00));

                radialGradient.GradientStops.Add(new GradientStop(Colors.White, 0.00));
                radialGradient.GradientStops.Add(new GradientStop(Color.FromArgb(250, r, g, b), 0.8));
                //radialGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(175, r, g, b), 0.6));
                //radialGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(255, r, g, b), 1));
                //radialGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(225, r, g, b), 0.75));
                //radialGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(250, r, g, b), 1));
                //radialGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(175, r, g, b), 2.00));
                Background = radialGradient;
            }

        }
        public static Color Blend(Color color, Color backColor, double amount,byte alpha)
        { 
            byte r = (byte)((color.R * amount) + backColor.R * (1 - amount));
            byte g = (byte)((color.G * amount) + backColor.G * (1 - amount));
            byte b = (byte)((color.B * amount) + backColor.B * (1 - amount));
            return Color.FromArgb(alpha,r, g, b);
        }

        void ActivityButton_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {

            //// Get the brush
            //LinearGradientBrush b = element.GetValue( Control.BackgroundProperty ) as LinearGradientBrush;

            //// Get the ActualWidth of the sender
            //Double refZeroX = (double)element.GetValue( FrameworkElement.ActualWidthProperty );

            //// Get the new poit for the StartPoint and EndPoint of the Gradient
            //var p = new Point( e.GetPosition( element ).X / refZeroX, 1 );

            //// Set the new values
            //if ( b == null ) return;
            //b.StartPoint = new Point( 1 - p.X, 0 );
            //b.EndPoint = p;

            var b = Background as RadialGradientBrush;

            // Get the new poit for the StartPoint and EndPoint of the Gradient
            var p = new Point(e.GetPosition(this).X / ActualWidth, e.GetPosition(this).Y / ActualWidth);

            b.Transform =  new TranslateTransform(0,2);
            // Set the new values
            if (b == null) return;
            //b.GradientOrigin = new Point(1 - p.X, 0);
            b.Center = p;
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
