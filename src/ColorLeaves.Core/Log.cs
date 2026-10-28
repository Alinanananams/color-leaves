using System;
using System.IO;
using System.Text;

namespace ColorLeaves.Core;

public static class Log
{
	private static readonly string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Autodesk", "Revit", "ColorSheets", "log.txt");

	public static void Write(string message)
	{
		try
		{
			string directoryName = Path.GetDirectoryName(FilePath);
			if (!Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(DateTime.Now.ToString("HH:mm:ss.fff"));
			stringBuilder.Append("  ");
			stringBuilder.Append(message);
			File.AppendAllText(FilePath, stringBuilder.ToString() + Environment.NewLine, Encoding.UTF8);
		}
		catch
		{
		}
	}

	public static void Write(string context, Exception ex)
	{
		Write(context + ": " + ex.GetType().Name + " — " + ex.Message);
		Write("   " + ex.StackTrace);
		if (ex.InnerException != null)
		{
			Write("   inner: " + ex.InnerException.Message);
		}
	}
}
