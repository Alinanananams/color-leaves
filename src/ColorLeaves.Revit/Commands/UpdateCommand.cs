using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ColorLeaves.Core;

namespace ColorLeaves.Revit.Commands;

[Regeneration(RegenerationOption.Manual)]
[Transaction(TransactionMode.Manual)]
public class UpdateCommand : IExternalCommand
{
	private const string TempSetupPath = "ColorLeaves-setup.exe";

	private const string TempScriptPath = "ColorLeaves-update.cmd";

	public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
	{
		try
		{
			Version current = typeof(UpdateCommand).Assembly.GetName().Version;
			string currentText = current.ToString(3);

			Log.Write("Проверка обновлений: текущая версия " + currentText);
			ReleaseInfo latest = UpdateChecker.CheckLatest();
			if (latest == null)
			{
				TaskDialog.Show("Color Leaves — обновление", "Не удалось проверить обновления.\r\nПроверьте подключение к интернету и попробуйте позже.");
				return Result.Succeeded;
			}

			if (!UpdateChecker.IsNewer(latest, current))
			{
				TaskDialog.Show("Color Leaves — обновление", "У вас установлена последняя версия (" + currentText + ").");
				return Result.Succeeded;
			}

			string latestText = (string.IsNullOrEmpty(latest.Tag) ? latest.Version.ToString(3) : latest.Tag);
			TaskDialog taskDialog = new TaskDialog("Color Leaves — доступно обновление")
			{
				MainInstruction = "Доступна версия " + latestText + " (у вас " + currentText + ")",
				MainContent = (string.IsNullOrEmpty(latest.Notes) ? "" : latest.Notes + "\r\n\r\n") + "Скачать и установить сейчас?",
				MainIcon = TaskDialogIcon.TaskDialogIconInformation,
				CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No,
				DefaultButton = TaskDialogResult.Yes
			};

			if (taskDialog.Show() == TaskDialogResult.Yes)
			{
				if (string.IsNullOrEmpty(latest.AssetUrl))
				{
					TaskDialog.Show("Color Leaves — обновление", "В релизе " + latest.Version.ToString(3) + " не найден установочный файл.");
					return Result.Succeeded;
				}
				DownloadAndPrepare(latest);
			}
			return Result.Succeeded;
		}
		catch (Exception ex)
		{
			Log.Write("UpdateCommand: исключение", ex);
			TaskDialog.Show("Color Leaves — обновление", "Ошибка проверки обновлений: " + ex.Message);
			return Result.Succeeded;
		}
	}

	private static void DownloadAndPrepare(ReleaseInfo release)
	{
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

		TaskDialog.Show("Color Leaves — обновление", "Обновление скачано.\r\n\r\nЗакройте Revit — новая версия установится автоматически.");
	}
}
