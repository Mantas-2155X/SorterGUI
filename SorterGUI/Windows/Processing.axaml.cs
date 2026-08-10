using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace SorterGUI.Windows;

public partial class Processing : Window, INotifyPropertyChanged
{
	private float progress;
	public float Progress
	{
		get => progress;
		set
		{
			progress = value;
			OnPropertyChanged();
		}
	}

	public bool CanClose;

	#region Text

	public string ProcessingText => "Processing...";

	#endregion
	
	#region Colors

	public IBrush PrimaryColor => new SolidColorBrush(new Color(255, 80, 80, 80));
	public IBrush SecondaryColor => new SolidColorBrush(new Color(255, 50, 50, 50));

	#endregion
	
	#region Window

	event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
	{
		add => _propertyChanged += value;
		remove => _propertyChanged -= value;
	}

	private event PropertyChangedEventHandler? _propertyChanged;

	public Processing()
	{
		InitializeComponent();
		DataContext = this;
	}
	
	private void InitializeComponent()
	{
		AvaloniaXamlLoader.Load(this);
	}
	
	protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		_propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
	
	protected override void OnClosing(WindowClosingEventArgs e)
	{
		if (!CanClose)
		{
			// Prevent closing when processing
			e.Cancel = true;
		}
		
		base.OnClosing(e);
	}
	
	#endregion
}