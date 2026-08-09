using System;
using Avalonia.Media;
using SorterGUI.Utilities;
using SQLite;

namespace SorterGUI.Items;

public class HistoryItem
{
	[PrimaryKey, AutoIncrement]
	public long Id { get; set; }
	
	public long LeftId { get; set; }
	public long RightId { get; set; }

	public long LeftEloChange { get; set; }
	public long RightEloChange { get; set; }

	public string LeftName
	{
		get
		{
			var item = Database.Instance.GetImageItem(LeftId);
			return item == null ? "" : item.GetName();
		}
	}
	
	public string RightName
	{
		get
		{
			var item = Database.Instance.GetImageItem(RightId);
			return item == null ? "" : item.GetName();
		}
	}
	
	public string LeftEloChangeText => $"{(LeftEloChange < 0 ? "-" : "+")}{LeftEloChange}";
	public string RightEloChangeText => $"{(RightEloChange < 0 ? "-" : "+")}{RightEloChange}";

	public IBrush LeftColor => getColor(LeftEloChange);
	public IBrush RightColor => getColor(RightEloChange);

	public HistoryItem()
	{
		
	}
	
	public HistoryItem(long leftId, long leftEloChange, long rightId, long rightEloChange)
	{
		LeftId = leftId;
		RightId = rightId;
		LeftEloChange = leftEloChange;
		RightEloChange = rightEloChange;
	}
	
	private IBrush getColor(long eloChange)
	{
		return eloChange < 0 ? Brushes.OrangeRed : Brushes.SpringGreen;
	}

	public long GetEloChange()
	{
		return Math.Abs(LeftEloChange - RightEloChange);
	}
}