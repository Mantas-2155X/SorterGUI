using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using SorterGUI.Extensions;
using SorterGUI.Items;
using SorterGUI.Utilities;

namespace SorterGUI.Windows;

public partial class MainWindow : Window, INotifyPropertyChanged
{
	public Logger Logger { get; }
	public Settings Settings { get; }
	public Database Database { get; }

	public ObservableCollection<HistoryItem> HistoryItems { get; } = new ();
	public ObservableCollection<ImageItem> ImageItems { get; } = new ();

	private int selectedTabIndex = -1;
	public int SelectedTabIndex
	{
		get => selectedTabIndex;
		set
		{
			if (selectedTabIndex == value)
				return;

			selectedTabIndex = this.GetControl<TabControl>("TabControl").SelectedIndex;
			OnPropertyChanged();
			
			if (value == 0) // Statistics
			{
				setupStatistics();
			}
			else if (value == 1) // Images
			{
				setupImages();
			}
			else if (value == 2) // Sort
			{
				setupSortImages();
			}
			else if (value == 3) // History
			{
				// Already calls setup
				HistoryPage = 1;
			}
			else if (value == 4) // Options
			{
			
			}
			else if (value == 5) // About
			{

			}
		}
	}
	
	private int historyPage = 1;
	public int HistoryPage
	{
		get => historyPage;
		set
		{
			if (value < 1 || value > Database.GetHistoryPages(HistoryPerPage))
			{
				this.GetControl<NumericUpDown>("HistoryPage").Value = historyPage;
				value = historyPage;
			}

			historyPage = value;
			OnPropertyChanged();
			
			setupHistory();
		}
	}
	
	public int HistoryPerPage => 50;
	
	public int SortImagesRetries;
	public bool Cooldown;

	public ImageItem? LeftImage;
	public ImageItem? RightImage;

	public CancellationTokenSource? SortCancellationTokenSource = new ();
	public CancellationTokenSource? HistoryCancellationTokenSource = new ();

	#region Text

	public string StatisticsTabText => "Statistics";
	public string SortImagesTabText => "Sort";
	public string HistoryTabText => "History";
	public string ImagesTabText => "Images";
	public string OptionsTabText => "Options";
	public string AboutTabText => "About";
	
	public string WhichDoYouPreferText => "Which do you prefer?";
	public string NotEnoughImagesText => "At least two images must be registered before sorting";

	public string ImageText => "Image";
	public string FilenameText => "Filename";
	public string EloText => "Elo";
	public string MatchesText => "Matches";
	
	public string LeftNameText => "Left Name";
	public string RightNameText => "Right Name";
	public string EloChangeText => "Elo Change";
	
	public string ImagesPathText => "Images Path";
	public string ImagesPathSettingText
	{
		get => Settings.GetStringSetting("imagespath");
		set
		{
			Settings.SetSetting("imagespath", value);
			OnPropertyChanged();
		}
	}

	public string PickText => "Pick";
	public string DatabaseText => "Database";
	public string DatabaseDescText => "Remove all statistics, history and registered images ratings";
	public string ResetText => "Reset";
	public string ImagesText => "Images";
	public string SyncDescText => "Register changes from the images path to the database";
	public string SyncText => "Sync";
	public string BottomBarText => "Bottom Bar";
	public string BottomBarDescText => "Show bottom bar buttons";
	public bool BottomBarSettingText
	{
		get => Settings.GetBoolSetting("bottombar");
		set
		{
			Settings.SetSetting("bottombar", value);
			OnPropertyChanged();
		}
	}

	public string AppVersionText => $"SorterGUI v{Assembly.GetEntryAssembly()!.GetName().Version}";
	public string AboutText => "\nMade for Sopina with the sole reason of sorting through images of [redacted].\nWill this ever be used for good and not evil? Who knows.\n\nHey, it has its purpose, so why not?";
	public string MadeByText => "Made by 2155X";

