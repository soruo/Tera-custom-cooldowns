﻿using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace TCC.Windows
{
    public class TccWindow : Window
    {
        protected IntPtr _handle;
        protected bool _ignoreSize;
        protected bool clickThru;
        public bool ClickThru
        {
            get { return clickThru; }
            set
            {
                if (clickThru == value) return;
                clickThru = value;

                if (clickThru) FocusManager.MakeTransparent(_handle);
                else FocusManager.UndoTransparent(_handle);

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ClickThru"));
            }
        }

        protected WindowSettings _settings;

        protected void InitWindow(WindowSettings ws, bool canClickThru = true, bool canHide = true, bool ignoreSize = true)
        {
            _handle = new WindowInteropHelper(this).Handle;
            FocusManager.MakeUnfocusable(_handle);
            FocusManager.HideFromToolBar(_handle);
            Topmost = true;
            ContextMenu = new System.Windows.Controls.ContextMenu();

            if (canHide)
            {
                var HideButton = new System.Windows.Controls.MenuItem() { Header = "Hide" };
                HideButton.Click += (s, ev) =>
                {
                    SetVisibility(Visibility.Hidden);
                };
                ContextMenu.Items.Add(HideButton);
            }

            if (canClickThru)
            {
                var ClickThruButton = new System.Windows.Controls.MenuItem() { Header = "Click through" };
                ClickThruButton.Click += (s, ev) =>
                {
                    SetClickThru(true);
                };
                ContextMenu.Items.Add(ClickThruButton);
            }

            _settings = ws;
            Left = ws.X;
            Top = ws.Y;
            if (!ignoreSize)
            {
                if (ws.H != 0) Height = ws.H;
                if (ws.W != 0) Width = ws.W;
            }
            _ignoreSize = ignoreSize;
            Visibility = ws.Visibility;
            SetClickThru(ws.ClickThru);
            ((FrameworkElement)this.Content).Opacity = 0;

            WindowManager.TccVisibilityChanged += OpacityChange;
            WindowManager.TccDimChanged += OpacityChange;
            SizeChanged += TccWindow_SizeChanged;
            Closed += TccWindow_Closed;
            Loaded += TccWindow_Loaded;
        }

        private void TccWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_settings.Enabled) CloseWindowSafe();
        }

        private void TccWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_ignoreSize) return;
            _settings.W = ActualWidth;
            _settings.H = ActualHeight;
            SettingsManager.SaveSettings();
        }

        private void TccWindow_Closed(object sender, EventArgs e)
        {
            Dispatcher.InvokeShutdown();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OpacityChange(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsTccVisible")
            {
                if (WindowManager.IsTccVisible)
                {
                    if (WindowManager.IsTccDim && _settings.AutoDim)
                    {
                        AnimateContentOpacity(_settings.DimOpacity);
                    }
                    else
                    {
                        AnimateContentOpacity(1);
                    }
                }
                else
                {
                    if (_settings.ShowAlways) return;
                    AnimateContentOpacity(0);
                }
            }

            if (e.PropertyName == "IsTccDim")
            {
                if (!WindowManager.IsTccVisible) return;

                if (WindowManager.IsTccDim && _settings.AutoDim)
                {
                    AnimateContentOpacity(_settings.DimOpacity);
                    if (SettingsManager.ClickThruWhenDim) FocusManager.MakeTransparent(_handle);
                }
                else
                {
                    AnimateContentOpacity(1);
                        if (!_settings.ClickThru) FocusManager.UndoTransparent(_handle);
                }
            }
        }
        public void SetClickThru(bool t)
        {
            ClickThru = t;
        }
        public void SetVisibility(Visibility v)
        {
            if (!Dispatcher.Thread.IsAlive)
            {
                return;
            }
            Dispatcher.Invoke(() =>
            {
                Visibility = v;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Visibility"));
            });
        }
        public void AnimateContentOpacity(double opacity)
        {
            Dispatcher.InvokeIfRequired(() =>
            {
                ((FrameworkElement)this.Content).BeginAnimation(OpacityProperty, new DoubleAnimation(opacity, TimeSpan.FromMilliseconds(250)));
            }, System.Windows.Threading.DispatcherPriority.DataBind);
        }
        public void RefreshTopmost()
        {
            Dispatcher.InvokeIfRequired(() => { Topmost = false; Topmost = true; }, System.Windows.Threading.DispatcherPriority.DataBind);
        }
        public void RefreshSettings(WindowSettings ws)
        {
            _settings = ws;
        }
        protected void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!_ignoreSize) ResizeMode = ResizeMode.NoResize;
                DragMove();
                if (!_ignoreSize) ResizeMode = ResizeMode.CanResize;
                var screen = Screen.FromHandle(new WindowInteropHelper(this).Handle);
                var source = PresentationSource.FromVisual(this);
                if (source?.CompositionTarget == null) return;
                var m = source.CompositionTarget.TransformToDevice;
                var dx = m.M11;
                var dy = m.M22;
                var newLeft = Left * dx;
                var newTop = Top * dx;
                _settings.X = newLeft / dx;
                _settings.Y = newTop / dy;

                SettingsManager.SaveSettings();
            }
            catch (Exception) { }
        }
        public void CloseWindowSafe()
        {
            if (Dispatcher.CheckAccess())
                Close();
            else
                Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(Close));
        }
    }
}