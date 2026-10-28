using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ColorLeaves.Core;
using ColorLeaves.Revit.UI;

namespace ColorLeaves.Revit.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class CreateParameterCommand : IExternalCommand
{
	public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
	{
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Expected O, but got Unknown
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		Log.Write("CreateParameter: старт");
		try
		{
			Document document = commandData.Application.ActiveUIDocument.Document;
			Bridge.SetUiApp(commandData.Application);
			string info;
			bool flag = ParameterInstaller.EnsureStageParameter(document, out info);
			Bridge.RequestRefresh();
			Bridge.Tick();
			SheetPaneProvider.Refresh();
			Log.Write("CreateParameter: " + (flag ? "ок" : "не удалось") + " — " + info);
			TaskDialog val = new TaskDialog("Цветные листики");
			val.MainInstruction = (flag ? "Параметр готов" : "Не удалось создать параметр");
			val.MainContent = info;
			val.Show();
			return (Result)((!flag) ? (-1) : 0);
		}
		catch (Exception ex)
		{
			Log.Write("CreateParameter: ИСКЛЮЧЕНИЕ", ex);
			Bridge.ShowError("Ошибка создания параметра", ex.Message);
			return (Result)(-1);
		}
	}
}
