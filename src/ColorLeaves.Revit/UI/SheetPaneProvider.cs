using System;
using System.Windows.Forms.Integration;
using Autodesk.Revit.UI;
using ColorLeaves.Core;

using Color = System.Drawing.Color;

namespace ColorLeaves.Revit.UI;

public class SheetPaneProvider : IDockablePaneProvider
{
	private static SheetGridControl _control;

	public void SetupDockablePane(DockablePaneProviderData data)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		Log.Write("SetupDockablePane: вызван");
		try
		{
			DockablePaneState val = new DockablePaneState();
			val.DockPosition = (DockPosition)59424;
			val.TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser;
			data.InitialState = val;
			data.VisibleByDefault = true;
			if (_control == null)
			{
				_control = new SheetGridControl();
			}
			WindowsFormsHost windowsFormsHost = new WindowsFormsHost();
			windowsFormsHost.Child = _control;
			data.FrameworkElement = windowsFormsHost;
			Log.Write("SetupDockablePane: элемент панели создан");
		}
		catch (Exception ex)
		{
			Log.Write("SetupDockablePane: ИСКЛЮЧЕНИЕ", ex);
		}
	}

	public static void Refresh()
	{
		if (_control == null)
		{
			return;
		}
		try
		{
			_control.RefreshNow();
		}
		catch (Exception ex)
		{
			Log.Write("Refresh панели: " + ex.Message);
		}
	}
}
