using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ColorLeaves.Revit.Services;

namespace ColorLeaves.Revit.Commands;

[Transaction(TransactionMode.Manual)]
public class ColorSheetsCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDoc = commandData.Application.ActiveUIDocument;
        if (uiDoc == null)
        {
            TaskDialog.Show("Color Leaves", "Откройте проект и повторите команду.");
            return Result.Cancelled;
        }

        try
        {
            var doc = uiDoc.Document;

            var service = new SheetColorService(doc);
            service.ProcessSheets();

            TaskDialog.Show("Color Leaves", "Команда выполнена успешно.");
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            // Показываем полный текст исключения, чтобы сбои были диагностируемыми
            var dialog = new TaskDialog("Color Leaves — ошибка")
            {
                MainInstruction = "При выполнении команды произошла ошибка.",
                MainContent = ex.ToString(),
                MainIcon = TaskDialogIcon.TaskDialogIconError
            };
            dialog.Show();

            message = ex.Message;
            return Result.Failed;
        }
    }
}
