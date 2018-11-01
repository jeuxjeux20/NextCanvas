﻿namespace NextCanvas.ViewModels
{
    public class ViewModelBase<T> : PropertyChangedObject, IViewModel<T> where T : new()
    {
        public ViewModelBase(T model)
        {
            Model = model;
        }

        public ViewModelBase()
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            // I know this isn't the best :c
            // but it's not bad according that it's the effect we want
            Model = BuildDefaultModel();
        }

        public T Model { get; protected set; }

        public override string ToString()
        {
            var modelToString = Model.ToString();
            return modelToString == Model.GetType().ToString() ? base.ToString() : modelToString;
        }

        protected virtual T BuildDefaultModel()
        {
            return new T();
        }
    }
}