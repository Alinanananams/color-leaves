using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using ColorLeaves.Core;
using ColorLeaves.Revit.UI;

namespace ColorLeaves.Revit;

public class App : IExternalApplication
{
	public const string RibbonTabName = "Цветные листики";

	public const string PaneName = "Цветные листики";

	public static readonly Guid PaneId = new Guid("3A8F61D4-9C27-4E85-B1A0-7F62D4C93E15");

	public Result OnStartup(UIControlledApplication application)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		Log.Write("OnStartup: начало");
		try
		{
			application.RegisterDockablePane(new DockablePaneId(PaneId), "Цветные листики", (IDockablePaneProvider)(object)new SheetPaneProvider());
			Log.Write("OnStartup: панель зарегистрирована");
		}
		catch (Exception ex)
		{
			Log.Write("OnStartup: ОШИБКА регистрации панели: " + ex);
			return (Result)0;
		}
		application.ViewActivated += OnViewActivated;
		application.Idling += OnIdling;
		application.ControlledApplication.DocumentChanged += OnDocumentChanged;
		application.DockableFrameVisibilityChanged += OnDockableFrameVisibilityChanged;
		CreateRibbon(application);
		Log.Write("OnStartup: завершено");
		return (Result)0;
	}

	public Result OnShutdown(UIControlledApplication application)
	{
		application.ViewActivated -= OnViewActivated;
		application.Idling -= OnIdling;
		application.ControlledApplication.DocumentChanged -= OnDocumentChanged;
		application.DockableFrameVisibilityChanged -= OnDockableFrameVisibilityChanged;
		Log.Write("OnShutdown");
		return (Result)0;
	}

	private void OnDocumentChanged(object sender, Autodesk.Revit.DB.Events.DocumentChangedEventArgs e)
	{
		try
		{
			Bridge.OnDocumentChanged(e.GetDocument(), e.GetModifiedElementIds());
		}
		catch (Exception ex)
		{
			Log.Write("DocumentChanged: ошибка " + ex.Message);
		}
	}

	private void OnViewActivated(object sender, ViewActivatedEventArgs e)
	{
		try
		{
			// Фиксируем UIApplication как можно раньше — без него не работает переход к листу
			Document doc = ((RevitAPIPostDocEventArgs)e).Document;
			Bridge.SetUiApp(new UIApplication(doc.Application));
			Bridge.SetDocument(doc);
			Bridge.OnViewChanged(doc, doc.ActiveView);
			Log.Write("ViewActivated: документ сменён");
		}
		catch (Exception ex)
		{
			Log.Write("ViewActivated: ошибка " + ex.Message);
		}
	}

	private void OnIdling(object sender, IdlingEventArgs e)
	{
		try
		{
			// Одноразовый паттерн: при простое плагин не делает ничего.
			// Работа выполняется только когда пользователь что-то запросил
			if (!Bridge.HasQueuedWork)
			{
				return;
			}
			Bridge.Tick();
		}
		catch (Exception ex)
		{
			Log.Write("Idling: " + ex.Message);
		}
	}

	private void OnDockableFrameVisibilityChanged(object sender, DockableFrameVisibilityChangedEventArgs e)
	{
		try
		{
			// При изменении видимости нашей панели — обновить занятость листов и данные
			if (e.PaneId == new DockablePaneId(PaneId))
			{
				Bridge.RequestWorkshareCheck();
				Bridge.RequestRefresh();
			}
		}
		catch (Exception ex)
		{
			Log.Write("DockableFrameVisibilityChanged: " + ex.Message);
		}
	}

	private static void CreateRibbon(UIControlledApplication application)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Expected O, but got Unknown
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Expected O, but got Unknown
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Expected O, but got Unknown
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Expected O, but got Unknown
		try
		{
			try
			{
				application.CreateRibbonTab("Цветные листики");
				Log.Write("Вкладка «Цветные листики» создана");
			}
			catch (Exception)
			{
				Log.Write("Вкладка «Цветные листики» уже существует");
			}
			RibbonPanel val = application.CreateRibbonPanel("Цветные листики", "Цветные листики");
			string location = Assembly.GetExecutingAssembly().Location;
			PushButtonData val2 = new PushButtonData("ColorLeaves_Show", "Показать\nлисты", location, "ColorLeaves.Revit.Commands.ShowPaneCommand");
			PushButtonData val5 = new PushButtonData("ColorLeaves_CreateParameter", "Создать\nпараметр", location, "ColorLeaves.Revit.Commands.CreateParameterCommand");
			PushButtonData val6 = new PushButtonData("ColorLeaves_Settings", "Настройки", location, "ColorLeaves.Revit.Commands.SettingsCommand");
			val6.ToolTip = "Версия плагина и проверка обновлений";
			ApplyIcon(val2, "sheet.png");
			ApplyIcon(val5, "parameter.png");
			ApplyIcon(val6, "settings.png");
			val.AddItem((RibbonItemData)(object)val2);
			val.AddItem((RibbonItemData)(object)val5);
			val.AddItem((RibbonItemData)(object)val6);
			Log.Write("Лента создана на вкладке «Цветные листики»");
		}
		catch (Exception ex2)
		{
			Log.Write("Лента: ОШИБКА " + ex2.Message);
		}
	}

	private static void ApplyIcon(PushButtonData data, string fileName)
	{
		try
		{
			string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			string text = Path.Combine(directoryName, fileName);
			if (File.Exists(text))
			{
				BitmapImage bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				bitmapImage.UriSource = new Uri(text, UriKind.Absolute);
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.EndInit();
				bitmapImage.Freeze();
				((ButtonData)data).Image = bitmapImage;
				((ButtonData)data).LargeImage = bitmapImage;
			}
		}
		catch (Exception ex)
		{
			Log.Write("Иконка " + fileName + " не загружена: " + ex.Message);
		}
	}
}
