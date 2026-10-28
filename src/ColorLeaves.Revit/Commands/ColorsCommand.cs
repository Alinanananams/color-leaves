using System;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ColorLeaves.Core;
using ColorLeaves.Revit.UI;

namespace ColorLeaves.Revit.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class ColorsCommand : IExternalCommand
{
	public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		Log.Write("Colors: старт");
		try
		{
			using (SheetColorsForm sheetColorsForm = new SheetColorsForm())
			{
				if (sheetColorsForm.ShowDialog() != DialogResult.OK)
				{
					Log.Write("Colors: отменено");
					return (Result)1;
				}
			}
			StageSettings.Instance.Save();
			SheetPaneProvider.Refresh();
			Log.Write("Colors: сохранено");
			return (Result)0;
		}
		catch (Exception ex)
		{
			Log.Write("Colors: ИСКЛЮЧЕНИЕ", ex);
			Bridge.ShowError("Ошибка настройки цветов", ex.Message);
			return (Result)(-1);
		}
	}
}
