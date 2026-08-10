using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace SorterGUI.Extensions;

public class RoundedImage:Image
{
	public static readonly RoutedEvent<RoutedEventArgs> ClickEvent = RoutedEvent.Register<RoundedImage, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);
	
	public event EventHandler<RoutedEventArgs>? Click
	{
		add => AddHandler(ClickEvent, value);
		remove => RemoveHandler(ClickEvent, value);
	}
	
	public static readonly AttachedProperty<double> CornerRadiusProperty = AvaloniaProperty.RegisterAttached<RoundedImage, double>("CornerRadius", typeof(RoundedImage), 5);
    
	private bool isPointerDown;

	public static void SetCornerRadius(AvaloniaObject element, double parameter)
	{
		element.SetValue(CornerRadiusProperty, parameter);
	}

	public static double GetCornerRadius(AvaloniaObject element)
	{
		return element.GetValue(CornerRadiusProperty);
	}
    
	protected override Size MeasureOverride(Size availableSize)
	{
		var source = Source;
		
		Size result = new ();

		if (source != null)
			result = Stretch.CalculateSize(availableSize, source.Size, StretchDirection);
		
		Clip = new RectangleGeometry(new Rect(0, 0, result.Width, result.Height), GetCornerRadius(this), GetCornerRadius(this));
		
		return result;
	}
	
	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		base.OnPointerPressed(e);

		if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
			return;

		isPointerDown = true;
		e.Handled = true;
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		base.OnPointerReleased(e);

		if (!isPointerDown)
			return;

		isPointerDown = false;

		if (!Bounds.Contains(e.GetPosition(this)))
			return;

		RaiseEvent(new RoutedEventArgs(ClickEvent));
		e.Handled = true;
	}

	protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
	{
		base.OnPointerCaptureLost(e);
		isPointerDown = false;
	}
}