﻿#region

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
            if (modal)
            {
                Dispatcher.Invoke(ShowDialog);
            }
            else
            {
                Dispatcher.Invoke(Show);
            }
        }

        protected void RefreshUI(DispatcherPriority priority = DispatcherPriority.DataBind)
        {
            Dispatcher.Invoke(() => { }, priority);
        }
    }
}