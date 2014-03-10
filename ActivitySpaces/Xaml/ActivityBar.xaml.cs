using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using NooSphere.Infrastructure.ActivityBase;
using NooSphere.Infrastructure.Discovery;
using NooSphere.Infrastructure.Helpers;
using NooSphere.Infrastructure.Web;
using NooSphere.Model;
using ABC.Windows;
using ABC.Windows.Desktop;
using ABC.Windows.Desktop.Settings;
using ActivitySpaces.Input;
using ActivitySpaces.Xaml.PopUp;
using NooSphere.Model.Device;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using System.Diagnostics;
using MenuItem = System.Windows.Controls.MenuItem;
using ModifierKeys = ActivitySpaces.Input.ModifierKeys;
using System.Windows.Controls;
using Binding = System.Windows.Data.Binding;
using Orientation = System.Windows.Controls.Orientation;
using WindowInfo = ABC.Windows.Window;
using WPFWindow = System.Windows.Window;
using System.Threading.Tasks;

namespace ActivitySpaces.Xaml
{
    public partial class ActivityBar : INotifyPropertyChanged
    {
        public static DataLogger Datalog;

        private ActivityClient _client;
        private Device _device;

        #region VDM and UI
        private readonly List<WPFWindow> _popUpWindows = new List<WPFWindow>();
        private readonly Dictionary<string, Proxy> _proxies = new Dictionary<string, Proxy>();

        public static VirtualDesktopManager DesktopManager;
        private readonly KeyboardHook _keyboard = new KeyboardHook();

        private readonly Proxy _homeProxy;
        private readonly ActivityWindow _activityWindow;
        private readonly StartMenu _startMenu;
        private readonly string _startupDesktopPath;

        private Proxy _currentProxy;
        private ActivityButton _lastclickedButton;
        public bool ClickDetected = false;

        private readonly Dictionary<DockPosition, MenuItem> _dockingMenu =
            new Dictionary<DockPosition, MenuItem>();

        private bool _horizontalMode = true;
        RenderMode _selectedRenderStyle = RenderMode.Image;
        private readonly List<StackPanel> _panels = new List<StackPanel>();

        const int ActivityWindowOffset = 5;

        Thickness _buttonMargin = new Thickness( 0, 0, 0, 0 );
        public Thickness ButtonMargin {
            get { return _buttonMargin; }
            set
            {
                _buttonMargin = value;
                NotifyPropertyChanged();
            }
        }

        protected override void UpdateInterface()
        {
           
        }

        Thickness _contentPanelMargin = new Thickness(0, 0, 0, 0);
        public Thickness ContentPanelMargin
        {
            get { return _contentPanelMargin; }
            set
            {
                _contentPanelMargin = value;
                NotifyPropertyChanged();
            }
        }

        Orientation _panelOrientation = Orientation.Horizontal;
        public Orientation PanelOrientation
        {
            get { return _panelOrientation; }
            set
            {
                _panelOrientation = value;
                NotifyPropertyChanged();
            }
        }

        Thickness _panelMargin = new Thickness(0, 0, 0, 0);
        public Thickness PanelMargin
        {
            get { return _panelMargin; }
            set
            {
                _panelMargin = value;
                NotifyPropertyChanged();
            }
        }


        double _buttonHeight;
        public double ButtonHeight
        {
            get
            {
                return _buttonHeight;
            }
            set
            {
                _buttonHeight = value;
                NotifyPropertyChanged();
            }
        }

        double _buttonWidth;
        public double ButtonWidth
        {
            get
            {
                return _buttonWidth;
            }
            set
            {
                _buttonWidth = value;
                NotifyPropertyChanged();
            }
        }

        public override sealed int VerticalModeSize { get; set; }
        public override sealed int HorizontalModeSize { get; set; }

        double _startButtonHeight;

        const double ActivityButtonSpacing = 1;
        const double PanelSpacingBorderCompensation = -2;
        const double PanelSpacing = -1;
        const double Borderwidth = 1;

        Size _verticalButtonSize = new Size(62, 46);
        int _horizontalButtonWidthCollapsed = 60;
        int _horizontalButtonWidthOpen = 150;
        int _horizontalButtonHeight = 40;
           

