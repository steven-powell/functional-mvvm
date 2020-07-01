# functional-mvvm
Components to make MVVM more concise, easy and testable.

## Example
```cs
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
```
In the example above any changes to properties with setters will trigger PropertyChanged events. The properties with getters only are calculated values. For instance FullName is defined as $"{FirstName} {LastName}". Any changes to FirstName or LastName will recalculate FullName and also trigger a PropertyChanged event for FullName.