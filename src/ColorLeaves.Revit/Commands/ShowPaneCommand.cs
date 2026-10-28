using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ColorLeaves.Core;
using ColorLeaves.Revit.UI;

namespace ColorLeaves.Revit.Commands;

[Regeneration(RegenerationOption.Manual)]
[Transaction(TransactionMode.Manual)]
public class ShowPaneCommand : IExternalCommand
{
	public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected O, but got Unknown
		Log.Write("ShowPane: старт");
		UIApplication application;
		try
		{
			application = commandData.Application;
		}
		catch (Exception ex)
		{
			Log.Write("ShowPane: нет UIApplication", ex);
			Bridge.ShowError("Не удалось открыть панель", ex.Message);
			return (Result)(-1);
		}
		try
		{
			DockablePane dockablePane = application.GetDockablePane(new DockablePaneId(App.PaneId));
			Log.Write("ShowPane: GetDockablePane -> " + ((dockablePane == null) ? "null" : "ok"));
			if (dockablePane != null)
			{
				dockablePane.Show();
			}
			Log.Write("ShowPane: панель показана");
		}
		catch (Exception ex2)
		{
			Log.Write("ShowPane: ОШИБКА показа панели", ex2);
			Bridge.ShowError("Не удалось открыть панель", ex2.Message);
			return (Result)(-1);
		}
		try
		{
			Bridge.SetUiApp(application);
			Bridge.RequestRefresh();
			Bridge.Tick();
			SheetPaneProvider.Refresh();
			Log.Write("ShowPane: готово, листов = " + Bridge.Rows.Count);
		}
		catch (Exception ex3)
		{
			Log.Write("ShowPane: ОШИБКА чтения модели", ex3);
		}
		return (Result)0;
	}
}
