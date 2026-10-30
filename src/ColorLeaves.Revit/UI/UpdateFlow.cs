using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using ColorLeaves.Core;

namespace ColorLeaves.Revit.UI;

/// <summary>Проверка и скачивание обновлений: общая логика для окна настроек.</summary>
public static class UpdateFlow
{
	private const string TempSetupPath = "ColorLeaves-setup.exe";

	private const string TempScriptPath = "ColorLeaves-update.cmd";

	/// <summary>Проверяет обновления. Возвращает короткий текст для окна; available — есть ли обновление.</summary>
	public static string Check(out ReleaseInfo latest, out bool available)
	{
		latest = null;
		available = false;
		Version current = typeof(UpdateFlow).Assembly.GetName().Version;
		string currentText = current.ToString(3);
		Log.Write("Проверка обновлений: текущая версия " + currentText);
		latest = UpdateChecker.CheckLatest();
		if (latest == null)
		{
			return "Не удалось проверить обновления. Проверьте интернет и попробуйте позже.";
		}
		if (!UpdateChecker.IsNewer(latest, current))
		{
			return "У вас последняя версия (v" + currentText + ").";
		}
		available = true;
		string tag = string.IsNullOrEmpty(latest.Tag) ? ("v" + latest.Version.ToString(3)) : latest.Tag;
		return "Доступна версия " + tag + " (у вас v" + currentText + ").";
	}

	/// <summary>Скачивает setup и готовит автоустановку после закрытия Revit. Возвращает короткий статус.</summary>
	public static string DownloadAndPrepare(ReleaseInfo release)
	{
		if (string.IsNullOrEmpty(release.AssetUrl))
		{
			return "В релизе не найден установочный файл.";
		}
		string tempPath = Path.GetTempPath();
		string setupPath = Path.Combine(tempPath, TempSetupPath);
		string scriptPath = Path.Combine(tempPath, TempScriptPath);
		Log.Write("Обновление: скачиваю " + release.AssetUrl);
		ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
		using (WebClient webClient = new WebClient())
		{
			webClient.Headers.Add("User-Agent", "ColorLeaves-Updater");
			webClient.DownloadFile(release.AssetUrl, setupPath);
		}
		Log.Write("Обновление: скачано в " + setupPath);
		// Скрипт ждёт закрытия Revit (DLL освобождается) и запускает установщик в тихом режиме
		string script = "@echo off\r\n"
			+ ":wait\r\n"
			+ "timeout /t 2 /nobreak >nul\r\n"
			+ "tasklist /FI \"IMAGENAME eq Revit.exe\" 2>nul | find /I \"Revit.exe\" >nul\r\n"
			+ "if not errorlevel 1 goto wait\r\n"
			+ "start \"\" \"" + setupPath + "\" /silent\r\n"
			+ "del \"%~f0\"\r\n";
		File.WriteAllText(scriptPath, script);
		Process.Start(new ProcessStartInfo
		{
			FileName = "cmd.exe",
			Arguments = "/c \"" + scriptPath + "\"",
			CreateNoWindow = true,
			UseShellExecute = false
		});
		Log.Write("Обновление: скрипт запущен, применится после закрытия Revit");
		return "Обновление скачано. Закройте Revit — установится автоматически.";
	}
}
