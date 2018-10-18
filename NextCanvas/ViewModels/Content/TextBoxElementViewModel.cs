﻿using NextCanvas.Models.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextCanvas.ViewModels.Content
{
    public class TextBoxElementViewModel : ContentElementViewModel<TextBoxElement>
    {
        public string RtfText
        {
            get { return Model.RtfText; }
            set { Model.RtfText = value; OnPropertyChanged(nameof(RtfText)); }
        }
    }
}
