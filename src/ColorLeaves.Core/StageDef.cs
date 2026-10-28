using System;
using System.Drawing;

namespace ColorLeaves.Core;

public class StageDef
{
	public string Name { get; set; }

	public string ColorHex { get; set; }

	public Color Color
	{
		get
		{
			if (string.IsNullOrEmpty(ColorHex))
			{
				return Color.White;
			}
			try
			{
				string text = ColorHex.TrimStart('#');
				if (text.Length != 6)
				{
					return Color.White;
				}
				return Color.FromArgb(Convert.ToInt32(text.Substring(0, 2), 16), Convert.ToInt32(text.Substring(2, 2), 16), Convert.ToInt32(text.Substring(4, 2), 16));
			}
			catch
			{
				return Color.White;
			}
		}
		set
		{
			ColorHex = value.R.ToString("X2") + value.G.ToString("X2") + value.B.ToString("X2");
		}
	}

	public StageDef()
	{
	}

	public StageDef(string name, string colorHex)
	{
		Name = name;
		ColorHex = colorHex;
	}

	public StageDef Clone()
	{
		return new StageDef(Name, ColorHex);
	}
}
