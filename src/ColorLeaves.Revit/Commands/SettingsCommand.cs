using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ColorLeaves.Core;
using ColorLeaves.Revit.UI;

namespace ColorLeaves.Revit.Commands;

[Regeneration(RegenerationOption.Manual)]
[Transaction(TransactionMode.Manual)]
public class SettingsCommand : IExternalCommand
{
	public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
	{
		try
		{
			using SettingsForm settingsForm = new SettingsForm();
			settingsForm.ShowDialog();
			return Result.Succeeded;
		}
		catch (Exception ex)
		{
			Log.Write("SettingsCommand: " + ex.Message);
			message = ex.Message;
			return Result.Failed;
		}
	}
}
