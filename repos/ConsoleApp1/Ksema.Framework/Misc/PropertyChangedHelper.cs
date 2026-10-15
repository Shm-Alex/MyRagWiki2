using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Ksema.Framework;

namespace Framework.Misc
{
	[DataContract]
    public abstract class PropertyChangedHelper : INotifyPropertyChanged, IObservableNotifyPropertyChanged
    {
        /// <summary>
        /// Warns the developer if this object does not have
        /// a public property with the specified name. This 
        /// method does not exist in a Release build.
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public void VerifyPropertyName(string propertyName)
        {
            // Verify that the property name matches a real,  
            // public, instance property on this object.
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                string msg = "Invalid property name: " + propertyName;

                if (this.ThrowOnInvalidPropertyName)
                    throw new Exception(msg);
                else
                    Debug.Fail(msg);
            }
        }

        /// <summary>
        /// Returns whether an exception is thrown, or if a Debug.Fail() is used
        /// when an invalid property name is passed to the VerifyPropertyName method.
        /// The default value is false, but subclasses used by unit tests might 
        /// override this property's getter to return true.
        /// </summary>
        protected virtual bool ThrowOnInvalidPropertyName { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangedEventHandler PropertyChangedWeak
        {
            add
            {
                PropertyChanged += value.MakeWeak(eh => this.PropertyChanged -= eh);
            }
            remove { }
        }

        public Subject<IEvent<PropertyChangedEventArgs>> _propertyChangedSubject;

        public IObservable<IEvent<PropertyChangedEventArgs>> PropertyChangedObservable
        {
            get
            {
                if (_propertyChangedSubject == null)
                {
                    _propertyChangedSubject = new Subject<IEvent<PropertyChangedEventArgs>>();
                }
                return _propertyChangedSubject;
            }
        }

        /// <summary>
        /// Notify using pre-made PropertyChangedEventArgs
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            if (NeedRaisePropertyChanged())
            {
                RaisePropertyChanged(args);
            }
            if (NeedRaiseOnNext())
            {
                RaiseOnNext(args);
            }
        }

        protected void RaisePropertyChanged(PropertyChangedEventArgs args)
        {
            PropertyChanged(this, args);
        }

        protected void RaiseOnNext(PropertyChangedEventArgs args)
        {
            _propertyChangedSubject.OnNext(Event.Create(this, args));
        }

        protected bool NeedRaisePropertyChanged()
        {
            return PropertyChanged != null;
        }

        protected bool NeedRaiseOnNext()
        {
            return _propertyChangedSubject != null;
        }
        /// <summary>
        /// Notify using String property name
        /// </summary>
        protected void OnPropertyChanged(String propertyName)
        {
            //this.VerifyPropertyName(propertyName);
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Creates PropertyChangedEventArgs
        /// </summary>
        /// <param name="propertyExpression">Expression to make 
        /// PropertyChangedEventArgs out of</param>
        /// <returns>PropertyChangedEventArgs</returns>
        public static PropertyChangedEventArgs CreateArgs<T>(
            Expression<Func<T, Object>> propertyExpression)
        {
            return new PropertyChangedEventArgs(
                GetPropertyName<T>(propertyExpression));
        }

        /// <summary>
        /// Creates PropertyChangedEventArgs
        /// </summary>
        /// <param name="propertyExpression">Expression to make 
        /// PropertyChangedEventArgs out of</param>
        /// <returns>PropertyChangedEventArgs</returns>
        public static string GetPropertyName<T>(
            Expression<Func<T, Object>> propertyExpression)
        {
            var lambda = propertyExpression as LambdaExpression;
            MemberExpression memberExpression;
            if (lambda.Body is UnaryExpression)
            {
                var unaryExpression = lambda.Body as UnaryExpression;
                memberExpression = unaryExpression.Operand as MemberExpression;
            }
            else
            {
                memberExpression = lambda.Body as MemberExpression;
            }

            var propertyInfo = memberExpression.Member as PropertyInfo;

            return propertyInfo.Name;
        }
    }
}
