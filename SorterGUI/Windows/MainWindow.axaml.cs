using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using SorterGUI.Items;
using SorterGUI.Utilities;

namespace SorterGUI.Windows;

public class MainWindow : Window, INotifyPropertyChanged
{
	public Logger Logger { get; }
	public Settings Settings { get; }
	public Database Database { get; }

	public ObservableCollection<HistoryItem> HistoryItems { get; } = new ();

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
			else if (value == 1) // Sort Images
			{
				setupSortImages();
			}
			else if (value == 2) // History
			{
				// Already calls setup
				HistoryPage = 1;
			}
			else if (value == 3) // Options
			{
			
			}
			else if (value == 4) // About
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
	
	public bool PickingWinner;

	public ImageItem? LeftImage;
	public ImageItem? RightImage;

	public CancellationTokenSource? CancellationTokenSource = new ();

	#region Text

	public string StatisticsTabText => "Statistics";
	public string SortImagesTabText => "Sort Images";
	public string HistoryTabText => "History";
	public string OptionsTabText => "Options";
	public string AboutTabText => "About";
	
	public string WhichDoYouPreferText => "Which do you prefer?";
	public string NotEnoughImagesText => "At least two images must be registered before sorting";
	
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
		
			var result = await confirmation.ShowDialog<bool>(this);
			if (!result)
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
	
	public void OnLeftImageClicked(object? sender, RoutedEventArgs e)
	{
		if (PickingWinner)
			return;
		
		_ = pickWinner(true);
	}
	
	public void OnRightImageClicked(object? sender, RoutedEventArgs e)
	{
		if (PickingWinner)
			return;

		_ = pickWinner(false);
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
	
	public void OnExitClicked(object? sender, RoutedEventArgs e)
	{
		Close();
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

	private void setupSortImages()
	{
		var availableImages = (float)Database.GetImagesCount();
		var matchedImages = availableImages - Database.GetUnmatchedImagesCount();

		this.GetControl<Label>("MatchedImages").Content = $"Matched Images: {matchedImages}/{availableImages} ({matchedImages / availableImages * 100f:F0}%)";
		this.GetControl<Label>("Variation").Content = $"Variation: ~{Database.GetVariation():F2}";

		var imageCount = Database.GetImagesCount();
		if (imageCount < 2)
		{
			this.GetControl<Label>("SortUnavailable").IsVisible = true;
			this.GetControl<TextBlock>("Sort1").IsVisible = false;
			this.GetControl<Grid>("Sort2").IsVisible = false;
			this.GetControl<Grid>("Sort3").IsVisible = false;
			
			Logger.Instance.Log("Less than two images registered, skipping sorting");
			return;
		}

		this.GetControl<Label>("SortUnavailable").IsVisible = false;
		this.GetControl<TextBlock>("Sort1").IsVisible = true;
		this.GetControl<Grid>("Sort2").IsVisible = true;
		this.GetControl<Grid>("Sort3").IsVisible = true;

		LeftImage = Database.GetRandomImage();
		RightImage = Database.GetRandomImage(LeftImage);
		
		if (LeftImage == null || RightImage == null)
		{
			if (SortImagesRetries >= 3)
			{
				this.GetControl<TextBlock>("LeftImageTitle").Text = "";
				this.GetControl<Label>("RightImageTitle").Content = "";

				this.GetControl<Image>("LeftImage").Source = null;
				this.GetControl<Image>("RightImage").Source = null;
				
				Logger.Instance.Log("Hit the limit for null images, giving up");
				return;
			}
			
			Logger.Instance.Log("One or both images are null, retrying");
			setupSortImages();
			return;
		}

		SortImagesRetries = 0;
		
		this.GetControl<TextBlock>("LeftImageTitle").Text = LeftImage.GetName();
		this.GetControl<Label>("RightImageTitle").Content = RightImage.GetName();

		this.GetControl<Image>("LeftImage").Source = LeftImage.GetImage();
		this.GetControl<Image>("RightImage").Source = RightImage.GetImage();
	}

	private async Task pickWinner(bool leftWon)
	{
		try
		{
			PickingWinner = true;

			var leftButton = this.GetControl<Button>("LeftImageButton");
			var rightButton = this.GetControl<Button>("RightImageButton");
		
			leftButton.IsEnabled = false;
			leftButton.Classes.Clear();
			leftButton.Classes.Add(leftWon ? "GreenDisabledBorder" : "RedDisabledBorder");
		
			rightButton.IsEnabled = false;
			rightButton.Classes.Clear();
			rightButton.Classes.Add(!leftWon ? "GreenDisabledBorder" : "RedDisabledBorder");
		
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
			
			await Task.Delay(300);
			setupSortImages();
			
			leftButton.IsEnabled = true;
			leftButton.Classes.Clear();
			leftButton.Classes.Add("GreenHoverBorder");

			rightButton.IsEnabled = true;
			rightButton.Classes.Clear();
			rightButton.Classes.Add("GreenHoverBorder");

			PickingWinner = false;
		}
		catch (Exception ex)
		{
			Logger.Instance.Log(ex.ToString());
		}
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
		
		if (CancellationTokenSource != null)
		{
			CancellationTokenSource.Cancel(); 
			CancellationTokenSource = null;
		}
		
		CancellationTokenSource = new CancellationTokenSource();

		_ = populateHistoryAsync(list, CancellationTokenSource.Token);
	}

	private async Task populateHistoryAsync(IList itemsSource, CancellationToken token)
	{
		try
		{
			token.ThrowIfCancellationRequested();

			var items = Database.GetHistoryItems(HistoryPage * HistoryPerPage - HistoryPerPage, HistoryPerPage, true);

			for (var i = 0; i < items.Count; i++)
			{
				if (i % 5 == 0)
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