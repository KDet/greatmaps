﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace GMap.NET.ObjectModel
{
	/// <summary>
	/// A base class for objects of which the properties must be observable.
	/// 
	/// </summary>
	public class ObservableObject : INotifyPropertyChanged
	{
		/// <summary>
		/// Raises the PropertyChanged event if needed.
		/// 
		/// </summary>
		/// 
		/// <remarks>
		/// If the propertyName parameter
		///             does not correspond to an existing property on the current class, an
		///             exception is thrown in DEBUG configuration only.
		/// </remarks>
		/// <param name="propertyName">(optional) The name of the property that
		///             changed.</param>
		protected virtual void RaisePropertyChanged(string propertyName)
		{
			var changedEventHandler = PropertyChanged;
			changedEventHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		/// <summary>
		/// Raises the PropertyChanged event if needed.
		/// 
		/// </summary>
		/// <typeparam name="T">The type of the property that
		///             changed.</typeparam><param name="propertyExpression">An expression identifying the property
		///             that changed.</param>
		protected virtual void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpression)
		{
			var changedEventHandler = PropertyChanged;
			if (changedEventHandler == null)
				return;
			var propertyName = GetPropertyName(propertyExpression);
			changedEventHandler(this, new PropertyChangedEventArgs(propertyName));
		}
		/// <summary>
		/// Extracts the name of a property from an expression.
		/// 
		/// </summary>
		/// <typeparam name="TProp">The type of the property.</typeparam><param name="propertyExpression">An expression returning the property's name.</param>
		/// <returns>
		/// The name of the property returned by the expression.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If the expression is null.</exception><exception cref="T:System.ArgumentException">If the expression does not represent a property.</exception>
		protected static string GetPropertyName<TProp>(Expression<Func<TProp>> propertyExpression)
		{
			if (propertyExpression == null)
				throw new ArgumentNullException("propertyExpression");
			var memberExpression = propertyExpression.Body as MemberExpression;
			if (memberExpression == null)
				throw new ArgumentException(@"Invalid argument", "propertyExpression");
			var propertyInfo = memberExpression.Member as PropertyInfo;
			if (propertyInfo == null)
				throw new ArgumentException(@"Argument is not a property", "propertyExpression");
			return propertyInfo.Name;
		}
		/// <summary>
		/// Assigns a new value to the property. Then, raises the
		///             PropertyChanged event if needed.
		/// 
		/// </summary>
		/// <typeparam name="T">The type of the property that
		///             changed.</typeparam><param name="propertyExpression">An expression identifying the property
		///             that changed.</param><param name="field">The field storing the property's value.</param><param name="newValue">The property's value after the change
		///             occurred.</param>
		/// <returns>
		/// True if the PropertyChanged event has been raised,
		///             false otherwise. The event is not raised if the old
		///             value is equal to the new value.
		/// </returns>
		protected bool Set<T>(Expression<Func<T>> propertyExpression, ref T field, T newValue)
		{
			if (EqualityComparer<T>.Default.Equals(field, newValue))
				return false;
			field = newValue;
			RaisePropertyChanged(propertyExpression);
			return true;
		}
		/// <summary>
		/// Assigns a new value to the property. Then, raises the
		///             PropertyChanged event if needed.
		/// 
		/// </summary>
		/// <typeparam name="T">The type of the property that
		///             changed.</typeparam><param name="propertyName">The name of the property that
		///             changed.</param><param name="field">The field storing the property's value.</param><param name="newValue">The property's value after the change
		///             occurred.</param>
		/// <returns>
		/// True if the PropertyChanged event has been raised,
		///             false otherwise. The event is not raised if the old
		///             value is equal to the new value.
		/// </returns>
		protected bool Set<T>(string propertyName, ref T field, T newValue)
		{
			if (EqualityComparer<T>.Default.Equals(field, newValue))
				return false;
			field = newValue;
			// ReSharper disable once ExplicitCallerInfoArgument
			RaisePropertyChanged(propertyName);
			return true;
		}
		/// <summary>
		/// Assigns a new value to the property. Then, raises the
		///             PropertyChanged event if needed.
		/// 
		/// </summary>
		/// <typeparam name="T">The type of the property that
		///             changed.</typeparam><param name="field">The field storing the property's value.</param><param name="newValue">The property's value after the change
		///             occurred.</param><param name="propertyName">(optional) The name of the property that
		///             changed.</param>
		/// <returns>
		/// True if the PropertyChanged event has been raised,
		///             false otherwise. The event is not raised if the old
		///             value is equal to the new value.
		/// </returns>
		protected bool Set<T>(ref T field, T newValue, string propertyName)
		{
			return Set(propertyName, ref field, newValue);
		}
		/// <summary>
		/// Raises the PropertyChanged event if needed, and broadcasts a
		///             PropertyChangedMessage using the Messenger instance (or the
		///             static default instance if no Messenger instance is available).
		/// 
		/// </summary>
		/// <typeparam name="T">The type of the property that
		///             changed.</typeparam><param name="propertyName">The name of the property that
		///             changed.</param><param name="oldValue">The property's value before the change
		///             occurred.</param><param name="newValue">The property's value after the change
		///             occurred.</param>
		/// <remarks>
		/// If the propertyName parameter
		///             does not correspond to an existing property on the current class, an
		///             exception is thrown in DEBUG configuration only.
		/// </remarks>
		protected virtual void RaisePropertyChanged<T>(string propertyName, T oldValue = default(T), T newValue = default(T))
		{
			if (string.IsNullOrEmpty(propertyName))
				throw new ArgumentException(@"This method cannot be called with an empty string", "propertyName");
			// ReSharper disable once ExplicitCallerInfoArgument
			RaisePropertyChanged(propertyName);
		}
		/// <summary>
		/// Raises the PropertyChanged event if needed, and broadcasts a
		///             PropertyChangedMessage using the Messenger instance (or the
		///             static default instance if no Messenger instance is available).
		/// 
		/// </summary>
		/// <typeparam name="T">The type of the property that
		///             changed.</typeparam><param name="propertyExpression">An expression identifying the property
		///             that changed.</param><param name="oldValue">The property's value before the change
		///             occurred.</param><param name="newValue">The property's value after the change
		///             occurred.</param>
		protected virtual void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpression, T oldValue, T newValue)
		{
			var propertyChangedHandler = PropertyChanged;
			if (propertyChangedHandler != null)
			{
				var propertyName = GetPropertyName(propertyExpression);
				propertyChangedHandler(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		/// <summary>
		/// Verifies that a property name exists in this ViewModel. This method
		///             can be called before the property is used, for instance before
		///             calling RaisePropertyChanged. It avoids errors when a property name
		///             is changed but some places are missed.
		/// 
		/// </summary>
		/// 
		/// <remarks>
		/// This method is only active in DEBUG mode.
		/// </remarks>
		/// <param name="propertyName">The name of the property that will be
		///             checked.</param>
		[Conditional("DEBUG")]
		[DebuggerStepThrough]
		public void VerifyPropertyName(string propertyName)
		{
			var type = GetType();
			if (!string.IsNullOrEmpty(propertyName) &&
				type.GetProperty(propertyName) == null)
				throw new ArgumentException(@"Property not found", propertyName);
		}

		/// <summary>
		/// Occurs after a property value changes.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;
	}
}