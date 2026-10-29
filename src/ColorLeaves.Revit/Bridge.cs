using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ColorLeaves.Core;
using ColorLeaves.Revit.UI;

namespace ColorLeaves.Revit;

public static class Bridge
{
	private static readonly TimeSpan MinScanInterval = new TimeSpan(0, 0, 1, 0);

	private static readonly TimeSpan CentralCheckInterval = new TimeSpan(0, 0, 0, 30);

	private static Document _document;

	private static UIApplication _uiApp;

	private static List<SheetRow> _rows = new List<SheetRow>();

	private static List<PendingStage> _pending = new List<PendingStage>();

	private static DateTime _lastScan = DateTime.MinValue;

	private static bool _forceScan = true;

	private static bool _autoFillRequested;

	private static bool _fillUnsetRequested;

	private static bool _paramCreateRequested;

	private static bool _paramCreateTried;

	private static string _message = "";

	private static bool _hasStageParameter;

	private static string _lastIdleError = "";

	private static string _lastStamp = "";

	private static bool _verifiedDocument;

	private static bool _unsetFilled;

	private static bool _isWorkshared;

	private static bool _hasChangesFromCentral;

	private static DateTime _lastSyncTime = DateTime.MinValue;

	private static DateTime _lastCentralCheck = DateTime.MinValue;

	private static string _lastDiagState = "";

	private static StageParamDiag _diag = new StageParamDiag();

	private static Dictionary<int, string> _lockedBy = new Dictionary<int, string>();

	public static List<SheetRow> Rows => _rows;

	public static string Message => _message;

	public static StageParamDiag Diag => _diag;

	public static bool CanEditStage => _hasStageParameter;

	public static bool HasDocument => _document != null;

	public static bool IsWorkshared => _isWorkshared;

	public static bool HasChangesFromCentral => _hasChangesFromCentral;

	public static DateTime LastSyncTime => _lastSyncTime;

	public static bool HasPending => _pending.Count > 0;

	public static int PendingCount => _pending.Count;

	public static event Action DataChanged;

	public static void SetUiApp(UIApplication app)
	{
		_uiApp = app;
		if (app != null && app.ActiveUIDocument != null)
		{
			SetDocument(app.ActiveUIDocument.Document);
		}
		RequestRefresh();
	}

	public static void SetDocument(Document doc)
	{
		bool flag = !object.ReferenceEquals(_document, doc);
		_document = doc;
		if (flag)
		{
			_forceScan = true;
			_verifiedDocument = false;
			_unsetFilled = false;
			_paramCreateTried = false;
		}
	}

