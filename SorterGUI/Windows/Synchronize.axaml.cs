using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using SorterGUI.Items;
using SorterGUI.Utilities;

namespace SorterGUI.Windows;

public class Synchronize : Window, INotifyPropertyChanged
{
	public ObservableCollection<SyncItem> RemoveItems { get; } = new ();
	public ObservableCollection<SyncItem> AddItems { get; } = new ();

	public CancellationTokenSource? CancellationTokenSource = new ();

	private List<string> imageExtensions = new ()
	{
		".png",
		".jpg",
		".jpeg"
	};

	private bool canSynchronize;
	public bool CanSynchronize
	{
		get => canSynchronize;
		set
		{
			canSynchronize = value;
			OnPropertyChanged();
		}
	}

	private bool synchronized = true;
	public bool Synchronized
	{
		get => synchronized;
		set
		{
			synchronized = value;
			OnPropertyChanged();
		}
	}
	
	#region Text

	public string RemoveText => "Remove from database";
	public string AddText => "Add to database";
	public string WarningText => "⚠ Changing an image file with the same name is not recognized ⚠";
	public string SynchronizeText => "Synchronize";
	public string CancelText => "Cancel";

	#endregion
	
	#region Colors

	public IBrush PrimaryColor => new SolidColorBrush(new Color(255, 80, 80, 80));
	public IBrush SecondaryColor => new SolidColorBrush(new Color(255, 50, 50, 50));

	#endregion

	#region Callbacks

	public void OnCancelClicked(object? sender, RoutedEventArgs e)
	{
		Close();
	}
	
	public async void OnSynchronizeClicked(object? sender, RoutedEventArgs e)
	{
		try
		{
			var removeListBox = this.GetControl<ListBox>("RemoveList");
			if (removeListBox.ItemsSource is not IList removeList)
			{
				Logger.Instance.Log("RemoveList items source is not an IList, not synchronizing");
				return;
			}
		
			var addListBox = this.GetControl<ListBox>("AddList");
			if (addListBox.ItemsSource is not IList addList)
			{
				Logger.Instance.Log("AddList items source is not an IList, not synchronizing");
				return;
			}
		
			var confirmation = new Confirmation();
		
			var result = await confirmation.ShowDialog<bool>(this);
			if (!result)
				return;

			Synchronized = false;
		
			for (var i = 0; i < removeList.Count; i++)
			{
				var path = ((SyncItem)removeList[i]!).RelativePath;
				Logger.Instance.Log($"Removing image item {path} from database");
			
				Database.Instance.RemoveImageItem(path);
			}
		
			for (var i = 0; i < addList.Count; i++)
			{
				var path = ((SyncItem)addList[i]!).RelativePath;
				Logger.Instance.Log($"Adding image item {path} to database");
			
				Database.Instance.AddImageItem(path);
			}

			Synchronized = true;
		
			Close();
		}
		catch (Exception ex)
		{
			Logger.Instance.Log(ex.ToString());
		}
	}

	#endregion
	
	#region Window

	event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
	{
		add => _propertyChanged += value;
		remove => _propertyChanged -= value;
	}

	private event PropertyChangedEventHandler? _propertyChanged;
	
	public Synchronize()
	{
		InitializeComponent();
		DataContext = this;
		setupItems();
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
		if (!Synchronized)
		{
			// Prevent closing when syncing
			e.Cancel = true;
		}
		else
		{
			if (CancellationTokenSource != null)
			{
				CancellationTokenSource.Cancel(); 
				CancellationTokenSource = null;
			}
		}
		
		base.OnClosing(e);
	}

	#endregion

	#region Methods

	private void setupItems()
	{
		var removeListBox = this.GetControl<ListBox>("RemoveList");
		if (removeListBox.ItemsSource is not IList removeList)
		{
			Logger.Instance.Log("RemoveList items source is not an IList, not populating");
			return;
		}
		
		var addListBox = this.GetControl<ListBox>("AddList");
		if (addListBox.ItemsSource is not IList addList)
		{
			Logger.Instance.Log("AddList items source is not an IList, not populating");
			return;
		}
		
		removeList.Clear();
		addList.Clear();
		
		var removeScrollViewer = removeListBox.FindDescendantOfType<ScrollViewer>();
		if (removeScrollViewer != null)
			removeScrollViewer.Offset = Vector.Zero;

		var addScrollViewer = addListBox.FindDescendantOfType<ScrollViewer>();
		if (addScrollViewer != null)
			addScrollViewer.Offset = Vector.Zero;
		
		var fileItems = getItems();
		if (fileItems == null)
			return;

		if (CancellationTokenSource != null)
		{
			CancellationTokenSource.Cancel(); 
			CancellationTokenSource = null;
		}
		
		CancellationTokenSource = new CancellationTokenSource();
		
		_ = populateListsAsync(fileItems, removeList, addList, CancellationTokenSource.Token);
	}

	private async Task populateListsAsync(List<SyncItem> fileItems, IList removeSource, IList addSource, CancellationToken token)
	{
		try
		{
			token.ThrowIfCancellationRequested();

			for (var i = 0; i < fileItems.Count; i++)
			{
				if (i % 5 == 0)
					await Task.Delay(25, token);

				var fileItem = fileItems[i];

				if (!Database.Instance.ImageItemExists(fileItem.RelativePath))
					addSource.Add(fileItem);
			}

			var databaseItems = Database.Instance.GetImageItems();
			for (var i = 0; i < databaseItems.Count; i++)
			{
				if (i % 5 == 0)
					await Task.Delay(25, token);

				var databaseItem = databaseItems[i];
				var fileItemExists = false;

				for (var k = 0; k < fileItems.Count; k++)
				{
					var fileItem = fileItems[k];
					if (fileItem.RelativePath != databaseItem.RelativePath)
						continue;

					fileItemExists = true;
					break;
				}

				if (!fileItemExists)
					removeSource.Add(new SyncItem { RelativePath = databaseItem.RelativePath });
			}

			CanSynchronize = true;
		}
		catch (TaskCanceledException)
		{
			
		}
		catch (Exception ex)
		{
			Logger.Instance.Log(ex.ToString());
		}
	}

	private List<SyncItem>? getItems()
	{
		var settingPath = Settings.Instance.GetStringSetting("imagespath").TrimEnd('/', '\\');
		if (!Directory.Exists(settingPath))
		{
			Logger.Instance.Log("Images path does not exist, ignoring");
			return null;
		}

		var list = new List<SyncItem>();
		
		var files = Directory.GetFiles(settingPath);
		for (var i = 0; i < files.Length; i++)
		{
			var fileInfo = new FileInfo(files[i]);
			
			if (!imageExtensions.Contains(fileInfo.Extension))
				continue;
			
			list.Add(new SyncItem
			{
				RelativePath = files[i][(settingPath.Length + 1)..]
			});
		}

		return list;
	}

	#endregion
}