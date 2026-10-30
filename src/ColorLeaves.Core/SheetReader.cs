using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.DB;

namespace ColorLeaves.Core;

public static class SheetReader
{
	private class NaturalComparer : IComparer<string>
	{
		public static readonly NaturalComparer Instance = new NaturalComparer();

		public int Compare(string x, string y)
		{
			x = x ?? "";
			y = y ?? "";
			int i = 0;
			int j = 0;
			while (i < x.Length && j < y.Length)
			{
				char c = x[i];
				char c2 = y[j];
				if (char.IsDigit(c) && char.IsDigit(c2))
				{
					int num = i;
					int num2 = j;
					for (; i < x.Length && char.IsDigit(x[i]); i++)
					{
					}
					for (; j < y.Length && char.IsDigit(y[j]); j++)
					{
					}
					string text = x.Substring(num, i - num).TrimStart('0');
					string text2 = y.Substring(num2, j - num2).TrimStart('0');
					if (text.Length != text2.Length)
					{
						return text.Length - text2.Length;
					}
					int num3 = string.CompareOrdinal(text, text2);
					if (num3 != 0)
					{
						return num3;
					}
				}
				else
				{
					int num4 = char.ToUpperInvariant(c).CompareTo(char.ToUpperInvariant(c2));
					if (num4 != 0)
					{
						return num4;
					}
					i++;
					j++;
				}
			}
			return x.Length - i - (y.Length - j);
		}
	}

	public const string StageParameterName = "Стадия готовности";

	public const string EditedByParameterName = "Редактирует";

	public static bool UseDefaultUnsetStage = true;

