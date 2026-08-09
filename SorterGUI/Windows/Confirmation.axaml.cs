using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace SorterGUI.Windows;

public class Confirmation : Window
{
	#region Text

	public string AreYouSureText => "Are you sure?";
	public string YesText => "Yes";
	public string NoText => "No";

	#endregion
	
	#region Colors

	public IBrush PrimaryColor => new SolidColorBrush(new Color(255, 80, 80, 80));
	public IBrush SecondaryColor => new SolidColorBrush(new Color(255, 50, 50, 50));

	#endregion

	#region Callbacks

	public void OnYesClicked(object? sender, RoutedEventArgs e)
	{
		Close(true);
	}
	
	public void OnNoClicked(object? sender, RoutedEventArgs e)
	{
		Close(false);
	}

	#endregion
	
	#region Window

	public Confirmation()
	{
		InitializeComponent();
		DataContext = this;
	}
	
	private void InitializeComponent()
	{
		AvaloniaXamlLoader.Load(this);
	}
	
	#endregion
}