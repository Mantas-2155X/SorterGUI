using SQLite;

namespace SorterGUI.Items;

public class Statistics
{
	[PrimaryKey, Unique]
	public string Key { get; set; }

	public string Value { get; set; }
}