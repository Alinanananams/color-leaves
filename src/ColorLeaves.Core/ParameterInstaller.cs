using System;
using System.IO;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;

namespace ColorLeaves.Core;

public static class ParameterInstaller
{
	public const string GroupName = "Цветные листики";

	public static bool EnsureStageParameter(Document doc, out string info)
	{
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Expected O, but got Unknown
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Expected O, but got Unknown
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		info = "";
		if (doc == null || doc.IsFamilyDocument)
		{
			info = "Нет открытого проекта.";
			return false;
		}
		Parameter val = SheetReader.LookupStageParameter(doc);
		if (val != null)
		{
			info = "Параметр «Стадия готовности» уже есть в проекте.";
			return true;
		}
		Log.Write("Создание параметра: начало");
		Application application = doc.Application;
		string text = "";
		try
		{
			text = application.SharedParametersFilename;
		}
		catch
		{
		}
		string text2 = Path.Combine(Path.GetTempPath(), "ColorSheets_" + Guid.NewGuid().ToString("N") + ".txt");
		DefinitionFile val2 = null;
		try
		{
			File.WriteAllText(text2, "*SECTION1" + Environment.NewLine);
			application.SharedParametersFilename = text2;
			val2 = application.OpenSharedParameterFile();
			if (val2 == null)
			{
				info = "Не удалось открыть временный файл общих параметров.";
				return false;
			}
			DefinitionGroup val3 = val2.Groups.Create("Цветные листики");
			ExternalDefinitionCreationOptions val4 = new ExternalDefinitionCreationOptions("Стадия готовности", (ParameterType)1);
			Definition obj2 = val3.Definitions.Create(val4);
			ExternalDefinition val5 = (ExternalDefinition)(object)((obj2 is ExternalDefinition) ? obj2 : null);
			if (val5 == null)
			{
				info = "Revit не создал определение параметра.";
				return false;
			}
			Category category = Category.GetCategory(doc, (BuiltInCategory)(-2003100));
			if (category == null)
			{
				info = "В проекте нет категории «Листы».";
				return false;
			}
			CategorySet val6 = application.Create.NewCategorySet();
			val6.Insert(category);
			InstanceBinding val7 = application.Create.NewInstanceBinding(val6);
			Transaction val8 = new Transaction(doc, "Параметр «Стадия готовности»");
			try
			{
				val8.Start();
				doc.ParameterBindings.Insert((Definition)(object)val5, (Binding)(object)val7, (BuiltInParameterGroup)(-5000123));
				val8.Commit();
			}
			finally
			{
				((IDisposable)val8)?.Dispose();
			}
			Log.Write("Создание параметра: успешно, GUID = " + val5.GUID);
			info = "Параметр «Стадия готовности» создан и привязан к листам.";
			return true;
		}
		catch (Exception ex)
		{
			Log.Write("Создание параметра: ИСКЛЮЧЕНИЕ", ex);
			info = "Не удалось создать параметр: " + ex.Message;
			return false;
		}
		finally
		{
			try
			{
				if (val2 != null)
				{
					((APIObject)val2).Dispose();
				}
			}
			catch
			{
			}
			try
			{
				if (!string.IsNullOrEmpty(text))
				{
					application.SharedParametersFilename = text;
				}
			}
			catch (Exception ex2)
			{
				Log.Write("Создание параметра: не удалось вернуть файл параметров: " + ex2.Message);
			}
			try
			{
				if (File.Exists(text2))
				{
					File.Delete(text2);
				}
			}
			catch
			{
			}
		}
	}
}
