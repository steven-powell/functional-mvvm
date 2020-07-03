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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq.Expressions;
using FunctionalMVVM.Extensions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace FunctionalMVVM.Tests
{
	public class Invoice : BaseViewModel
    {
		public int InvoiceNumber { get => Get(0); set => Set(value); }
		public double Total { get => Get(0.0); set => Set(value); }
		public bool IsPaid { get => Get(false); set => Set(value); }
    }
	public class Contact : BaseViewModel
	{
		public string FirstName { get => Get(""); set => Set(value); }
		public string LastName { get => Get(""); set => Set(value); }
		public string FullName => Get("");
		public string NationalPhoneNumber { get => Get(""); set => Set(value); }
		public int RegionCode { get => Get(1); set => Set(value); }
		public string PhoneNumber => Get("");
		public bool IsComplete => Get(false);
		public ObservableCollection<Invoice> Invoices => GetCollection<Invoice>();
		public bool IsCurrentCustomer => Get(false);
		public double AmountOwing => Get(0.0);

		public Contact()
		{
			Define(nameof(FullName), () => $"{FirstName} {LastName}");
			Define(nameof(PhoneNumber), () => RegionCode + "-" + NationalPhoneNumber);
			Define(nameof(IsComplete), () => !string.IsNullOrEmpty(LastName) && !string.IsNullOrEmpty(FirstName) && PhoneNumber.Length > 10);
			Define(nameof(IsCurrentCustomer), () => Invoices.Count > 0);
			Define(nameof(AmountOwing), () => Invoices.Where(inv => !inv.IsPaid).Sum(inv => inv.Total));
		}
	}

	[TestClass]
	public class ViewModelTests
	{
		[TestMethod]
		public void DefinedPropertiesRecalculateWhenDependentPropertiesChange()
		{
			var t = new Contact();
			t.FirstName = "Albert";
			t.LastName = "Einstein";
			t.NationalPhoneNumber = "000-000-0000";
			t.RegionCode = 44;
			Assert.AreEqual("Albert Einstein", t.FullName);
			Assert.AreEqual("44-000-000-0000", t.PhoneNumber);
			Assert.IsTrue(t.IsComplete);

			t.FirstName = "A.";
			Assert.AreEqual("A. Einstein", t.FullName);
			t.RegionCode = 55;
			Assert.AreEqual("55-000-000-0000", t.PhoneNumber);
			t.NationalPhoneNumber = "0";
			Assert.IsFalse(t.IsComplete);			
		}

		[TestMethod]
		public void INotifyPropertyChangedEventTriggersUpdate()
        {
			var t = new Contact();
			Assert.IsFalse(t.IsCurrentCustomer);
			t.Invoices.Add(new Invoice()
			{
				InvoiceNumber = 1,
				Total = 100.0
			});
			Assert.IsTrue(t.IsCurrentCustomer);
		}

		[TestMethod]
		public void CollectionChangesTriggerRecalculation()
		{
			var t = new Contact();
			t.Invoices.Add(new Invoice()
			{
				InvoiceNumber = 1,
				Total = 100.0
			});
			Assert.AreEqual(100.0, t.AmountOwing);
			t.Invoices.Add(new Invoice()
			{
				InvoiceNumber = 1,
				Total = 300.0
			});
			Assert.AreEqual(400.0, t.AmountOwing);
			t.Invoices.RemoveAt(1);
			Assert.AreEqual(100.0, t.AmountOwing);
		}


		public void TestExpr<T>(Expression<T> expression)
		{
			var members = expression.ExtractMembers();
		}
	}
}
