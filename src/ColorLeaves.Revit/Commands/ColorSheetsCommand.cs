using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using ColorLeaves.Revit.Services;

namespace ColorLeaves.Revit.Commands;

[Transaction(TransactionMode.Manual)]
public class ColorSheetsCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            var uiDoc = commandData.Application.ActiveUIDocument;
            var doc = uiDoc.Document;

            var service = new SheetColorService(doc);
            service.ProcessSheets();

            TaskDialog.Show("Color Leaves", "Команда выполнена успешно.");
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
