﻿using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using TCC.Data;
using TCC.Data.Chat;
using TCC.Interop.Proxy;
using TCC.Windows;

namespace TCC.Controls.Chat
{
    public partial class MessagePieceBodyControl
    {
        private MessagePieceBase _context;

        public MessagePieceBodyControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_context != null) return;
            _context = (MessagePieceBase)DataContext;
        }

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_context == null) return;
            _context.IsHovered = true;

        }
        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            _context.IsHovered = false;
        }
    }
}