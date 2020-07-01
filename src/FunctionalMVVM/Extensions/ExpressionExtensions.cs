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
				

				if(vm != null)
					_viewModelProperties.Add(Tuple.Create(vm, outerProp.Name));

				// outerObj is the actual object..
				

				//_propertyNames.Add(member.Name);
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
