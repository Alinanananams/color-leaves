using System;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ColorLeaves.Core.Constants;
using ColorLeaves.Revit.Commands;

namespace ColorLeaves.Revit;

public class App : IExternalApplication
{
    public Result OnStartup(UIControlledApplication application)
    {
        try
        {
            // Вкладку нужно создать явно, прежде чем добавлять на неё панель
            application.CreateRibbonTab(PluginConstants.TabName);

            var panel = application.CreateRibbonPanel(PluginConstants.TabName, PluginConstants.PanelName);

            var buttonData = new PushButtonData(
                "ColorSheets",
                "Цветные листики",
                typeof(App).Assembly.Location,
                typeof(ColorSheetsCommand).FullName)
            {
                ToolTip = "Запуск команды раскраски листов",
                LongDescription = "Плагин Color Leaves — автоматизация раскраски листов по сборкам",
                AvailabilityClassName = typeof(AlwaysAvailable).FullName
            };

            try
            {
                buttonData.LargeImage = new BitmapImage(new Uri("pack://application:,,,/ColorLeaves.Revit;component/Resources/icon_large.png"));
                buttonData.Image = new BitmapImage(new Uri("pack://application:,,,/ColorLeaves.Revit;component/Resources/icon_small.png"));
            }
            catch (Exception iconEx)
            {
                // Отсутствие иконок не должно мешать работе плагина
                System.Diagnostics.Debug.WriteLine("Не удалось загрузить иконки: " + iconEx.Message);
            }

            panel.AddItem(buttonData);
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            var dialog = new TaskDialog("Color Leaves — ошибка запуска")
            {
                MainInstruction = "Не удалось инициализировать плагин.",
                MainContent = ex.ToString(),
                MainIcon = TaskDialogIcon.TaskDialogIconError
            };
            dialog.Show();
            return Result.Failed;
        }
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
