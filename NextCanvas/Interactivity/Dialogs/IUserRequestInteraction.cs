﻿#region

using System;

#endregion

namespace NextCanvas.Interactivity.Dialogs
{
    public interface IUserRequestInteraction : IUserInteraction<DialogResultEventArgs>, IContentInteraction
    {
       
    }

    public class DialogResultEventArgs : EventArgs
    {
        public string ChosenButtonText { get; }
        public bool IsAccept { get; }
        public DialogResultEventArgs(string chosen, bool isAccept)
        {
            ChosenButtonText = chosen;
            IsAccept = isAccept;
        }
    }
}