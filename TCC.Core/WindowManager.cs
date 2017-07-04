﻿using DamageMeter.Sniffing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TCC.ViewModels;
using TCC.Windows;

namespace TCC
{
    public static class WindowManager
    {
        public static CooldownWindow CooldownWindow;
        public static CharacterWindow CharacterWindow;
        public static BossGageWindow BossGauge;
        public static AbnormalitiesWindow BuffBar;
        public static GroupWindow GroupWindow;
        public static ClassWindow ClassWindow;
        public static ChatWindow ChatWindow;
        public static SettingsWindow Settings;

        public static ContextMenu ContextMenu;

        public static System.Windows.Forms.NotifyIcon TrayIcon;
        public static Icon DefaultIcon;
        public static Icon ConnectedIcon;


        private static bool clickThru;
        public static bool ClickThru
        {
            get => clickThru;
            set
            {
                if (clickThru != value)
                {
                    clickThru = value;
                    ClickThruChanged?.Invoke(null, new PropertyChangedEventArgs("ClickThru"));
                }
            }
        }

        private static bool isTccVisible;
        public static bool IsTccVisible
        {
            get
            {
                if (SessionManager.Logged && !SessionManager.LoadingScreen && IsFocused)
                {
                    isTccVisible = true;
                    return isTccVisible;
                }
                else
                {
                    isTccVisible = false;
                    return isTccVisible;
                }
            }
            set
            {
                if (isTccVisible != value)
                {
                    isTccVisible = value;
                    NotifyVisibilityChanged();
                }
            }
        }
        private static bool isFocused;
        public static bool IsFocused
        {
            get => isFocused;
            set
            {
                if (isFocused != value)
                {
                    isFocused = value;
                    NotifyVisibilityChanged();
                }
            }
        }

        static System.Timers.Timer _undimTimer;

        private static bool skillsEnded = true;
        public static bool SkillsEnded
        {
            get => skillsEnded;
            set
            {
                if (value == false)
                {
                    _undimTimer.Stop();
                    _undimTimer.Start();
                }
                if (skillsEnded == value) return;
                skillsEnded = value;
                NotifyDimChanged();
            }
        }

        public static bool IsTccDim
        {
            get => SkillsEnded && !SessionManager.Encounter; // add more conditions here if needed
        }

        public static void NotifyDimChanged()
        {
            TccDimChanged?.Invoke(null, new PropertyChangedEventArgs("IsTccDim"));
        }
        public static void NotifyVisibilityChanged()
        {
            TccVisibilityChanged?.Invoke(null, new PropertyChangedEventArgs("IsTccVisible"));
        }

        public static event PropertyChangedEventHandler ClickThruChanged;
        public static event PropertyChangedEventHandler TccVisibilityChanged;
        public static event PropertyChangedEventHandler TccDimChanged;

        public static Visibility StaminaGaugeVisibility;
        public static double StaminaGaugeTop;
        public static double StaminaGaugeLeft;

