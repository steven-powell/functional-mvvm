# functional-mvvm
Components to make MVVM more concise, easy and testable.

## Example
```cs
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
```
In the example above any changes to properties with setters will trigger PropertyChanged events. The properties with getters only are calculated values. For instance FullName is defined as $"{FirstName} {LastName}". Any changes to FirstName or LastName will recalculate FullName and also trigger a PropertyChanged event for FullName.

If an object that implements INotifyCollectionChanged is refered to in a Define expression then any changes to the collection content will trigger the expresion to be recalculated.

BaseViewModel implements IDisposable so that when it's disposed all event handlers are cleaned up.