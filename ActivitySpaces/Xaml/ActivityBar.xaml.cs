using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using ABC.Model;
using ABC.Windows;
using ABC.Windows.Desktop;
using ABC.Windows.Desktop.Settings;
using ActivitySpaces.Input;
using ActivitySpaces.Xaml.PopUp;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using System.Diagnostics;
using MenuItem = System.Windows.Controls.MenuItem;
using ModifierKeys = ActivitySpaces.Input.ModifierKeys;
using System.Windows.Controls;
using Binding = System.Windows.Data.Binding;
using Orientation = System.Windows.Controls.Orientation;
using System.Windows.Media;

namespace ActivitySpaces.Xaml
{
    public partial class ActivityBar : INotifyPropertyChanged
    {
        public static DataLogger Datalog;

        private readonly List<Window> _popUpWindows = new List<Window>();
        private readonly Dictionary<string, Proxy> _proxies = new Dictionary<string, Proxy>();
        private readonly VirtualDesktopManager _desktopManager = new VirtualDesktopManager(new LoadedSettings());
        private readonly WindowMonitor _windowMonitor = new WindowMonitor();
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

        public ActivityBar()
        {
            InitializeComponent();

            Loaded += ActivityBarLoaded;

            DataContext = this;

            Width = Height = 0;
            VerticalModeSize = 62;
            HorizontalModeSize = 40;

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
                    Desktop = _desktopManager.StartupDesktop
                };
            _currentProxy = _homeProxy;

            _proxies.Add( _homeProxy.Activity.Id, _homeProxy );

            _windowMonitor = new WindowMonitor();
            _windowMonitor.WindowActivated += wMon_WindowActivated;
            _windowMonitor.WindowCreated += wMon_WindowCreated;
            _windowMonitor.WindowDestroyed += wMon_WindowDestroyed;

            _windowMonitor.Start();

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
        }

        #region Private Methods
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
        protected override void UpdateInterface()
        {
            //No UI changes needed
        }
        public void AddEmptyActivity()
        {
            AddActivitySpace(GetInitializedActivity());
        }
        private void RemoveActivitySpaces(string id)
        {
            Datalog.Log(LoggerType.ActivityRemoved, _proxies[id].Activity.Name + " {guid}:" + _proxies[id].Activity.Id);
            Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() =>
                { if (pnlActivityButtons.Children != null) pnlActivityButtons.Children.Remove(_proxies[id].Button); }));
            _proxies.Remove( id );
            Datalog.Log(LoggerType.ActivityRemoved, "Removed activity " + id);

        }
        private void AddActivitySpace(Activity activity)
        {
            Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() =>
            {
                var path = Directory.CreateDirectory(@"c://activities/" + activity.Id);
                var p = new Proxy {Desktop = _desktopManager.CreateEmptyDesktop(path.FullName), Activity = activity};

                var b = new ActivityButton(new Uri("pack://application:,,,/Images/activity.PNG"), activity.Name,this) { RenderMode = _selectedRenderStyle, ActivityId = p.Activity.Id, AllowDrop = true };

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
            _desktopManager.SwitchToDesktop(_homeProxy.Desktop);
            _currentProxy = _homeProxy;
            _desktopManager.Merge(proxy.Desktop, _desktopManager.StartupDesktop);

            Datalog.Log(LoggerType.ActivityRemoved, "Deleted activity " + proxy.Activity.Id);
            RemoveActivitySpaces(proxy.Activity.Id);
        }
        public void EditActivity(Activity ac, bool renderText)
        {
            if (_lastclickedButton.Text != ac.Name)
            {
                Datalog.Log(LoggerType.ActivityButtonNameChanged, "Changed name of activity " +ac.Id +" from " + _lastclickedButton.Text + " to " + ac.Name);
                _lastclickedButton.Text = ac.Name;
            }
            if (ac.Meta.Data != null)
            {
                if (File.Exists(ac.Meta.Data))
                {
                    Datalog.Log(LoggerType.ActivityButtonIconChanged, "Changed icon of activity " + ac.Id + " from " + ac.Meta.Data + " to " + _lastclickedButton.Image);
                    _lastclickedButton.Image = new Uri(ac.Meta.Data);
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
            _desktopManager.SwitchToDesktop(proxy.Desktop);
            Datalog.Log(LoggerType.ActivitySwitched, "Switched from activity " +_currentProxy.Activity + " to "+proxy.Desktop);
            _currentProxy = proxy;
            UpdateActivityButtons();
        }
        public void ExitApplication()
        {
            DesktopManager.ChangeDesktopFolder(_startupDesktopPath);

            HideAllPopups();

            SaveActivities();

            Close();

            _desktopManager.Close();

            Environment.Exit(0);
        }
        private void SaveActivities()
        {
            var s = new DataContractSerializer(typeof(List<SavedProxy>));
            using (var fs = File.Open(_dataDirectory + "activities.xml", FileMode.Create))
            {
                var sProxList = (from prox in _proxies.Values where prox != _homeProxy select prox.GetSaveableProxy()).ToList();
                s.WriteObject(fs, sProxList);
            }
        }
        private void LoadActivities()
        {
            if (!File.Exists(_dataDirectory + "activities.xml")) return;
            var s = new DataContractSerializer(typeof(List<SavedProxy>));
            using (var fs = File.Open(_dataDirectory+ "activities.xml", FileMode.Open))
            {
                var list = (List<SavedProxy>)s.ReadObject(fs);
                foreach (var sProx in list)
                {
                    var proxy = new Proxy
                        {
                            Activity = sProx.Activity,
                            Button = new ActivityButton(this)
                                {
                                    RenderMode = sProx.Button.RenderMode,
                                    Image = sProx.Button.Image,
                                    Text = sProx.Button.Text,
                                    ActivityId = sProx.Activity.Id
                                },
                            Desktop = _desktopManager.CreateDesktopFromSession(sProx.Sessions,sProx.Folder)
                        };
                    AddActivitySpaces(proxy);
                }
            }
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
            _desktopManager.SwitchToDesktop(_homeProxy.Desktop);
            Datalog.Log(LoggerType.ActivityHome, "Startup activity");
        }
        #endregion

        #region Event Handlers
        private void ActivityBarLoaded(object sender, RoutedEventArgs e)
        {
            LoadActivities();
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
        private void wMon_WindowDestroyed(IntPtr oldWindowHandle)
        {
            Datalog.Log(LoggerType.WindowDestroyed, "Window " + oldWindowHandle + " closed");
        }
        private void wMon_WindowCreated(WindowInfo newWindow)
        {
            Datalog.Log(LoggerType.WindowCreated, "Window " + newWindow.GetTitle() + " opened");
        }
        private void wMon_WindowActivated(WindowInfo window, bool fullscreen)
        {

            Datalog.Log(LoggerType.WindowActivated, "Window " + window.GetTitle() + " opened");
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
            var fInfo = new FileInfo(droppedFilePaths[0]); ;
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
        #endregion

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
