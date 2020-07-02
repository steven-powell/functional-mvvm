using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using FunctionalMVVM.Extensions;

namespace FunctionalMVVM
{
	public abstract class BaseViewModel : INotifyPropertyChanged, IDisposable
	{
		public event PropertyChangedEventHandler PropertyChanged;
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
				// set initial value.
				Set(compiledExpression(), memberName);
			}
		}


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
