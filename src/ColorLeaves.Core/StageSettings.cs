using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Xml;

namespace ColorLeaves.Core;

public class StageSettings
{
	public const string Unset = "Не задана";

	private static StageSettings _instance;

	public List<StageDef> Stages { get; private set; }

	public static StageSettings Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new StageSettings();
				_instance.Load();
			}
			return _instance;
		}
	}

	public List<string> Names
	{
		get
		{
			List<string> list = new List<string>();
			foreach (StageDef stage in Stages)
			{
				list.Add(stage.Name);
			}
			return list;
		}
	}

	private static string FilePath
	{
		get
		{
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Autodesk", "Revit", "ColorSheets");
			return Path.Combine(path, "stages.xml");
		}
	}

	private StageSettings()
	{
		Stages = CreateDefaults();
	}

	public static List<StageDef> CreateDefaults()
	{
		List<StageDef> list = new List<StageDef>();
		list.Add(new StageDef("Черновик", "D9D9D9"));
		list.Add(new StageDef("На проверке", "FFE699"));
		list.Add(new StageDef("Проверен", "BDD7EE"));
		list.Add(new StageDef("Утверждён", "C6E0B4"));
		list.Add(new StageDef("Выпущен", "A9D08E"));
		list.Add(new StageDef("Не выдавать", "F8CBAD"));
		list.Add(new StageDef("Не задана", "FFFFFF"));
		return list;
	}

	public StageDef Find(string name)
	{
		if (name == null)
		{
			return null;
		}
		for (int i = 0; i < Stages.Count; i++)
		{
			if (string.Equals(Stages[i].Name, name, StringComparison.OrdinalIgnoreCase))
			{
				return Stages[i];
			}
		}
		return null;
	}

	public Color ColorOf(string name)
	{
		return Find(name)?.Color ?? Color.White;
	}

	public void Load()
	{
		try
		{
			string filePath = FilePath;
			if (!File.Exists(filePath))
			{
				return;
			}
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(filePath);
			XmlNodeList xmlNodeList = xmlDocument.SelectNodes("/stages/stage");
			if (xmlNodeList == null || xmlNodeList.Count == 0)
			{
				return;
			}
			List<StageDef> list = new List<StageDef>();
			foreach (XmlNode item in xmlNodeList)
			{
				string text = ((item.Attributes["name"] == null) ? null : item.Attributes["name"].Value);
				string colorHex = ((item.Attributes["color"] == null) ? null : item.Attributes["color"].Value);
				if (!string.IsNullOrEmpty(text))
				{
					list.Add(new StageDef(text, colorHex));
				}
			}
			if (list.Count > 0)
			{
				Stages = list;
			}
		}
		catch
		{
			Stages = CreateDefaults();
		}
	}

	public void Save()
	{
		try
		{
			string filePath = FilePath;
			string directoryName = Path.GetDirectoryName(filePath);
			if (!Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			XmlDocument xmlDocument = new XmlDocument();
			XmlElement xmlElement = xmlDocument.CreateElement("stages");
			xmlDocument.AppendChild(xmlElement);
			foreach (StageDef stage in Stages)
			{
				XmlElement xmlElement2 = xmlDocument.CreateElement("stage");
				xmlElement2.SetAttribute("name", stage.Name ?? "");
				xmlElement2.SetAttribute("color", stage.ColorHex ?? "FFFFFF");
				xmlElement.AppendChild(xmlElement2);
			}
			xmlDocument.Save(filePath);
		}
		catch (Exception)
		{
		}
	}
}