	/// <summary>
	/// Точечное обновление по событию DocumentChanged: перечитывает только изменённые листы
	/// (например, когда коллега заполнил параметр «Редактирует» и изменения пришли с синхронизацией).
	/// Полный скан модели не выполняется — таблица обновляется без лагов.
	/// </summary>
	public static void OnDocumentChanged(Document doc, ICollection<ElementId> modifiedIds)
	{
		try
		{
			if (doc == null || modifiedIds == null || modifiedIds.Count == 0)
			{
				return;
			}
			bool hasSheets = false;
			foreach (ElementId id in modifiedIds)
			{
				if (doc.GetElement(id) is ViewSheet)
				{
					hasSheets = true;
					break;
				}
			}
			if (!hasSheets)
			{
				return;
			}
			if (!object.ReferenceEquals(_document, doc))
			{
				SetDocument(doc);
			}
			bool anyChanged = false;
			bool needFullScan = false;
			foreach (ElementId id2 in modifiedIds)
			{
				SheetRow sheetRow = SheetReader.ReadOne(doc, id2);
				if (sheetRow == null)
				{
					continue;
				}
				SheetRow sheetRow2 = _rows.Find((SheetRow r) => r.Id == sheetRow.Id);
				if (sheetRow2 == null)
				{
					needFullScan = true;
					continue;
				}
				if (!string.Equals(sheetRow2.Number, sheetRow.Number, StringComparison.Ordinal) ||
					!string.Equals(sheetRow2.Name, sheetRow.Name, StringComparison.Ordinal) ||
					!string.Equals(sheetRow2.Stage, sheetRow.Stage, StringComparison.Ordinal) ||
					!string.Equals(sheetRow2.EditedBy, sheetRow.EditedBy, StringComparison.Ordinal) ||
					!string.Equals(sheetRow2.CheckedBy, sheetRow.CheckedBy, StringComparison.Ordinal) ||
					!string.Equals(sheetRow2.ApprovedBy, sheetRow.ApprovedBy, StringComparison.Ordinal) ||
					!string.Equals(sheetRow2.IssueDate, sheetRow.IssueDate, StringComparison.Ordinal) ||
					sheetRow2.StageIsManual != sheetRow.StageIsManual)
				{
					sheetRow2.Number = sheetRow.Number;
					sheetRow2.Name = sheetRow.Name;
					sheetRow2.Stage = sheetRow.Stage;
					sheetRow2.StageIsManual = sheetRow.StageIsManual;
					sheetRow2.CheckedBy = sheetRow.CheckedBy;
					sheetRow2.ApprovedBy = sheetRow.ApprovedBy;
					sheetRow2.IssueDate = sheetRow.IssueDate;
					sheetRow2.EditedBy = sheetRow.EditedBy;
					anyChanged = true;
					Log.Write("DocumentChanged: лист " + sheetRow2.Number + " обновлён по событию");
				}
			}
			if (needFullScan)
			{
				_forceScan = true;
			}
			if (anyChanged)
			{
				Raise();
			}
		}
		catch (Exception ex)
		{
			Log.Write("DocumentChanged: ошибка " + ex.Message);
		}
	}

	public static void RequestRefresh()
	{
		_forceScan = true;
	}

	public static void Tick()
	{
		if (_document == null || (!_forceScan && DateTime.UtcNow - _lastScan < MinScanInterval))
		{
			return;
		}
		_lastScan = DateTime.UtcNow;
		_forceScan = false;
		try
		{
			if (_uiApp != null)
			{
				try
				{
					if (_uiApp.ActiveUIDocument != null)
					{
						SetDocument(_uiApp.ActiveUIDocument.Document);
					}
				}
				catch (Exception ex)
				{
					if (ex.Message != _lastIdleError)
					{
						Log.Write("Bridge.Tick: нет активного документа", ex);
						_lastIdleError = ex.Message;
					}
					return;
				}
			}
			if (_document.IsFamilyDocument)
			{
				SetMessage("Открыт файл семейства — листов нет.");
				return;
			}
			if (_autoFillRequested)
			{
				_autoFillRequested = false;
				ApplyAutoFill();
			}
			if (_fillUnsetRequested)
			{
				_fillUnsetRequested = false;
				FillUnsetStagesOnce();
			}
			ApplyPending();
			VerifyWriteOnce();
			CheckCentralStatus();
			_diag = SheetReader.Diagnose(_document);
			_hasStageParameter = _diag.Writable;
			if (!_hasStageParameter)
			{
				bool flag = _paramCreateRequested || (!_isWorkshared && !_paramCreateTried);
				_paramCreateRequested = false;
				if (flag)
				{
					_paramCreateTried = true;
					string info;
					bool flag2 = ParameterInstaller.EnsureStageParameter(_document, out info);
					Log.Write("Создание параметра " + (_paramCreateRequested ? "(по кнопке)" : "(авто)") + ": " + (flag2 ? "ок" : "не удалось") + " — " + info);
					_diag = SheetReader.Diagnose(_document);
					_hasStageParameter = _diag.Writable;
				}
				else if (_isWorkshared)
				{
					Log.Write("Параметр не найден в общей модели — автосоздание пропущено");
				}
			}
			_rows = SheetReader.Read(_document);
			LogDiagnosisIfChanged();
			if (_diag.Writable)
			{
				SetMessage("");
			}
			else if (_diag.Found)
			{
				SetMessage(_diag.Detail);
				Log.Write("Параметр стадии найден, но запись недоступна: " + _diag.Detail);
			}
			else
			{
				if (_isWorkshared)
				{
					SetMessage("В общей модели параметр «Стадия готовности» не создан автоматически: у каждого участника он получил бы свой GUID и конфликтовал при синхронизации. Пусть его создаст один участник и синхронизирует модель — остальным параметр придёт сам. Нажмите «Создать параметр», если уверены, что модель ваша.");
				}
				else
				{
					SetMessage("Параметр «Стадия готовности» не найден. Нажмите «Создать параметр» — плагин добавит его к листам сам.");
				}
				Log.Write("Параметр стадии не найден. Похожие параметры: " + _diag.Similar.Count);
				foreach (string item in _diag.Similar)
				{
					Log.Write("   " + item);
				}
			}
		}
		catch (Exception ex2)
		{
			if (ex2.Message != _lastIdleError)
			{
				Log.Write("Bridge.Tick", ex2);
				_lastIdleError = ex2.Message;
			}
			SetMessage("Ошибка чтения модели: " + ex2.Message);
			_document = null;
			_rows = new List<SheetRow>();
		}
		_lastIdleError = "";
		string text = ComputeStamp();
		if (text != _lastStamp)
		{
			_lastStamp = text;
			Raise();
		}
	}

