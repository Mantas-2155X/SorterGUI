using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SorterGUI.Utilities;

public class Settings
{
	public static Settings Instance;

	private string path = "Data/Settings.json";

	private Dictionary<string, object> settings = new ()
	{
		{ "imagespath", "" },
		{ "bottombar", false },
	};

	public Settings()
	{
		Instance = this;
		initializeSettings();
	}

	private void initializeSettings()
	{
		loadSettings();
		saveSettings();
	}
	
	private void loadSettings()
	{
		Logger.Instance.Log("Loading settings");
		
		var directory = path[..path.IndexOf('/')];
		
		if (!Directory.Exists(directory))
			Directory.CreateDirectory(directory);
		
		if (!File.Exists(path))
		{
			Logger.Instance.Log("Settings file is not found, skipping load");
			return;
		}
		
		var json = File.ReadAllText(path);
		
		if (string.IsNullOrEmpty(json))
		{
			Logger.Instance.Log("Settings file is empty, skipping load");
			return;
		}
		
		var deserialized = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
		if (deserialized == null)
		{
			Logger.Instance.Log("Deserialized settings are null, skipping load");
			return;
		}

		if (deserialized.Count == 0)
		{
			Logger.Instance.Log("Deserialized settings are empty, skipping load");
			return;
		}

		settings.Clear();
		
		foreach (var (key, value) in deserialized)
		{
			if (value is JsonElement jsonElement)
			{
				switch (jsonElement.ValueKind)
				{
					case JsonValueKind.String:
						settings[key] = jsonElement.GetString() ?? "";
						break;
					case JsonValueKind.False or JsonValueKind.True:
						settings[key] = jsonElement.GetBoolean();
						break;
					case JsonValueKind.Number:
						settings[key] = jsonElement.GetInt64();
						break;
					default:
						Logger.Instance.Log($"Skipping setting {key} because the value type {jsonElement.ValueKind} is not supported");
						break;
				}
			}
			else
			{
				switch (value)
				{
					case string or bool or long:
						settings[key] = value;
						break;
					default:
						Logger.Instance.Log($"Skipping setting {key} because the value type {value.GetType()} is not supported");
						break;
				}
			}
		}
		
		Logger.Instance.Log($"Loaded {settings.Count} settings");
	}

	private void saveSettings()
	{
		Logger.Instance.Log("Saving settings");
		
		var serialized = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true});
		File.WriteAllText(path, serialized);
		
		Logger.Instance.Log($"Saved {settings.Count} settings");
	}
	
	private object? getSetting(string key)
	{
		return settings.GetValueOrDefault(key);
	}

	public string GetStringSetting(string key)
	{
		var setting = getSetting(key);
		if (setting == null)
		{
			Logger.Instance.Log($"Setting with key {key} is not found");
			return "";
		}

		return Convert.ToString(setting) ?? "";
	}
	
	public bool GetBoolSetting(string key)
	{
		var setting = getSetting(key);
		if (setting == null)
		{
			Logger.Instance.Log($"Setting with key {key} is not found");
			return false;
		}

		return Convert.ToBoolean(setting);
	}
	
	public long GetLongSetting(string key)
	{
		var setting = getSetting(key);
		if (setting == null)
		{
			Logger.Instance.Log($"Setting with key {key} is not found");
			return -1;
		}
		
		return Convert.ToInt64(setting);
	}

	public void SetSetting(string key, object value)
	{
		if (value is not string and not bool and not long)
		{
			Logger.Instance.Log($"Setting with key {key} and type {value.GetType()} is not supported, ignoring");
			return;
		}
		
		settings[key] = value;
		saveSettings();
	}
}