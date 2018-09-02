﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using TCC.Data;
using TCC.ViewModels;

namespace TCC.Controls.ClassBars
{
    /// <summary>
    /// Logica di interazione per EdgeArrowLayout.xaml
    /// </summary>
    public partial class EdgeArrowLayout : UserControl
    {
        private WarriorBarManager _dc;
        public EdgeArrowLayout()
        {
            InitializeComponent();
            this.Loaded += (_, __) =>
            {
                _dc = DataContext as WarriorBarManager;
                if (_dc != null) _dc.EdgeCounter.PropertyChanged += OnEdgePropertyChanged;
                else Console.WriteLine("[EdgeArrowLayout] DataContext is null!");
            };
        }

        private List<UIElement> GetSortedEdge()
        {
            var ret = new List<UIElement>();
            for (int i = 4; i >= 0; i--)
            {
                ret.Add(Edge5to1.Children[i]);
            }
            for (int i = 4; i >= 0; i--)
            {
                ret.Add(Edge10to6.Children[i]);
            }
            return ret;
        }
        private void OnEdgePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Counter.Val):
                    var rects = GetSortedEdge();
                    for (int i = 0; i < 10; i++)
                    {
                        if (i < _dc.EdgeCounter.Val)
                        {
                            rects[i].Opacity = 1;
                            (rects[i] as Rectangle).Fill = i < 8 ? i == 7 ? App.Current.FindResource("AquadraxColor") as SolidColorBrush :
                                                                            App.Current.FindResource("IgnidraxColor") as SolidColorBrush :
                                                                            App.Current.FindResource("HpColor") as SolidColorBrush;
                        }
                        else
                        {
                            rects[i].Opacity = 0.1;
                            (rects[i] as Rectangle).Fill = Brushes.White;
                        }
                    }
                    if (_dc.EdgeCounter.Val == 8 || _dc.EdgeCounter.Val == 10)
                    {
                        if (_dc.EdgeCounter.Val == 10)
                        {
                            MainEdgeGrid.Effect =
                                new DropShadowEffect { BlurRadius = 15, ShadowDepth = 0, Color = Colors.White };
                        }
                    }
                    else
                    {
                        MainEdgeGrid.Effect = App.Current.FindResource("DropShadow") as DropShadowEffect;
                    }
                    break;
            }

        }
    }
}