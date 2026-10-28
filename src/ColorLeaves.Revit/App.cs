using System;
using System.Windows.Media.Imaging;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ColorLeaves.Core.Constants;
using ColorLeaves.Revit.Commands;

namespace ColorLeaves.Revit;

[Transaction(TransactionMode.Manual)]
public class App : IExternalApplication
{
    public Result OnStartup(UIControlledApplication application)
    {
        var panel = application.CreateRibbonPanel(PluginConstants.TabName, PluginConstants.PanelName);
        var buttonData = new PushButtonData(
            "ColorSheets",
            "Цветные листики",
            typeof(App).Assembly.Location,
            typeof(ColorSheetsCommand).FullName);

        buttonData.LargeImage = new BitmapImage(new Uri("pack://application:,,,/ColorLeaves.Revit;component/Resources/icon_large.png"));
        buttonData.Image = new BitmapImage(new Uri("pack://application:,,,/ColorLeaves.Revit;component/Resources/icon_small.png"));
        buttonData.ToolTip = PluginConstants.PluginName;
        buttonData.LongDescription = "Запуск команды раскраски листов по сборкам";
        buttonData.AvailabilityClassName = typeof(AlwaysAvailable).FullName;

        panel.AddItem(buttonData);
        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        return Result.Succeeded;
    }
}

public class AlwaysAvailable : IExternalCommandAvailability
{
    public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories) => true;
}
