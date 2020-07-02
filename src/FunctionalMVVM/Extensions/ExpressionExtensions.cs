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