        public double StartButtonHeight
        {
            get { return _startButtonHeight; }
            set
            {
                _startButtonHeight = value;
                NotifyPropertyChanged();
            }
        }
        double _startButtonWidth;
        public double StartButtonWidth
        {
            get { return _startButtonWidth; }
            set
            {
                _startButtonWidth = value;
                NotifyPropertyChanged();
            }
        }

        string _dataDirectory;
        #endregion

        public ActivityBar()
        {
            InitializeComponent();

            DataContext = this;

            Width = Height = 0;
            VerticalModeSize = 62;
            HorizontalModeSize = 40;

            var settings = new LoadedSettings(true);
            DesktopManager = new VirtualDesktopManager(settings);

            _startupDesktopPath = Environment.GetFolderPath( Environment.SpecialFolder.Desktop );

            var logDirectory = AppDomain.CurrentDomain.BaseDirectory + "/log/";
            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);

            var _dataDirectory = AppDomain.CurrentDomain.BaseDirectory + "/data/";
            if (!Directory.Exists(_dataDirectory))
                Directory.CreateDirectory(_dataDirectory);

            Datalog = new DataLogger(logDirectory + Guid.NewGuid() + ".log");

            _activityWindow = new ActivityWindow( this );
            _popUpWindows.Add( _activityWindow );

            _startMenu = new StartMenu( this );
            _popUpWindows.Add( _startMenu );

            _homeProxy = new Proxy
                {
                    Activity = new Activity
                        {
                            Name = "Home"
                        }, 
                    Desktop = DesktopManager.StartupDesktop
                };
            _currentProxy = _homeProxy;

            _proxies.Add( _homeProxy.Activity.Id, _homeProxy );

            _keyboard.KeyPressed += KeyboardKeyPressed;

            _keyboard.RegisterHotKey(0,ModifierKeys.Alt, Keys.N);
            _keyboard.RegisterHotKey(1, ModifierKeys.Alt, Keys.Left);
            _keyboard.RegisterHotKey(2, ModifierKeys.Alt, Keys.Right);
            _keyboard.RegisterHotKey(3,ModifierKeys.Alt,Keys.H);

            UpdateActivityButtons();

            _dockingMenu.Add(DockPosition.Left,btnLeft);
            _dockingMenu.Add(DockPosition.Top, btnTop);
            _dockingMenu.Add(DockPosition.Right, btnRight);
            _dockingMenu.Add(DockPosition.Bottom, btnBottom);

            _panels.Add(pnlActivityButtons);
            _panels.Add(pnlButton);
            _panels.Add(pnlContent);
     
            RunDiscovery();
        }

        private void StartClient(WebConfiguration config)
        {
            if (_client != null)
                return;

                _device = new Device()
                {
                    DeviceType = DeviceType.Laptop,
                    TagValue = "204"
                };

                _client = new ActivityClient(config.Address, config.Port,_device);
                _client.ActivityAdded += _client_ActivityAdded;
                _client.ActivityRemoved += _client_ActivityRemoved;
                _client.ResourceAdded += _client_ResourceAdded;

                foreach (var act in _client.Activities.Values)
                {
                    Dispatcher.Invoke(() =>
                    {
                        AddActivitySpace(act as Activity);
                    });
                }
        }

        void _client_ResourceAdded(object sender, ResourceEventArgs e)
        {
        }
        void _client_ActivityRemoved(object sender, NooSphere.Infrastructure.ActivityRemovedEventArgs e)
        {
            Dispatcher.Invoke(() => RemoveActivitySpaces(e.Id));
        }
        void _client_ActivityAdded(object sender, NooSphere.Infrastructure.ActivityEventArgs e)
        {
            Dispatcher.Invoke(() => AddActivitySpace(e.Activity as Activity));
        }
        private void RunDiscovery()
        {
            var disco = new DiscoveryManager();

            disco.DiscoveryAddressAdded += (sender, e) =>
            {
                var foundWebConfiguration = new WebConfiguration(e.ServiceInfo.Address);
                StartClient(foundWebConfiguration);
            };
            disco.Find(DiscoveryType.Zeroconf);

        }

