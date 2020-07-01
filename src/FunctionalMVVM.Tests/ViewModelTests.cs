using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq.Expressions;
using FunctionalMVVM.Extensions;
using System;

namespace FunctionalMVVM.Tests
{
	public class Contact : BaseViewModel
	{
		public string FirstName { get => Get(""); set => Set(value); }
		public string LastName { get => Get(""); set => Set(value); }
		public string FullName => Get("");
		public string NationalPhoneNumber { get => Get(""); set => Set(value); }
		public int RegionCode { get => Get(1); set => Set(value); }
		public string PhoneNumber => Get("");
		public bool IsComplete => Get(false);

		public Contact()
		{
			Define(nameof(FullName), () => $"{FirstName} {LastName}");
			Define(nameof(PhoneNumber), () => RegionCode + "-" + NationalPhoneNumber);
			Define(nameof(IsComplete), () => !string.IsNullOrEmpty(LastName) && !string.IsNullOrEmpty(FirstName) && PhoneNumber.Length > 10);
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


		public void TestExpr<T>(Expression<T> expression)
		{
			var members = expression.ExtractMembers();
		}
	}
}