        static List<Delegate> WindowLoadingDelegates = new List<Delegate>
        {
            new Action(LoadCooldownWindow),
            new Action(LoadBossGaugeWindow),
            new Action(LoadBuffBarWindow),
            new Action(LoadGroupWindow),
            new Action(LoadChatWindow),
            new Action(LoadCharWindow),
            new Action(LoadClassWindow)
        };
        static void LoadCharWindow()
        {
            var charWindowThread = new Thread(new ThreadStart(() =>
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                CharacterWindow = new CharacterWindow();
                CharacterWindowManager.Instance.Player = new Data.Player();
                CharacterWindow.Show();
                waiting = false;
                Dispatcher.Run();
            }));
            charWindowThread.Name = "Character window thread";
            charWindowThread.SetApartmentState(ApartmentState.STA);
            charWindowThread.Start();
            Console.WriteLine("Char window loaded");
        }
        static void LoadCooldownWindow()
        {
            var cooldownWindowThread = new Thread(new ThreadStart(() =>
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                CooldownWindow = new CooldownWindow();
                CooldownWindow.Show();
                waiting = false;
                Dispatcher.Run();
            }));
            cooldownWindowThread.Name = "Cooldown bar thread";
            cooldownWindowThread.SetApartmentState(ApartmentState.STA);
            cooldownWindowThread.Start();
            Console.WriteLine("Cd window loaded");


        }
        static void LoadBossGaugeWindow()
        {

            var bossGaugeThread = new Thread(new ThreadStart(() =>
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                BossGauge = new BossGageWindow();
                BossGageWindowManager.Instance.CurrentNPCs = new SynchronizedObservableCollection<Data.Boss>(BossGauge.Dispatcher);
                BossGauge.Show();
                waiting = false;

                Dispatcher.Run();
            }));
            bossGaugeThread.Name = "Boss gauge thread";
            bossGaugeThread.SetApartmentState(ApartmentState.STA);
            bossGaugeThread.Start();
            Console.WriteLine("Boss window loaded");

        }
        static void LoadBuffBarWindow()
        {
            var buffBarThread = new Thread(new ThreadStart(() =>
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                BuffBar = new AbnormalitiesWindow();
                BuffBarWindowManager.Instance.Player = new Data.Player();
                BuffBar.Show();
                waiting = false;

                Dispatcher.Run();
            }));
            buffBarThread.Name = "Buff bar thread";
            buffBarThread.SetApartmentState(ApartmentState.STA);
            buffBarThread.Start();
            Console.WriteLine("Buff window loaded");


        }
        static void LoadGroupWindow()
        {
            var groupWindowThread = new Thread(new ThreadStart(() =>
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                GroupWindow = new GroupWindow();
                GroupWindowManager.Instance.Dps = new SynchronizedObservableCollection<Data.User>(GroupWindow.Dispatcher);
                GroupWindowManager.Instance.Healers = new SynchronizedObservableCollection<Data.User>(GroupWindow.Dispatcher);
                GroupWindowManager.Instance.Tanks = new SynchronizedObservableCollection<Data.User>(GroupWindow.Dispatcher);
                GroupWindow.Show();
                waiting = false;

                Dispatcher.Run();
            }));
            groupWindowThread.Name = "Group window thread";
            groupWindowThread.SetApartmentState(ApartmentState.STA);
            groupWindowThread.Start();
            Console.WriteLine("Group window loaded");

        }
        static void LoadChatWindow()
        {
            var chatWindowThread = new Thread(new ThreadStart(() =>
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                ChatWindow = new ChatWindow();
                ChatWindow.Show();
                waiting = false;

                Dispatcher.Run();
            }));
            chatWindowThread.Name = "Chat thread";
            chatWindowThread.SetApartmentState(ApartmentState.STA);
            chatWindowThread.Start();
            Console.WriteLine("Chat window loaded");

        }
        static void LoadClassWindow()
        {      
            var t = new Thread(new ThreadStart(() =>
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                ClassWindow = new ClassWindow();
                ClassWindow.Closed += (s, ev) => ClassWindow.Dispatcher.InvokeShutdown();
                ClassWindow.Show();
                waiting = false;

                Dispatcher.Run();
            }));
            t.Name = "Class bar thread";
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            Console.WriteLine("Class window loaded");


        }
        static bool waiting;
        static void LoadWindows()
        {
            waiting = true;
            foreach (var del in WindowLoadingDelegates)
            {
                waiting = true;
                del.DynamicInvoke();
                while (waiting) { }
            }
            Console.WriteLine("Windows loaded");

        }
        public static void Init()
        {
            LoadWindows();
            ContextMenu = new ContextMenu();
            DefaultIcon = new Icon(Application.GetResourceStream(new Uri("resources/tcc-logo.ico", UriKind.Relative)).Stream);
            ConnectedIcon = new Icon(Application.GetResourceStream(new Uri("resources/tcc-logo-on.ico", UriKind.Relative)).Stream);
            TrayIcon = new System.Windows.Forms.NotifyIcon()
            {
                Icon = DefaultIcon,
                Visible = true
            };
            TrayIcon.MouseDown += NI_MouseDown;
            TrayIcon.MouseDoubleClick += TrayIcon_MouseDoubleClick;
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            TrayIcon.Text = String.Format("TCC v{0}.{1}.{2}", v.Major, v.Minor, v.Build);
            var CloseButton = new MenuItem() { Header = "Close" };

            CloseButton.Click += (s, ev) => App.CloseApp();
            ContextMenu.Items.Add(CloseButton);

            _undimTimer = new System.Timers.Timer(5000);
            _undimTimer.Elapsed += _undimTimer_Elapsed;

            FocusManager.FocusTimer = new System.Timers.Timer(1000);
            FocusManager.FocusTimer.Elapsed += FocusManager.CheckForegroundWindow;

            ClickThruChanged += (s, ev) => UpdateClickThru();
        }

        private static void _undimTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            SkillsEnded = true;
            _undimTimer.Stop();
        }

        private static void TrayIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (Settings == null)
            {
                Settings = new SettingsWindow()
                {
                    Name = "Settings"
                };
            }
            Settings.Opacity = 0;
            Settings.Show();
            Settings.BeginAnimation(Window.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)));
        }

        public static void Dispose()
        {
            FocusManager.FocusTimer.Stop();
            TrayIcon.Visible = false;


            foreach (Window w in Application.Current.Windows)
            {
                w.Close();
            }
        }

        static bool isForeground = false;
        private static void SetClickThru()
        {
            foreach (Window w in Application.Current.Windows)
            {
                if (w.GetType() == typeof(SettingsWindow)) continue;
                FocusManager.MakeTransparent(new WindowInteropHelper(w).Handle);
            }
        }
        private static void UnsetClickThru()
        {
            foreach (Window w in Application.Current.Windows)
            {
                if (w.GetType() == typeof(SettingsWindow)) continue;
                FocusManager.UndoTransparent(new WindowInteropHelper(w).Handle);
            }

        }
        private static void UpdateClickThru()
        {
            if (ClickThru)
            {
                SetClickThru();
            }
            else
            {
                UnsetClickThru();
            }

        }
        private static DoubleAnimation OpacityAnimation(double to)
        {
            return new DoubleAnimation(to, TimeSpan.FromMilliseconds(300)) { EasingFunction = new QuadraticEase() };
        }
        private static void NI_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                ContextMenu.IsOpen = true;
            }
            else if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                ContextMenu.IsOpen = false;
            }
        }
    }
}