	public static List<SheetRow> Read(Document doc)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		List<SheetRow> list = new List<SheetRow>();
		if (doc == null || doc.IsFamilyDocument)
		{
			return list;
		}
		FilteredElementCollector val = new FilteredElementCollector(doc);
		val.OfClass(typeof(ViewSheet));
		foreach (Element item in val)
		{
			ViewSheet val2 = (ViewSheet)(object)((item is ViewSheet) ? item : null);
			if (val2 != null && !val2.IsPlaceholder)
			{
				SheetRow sheetRow = new SheetRow();
				Fill(sheetRow, val2);
				list.Add(sheetRow);
			}
		}
		list.Sort((SheetRow a, SheetRow b) => NaturalComparer.Instance.Compare(a.Number, b.Number));
		return list;
	}

	/// <summary>Читает данные одного листа (для точечного обновления по DocumentChanged).</summary>
	public static SheetRow ReadOne(Document doc, ElementId id)
	{
		ViewSheet viewSheet = doc.GetElement(id) as ViewSheet;
		if (viewSheet == null || viewSheet.IsPlaceholder)
		{
			return null;
		}
		SheetRow sheetRow = new SheetRow();
		Fill(sheetRow, viewSheet);
		return sheetRow;
	}

	private static void Fill(SheetRow sheetRow, ViewSheet val2)
	{
		sheetRow.Id = ((Element)val2).Id.IntegerValue;
		sheetRow.Number = Text((Element)(object)val2, (BuiltInParameter)(-1007401));
		sheetRow.Name = Text((Element)(object)val2, (BuiltInParameter)(-1007400));
		if (sheetRow.Number.Length == 0)
		{
			sheetRow.Number = val2.SheetNumber ?? "";
		}
		if (sheetRow.Name.Length == 0)
		{
			sheetRow.Name = ((Element)val2).Name ?? "";
		}
		sheetRow.CheckedBy = Text((Element)(object)val2, (BuiltInParameter)(-1007405));
		sheetRow.ApprovedBy = Text((Element)(object)val2, (BuiltInParameter)(-1007408));
		sheetRow.IssueDate = Text((Element)(object)val2, (BuiltInParameter)(-1006322));
		sheetRow.EditedBy = ReadEditedBy(val2);
		string text = ReadManualStage(val2);
		if (!string.IsNullOrEmpty(text))
		{
			sheetRow.Stage = text;
			sheetRow.StageIsManual = true;
		}
		else if (UseDefaultUnsetStage)
		{
			sheetRow.Stage = "Не задана";
			sheetRow.StageIsManual = true;
		}
		else
		{
			sheetRow.Stage = DetectStage(sheetRow);
			sheetRow.StageIsManual = false;
		}
	}

	public static bool HasStageParameter(Document doc)
	{
		return Diagnose(doc).Writable;
	}

	public static string ReadManualStage(ViewSheet sheet)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Invalid comparison between Unknown and I4
		Parameter val = ((Element)sheet).LookupParameter("Стадия готовности");
		if (val == null)
		{
			return "";
		}
		if ((int)val.StorageType != 3)
		{
			return "";
		}
		string text = val.AsString();
		if (text != null)
		{
			return text.Trim();
		}
		return "";
	}

	/// <summary>Читает текстовый параметр по имени (пустая строка, если параметра нет).</summary>
	public static string LookupText(Element element, string parameterName)
	{
		string value;
		ReadTextWithReason(element, parameterName, out value);
		return value;
	}

	/// <summary>
	/// Читает текстовый параметр и объясняет причину пустого результата:
	/// «не найден», «только чтение», «не текст». При успехе reason пустой.
	/// </summary>
	public static string ReadTextWithReason(Element element, string parameterName, out string value)
	{
		value = "";
		if (element == null)
		{
			return "элемент не найден";
		}
		Parameter val = element.LookupParameter(parameterName);
		if (val == null)
		{
			return "параметр с таким именем не найден";
		}
		if (val.IsReadOnly)
		{
			return "параметр только для чтения";
		}
		try
		{
			if ((int)val.StorageType != 3)
			{
				return "тип данных не «Текст», а " + val.StorageType;
			}
			value = val.AsString() ?? "";
			return "";
		}
		catch (Exception ex)
		{
			return "ошибка чтения: " + ex.Message;
		}
	}

	/// <summary>
	/// Кто редактирует лист. В русской версии Revit «Редактирует» — это ещё и
	/// встроенный параметр совместной работы (только чтение): Revit сам вписывает
	/// туда имя пользователя, когда тот берёт лист, и очищает при освобождении.
	/// LookupParameter по имени находит именно его, поэтому встроенный читаем
	/// напрямую по BuiltInParameter, а пользовательский текстовый параметр —
	/// как запасной вариант (в проектах, где встроенного нет).
	/// </summary>
	public static string ReadEditedBy(ViewSheet sheet)
	{
		if (sheet == null)
		{
			return "";
		}
		try
		{
			Parameter builtin = ((Element)sheet).get_Parameter(BuiltInParameter.EDITED_BY);
			if (builtin != null)
			{
				string who = builtin.AsString();
				if (!string.IsNullOrEmpty(who))
				{
					return who.Trim();
				}
			}
		}
		catch
		{
		}
		return LookupText((Element)(object)sheet, EditedByParameterName);
	}

	/// <summary>Есть ли на листе встроенный параметр «Редактирует» (в общих моделях его ведёт сам Revit).</summary>
	public static bool HasBuiltInEditedBy(ViewSheet sheet)
	{
		if (sheet == null)
		{
			return false;
		}
		try
		{
			return ((Element)sheet).get_Parameter(BuiltInParameter.EDITED_BY) != null;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Отчёт для диагностики: все параметры с именем вида «Редактир…» на листе и его типе.
	/// Показывает точное имя и коды символов (вскрывает латинские буквы-двойники и
	/// невидимые пробелы), read-only, тип данных, значение и GUID общего параметра.
	/// </summary>
	public static string DescribeEditedByParameters(ViewSheet sheet)
	{
		StringBuilder report = new StringBuilder();
		AppendEditedByParamReport(report, "ЭКЗЕМПЛЯР листа", (Element)sheet);
		Element typeElement = null;
		try
		{
			typeElement = ((Element)sheet).Document.GetElement(((Element)sheet).GetTypeId());
		}
		catch
		{
		}
		if (typeElement != null)
		{
			AppendEditedByParamReport(report, "ТИП листа", typeElement);
		}
		if (report.Length == 0)
		{
			report.AppendLine("  параметр с именем вида «Редактир…» не найден ни на листе, ни на типе.");
		}
		return report.ToString();
	}

	private static void AppendEditedByParamReport(StringBuilder report, string where, Element element)
	{
		bool any = false;
		IList<Parameter> parameters = element.GetOrderedParameters();
		foreach (Parameter parameter in parameters)
		{
			string name = "";
			try
			{
				name = parameter.Definition.Name ?? "";
			}
			catch
			{
			}
			if (name.IndexOf("редактир", StringComparison.OrdinalIgnoreCase) < 0)
			{
				continue;
			}
			if (!any)
			{
				report.AppendLine(where + ":");
				any = true;
			}
			string shared = parameter.IsShared ? ("общий, GUID=" + parameter.GUID) : "не общий (встроенный либо проектный)";
			string state;
			if (parameter.IsReadOnly)
			{
				state = "ТОЛЬКО ЧТЕНИЕ";
			}
			else if ((int)parameter.StorageType != 3)
			{
				state = "тип данных не «Текст», а " + parameter.StorageType;
			}
			else
			{
				string value = "";
				try
				{
					value = parameter.AsString() ?? "";
				}
				catch
				{
				}
				state = string.IsNullOrEmpty(value) ? "пусто" : ("значение «" + value + "»");
			}
			report.AppendLine("  имя: «" + name + "»  (" + shared + ", " + state + ")");
			report.AppendLine("  коды имени: " + CharCodes(name));
		}
	}

	/// <summary>Коды символов строки в hex — видно любое отличие от канонического имени.</summary>
	private static string CharCodes(string text)
	{
		StringBuilder sb = new StringBuilder();
		foreach (char c in text)
		{
			if (sb.Length > 0)
			{
				sb.Append(' ');
			}
			sb.Append(((int)c).ToString("X4"));
		}
		return sb.ToString();
	}

	public static Parameter LookupStageParameter(Document doc)
	{
		return Diagnose(doc).Parameter;
	}

	public static StageParamDiag Diagnose(Document doc)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Expected O, but got Unknown
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Invalid comparison between Unknown and I4
		StageParamDiag stageParamDiag = new StageParamDiag();
		if (doc == null || doc.IsFamilyDocument)
		{
			return stageParamDiag;
		}
		FilteredElementCollector val = new FilteredElementCollector(doc);
		val.OfClass(typeof(ViewSheet));
		List<ViewSheet> list = new List<ViewSheet>();
		foreach (Element item in val)
		{
			ViewSheet val2 = (ViewSheet)(object)((item is ViewSheet) ? item : null);
			if (val2 != null && !val2.IsPlaceholder)
			{
				list.Add(val2);
			}
		}
		stageParamDiag.SheetsTotal = list.Count;
		foreach (ViewSheet item2 in list)
		{
			foreach (Parameter parameter in ((Element)item2).Parameters)
			{
				Parameter val3 = parameter;
				string text = ((val3 == null) ? "" : val3.Definition.Name);
				if (text.Length != 0)
				{
					stageParamDiag.AllNames.Add(text);
					if (text.IndexOf("стадия", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						stageParamDiag.Similar.Add("«" + text + "» — " + Describe(val3));
					}
				}
			}
			Parameter val4 = ((Element)item2).LookupParameter("Стадия готовности");
			if (val4 == null)
			{
				continue;
			}
			if ((int)val4.StorageType != 3)
			{
				stageParamDiag.Found = true;
				stageParamDiag.Parameter = val4;
				stageParamDiag.Writable = false;
				stageParamDiag.Detail = "Параметр найден, но он не текстовый (" + Describe(val4) + "). Тип данных должен быть «Текст».";
				continue;
			}
			if (((APIObject)val4).IsReadOnly)
			{
				stageParamDiag.Found = true;
				stageParamDiag.Parameter = val4;
				stageParamDiag.Writable = false;
				stageParamDiag.SheetNumber = item2.SheetNumber;
				stageParamDiag.Detail = "Параметр найден, но доступен только для чтения (" + Describe(val4) + "). Переведите его в «Экземпляр» на вкладке «Параметры проекта → Общие → Листы».";
				continue;
			}
			stageParamDiag.Found = true;
			stageParamDiag.Writable = true;
			stageParamDiag.Parameter = val4;
			stageParamDiag.SheetNumber = item2.SheetNumber;
			stageParamDiag.Detail = "Параметр найден, стадию можно менять.";
			return stageParamDiag;
		}
		if (!stageParamDiag.Found)
		{
			stageParamDiag.Detail = "Параметр «Стадия готовности» у листов не найден.";
		}
		return stageParamDiag;
	}

	private static string Describe(Parameter p)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Invalid comparison between Unknown and I4
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Invalid comparison between Unknown and I4
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Invalid comparison between Unknown and I4
		if (p == null)
		{
			return "нет";
		}
		string text = (((int)p.StorageType == 3) ? "текст" : (((int)p.StorageType == 1) ? "целое" : (((int)p.StorageType == 2) ? "вещественное" : (((int)p.StorageType == 4) ? "ссылка на элемент" : "иной тип"))));
		string text2 = ((p.Element == null) ? "" : ((p.Element is ElementType) ? "привязан к ТИПУ листа" : "привязан к экземпляру"));
		string text3 = (((APIObject)p).IsReadOnly ? ", только чтение" : "");
		return text + ", " + text2 + text3;
	}

	private static Element FirstSheet(Document doc)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		FilteredElementCollector val = new FilteredElementCollector(doc);
		val.OfClass(typeof(ViewSheet));
		foreach (Element item in val)
		{
			ViewSheet val2 = (ViewSheet)(object)((item is ViewSheet) ? item : null);
			if (val2 != null && !val2.IsPlaceholder)
			{
				return (Element)(object)val2;
			}
		}
		return null;
	}

	public static string DetectStage(SheetRow row)
	{
		if (!string.IsNullOrEmpty(row.ApprovedBy))
		{
			return "Утверждён";
		}
		if (!string.IsNullOrEmpty(row.CheckedBy))
		{
			return "Проверен";
		}
		if (!string.IsNullOrEmpty(row.IssueDate))
		{
			return "Выпущен";
		}
		return "Черновик";
	}

	public static string Text(Element el, BuiltInParameter bip)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		if (el == null)
		{
			return "";
		}
		Parameter val = el.get_Parameter(bip);
		if (val == null || ((APIObject)val).IsReadOnly)
		{
			return "";
		}
		try
		{
			string text = val.AsValueString();
			return (text == null) ? "" : text.Trim();
		}
		catch
		{
			return "";
		}
	}
}