	public string ExitText => "Exit";

	#endregion

	#region Colors

	public IBrush PrimaryColor => new SolidColorBrush(new Color(255, 80, 80, 80));
	public IBrush SecondaryColor => new SolidColorBrush(new Color(255, 50, 50, 50));
	public IBrush AlternateColor => new SolidColorBrush(new Color(255, 65, 65, 65));

	#endregion

	#region Callbacks

	public async void OnPickImagesPathClicked(object? sender, RoutedEventArgs e)
	{
		try
		{
			var directories = await GetTopLevel(this)!.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
			{
				Title = "Select Images Directory",
				AllowMultiple = false
			});

			if (directories.Count != 1)
			{
				Logger.Instance.Log("No images directory picked");
				return;
			}

			var path = directories[0].TryGetLocalPath() ?? "";
			if (!Directory.Exists(path))
			{
				Logger.Instance.Log("Picked images directory does not exist");
				return;
			}
		
			ImagesPathSettingText = path;
		}
		catch (Exception ex)
		{
			Logger.Instance.Log(ex.ToString());
		}
	}
	
	public async void OnResetDatabaseClicked(object? sender, RoutedEventArgs e)
	{
		try
		{
			var confirmation = new Confirmation();
		
			if (!await confirmation.ShowDialog<bool>(this))
				return;

			Database.ClearDatabase();
		}
		catch (Exception ex)
		{
			Logger.Instance.Log(ex.ToString());
		}
	}
	
	public async void OnSyncClicked(object? sender, RoutedEventArgs e)
	{
		try
		{
			var synchronize = new Synchronize();
			await synchronize.ShowDialog(this);
		}
		catch (Exception ex)
		{
			Logger.Instance.Log(ex.ToString());
		}
	}

	public void OnExitClicked(object? sender, RoutedEventArgs e)
	{
		Close();
	}
	
	public void OnLeftImageClicked(object? sender, RoutedEventArgs e)
	{
		if (Cooldown)
			return;
		
		pickWinner(true);
	}
	
	public void OnRightImageClicked(object? sender, RoutedEventArgs e)
	{
		if (Cooldown)
			return;

		pickWinner(false);
	}
	
	public void OnFirstHistoryPageClicked(object? sender, RoutedEventArgs e)
	{
		HistoryPage = 1;
	}
	
	public void OnPreviousHistoryPageClicked(object? sender, RoutedEventArgs e)
	{
		if (HistoryPage <= 1)
			return;

		HistoryPage--;
	}
	
	public void OnNextHistoryPageClicked(object? sender, RoutedEventArgs e)
	{
		if (HistoryPage >= Database.GetHistoryPages(HistoryPerPage))
			return;

		HistoryPage++;
	}
	
	public void OnLastHistoryPageClicked(object? sender, RoutedEventArgs e)
	{
		HistoryPage = Database.GetHistoryPages(HistoryPerPage);
	}

	public void OnHistoryPageKeyDown(object? sender, KeyEventArgs e)
	{
		if (sender is not NumericUpDown numericUpDown)
			return;
		
		if (e.Key is Key.Enter)
		{
			if (string.IsNullOrWhiteSpace(numericUpDown.Text))
				numericUpDown.Value = 1;
			
			GetTopLevel(this)!.FocusManager.Focus(null);
			e.Handled = true;
			
			return;
		}
		
		if (e.Key is Key.Back or Key.Delete or Key.Left or Key.Right or Key.Home or Key.End or Key.Tab or Key.Escape)
			return;

		if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
			return;

		var symbol = e.KeySymbol;
		if (!string.IsNullOrEmpty(symbol) && char.IsDigit(symbol[0]))
			return;

		e.Handled = true;
	}
	
	public void OnHistoryPageGotFocus(object? sender, FocusChangedEventArgs e)
	{
		if (sender is not TextBox textBox)
			return;

		textBox.SelectAll();
	}
	
	public void OnThumbnailClicked(object? sender, RoutedEventArgs e)
	{
		try
		{
			if (sender is not RoundedImage image)
				return;
			
			var listBoxItem = image.FindAncestorOfType<ListBoxItem>();
			if (listBoxItem == null || listBoxItem.DataContext is not ImageItem imageItem)
				return;

			if (!imageItem.FileExists(out var fileInfo))
				return;

			var startInfo = new ProcessStartInfo
			{
				FileName = fileInfo.FullName,
				UseShellExecute = true
			};
        
			Process.Start(startInfo);
		}
		catch (Exception ex)
		{
			Logger.Log(ex.ToString());
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
	
	public MainWindow()
	{
		Logger = new Logger();
		Settings = new Settings();
		Database = new Database();
		
		InitializeComponent();
		DataContext = this;
		SelectedTabIndex = 0;
	}
	
	private void InitializeComponent()
	{
		AvaloniaXamlLoader.Load(this);
	}
	
	protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		_propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
	
	#endregion

	#region Methods

	private void setupStatistics()
	{
		this.GetControl<Label>("AvailableImages").Content = $"Available Images: {Database.GetImagesCount()}";
		this.GetControl<Label>("UnmatchedImages").Content = $"Unmatched Images: {Database.GetUnmatchedImagesCount()}";
		this.GetControl<Label>("StatisticsVariation").Content = $"Variation: ~{Database.GetVariation():F2}";
		this.GetControl<Label>("TotalComparisons").Content = $"Total Comparisons: {Database.GetTotalComparisons()}";
	}

	private void setupSortImages(bool isCooldown = false, bool leftWon = false)
	{
		var enoughImages = Database.GetImagesCount() >= 2;
		
		this.GetControl<Label>("SortUnavailable").IsVisible = !enoughImages;
		this.GetControl<TextBlock>("Sort1").IsVisible = enoughImages;
		this.GetControl<Grid>("Sort2").IsVisible = enoughImages;
		this.GetControl<Grid>("Sort3").IsVisible = enoughImages;
		
		if (!enoughImages)
		{
			Logger.Instance.Log("Less than two images registered, skipping sorting");
			return;
		}
		
		var availableImages = (float)Database.GetImagesCount();
		var matchedImages = availableImages - Database.GetUnmatchedImagesCount();

		if (isCooldown)
			lockImageButtons(leftWon);
		else
			unlockImageButtons();
		
		this.GetControl<Label>("MatchedImages").Content = $"Matched Images: {matchedImages}/{availableImages} ({matchedImages / availableImages * 100f:F0}%)";
		this.GetControl<Label>("Variation").Content = $"Variation: ~{Database.GetVariation():F2}";
		
		LeftImage = Database.GetRandomImage();
		RightImage = Database.GetRandomImage(LeftImage);
		
		if (LeftImage == null || RightImage == null)
		{
			clearImages();
			
			if (SortImagesRetries >= 3)
			{
				Logger.Instance.Log("Hit the limit for null images, giving up");
				return;
			}
			
			Logger.Instance.Log("One or both images are null, retrying");
			setupSortImages();
			return;
		}

		if (!isCooldown)
			clearImages();
		
		SortImagesRetries = 0;

		if (SortCancellationTokenSource != null)
		{
			SortCancellationTokenSource.Cancel();
			SortCancellationTokenSource = null;
		}
		
		SortCancellationTokenSource = new CancellationTokenSource();
		
		_ = loadImages(SortCancellationTokenSource.Token, isCooldown);
	}

	private async Task loadImages(CancellationToken token, bool isCooldown)
	{
		try
		{
			token.ThrowIfCancellationRequested();
			
			var leftTask = LeftImage!.GetImageAsync();
			var rightTask = RightImage!.GetImageAsync();
			var cooldownTask = Task.Delay(300, token);

			var leftImage = this.GetControl<Image>("LeftImage");
			var rightImage = this.GetControl<Image>("RightImage");

			if (leftImage.Source is Bitmap leftBitmap)
				leftBitmap.Dispose();
			
			if (rightImage.Source is Bitmap rightBitmap)
				rightBitmap.Dispose();
			
			if (isCooldown)
				await Task.WhenAll(leftTask, rightTask, cooldownTask);
			else
				await Task.WhenAll(leftTask, rightTask);
			
			this.GetControl<TextBlock>("LeftImageTitle").Text = LeftImage.GetName();
			this.GetControl<Label>("RightImageTitle").Content = RightImage.GetName();

			leftImage.Source = leftTask.Result;
			rightImage.Source = rightTask.Result;

			if (isCooldown)
				unlockImageButtons();
		}
		catch (TaskCanceledException)
		{
			
		}
		catch (Exception ex)
		{
			Logger.Instance.Log(ex.ToString());
		}
	}

	private void clearImages()
	{
		this.GetControl<TextBlock>("LeftImageTitle").Text = "";
		this.GetControl<Label>("RightImageTitle").Content = "";

		var leftImage = this.GetControl<Image>("LeftImage");
		var rightImage = this.GetControl<Image>("RightImage");

		if (leftImage.Source is Bitmap leftBitmap)
			leftBitmap.Dispose();
		
		if (rightImage.Source is Bitmap rightBitmap)
			rightBitmap.Dispose();
		
		leftImage.Source = null;
		rightImage.Source = null;
	}

	private void pickWinner(bool leftWon)
	{
		getEloChange(leftWon ? LeftImage! : RightImage!, out var winnerEloChange, out var loserEloChange);
		
		LeftImage!.Matches += 1;
		LeftImage.Elo += leftWon ? winnerEloChange : loserEloChange;

		RightImage!.Matches += 1;
		RightImage.Elo += leftWon ? loserEloChange : winnerEloChange;

		var newVariation = getNewVariation();

		Logger.Log($"{(leftWon ? LeftImage.RelativePath : RightImage.RelativePath)} (+{winnerEloChange}) VS ({loserEloChange}) {(leftWon ? RightImage.RelativePath : LeftImage.RelativePath)} | Var {Database.GetVariation():F2} -> {newVariation:F2}");
			
		Database.AddHistoryItem(LeftImage!.Id, RightImage!.Id, leftWon ? winnerEloChange : loserEloChange, leftWon ? loserEloChange : winnerEloChange);
			
		Database.SetTotalComparisons(Database.GetTotalComparisons() + 1);
		Database.SetVariation(newVariation);

		Database.UpdateImageItem(LeftImage);
		Database.UpdateImageItem(RightImage);
		
		setupSortImages(true, leftWon);
	}

	private void lockImageButtons(bool leftWon)
	{
		var leftButton = this.GetControl<Button>("LeftImageButton");
		var rightButton = this.GetControl<Button>("RightImageButton");
		
		leftButton.IsEnabled = false;
		leftButton.Classes.Clear();
		leftButton.Classes.Add(leftWon ? "GreenDisabledBorder" : "RedDisabledBorder");
		
		rightButton.IsEnabled = false;
		rightButton.Classes.Clear();
		rightButton.Classes.Add(!leftWon ? "GreenDisabledBorder" : "RedDisabledBorder");
	}
	
	private void unlockImageButtons()
	{
		var leftButton = this.GetControl<Button>("LeftImageButton");
		var rightButton = this.GetControl<Button>("RightImageButton");
				
		leftButton.IsEnabled = true;
		leftButton.Classes.Clear();
		leftButton.Classes.Add("GreenHoverBorder");

		rightButton.IsEnabled = true;
		rightButton.Classes.Clear();
		rightButton.Classes.Add("GreenHoverBorder");
	}
	
	private void getEloChange(ImageItem winner, out long winnerEloChange, out long loserEloChange)
	{
		var leftWon = LeftImage == winner ? 1 : 0;
		var rightWon = RightImage == winner ? 1 : 0;
		
		var expectedLeft = 1 / (Math.Pow(10, (RightImage!.Elo - LeftImage!.Elo) / 400d) + 1);
		var expectedRight = 1 / (Math.Pow(10, (LeftImage!.Elo - RightImage!.Elo) / 400d) + 1);

		var leftEloChange = 32 * (leftWon - expectedLeft);
		var rightEloChange = 32 * (rightWon - expectedRight);
		
		winnerEloChange = leftWon == 1 ? (long)Math.Round(leftEloChange) : (long)Math.Round(rightEloChange);
		loserEloChange = leftWon == 1 ? (long)Math.Round(rightEloChange) : (long)Math.Round(leftEloChange);
	}

	private float getNewVariation()
	{
		var compareAmount = Math.Max((long)(Database.GetImagesCount() * 0.15f), 15L);
		var totalElo = 0f;
		
		var historyItems = Database.GetHistoryItems(compareAmount, true);
		for (var i = 0; i < historyItems.Count; i++)
			totalElo += historyItems[i].GetEloChange();
		
		return totalElo / compareAmount;
	}
	
	private void setupHistory()
	{
		var listBox = this.GetControl<ListBox>("HistoryList");
		if (listBox.ItemsSource is not IList list)
		{
			Logger.Instance.Log("HistoryList items source is not an IList, not populating");
			return;
		}

		list.Clear();
		
		var scrollViewer = listBox.FindDescendantOfType<ScrollViewer>();
		if (scrollViewer != null)
			scrollViewer.Offset = Vector.Zero;
		
		if (HistoryCancellationTokenSource != null)
		{
			HistoryCancellationTokenSource.Cancel(); 
			HistoryCancellationTokenSource = null;
		}
		
		HistoryCancellationTokenSource = new CancellationTokenSource();

		_ = populateHistoryAsync(list, HistoryCancellationTokenSource.Token);
	}

	private async Task populateHistoryAsync(IList itemsSource, CancellationToken token)
	{
		try
		{
			token.ThrowIfCancellationRequested();

			var items = Database.GetHistoryItems(HistoryPage * HistoryPerPage - HistoryPerPage, HistoryPerPage, true);

			for (var i = 0; i < items.Count; i++)
			{
				if (i % 25 == 0)
					await Task.Delay(25, token);

				itemsSource.Add(items[i]);
			}
		}
		catch (TaskCanceledException)
		{
			
		}
		catch (Exception ex)
		{
			Logger.Instance.Log(ex.ToString());
		}
	}
	
	private void setupImages()
	{
		var listBox = this.GetControl<ListBox>("ImagesList");
		if (listBox.ItemsSource is not IList list)
		{
			Logger.Instance.Log("ImagesList items source is not an IList, not populating");
			return;
		}

		list.Clear();
		
		var scrollViewer = listBox.FindDescendantOfType<ScrollViewer>();
		if (scrollViewer != null)
			scrollViewer.Offset = Vector.Zero;
		
		if (HistoryCancellationTokenSource != null)
		{
			HistoryCancellationTokenSource.Cancel(); 
			HistoryCancellationTokenSource = null;
		}
		
		HistoryCancellationTokenSource = new CancellationTokenSource();

		_ = populateImagesAsync(list, HistoryCancellationTokenSource.Token);
	}
	
	private async Task populateImagesAsync(IList itemsSource, CancellationToken token)
	{
		try
		{
			token.ThrowIfCancellationRequested();

			var items = Database.GetImageItems(true, false);

			for (var i = 0; i < items.Count; i++)
			{
				if (i % 25 == 0)
					await Task.Delay(25, token);

				itemsSource.Add(items[i]);
			}
		}
		catch (TaskCanceledException)
		{
			
		}
		catch (Exception ex)
		{
			Logger.Instance.Log(ex.ToString());
		}
	}

	#endregion
}