	private static string ComputeStamp()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(_rows.Count).Append('|').Append(_message)
			.Append('|')
			.Append(_hasStageParameter)
			.Append('|');
		foreach (SheetRow row in _rows)
		{
			stringBuilder.Append(row.Id).Append('=').Append(row.Stage)
				.Append(';');
		}
		return stringBuilder.ToString();
	}

	private static void VerifyWriteOnce()
	{
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Expected O, but got Unknown
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Invalid comparison between Unknown and I4
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Expected O, but got Unknown
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Invalid comparison between Unknown and I4
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Expected O, but got Unknown
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		if (_verifiedDocument)
		{
			return;
		}
		_verifiedDocument = true;
		try
		{
			Log.Write("Состояние документа: IsReadOnly=" + _document.IsReadOnly + ", IsModifiable=" + _document.IsModifiable + ", IsReadOnlyFile=" + _document.IsReadOnlyFile + ", IsFamilyDocument=" + _document.IsFamilyDocument + ", Path=" + _document.PathName);
			if (_document.IsReadOnly)
			{
				Log.Write("Проверка записи: документ IsReadOnly");
				return;
			}
			if (!_document.IsModifiable)
			{
				Log.Write("Проверка записи: IsModifiable=false, пробую запись всё равно");
			}
			Parameter val = SheetReader.LookupStageParameter(_document);
			if (val == null)
			{
				Log.Write("Проверка записи: параметр не найден");
				return;
			}
			if (((APIObject)val).IsReadOnly)
			{
				Log.Write("Проверка записи: параметр IsReadOnly");
				return;
			}
			if ((int)val.StorageType != 3)
			{
				Log.Write("Проверка записи: параметр не текстовый");
				return;
			}
			FilteredElementCollector val2 = new FilteredElementCollector(_document);
			val2.OfClass(typeof(ViewSheet));
			foreach (Element item in val2)
			{
				ViewSheet val3 = (ViewSheet)(object)((item is ViewSheet) ? item : null);
				if (val3 == null)
				{
					continue;
				}
				Parameter val4 = ((Element)val3).LookupParameter("Стадия готовности");
				if (val4 == null || ((APIObject)val4).IsReadOnly || (int)val4.StorageType != 3)
				{
					continue;
				}
				string text = val4.AsString() ?? "";
				string text2;
				try
				{
					Transaction val5 = new Transaction(_document, "Проверка записи стадии");
					try
					{
						val5.Start();
						val4.Set("__проверка__");
						val5.Commit();
					}
					finally
					{
						((IDisposable)val5)?.Dispose();
					}
					text2 = SheetReader.ReadManualStage(val3);
				}
				finally
				{
					try
					{
						Transaction val6 = new Transaction(_document, "Проверка записи стадии");
						try
						{
							val6.Start();
							val4.Set(text);
							val6.Commit();
						}
						finally
						{
							((IDisposable)val6)?.Dispose();
						}
					}
					catch (Exception ex)
					{
						Log.Write("Проверка записи: НЕ УДАЛОСЬ вернуть значение: " + ex.Message);
					}
				}
				string text3 = val4.AsString() ?? "";
				Log.Write("Проверка записи: лист " + val3.SheetNumber + " | записали '__проверка__', прочитали '" + text2 + "' -> " + ((text2 == "__проверка__") ? "ЧТЕНИЕ РАБОТАЕТ" : "ЧТЕНИЕ НЕ РАБОТАЕТ") + " | вернули '" + text + "', в параметре '" + text3 + "' -> " + ((text3 == text) ? "ВОССТАНОВЛЕНО" : "НЕ ВОССТАНОВЛЕНО"));
				return;
			}
			Log.Write("Проверка записи: не нашлось листа с параметром");
		}
		catch (Exception ex2)
		{
			Log.Write("Проверка записи: исключение", ex2);
		}
	}

	private static void CheckCentralStatus()
	{
		try
		{
			_isWorkshared = _document.IsWorkshared;
		}
		catch
		{
			_isWorkshared = false;
		}
		if (!_isWorkshared)
		{
			_hasChangesFromCentral = false;
			_lastCentralCheck = DateTime.MinValue;
			RefreshLockedOwners();
		}
		else
		{
			if (DateTime.UtcNow - _lastCentralCheck < CentralCheckInterval)
			{
				return;
			}
			_lastCentralCheck = DateTime.UtcNow;
			bool hasChangesFromCentral = _hasChangesFromCentral;
			try
			{
				_hasChangesFromCentral = _document.HasAllChangesFromCentral();
			}
			catch (Exception ex)
			{
				if (ex.Message != _lastIdleError)
				{
					Log.Write("Проверка центра: " + ex.Message);
					_lastIdleError = ex.Message;
				}
				return;
			}
			if (_hasChangesFromCentral != hasChangesFromCentral)
			{
				Log.Write("Центральная модель: " + (_hasChangesFromCentral ? "есть изменения, которых нет у нас" : "расхождений нет"));
			}
			if (_hasChangesFromCentral)
			{
				MarkCentralUpdates();
			}
			RefreshLockedOwners();
		}
	}

	private static void MarkCentralUpdates()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Invalid comparison between Unknown and I4
		int num = 0;
		foreach (SheetRow row in _rows)
		{
			ModelUpdatesStatus modelUpdatesStatus = GetModelUpdatesStatus(row.Id);
			if (row.UpdatedInCentral = (int)modelUpdatesStatus == 3)
			{
				num++;
			}
		}
		Log.Write("Листов с обновлениями из центра: " + num);
	}

	public static bool SynchronizeWithCentral(out string info)
	{
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Expected O, but got Unknown
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Expected O, but got Unknown
		info = "";
		try
		{
			if (_document == null)
			{
				info = "Нет открытого документа.";
				return false;
			}
			if (!_document.IsWorkshared)
			{
				info = "Модель не в общем доступе — синхронизация не нужна.";
				Log.Write("Синхронизация пропущена: модель не workshared.");
				return true;
			}
			Dictionary<int, string> dictionary = new Dictionary<int, string>();
			foreach (SheetRow row in _rows)
			{
				dictionary[row.Id] = row.Stage;
			}
			Log.Write("Синхронизация с центральной моделью: начало");
			TransactWithCentralOptions val = new TransactWithCentralOptions();
			SynchronizeWithCentralOptions val2 = new SynchronizeWithCentralOptions();
			val2.SaveLocalAfter = true;
			val2.Compact = false;
			val2.Comment = "Синхронизация из «Цветных листиков»";
			_document.SynchronizeWithCentral(val, val2);
			_lastSyncTime = DateTime.Now;
			_lastCentralCheck = DateTime.MinValue;
			_forceScan = true;
			RequestRefresh();
			List<SheetRow> list = SheetReader.Read(_document);
			int num = 0;
			int num2 = 0;
			foreach (SheetRow item in list)
			{
				if (!dictionary.TryGetValue(item.Id, out var value))
				{
					num2++;
				}
				else if (!string.Equals(value, item.Stage, StringComparison.Ordinal))
				{
					num++;
				}
			}
			_rows = list;
			info = "Синхронизация выполнена в " + _lastSyncTime.ToString("HH:mm:ss") + ". Обновилось стадий: " + num + ((num2 > 0) ? (", новых листов: " + num2) : "") + ".";
			Log.Write("Синхронизация: завершена успешно. " + info);
			return true;
		}
		catch (Exception ex)
		{
			info = "Синхронизация не выполнена: " + ex.Message;
			Log.Write("Синхронизация: исключение", ex);
			return false;
		}
	}

	public static CheckoutStatus GetCheckoutStatus(int sheetId)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		if (_document == null || !_isWorkshared)
		{
			return (CheckoutStatus)2;
		}
		try
		{
			return WorksharingUtils.GetCheckoutStatus(_document, new ElementId(sheetId));
		}
		catch
		{
			return (CheckoutStatus)2;
		}
	}

	public static string GetCheckoutOwner(int sheetId)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Invalid comparison between Unknown and I4
		if (_document == null || !_isWorkshared)
		{
			return "";
		}
		try
		{
			string text = null;
			CheckoutStatus checkoutStatus = WorksharingUtils.GetCheckoutStatus(_document, new ElementId(sheetId), out text);
			if ((int)checkoutStatus != 1)
			{
				return "";
			}
			return text ?? "";
		}
		catch
		{
			return "";
		}
	}

	public static string GetKnownOwner(int sheetId)
	{
		if (!_lockedBy.TryGetValue(sheetId, out var value))
		{
			return "";
		}
		return value;
	}

	private static void RefreshLockedOwners()
	{
		_lockedBy.Clear();
		if (!_isWorkshared)
		{
			return;
		}
		foreach (SheetRow row in _rows)
		{
			string value = (row.LockedBy = GetCheckoutOwner(row.Id));
			if (!string.IsNullOrEmpty(value))
			{
				_lockedBy[row.Id] = value;
			}
		}
	}

	public static ModelUpdatesStatus GetModelUpdatesStatus(int sheetId)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		if (_document == null || !_isWorkshared)
		{
			return (ModelUpdatesStatus)0;
		}
		try
		{
			return WorksharingUtils.GetModelUpdatesStatus(_document, new ElementId(sheetId));
		}
		catch
		{
			return (ModelUpdatesStatus)0;
		}
	}

	public static void RequestCreateParameter()
	{
		_paramCreateRequested = true;
		_paramCreateTried = false;
		_forceScan = true;
	}

	public static void RequestFillUnset()
	{
		_fillUnsetRequested = true;
		_unsetFilled = false;
		_forceScan = true;
	}

	private static void FillUnsetStagesOnce()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Invalid comparison between Unknown and I4
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected O, but got Unknown
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Expected O, but got Unknown
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Invalid comparison between Unknown and I4
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Invalid comparison between Unknown and I4
		if (_unsetFilled)
		{
			return;
		}
		try
		{
			if (_document.IsReadOnly)
			{
				return;
			}
			Parameter val = SheetReader.LookupStageParameter(_document);
			if (val == null || ((APIObject)val).IsReadOnly || (int)val.StorageType != 3)
			{
				return;
			}
			FilteredElementCollector val2 = new FilteredElementCollector(_document);
			val2.OfClass(typeof(ViewSheet));
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			bool flag = false;
			Transaction val3 = new Transaction(_document, "Стадия готовности: «Не задана»");
			try
			{
				val3.Start();
				foreach (Element item in val2)
				{
					ViewSheet val4 = (ViewSheet)(object)((item is ViewSheet) ? item : null);
					if (val4 == null || val4.IsPlaceholder)
					{
						continue;
					}
					Parameter val5 = ((Element)val4).LookupParameter("Стадия готовности");
					if (val5 == null || ((APIObject)val5).IsReadOnly || (int)val5.StorageType != 3)
					{
						num2++;
						continue;
					}
					string text = val5.AsString() ?? "";
					if (text.Length <= 0)
					{
						if (_isWorkshared && (int)WorksharingUtils.GetCheckoutStatus(_document, ((Element)val4).Id) == 1)
						{
							num3++;
							continue;
						}
						val5.Set("Не задана");
						num++;
						flag = true;
					}
				}
				val3.Commit();
			}
			finally
			{
				((IDisposable)val3)?.Dispose();
			}
			_unsetFilled = true;
			if (flag)
			{
				_forceScan = true;
				Log.Write("Проставлено «Не задана» листам: " + num + " (пропущено без параметра: " + num2 + ", занято другими: " + num3 + ")");
			}
			else
			{
				Log.Write("Все листы уже имеют стадию, дописывать нечего (занято другими: " + num3 + ")");
			}
		}
		catch (Exception ex)
		{
			_unsetFilled = true;
			Log.Write("Простановка «Не задана»: исключение", ex);
		}
	}

	private static void LogDiagnosisIfChanged()
	{
		string text = (_diag.Found ? "найден" : "не найден") + " / запись " + (_diag.Writable ? "разрешена" : "запрещена") + " / листов " + _diag.SheetsTotal + " / общая модель " + _isWorkshared + " / расхождения с центром " + _hasChangesFromCentral + " / " + _diag.Detail + " / похожих параметров " + _diag.Similar.Count;
		if (text == _lastDiagState)
		{
			return;
		}
		_lastDiagState = text;
		Log.Write("ДИАГНОСТИКА: " + text);
		if (!string.IsNullOrEmpty(_diag.SheetNumber))
		{
			Log.Write("ДИАГНОСТИКА: найден на листе " + _diag.SheetNumber);
		}
		HashSet<string> hashSet = new HashSet<string>(_diag.Similar);
		foreach (string item in hashSet)
		{
			Log.Write("ДИАГНОСТИКА:   параметр " + item);
		}
		Log.Write("ДИАГНОСТИКА: строк в таблице " + _rows.Count);
		List<string> list = new List<string>(_diag.AllNames);
		list.Sort(StringComparer.OrdinalIgnoreCase);
		Log.Write("ДИАГНОСТИКА: всего уникальных параметров у листов " + list.Count);
		foreach (string item2 in list)
		{
			Log.Write("ПАРАМЕТР ЛИСТА: " + item2);
		}
		if (_rows.Count <= 0)
		{
			return;
		}
		int num = 0;
		foreach (SheetRow row in _rows)
		{
			if (row.StageIsManual)
			{
				num++;
			}
		}
		Log.Write("ДИАГНОСТИКА: листов с ручной стадией " + num);
		Log.Write("ДИАГНОСТИКА: пример " + _rows[0].Number + " = " + _rows[0].Stage);
	}

	private static void ApplyPending()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Invalid comparison between Unknown and I4
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Expected O, but got Unknown
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Expected O, but got Unknown
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Invalid comparison between Unknown and I4
		if (_pending.Count == 0)
		{
			return;
		}
		Parameter val = SheetReader.LookupStageParameter(_document);
		if (val == null || ((APIObject)val).IsReadOnly || (int)val.StorageType != 3)
		{
			SetMessage("Нет доступного для записи параметра листа «Стадия готовности» — стадию вручную задать нельзя.");
			Log.Write("ApplyPending: нет параметра для записи, в очереди " + _pending.Count);
			return;
		}
		List<PendingStage> list = new List<PendingStage>(_pending);
		_pending.Clear();
		Log.Write("ApplyPending: записываю " + list.Count);
		try
		{
			int num = 0;
			int num2 = 0;
			List<string> list2 = new List<string>();
			Transaction val2 = new Transaction(_document, "Стадия готовности листа");
			try
			{
				val2.Start();
				foreach (PendingStage item in list)
				{
					Element element = _document.GetElement(new ElementId(item.SheetId));
					ViewSheet val3 = (ViewSheet)(object)((element is ViewSheet) ? element : null);
					if (val3 == null)
					{
						num2++;
						continue;
					}
					string checkoutOwner = GetCheckoutOwner(item.SheetId);
					if (!string.IsNullOrEmpty(checkoutOwner))
					{
						list2.Add(val3.SheetNumber + " — " + checkoutOwner);
						num2++;
						continue;
					}
					Parameter val4 = ((Element)val3).LookupParameter("Стадия готовности");
					if (val4 == null || ((APIObject)val4).IsReadOnly || (int)val4.StorageType != 3)
					{
						num2++;
						continue;
					}
					val4.Set(item.Stage);
					num++;
				}
				val2.Commit();
			}
			finally
			{
				((IDisposable)val2)?.Dispose();
			}
			_forceScan = true;
			Log.Write("ApplyPending: записано " + num + ", пропущено " + num2);
			if (list2.Count > 0)
			{
				Log.Write("ApplyPending: заняты другими: " + string.Join("; ", list2.ToArray()));
				ReportLockedSummary("Записано: " + num + ".", list2);
			}
			else if (num2 > 0)
			{
				SetMessage("Записано: " + num + ". Пропущено листов без параметра: " + num2 + ".");
			}
		}
		catch (Exception ex)
		{
			Log.Write("Bridge.ApplyPending", ex);
			SetMessage("Не удалось применить стадию: " + ex.Message);
			Warn("Не удалось записать стадию." + Environment.NewLine + Environment.NewLine + ex.Message);
		}
	}

	private static void ReportLockedSummary(string prefix, List<string> blocked)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(prefix + " Пропущено листов, открытых другими: " + blocked.Count + ".");
		stringBuilder.AppendLine();
		int num = 0;
		foreach (string item in blocked)
		{
			if (num++ >= 15)
			{
				stringBuilder.AppendLine("...и ещё " + (blocked.Count - 15));
				break;
			}
			stringBuilder.AppendLine("  " + item);
		}
		Warn(stringBuilder.ToString());
	}

	public static void QueueStage(int sheetId, string stage)
	{
		if (string.IsNullOrEmpty(stage))
		{
			return;
		}
		for (int num = _pending.Count - 1; num >= 0; num--)
		{
			if (_pending[num].SheetId == sheetId)
			{
				_pending.RemoveAt(num);
			}
		}
		PendingStage pendingStage = new PendingStage();
		pendingStage.SheetId = sheetId;
		pendingStage.Stage = stage;
		_pending.Add(pendingStage);
		_forceScan = true;
		Log.Write("В очередь добавлен лист " + sheetId + " = " + stage);
	}

	public static void RequestAutoFill()
	{
		_autoFillRequested = true;
		_forceScan = true;
	}

	private static void ApplyAutoFill()
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Expected O, but got Unknown
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Invalid comparison between Unknown and I4
		Parameter val = SheetReader.LookupStageParameter(_document);
		if (val == null || ((APIObject)val).IsReadOnly)
		{
			SetMessage("Нет параметра листа «Стадия готовности» — проставить стадии некуда.");
			return;
		}
		if (!_document.IsModifiable)
		{
			SetMessage("Документ доступен только для чтения.");
			return;
		}
		try
		{
			int num = 0;
			List<string> list = new List<string>();
			Transaction val2 = new Transaction(_document, "Стадии готовности листов");
			try
			{
				val2.Start();
				FilteredElementCollector val3 = new FilteredElementCollector(_document);
				val3.OfClass(typeof(ViewSheet));
				foreach (Element item in val3)
				{
					ViewSheet val4 = (ViewSheet)(object)((item is ViewSheet) ? item : null);
					if (val4 == null || val4.IsPlaceholder)
					{
						continue;
					}
					string checkoutOwner = GetCheckoutOwner(((Element)val4).Id.IntegerValue);
					if (!string.IsNullOrEmpty(checkoutOwner))
					{
						list.Add(val4.SheetNumber + " — " + checkoutOwner);
						continue;
					}
					Parameter val5 = ((Element)val4).LookupParameter("Стадия готовности");
					if (val5 != null && !((APIObject)val5).IsReadOnly && (int)val5.StorageType == 3)
					{
						SheetRow sheetRow = new SheetRow();
						sheetRow.Id = ((Element)val4).Id.IntegerValue;
						sheetRow.CheckedBy = SheetReader.Text((Element)(object)val4, (BuiltInParameter)(-1007405));
						sheetRow.ApprovedBy = SheetReader.Text((Element)(object)val4, (BuiltInParameter)(-1007408));
						sheetRow.IssueDate = SheetReader.Text((Element)(object)val4, (BuiltInParameter)(-1006322));
						val5.Set(SheetReader.DetectStage(sheetRow));
						num++;
					}
				}
				val2.Commit();
			}
			finally
			{
				((IDisposable)val2)?.Dispose();
			}
			_forceScan = true;
			if (list.Count > 0)
			{
				Log.Write("Автозаполнение: заняты другими " + list.Count + " листов");
				ReportLockedSummary("Проставлено стадий: " + num + ".", list);
			}
			else
			{
				Report("Проставлено стадий: " + num + ".");
			}
		}
		catch (Exception ex)
		{
			SetMessage("Не удалось проставить стадии: " + ex.Message);
			Warn("Не удалось проставить стадии." + Environment.NewLine + Environment.NewLine + ex.Message);
		}
	}

	private static void SetMessage(string text)
	{
		_message = text ?? "";
	}

	private static void Raise()
	{
		Action dataChanged = Bridge.DataChanged;
		if (dataChanged == null)
		{
			return;
		}
		try
		{
			dataChanged();
		}
		catch (Exception)
		{
		}
	}

	public static void ShowSheetLocked(string sheetNumber, string sheetName, string owner)
	{
		try
		{
			using SheetLockedForm sheetLockedForm = new SheetLockedForm(sheetNumber, sheetName, owner);
			sheetLockedForm.ShowDialog();
		}
		catch (Exception ex)
		{
			Log.Write("ShowSheetLocked: " + ex.Message);
		}
	}

	public static void Warn(string text)
	{
		try
		{
			MessageBox.Show(text ?? "", "Цветные листики", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
		catch
		{
			Log.Write("Предупреждение: " + text);
		}
	}

	public static void Report(string text)
	{
		try
		{
			MessageBox.Show(text ?? "", "Цветные листики", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}
		catch
		{
			Log.Write("Отчёт: " + text);
		}
	}

	public static void ShowError(string title, string details)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			TaskDialog val = new TaskDialog("Цветные листики");
			val.MainInstruction = title;
			val.MainContent = details ?? "";
			val.Show();
		}
		catch
		{
		}
	}

	public static void ShowPane()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected O, but got Unknown
		if (_uiApp == null)
		{
			return;
		}
		try
		{
			DockablePane dockablePane = _uiApp.GetDockablePane(new DockablePaneId(App.PaneId));
			if (dockablePane != null)
			{
				dockablePane.Show();
			}
		}
		catch
		{
		}
	}
}
