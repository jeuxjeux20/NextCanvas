﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextCanvas.ViewModels
{
    public class ViewModelBase<T> : PropertyChangedObject where T : new()
    {
        public T Model { get; set; }
        public ViewModelBase(T model) {
            Model = model;
        }
        public ViewModelBase()
        {
            Model = new T();
        }
    }
}
