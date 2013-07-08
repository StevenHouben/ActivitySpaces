using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Threading;
using Brushes = System.Windows.Media.Brushes;
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
                // Create an Array to acomodate the colors from the image
                var colors = new ArrayList();

                // simple loop to load each color to the array
                for ( var x = 0; x < bitmap.Width; x++ )
                {
                    for ( var y = 0; y < bitmap.Height; y++ )
                    {
                        var pixel = bitmap.GetPixel( x, y );
                        // verify if the color is transparent because a color of #00000000 would darker the brush
                        if ( pixel.A > 0x00 )
                            colors.Add( pixel );
                    }
                }

                // Using linq to get the average color RGB bytes
                byte r = (byte)Math.Floor( colors.Cast<System.Drawing.Color>().Average( c => c.R ) );
                byte g = (byte)Math.Floor( colors.Cast<System.Drawing.Color>().Average( c => c.G ) );
                byte b = (byte)Math.Floor( colors.Cast<System.Drawing.Color>().Average( c => c.B ) );

                // Instanciate and initialize the LinearGradientBrush that will be returned as the result of the operation
                var brush = new LinearGradientBrush { EndPoint = new Point( 0.5, 1.0 ), StartPoint = new Point( 0.5, 0.0 ) };
                brush.GradientStops.Add( new GradientStop( System.Windows.Media.Color.FromArgb( 0x00, r, g, b ), 0.00 ) );
                brush.GradientStops.Add( new GradientStop( System.Windows.Media.Color.FromArgb( 0xFF, r, g, b ), 1.00 ) );
                Background = brush;
            }
        }

        void ActivityButton_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // As already said the sender must be a Content Control
            if ( !( sender is ContentControl ) )
                return;

            ContentControl element = sender as ContentControl;

            // if the Brush is not a linearGradientBrush we dont need to do anything so just returns
            if ( !( element.GetValue( Control.BackgroundProperty ) is LinearGradientBrush ) )
            {
                //element.SetValue(ContentControl.BackgroundProperty, new LinearGradientBrush());
                return;
            }

            // Get the brush
            LinearGradientBrush b = element.GetValue( Control.BackgroundProperty ) as LinearGradientBrush;

            // Get the ActualWidth of the sender
            Double refZeroX = (double)element.GetValue( FrameworkElement.ActualWidthProperty );

            // Get the new poit for the StartPoint and EndPoint of the Gradient
            var p = new Point( e.GetPosition( element ).X / refZeroX, 1 );

            // Set the new values
            if ( b == null ) return;
            b.StartPoint = new Point( 1 - p.X, 0 );
            b.EndPoint = p;
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
