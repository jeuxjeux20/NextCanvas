﻿#region

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using NextCanvas.Interactivity;

#endregion

namespace NextCanvas
{
    public class InteractionWindow : Window, IInteractionBase
    {
        public InteractionWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        public virtual void CloseInteraction()
        {
            Dispatcher.Invoke(Close);
        }
        
        public virtual void ShowInteraction() => ShowInteraction(true);
        public virtual void ShowInteraction(bool modal)
        {
            Task.Run(() =>
            {
                if (modal)
                {
                    Dispatcher.BeginInvoke(new Action(() => ShowDialog()));
                }
                else
                {
                    Dispatcher.Invoke(Show);
                }
            });
        }
    }
}