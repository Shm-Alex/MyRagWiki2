using System.ComponentModel;
using System.Windows;

namespace Ksema.Framework.Utils
{
	public class CheckedItem : INotifyPropertyChanged
	{
		private bool _IsChecked;
		public bool IsChecked { get=>_IsChecked;
			set
			{
				_IsChecked = value;
				OnPropertyChanged(nameof(IsChecked));
			}
		}

		public void Select()
		{
			IsChecked = true;
		}

		public void UnSelect()
		{
			IsChecked = false;
		}
		private int _Key;
		public int Key
		{
			get => _Key;
			set
			{
				_Key = value;
				OnPropertyChanged(nameof(Key));
			}
		}
		private string _Title;
		public string Title
		{
			get => _Title;
			set
			{
				_Title = value;
				OnPropertyChanged(nameof(Title));
			}
		}

		public Visibility IsVisibility => IsVisible ? Visibility.Visible : Visibility.Collapsed; 
		private bool _isVisible=true;
		public bool IsVisible
		{
			get => _isVisible;
			set
			{
				_isVisible = value;
				OnPropertyChanged(nameof(IsVisibility));
			}
		}

		public CheckedItem()
		{
		}

		public CheckedItem( bool isChecked ,int key, string title)
		{
			IsChecked = isChecked;
			Key = key;
			Title = title;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}