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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace FunctionalMVVM.Extensions
{
	public static class ExpressionExtensions
	{
		private class Visitor : ExpressionVisitor
		{
			public IEnumerable<Tuple<INotifyPropertyChanged, string>> ViewModelProperties => _viewModelProperties;

			protected override Expression VisitMember(MemberExpression outerMember)
			{
				PropertyInfo outerProp = outerMember.Member as PropertyInfo;
				if (outerProp == null)
					return base.VisitMember(outerMember);
				MemberExpression innerMember = outerMember.Expression as MemberExpression
					?? outerMember as MemberExpression;
				if (innerMember == null)
					return base.VisitMember(outerMember);

				
				INotifyPropertyChanged vm = null;
				var propertyName = outerProp.Name;
				if (innerMember.Member is FieldInfo innerField)
				{
					ConstantExpression ce = (ConstantExpression)innerMember.Expression;
					object innerObj = ce.Value;
					object outerObj = innerField.GetValue(innerObj);
					vm = outerObj as INotifyPropertyChanged;
				}
				else if (outerMember.Expression is ConstantExpression outerCE)
				{
					vm = outerCE.Value as INotifyPropertyChanged;					
				}
				else if (outerMember.Expression is MemberExpression outerME)
				{
					propertyName = outerME.Member.Name;
					if(outerME.Expression is ConstantExpression outerMECE)
                    {
						vm = outerMECE.Value as INotifyPropertyChanged;
						if(vm != null && outerME.Member is PropertyInfo pi)
						{
							var propVal = pi.GetValue(vm);
							if(propVal is INotifyPropertyChanged propVm)
                            {
								// if the property is also INotifyPropertyChanged then listen for changes to it's property as well.
								_viewModelProperties.Add(Tuple.Create(propVm, outerMember.Member.Name));
                            }
                        }
                    }
				}


				if (vm != null)
					_viewModelProperties.Add(Tuple.Create(vm, propertyName));

				return base.VisitMember(outerMember);
			}

			private List<Tuple<INotifyPropertyChanged, string>> _viewModelProperties = new List<Tuple<INotifyPropertyChanged, string>>();
		}

		public static IEnumerable<Tuple<INotifyPropertyChanged, string>> ExtractMembers(this Expression expression)
		{
			var v = new Visitor();
			v.Visit(expression);
			foreach (var n in v.ViewModelProperties)
				Debug.WriteLine(n);
			return v.ViewModelProperties;
		}
	}
}
