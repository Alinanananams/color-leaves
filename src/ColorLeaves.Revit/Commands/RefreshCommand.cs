using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ColorLeaves.Core;
using ColorLeaves.Revit.UI;

namespace ColorLeaves.Revit.Commands;

[Regeneration(RegenerationOption.Manual)]
[Transaction(TransactionMode.Manual)]
public class RefreshCommand : IExternalCommand
{
	public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
	{
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		Log.Write("Refresh: старт");
		try
		{
			Bridge.SetUiApp(commandData.Application);
			Bridge.RequestRefresh();
			Bridge.Tick();
			int pendingCount = Bridge.PendingCount;
			if (pendingCount > 0)
			{
				Log.Write("Refresh: не применено изменений стадии: " + pendingCount);
			}
			string info;
			bool flag = Bridge.SynchronizeWithCentral(out info);
			Log.Write("Refresh: " + info);
			Bridge.RequestRefresh();
			Bridge.Tick();
			SheetPaneProvider.Refresh();
			if (!flag)
			{
				Bridge.ShowError("Обновление из центральной модели", info);
			}
			return (Result)0;
		}
		catch (Exception ex)
		{
			Log.Write("Refresh: ИСКЛЮЧЕНИЕ", ex);
			Bridge.ShowError("Ошибка обновления", ex.Message);
			return (Result)(-1);
		}
	}
}
