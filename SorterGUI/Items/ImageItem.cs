using System;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SorterGUI.Utilities;
using SQLite;

namespace SorterGUI.Items;

public class ImageItem
{
	[PrimaryKey, AutoIncrement]
	public long Id { get; set; }
	
	[Unique]
	public string RelativePath { get; set; }
	
	public long Matches { get; set; }
	public long Elo { get; set; }

	public ImageItem()
	{
		
	}
	
	public ImageItem(string relativePath, long elo, long matches)
	{
		RelativePath = relativePath;
		Matches = matches;
		Elo = elo;
	}

	public bool FileExists(out FileInfo fileInfo)
	{
		fileInfo = new FileInfo(Settings.Instance.GetStringSetting("imagespath") + "/" + RelativePath);
		return fileInfo.Exists;
	}
	
	public string GetName()
	{
		if (!FileExists(out var fileInfo))
			return RelativePath;
		
		return Path.GetFileNameWithoutExtension(fileInfo.Name);
	}
	
	public IImage GetImage()
	{
		if (!FileExists(out var fileInfo))
			return new Bitmap(AssetLoader.Open(new Uri("avares://SorterGUI/Assets/missingimage.png")));

		return new Bitmap(fileInfo.FullName);
	}
}