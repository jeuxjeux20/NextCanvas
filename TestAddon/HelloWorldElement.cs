﻿using NextCanvas;
using NextCanvas.Extensibility.Content;

namespace TestAddon
{
    [ContentAddonElement(Name = "Hello world!")]
    public class HelloWorldElement : ContentElementAddon
    {
        public HelloWorldElement() : this(null) { }

        public HelloWorldElement(ContentElementAddonModel m = null) : base(m)
        {
            Fun++;
            MoreWorldsCommand = new DelegateCommand(o => Fun++);
        }
        public int Fun
        {
            get => GetProperty<int>();
            set => SetProperty(value);
        }
        public DelegateCommand MoreWorldsCommand { get; private set; }
    }
}
