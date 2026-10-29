using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;

namespace ColorLeaves.Core;

public class ReleaseInfo
{
	public string Tag { get; set; }

	public Version Version { get; set; }

	public string Notes { get; set; }

	public string AssetUrl { get; set; }
}

/// <summary>
/// Проверка обновлений через GitHub Releases (репозиторий публичный, токен не нужен).
/// </summary>
public static class UpdateChecker
{
	private const string RepoApiUrl = "https://api.github.com/repos/Alinanananams/color-leaves/releases/latest";

	/// <summary>
	/// Возвращает информацию о последнем релизе или null, если проверить не удалось.
	/// </summary>
	public static ReleaseInfo CheckLatest()
	{
		try
		{
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			using WebClient webClient = new WebClient();
			webClient.Headers.Add("User-Agent", "ColorLeaves-Updater");
			webClient.Headers.Add("Accept", "application/vnd.github+json");
			string json = webClient.DownloadString(RepoApiUrl);
			return Parse(json);
		}
		catch (Exception ex)
		{
			Log.Write("Проверка обновлений: " + ex.Message);
			return null;
		}
	}

	public static ReleaseInfo Parse(string json)
	{
		JavaScriptSerializer serializer = new JavaScriptSerializer();
		Dictionary<string, object> dictionary = serializer.DeserializeObject(json) as Dictionary<string, object>;
		if (dictionary == null)
		{
			return null;
		}
		ReleaseInfo releaseInfo = new ReleaseInfo();
		releaseInfo.Tag = (dictionary.TryGetValue("tag_name", out object value) ? (value as string) : null) ?? "";
		releaseInfo.Notes = (dictionary.TryGetValue("body", out object value2) ? (value2 as string) : null) ?? "";
		if (!TryParseVersion(releaseInfo.Tag, out Version result))
		{
			return null;
		}
		releaseInfo.Version = result;
		if (dictionary.TryGetValue("assets", out object value3) && value3 is object[] array)
		{
			object[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				Dictionary<string, object> dictionary2 = array2[i] as Dictionary<string, object>;
				if (dictionary2 == null)
				{
					continue;
				}
				string text = ((dictionary2.TryGetValue("name", out object value4) ? (value4 as string) : null) ?? "");
				string text2 = ((dictionary2.TryGetValue("browser_download_url", out object value5) ? (value5 as string) : null) ?? "");
				if (text.EndsWith("-setup.exe", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(text2))
				{
					releaseInfo.AssetUrl = text2;
					break;
				}
			}
		}
		return releaseInfo;
	}

	public static bool TryParseVersion(string tag, out Version version)
	{
		version = null;
		if (string.IsNullOrEmpty(tag))
		{
			return false;
		}
		string s = tag.TrimStart('v', 'V');
		return Version.TryParse(s, out version);
	}

	public static bool IsNewer(ReleaseInfo release, Version current)
	{
		return release?.Version != null && release.Version > current;
	}
}