        protected override void UpdateBorders()
        {
            switch (DockPosition)
            {
                case DockPosition.Bottom:
                    OuterBorder.BorderThickness = InnerBorder.BorderThickness = new Thickness(0, Borderwidth, 0, 0);
                    PanelOrientation = Orientation.Horizontal;
                    _horizontalMode = true;
                    ContentPanelMargin = new Thickness(0, PanelSpacingBorderCompensation, 0, PanelSpacing);
                    ButtonMargin = new Thickness(ActivityButtonSpacing, 0, ActivityButtonSpacing, 0);
                    StartButtonHeight = HorizontalModeSize;
                    break;
                case DockPosition.Left:
                    OuterBorder.BorderThickness = InnerBorder.BorderThickness = new Thickness(0, 0, Borderwidth, 0);
                    PanelOrientation=Orientation.Vertical;
                    _horizontalMode = false;
                    ContentPanelMargin = new Thickness(PanelSpacing, 0, PanelSpacingBorderCompensation, 0);
                    ButtonMargin = new Thickness(0, ActivityButtonSpacing, 0, ActivityButtonSpacing);
                    StartButtonHeight = VerticalModeSize;
                    break;
                case DockPosition.None:
                    OuterBorder.BorderThickness = InnerBorder.BorderThickness = new Thickness(Borderwidth);
                    PanelOrientation = Orientation.Horizontal;
                    _horizontalMode = true;
                    ContentPanelMargin = new Thickness(0, 0, 0, 0);
                    ButtonMargin = new Thickness(0, 0, 0, 0);
                    StartButtonHeight = HorizontalModeSize;
                    break;
                case DockPosition.Right:
                    OuterBorder.BorderThickness = InnerBorder.BorderThickness = new Thickness(Borderwidth, 0, 0, 0);
                    PanelOrientation = Orientation.Vertical;
                    _horizontalMode = false;
                    ContentPanelMargin = new Thickness(PanelSpacingBorderCompensation, 0, PanelSpacing, 0);
                    ButtonMargin = new Thickness(0, ActivityButtonSpacing, 0, ActivityButtonSpacing);
                    StartButtonHeight = VerticalModeSize;
                    break;
                case DockPosition.Top:
                    OuterBorder.BorderThickness = InnerBorder.BorderThickness = new Thickness(0, 0, 0, Borderwidth);
                    PanelOrientation = Orientation.Horizontal;
                    _horizontalMode = true;
                    ContentPanelMargin = new Thickness(0, PanelSpacing, 0, PanelSpacingBorderCompensation);
                    ButtonMargin = new Thickness(ActivityButtonSpacing, 0, ActivityButtonSpacing, 0);
                    StartButtonHeight = HorizontalModeSize;
                    break;
            }
            ButtonHeight = _horizontalMode ? _horizontalButtonHeight : _verticalButtonSize.Height;
            ButtonWidth = _horizontalMode ? _horizontalButtonWidthCollapsed : _verticalButtonSize.Width;

            StartButtonWidth = _horizontalMode ? _horizontalButtonWidthCollapsed : _verticalButtonSize.Width;
            StartButtonHeight = _horizontalMode ? _horizontalButtonHeight : _verticalButtonSize.Width;

            UpdateDockingMenu();
            UpdateActivityButtonsUi();
        }
        private void UpdateActivityButtonsUi()
        {
            foreach ( var prox in _proxies.Values )
            {
                if(prox != _homeProxy)
                    SetButtonWidth( prox.Button );
            }
        }
        private void UpdateDockingMenu()
        {
            foreach (var menu in _dockingMenu.Values.Where(menu => menu.IsCheckable))
                menu.IsChecked = false;
            _dockingMenu[DockPosition].IsChecked = true;
        }
        public void AddEmptyActivity()
        {
            var ac = GetInitializedActivity();
            _client.AddActivity(ac);
        }
        private void RemoveActivitySpaces(string id)
        {
            Datalog.Log(LoggerType.ActivityRemoved, _proxies[id].Activity.Name + " {guid}:" + _proxies[id].Activity.Id);
            Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() =>
            {
                if (pnlActivityButtons.Children != null) 
                    pnlActivityButtons.Children.Remove(_proxies[id].Button);
            }));
            _proxies.Remove( id );
            Datalog.Log(LoggerType.ActivityRemoved, "Removed activity " + id);

        }
        private void AddActivitySpace(Activity activity)
        {
            Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() =>
            {
                var p = new Proxy {Desktop = DesktopManager.CreateEmptyDesktop(), Activity = activity};


                var b = new ActivityButton(activity.Logo != null ? _client.GetFileResourceUri(activity.Logo) : new Uri("pack://application:,,,/Images/activity.PNG"), activity.Name, this)
                {
                    RenderMode = _selectedRenderStyle, ActivityId = p.Activity.Id, AllowDrop = true
                };

                p.Button = b;
                InitializeActivitySpacesButton( b );

                _proxies.Add(p.Activity.Id, p);
                pnlActivityButtons.Children.Add(p.Button);
                Datalog.Log(LoggerType.ActivityCreated, activity.Name + " {guid}:" + activity.Id);
            }));

        }
        private void InitializeActivitySpacesButton( ActivityButton b )
        {
            b.Drop += BDrop;
            b.Click += BClick;
            b.MouseDown += BMouseDown;
            b.Style = (Style)FindResource( "Activitybarbutton" );

            b.SetBinding(MarginProperty,new Binding( "ButtonMargin" ){Source =  this});
            b.SetBinding(HeightProperty,new Binding("ButtonHeight"){Source = this});
            SetButtonWidth( b );
        }
        private void SetButtonWidth( ActivityButton b )
        {
            if ( !_horizontalMode )
            {
                b.Width = _verticalButtonSize.Width;
            }
            else
            {
                b.Width = b.RenderMode == RenderMode.Image ? _horizontalButtonWidthCollapsed : _horizontalButtonWidthOpen;
            }
        }
        private void AddActivitySpaces(Proxy prox)
        {
            Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() =>
                {
                    Directory.CreateDirectory(@"c://activities/" + prox.Activity.Id);
                    InitializeActivitySpacesButton( prox.Button );
                    _proxies.Add(prox.Activity.Id, prox);
                    pnlActivityButtons.Children.Add(prox.Button);
                    Datalog.Log(LoggerType.ActivityCreated, prox.Activity.Name + " {guid}:" + prox.Activity.Id);
                }));
            Datalog.Log(LoggerType.ActivityCreated, "Created activity " + prox.Activity.Id);
        }
        private Proxy GetProxyFromButton(ActivityButton b)
        {
            return _proxies[b.ActivityId];
        }
        private void ShowActivityWindow(ActivityButton btn)
        {
            HideAllPopups();
            var transform = btn.TransformToAncestor(this);
            var rootPoint = transform.Transform(new Point(0, 0));

            Point location;
            switch (DockPosition)
            {
                    case DockPosition.Bottom:
                    location = new Point(rootPoint.X, Screen.PrimaryScreen.WorkingArea.Height - _activityWindow.Height - ActivityWindowOffset);
                    break;
                    case DockPosition.Left:
                    location = new Point(rootPoint.X + Width + ActivityWindowOffset, rootPoint.Y);
                    break;
                    case DockPosition.Right:
                        location =  new Point(Screen.PrimaryScreen.WorkingArea.Width -_activityWindow.Width - ActivityWindowOffset,rootPoint.Y);
                    break;
                    case DockPosition.Top:
                    location = new Point(rootPoint.X, Height + ActivityWindowOffset);
                    break;
                default:
                    location = rootPoint;
                    break;
            }
            _activityWindow.Show(_proxies[btn.ActivityId].Activity, location, btn);
            Datalog.Log(LoggerType.ActivityManager, "Opened activity menu for " + _proxies[btn.ActivityId].Activity.Id);
        }
        public void DeleteActivity()
        {
            var proxy = GetProxyFromButton(_lastclickedButton);
            DesktopManager.SwitchToDesktop(_homeProxy.Desktop);
            _currentProxy = _homeProxy;
            DesktopManager.Merge(proxy.Desktop, DesktopManager.StartupDesktop);

            //Datalog.Log(LoggerType.ActivityRemoved, "Deleted activity " + proxy.Activity.Id);
            //RemoveActivitySpaces(proxy.Activity.Id);

            _client.RemoveActivity(proxy.Activity.Id);
        }
        public void EditActivity(Activity ac, bool renderText,string imgPath)
        {
            if (_lastclickedButton.Text != ac.Name)
            {
                Datalog.Log(LoggerType.ActivityButtonNameChanged, "Changed name of activity " +ac.Id +" from " + _lastclickedButton.Text + " to " + ac.Name);
                _lastclickedButton.Text = ac.Name;
                _client.UpdateActivity(ac);
            }
            if (ac.Meta.Data != null)
            {
                if (File.Exists(ac.Meta.Data) && imgPath !=null)
                {
                    Datalog.Log(LoggerType.ActivityButtonIconChanged, "Changed icon of activity " + ac.Id + " from " + ac.Meta.Data + " to " + _lastclickedButton.Image);
                    _lastclickedButton.Image = new Uri(ac.Meta.Data);
                    if (_lastclickedButton.Image == null) return;
                    _client.AddFileResource(ac, "LOGO", new MemoryStream(File.ReadAllBytes(imgPath)));
                }
            }
            var renderMode= renderText ? RenderMode.ImageAndText : RenderMode.Image;
            if ( _lastclickedButton.RenderMode != renderMode )
            {
                Datalog.Log( LoggerType.ActivityButtonRenderStyleChanged,
                             "Changed renderstyle of activity " + 
                            ac.Id + " from " + _lastclickedButton.RenderMode + " to " + renderMode );
                _lastclickedButton.RenderMode = renderMode;
                SetButtonWidth( _lastclickedButton );
            }
        }
        private void SwitchToProxy(Proxy proxy)
        {
           // DesktopManager.SwitchToDesktop(proxy.Desktop);
            Datalog.Log(LoggerType.ActivitySwitched, "Switched from activity " +_currentProxy.Activity + " to "+proxy.Desktop);
            _currentProxy = proxy;
            UpdateActivityButtons();
        }
        public void ExitApplication()
        {
            ABC.Windows.DesktopManager.ChangeDesktopFolder(_startupDesktopPath);

            HideAllPopups();

            //SaveActivities();

            Close();

            DesktopManager.Close();

            Environment.Exit(0);
        }
        private void HideAllPopups()
        {
            _popUpWindows.ForEach( w => w.Hide());
        }
        private void UpdateActivityButtons()
        {
            foreach (var prox in _proxies.Values.Where(prox => prox != _homeProxy))
                prox.Button.Selected = prox == _currentProxy;
            btnHome.Background.Opacity = _currentProxy != _homeProxy ? 0 : 0.5;
        }
        private void SwitchToHome()
        {
            _currentProxy = _homeProxy;
            UpdateActivityButtons();
            DesktopManager.SwitchToDesktop(_homeProxy.Desktop);
            Datalog.Log(LoggerType.ActivityHome, "Startup activity");
        }
        private void MenuItems_OnClick(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem == null) return;

            //uncheck the menu item as it will be checked after changing the dockingosition
            menuItem.IsChecked = false;

            DockPosition = (DockPosition)Enum.Parse(typeof(DockPosition), menuItem.Header.ToString());
        }
        private void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
            AddEmptyActivity();
        }
        private void KeyboardKeyPressed(object sender, KeyPressedEventArgs e)
        {
            if (e.Modifier == ModifierKeys.Alt)
            {
                switch (e.Key)
                {
                    case Keys.N:
                        {
                            AddEmptyActivity();
                            SwitchToProxy(_proxies.Values.Last());
                        }
                        break;
                    case Keys.Left:
                        {
                            //more than one activity
                            if (_proxies.Values.Count > 0)
                            {
                                var switchToIndex = 0;
                                var index = _proxies.Values.ToList().IndexOf(_currentProxy);

                                if (index == 0)
                                    switchToIndex = _proxies.Values.ToList().IndexOf(_proxies.Values.Last());
                                else
                                    switchToIndex = index - 1;
                                SwitchToProxy(_proxies.Values.ToList()[switchToIndex]);
                            }
                        }
                        break;
                    case Keys.Right:
                        {
                            if (_proxies.Values.Count > 0)
                            {
                                var switchToIndex = 0;
                                var index = _proxies.Values.ToList().IndexOf(_currentProxy);

                                if (index == _proxies.Values.ToList().IndexOf(_proxies.Values.Last()))
                                    switchToIndex = 0;
                                else
                                    switchToIndex = index + 1;
        
                               SwitchToProxy(_proxies.Values.ToList()[switchToIndex]);
                            }
                        }
                        break;
                    case Keys.H:
                        {
                            SwitchToHome();
                        }
                        break;
                }
            }
        }
        private void BMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                var isShown = (_activityWindow.Visibility == Visibility.Visible);
                var isCurrentButton = (sender as ActivityButton) == _lastclickedButton;

                HideAllPopups();

                if (!isShown || (!isCurrentButton))
                    ShowActivityWindow((ActivityButton) sender);
            }
            else
                HideAllPopups();
            _lastclickedButton = (ActivityButton)sender;
        }
        private void BtnHomeClick(object sender, RoutedEventArgs e)
        {
            SwitchToHome();
        }
        private void BClick(object sender, RoutedEventArgs e)
        {
            HideAllPopups();
            Datalog.Log(LoggerType.ActivitySwitched,
                        "Switch from " + _currentProxy.Activity.Id + " to " +
                        _proxies[((ActivityButton) sender).ActivityId].Activity.Id);

            SwitchToProxy(_proxies[((ActivityButton) sender).ActivityId]);
            _currentProxy = _proxies[((ActivityButton) sender).ActivityId];

            if(_client != null)
                _client.SendMessage(MessageType.ActivityChanged, _proxies[((ActivityButton)sender).ActivityId].Activity.Id);

            UpdateActivityButtons();
        }
        private void BDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
        }
        private void BDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var droppedFilePaths =
                e.Data.GetData(DataFormats.FileDrop, true) as string[];
            if (droppedFilePaths == null) return;

            foreach (var dp in droppedFilePaths)
            {
                var fInfo = new FileInfo(dp);
                var ext = fInfo.Extension;

                if (File.Exists(fInfo.FullName))
                {
                    if (ext == ".pdf")
                    {
                        Task.Factory.StartNew(() =>
                        {
                            var res = PdfConverter.ConvertPdfToFileBytes(fInfo.FullName);
                            if (res !=null)
                                _client.AddFileResource(GetProxyFromButton((ActivityButton)sender).Activity, "PDF", new MemoryStream(res));

                        });

                    }
                    else if(ext ==".jpg" || ext == ".png")
                        _client.AddFileResource(GetProxyFromButton((ActivityButton)sender).Activity, "IMG", new MemoryStream(File.ReadAllBytes(fInfo.FullName)));
                }
            }

           
        }
        private void btnClose_click(object sender, RoutedEventArgs e)
        {
            ExitApplication();
        }
        private void btnWin_click(object sender, RoutedEventArgs e)
        {
            Process.Start(AppDomain.CurrentDomain.BaseDirectory + "/vdm/ABC.Windows.Desktop.Monitor.exe");
        }
        private void btnWs_click(object sender, RoutedEventArgs e)
        {
            Process.Start(AppDomain.CurrentDomain.BaseDirectory + "/ws/WorkspaceSwitcher.exe");
        }
        private void ActivityBar_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
                return;

                HideAllPopups();
        }

        #region Helper

        /// <summary>
        /// Generates a default activity
        /// </summary>
        /// <returns>An intialized activity</returns>
        public Activity GetInitializedActivity()
        {
            var ac = new Activity
            {
                Name = "nameless",
                Description = "This is the description of the test activity - " + DateTime.Now
            };
            return ac;
        }

        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

    }
}