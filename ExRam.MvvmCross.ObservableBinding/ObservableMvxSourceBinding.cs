﻿// ExRam.MvvmCross.ObservableBinding (c) ExRam GmbH & Co. KG http://www.exram.de
// ExRam.MvvmCross.ObservableBinding is licensed using Microsoft Public License (Ms-PL)

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reflection;
using Cirrious.MvvmCross.Binding;
using Cirrious.MvvmCross.Binding.Bindings.Source;
using Cirrious.MvvmCross.Binding.Parse.PropertyPath.PropertyTokens;

namespace ExRam.MvvmCross.ObservableBinding
{
    public sealed class ObservableMvxSourceBinding : IMvxSourceBinding
    {
        public event EventHandler Changed;

        private readonly IDisposable _sourceSubscription;
        private readonly Type _sourceType;

        private object _currentValue;
        private IMvxSourceBinding _currentSubBinding;
        private IDisposable _currentSubBindingSubscription;

        public ObservableMvxSourceBinding(IObservable<object> source, List<MvxPropertyToken> remainingTokens)
        {
            this._sourceType = source is BindingToObservableWrapper
                                   ? ((BindingToObservableWrapper)source).SourceType.GetTypeInfo().GenericTypeArguments[0]
                                   : typeof(object);

            this._sourceSubscription = source
                .ToWeakObservable()
                .Subscribe(value =>
                {
                    this._currentValue = value;

                    if (this._currentSubBinding != null)
                    {
                        this._currentSubBinding.Dispose();
                        this._currentSubBinding = null;
                    }

                    if (this._currentSubBindingSubscription != null)
                    {
                        this._currentSubBindingSubscription.Dispose();
                        this._currentSubBindingSubscription = null;
                    }

                    if ((remainingTokens != null) && (remainingTokens.Count > 0))
                        this._currentSubBinding = MvxBindingSingletonCache.Instance.SourceBindingFactory.CreateBinding(value, remainingTokens);
                     
                    this._currentSubBindingSubscription = ((this._currentSubBinding != null) ? (new BindingToObservableWrapper(this._currentSubBinding)) : (Observable.Return<object>(null)))
                        .Subscribe((value2 => 
                        {
                            var changed2 = this.Changed;
                            if (changed2 != null)
                                changed2(this, EventArgs.Empty);
                        }));
                });
        }

        public object GetValue()
        {
            if (this._currentSubBinding != null)
                return this._currentSubBinding.GetValue();

            return this._currentValue;
        }

        public void SetValue(object value)
        {
            //Cannot possibly set a field of type IObservable<>?
        }

        public void Dispose()
        {
            if (this._currentSubBinding != null)
                this._currentSubBinding.Dispose();

            if (this._currentSubBindingSubscription != null)
                this._currentSubBindingSubscription.Dispose();

            this._sourceSubscription.Dispose();
        }

        public Type SourceType
        {
            get
            {
                return this._currentSubBinding != null ? this._currentSubBinding.SourceType : this._sourceType;
            }
        }
    }
}
