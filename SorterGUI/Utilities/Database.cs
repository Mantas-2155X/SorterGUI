using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SorterGUI.Items;
using SQLite;

namespace SorterGUI.Utilities;

public class Database
{
	public static Database Instance;
	
	private string path = "Data/Database.sqlite";

	private SQLiteConnection connection = null!;
	
	public Database()
	{
		Instance = this;
		initializeDatabase();
	}

	private void initializeDatabase()
	{
		Logger.Instance.Log("Initializing database");
		
		if (!File.Exists(path))
			createDatabase();
		else
			loadDatabase();
		
		Logger.Instance.Log("Initialized database");
	}

	private void createDatabase()
	{
		Logger.Instance.Log("Creating database");

		var directory = path[..path.IndexOf('/')];
		
		if (!Directory.Exists(directory))
			Directory.CreateDirectory(directory);
		
		connection = new SQLiteConnection(path);
		connection.CreateTable<Statistics>();
		connection.CreateTable<ImageItem>();
		connection.CreateTable<HistoryItem>();

		Logger.Instance.Log("Created database");
	}

	private void loadDatabase()
	{
		Logger.Instance.Log("Loading database");
		
		connection = new SQLiteConnection(path);

		Logger.Instance.Log("Loaded database");
	}
	
	public void ClearDatabase()
	{
		Logger.Instance.Log("Clearing database");
		
		connection = new SQLiteConnection(path);
		connection.DeleteAll<Statistics>();
		connection.DeleteAll<ImageItem>();
		connection.DeleteAll<HistoryItem>();
		
		Logger.Instance.Log("Cleared database");
	}

	public List<HistoryItem> GetHistoryItems(long amount, bool startFromLast)
	{
		if (startFromLast)
			return connection.Table<HistoryItem>().TakeLast((int)amount).ToList();

		return connection.Table<HistoryItem>().Take((int)amount).ToList();
	}
	
	public List<HistoryItem> GetHistoryItems(long startAt, int amountPerPage)
	{
		return connection.Table<HistoryItem>().Skip((int)startAt).Take(amountPerPage).ToList();
	}

	public void AddHistoryItem(long leftId, long rightId, long leftEloChange, long rightEloChange)
	{
		connection.Insert(new HistoryItem(leftId, leftEloChange, rightId, rightEloChange), typeof(HistoryItem));
	}
	
	public int GetHistoryPages(long amountPerPage)
	{
		var historyItemsCount = connection.Table<HistoryItem>().Count();
		return (int)Math.Ceiling((double)historyItemsCount / amountPerPage);
	}

	public List<ImageItem> GetImageItems()
	{
		return connection.Table<ImageItem>().ToList();
	}
	
	public ImageItem? GetImageItem(long id)
	{
		return connection.Table<ImageItem>().FirstOrDefault(obj => obj.Id == id);
	}
	
	public ImageItem? GetRandomImage(ImageItem? excludeImage = null)
	{
		if (excludeImage == null)
			return connection.Query<ImageItem>("SELECT * FROM ImageItem ORDER BY RANDOM() LIMIT 1").FirstOrDefault();
		
		return connection.Query<ImageItem>("SELECT * FROM ImageItem WHERE Id != ? ORDER BY RANDOM() LIMIT 1", excludeImage.Id).FirstOrDefault();
	}

	public void RemoveImageItem(string relativePath)
	{
		connection.Table<ImageItem>().Where(obj => obj.RelativePath == relativePath).Delete();
	}
	
	public void AddImageItem(string relativePath)
	{
		connection.Insert(new ImageItem(relativePath, 1000, 0), typeof(ImageItem));
	}
	
	public void UpdateImageItem(ImageItem imageItem)
	{
		connection.InsertOrReplace(imageItem, typeof(ImageItem));
	}

	public bool ImageItemExists(string relativePath)
	{
		return connection.Table<ImageItem>().Any(obj => obj.RelativePath == relativePath);
	}
	
	public long GetUnmatchedImagesCount()
	{
		return connection.Table<ImageItem>().Count(obj => obj.Matches == 0);
	}
	
	public long GetImagesCount()
	{
		return connection.Table<ImageItem>().Count();
	}

	public float GetVariation()
	{
		var value = connection.Table<Statistics>().FirstOrDefault(obj => obj.Key == "Variation");
		if (value == null)
			return 0;
		
		return Convert.ToSingle(value.Value);
	}
	
	public void SetVariation(float value)
	{
		connection.InsertOrReplace(new Statistics { Key = "Variation", Value = value.ToString(CultureInfo.InvariantCulture) }, typeof(Statistics));
	}
	
	public long GetTotalComparisons()
	{
		var value = connection.Table<Statistics>().FirstOrDefault(obj => obj.Key == "TotalComparisons");
		if (value == null)
			return 0;
		
		return Convert.ToInt64(value.Value);
	}

	public void SetTotalComparisons(long value)
	{
		connection.InsertOrReplace(new Statistics { Key = "TotalComparisons", Value = value.ToString() }, typeof(Statistics));
	}
}