using System.IO;
using Avalonia.Media;
using Avalonia.Media.Imaging;
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
	
	public string GetName()
	{
		var fileInfo = new FileInfo(Settings.Instance.GetStringSetting("imagespath") + "/" + RelativePath);
		if (!fileInfo.Exists)
			return "";
		
		return Path.GetFileNameWithoutExtension(fileInfo.Name);
	}
	
	public IImage? GetImage()
	{
		var fileInfo = new FileInfo(Settings.Instance.GetStringSetting("imagespath") + "/" + RelativePath);
		if (!fileInfo.Exists)
			return null;

		var bitmap = new Bitmap(fileInfo.FullName);
		return bitmap;
	}
}