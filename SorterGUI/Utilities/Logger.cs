using System;
using System.IO;

namespace SorterGUI.Utilities;

public class Logger
{
	public static Logger Instance;
	
	private string path = "Data/Logs.txt";

	public Logger()
	{
		Instance = this;
		initializeLogger();
	}

	private void initializeLogger()
	{
		var directory = path[..path.IndexOf('/')];
		
		if (!Directory.Exists(directory))
			Directory.CreateDirectory(directory);
	}
	
	public void Log(string message)
	{
		var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n";
		Console.Write(text);
		File.AppendAllText(path, text);
	}
}