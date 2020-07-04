#region License
// MIT License

// Copyright(c) 2020 Steven Leonard Powell

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using FunctionalMVVM.Extensions;

namespace FunctionalMVVM
{
	public abstract class BaseViewModel : INotifyPropertyChanged, INotifyDataErrorInfo, IDisposable
	{
		public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        /// <summary>
        /// Get the value of <paramref name="memberName"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="defaultValue"></param>
        /// <param name="memberName"></param>
        /// <returns></returns>
        protected T Get<T>(T defaultValue = default, [CallerMemberName] string memberName = null)
		{
			object value;
			if (_currentValues.TryGetValue(memberName, out value))
				return (T)value;
			else
				return defaultValue;
		}

		/// <summary>
		/// Version of Get that returns an ObservableCollection<T>. If not initialized then it creates the collection.
		/// Declaring a setter is optional.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="memberName"></param>
		/// <returns></returns>
		protected ObservableCollection<T> GetCollection<T>([CallerMemberName] string memberName = null)
        {
			var collection = Get(default(ObservableCollection<T> ), memberName);
			if(collection == null)
            {
				collection = new ObservableCollection<T>();
				Set(collection, memberName);
            }
			return collection;
        }

		/// <summary>
		/// Set the value of <paramref name="memberName"/> to <paramref name="value"/> and invoke the PropertyChanged event.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="memberName"></param>
		protected void Set(object value, [CallerMemberName] string memberName = null)
		{
			object currentValue;
			_currentValues.TryGetValue(memberName, out currentValue);
			_currentValues[memberName] = value;
			if (value?.Equals(currentValue) != true && !(value == null && currentValue == null))
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));

			RunValidation(memberName);
			RunValidation(string.Empty);
		}

		/// <summary>
		/// Defines the value of a member named <paramref name="memberName"/> with an <paramref name="expression"/>. The expression is used to calculate the value and any property references are extracted
		/// so changes to those properties will cause a recalculation and property change for <paramref name="memberName"/>.
		/// </summary>
		/// <param name="memberName"></param>
		/// <param name="expression"></param>
		/// <returns></returns>
		protected void Define(string memberName, Expression<Func<object>> expression)
		{
			var objectsName = expression.ExtractMembers();
			var compiledExpression = expression.Compile();
			foreach(var objName in objectsName)
			{
				PropertyChangedEventHandler handler = (s, e) =>
				{
					if (e.PropertyName == objName.Item2)
					{
						var value = compiledExpression();
						Set(value, memberName);
					}
				};
				
				objName.Item1.PropertyChanged += handler;

				_disposeActions.Add(() =>
					objName.Item1.PropertyChanged -= handler
				);

				var objType = objName.Item1.GetType();
				var propInfo = objType.GetProperty(objName.Item2);
				if (typeof(INotifyCollectionChanged).IsAssignableFrom(propInfo.PropertyType))
                {
					// changes to the items in the collection trigger recalculation.
					NotifyCollectionChangedEventHandler colHandler = (s, e) =>
					{
						var value = compiledExpression();
						Set(value, memberName);
					};

					var collection = (INotifyCollectionChanged)propInfo.GetValue(objName.Item1);
					collection.CollectionChanged += colHandler;
					_disposeActions.Add(() =>
						collection.CollectionChanged -= colHandler);
                }

				// set initial value.
				Set(compiledExpression(), memberName);
			}
		}
		protected void Validate(string memberName, Func<object> validationFunc)
			=> AddValidationRule(memberName, validationFunc);
        
		protected void ValidateWhole(Func<object> validationFunc)
			=> AddValidationRule(string.Empty, validationFunc);

		private void ClearErrors(string memberName) => _errors.Remove(memberName);
		private void RunValidation(string memberName)
        {
			ClearErrors(memberName);
			List<Func<object>> rules;
			if(_validationRules.TryGetValue(memberName, out rules))
            {
				var errors = rules.Select(rule => rule()).Where(error => error != null).ToList();
				if (errors.Any())
					_errors[memberName] = errors;
            }
        }
		private void AddValidationRule(string memberName, Func<object> rule)
		{
			List<Func<object>> rules = null;
			if(!_validationRules.TryGetValue(memberName, out rules))
            {
				rules = new List<Func<object>>();
				_validationRules[memberName] = rules;
            }
			rules.Add(rule);
		}


		#region INotifyDataErrorInfo
		public bool HasErrors => _errors.Any();
		public IEnumerable GetErrors(string propertyName)
		{
			List<object> errors;
			if (_errors.TryGetValue(propertyName, out errors))
				return errors;
			return new object[] { };
		}
		#endregion
		private Dictionary<string, List<Func<object>>> _validationRules = new Dictionary<string, List<Func<object>>>();
		private Dictionary<string, List<object>> _errors = new Dictionary<string, List<object>>();
		private Dictionary<string, object> _currentValues = new Dictionary<string, object>();
		#region IDisposable
		private List<Action> _disposeActions = new List<Action>();
		private bool disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects)
					foreach(var action in _disposeActions)
						action();
					_disposeActions.Clear();					
				}
				disposedValue = true;
			}
		}
	
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

        #endregion
    }
}
