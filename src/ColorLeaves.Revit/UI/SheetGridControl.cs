using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using ColorLeaves.Core;

using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;

namespace ColorLeaves.Revit.UI;

public class SheetGridControl : UserControl
{
	private const int ColNumber = 0;

	private const int ColName = 1;

	private const int ColEditedBy = 2;

	private const int ColStage = 3;

	private const int HeaderRowHeight = 26;

	/// <summary>Маркер строки-разделителя группы (группировка по стадиям).</summary>
	private class GroupHeader
	{
		public string Title;
	}

	private DataGridView _grid;

	private ToolStripTextBox _search;

	private ToolStripLabel _status;

	private ToolStripLabel _count;

	private ToolStripButton _autoButton;

	private ToolStripButton _diagButton;

	private ToolStripButton _unsetButton;

	private ToolStripButton _createParamButton;

	private ToolStripButton _groupButton;

	private ToolStripComboBox _stageFilter;

	private ToolStripLabel _modeLabel;

	private bool _suppress;

	private bool _lastEditable;

	private readonly Font _headerFont = new Font("Segoe UI", 9f, FontStyle.Bold);

	public SheetGridControl()
	{
		BuildUi();
		Bridge.DataChanged += OnDataChanged;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			Bridge.DataChanged -= OnDataChanged;
		}
		base.Dispose(disposing);
		if (disposing && _headerFont != null)
		{
			_headerFont.Dispose();
		}
	}

	private void BuildUi()
	{
		Dock = DockStyle.Fill;
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		ToolStrip toolStrip = new ToolStrip();
		toolStrip.GripStyle = ToolStripGripStyle.Hidden;
		toolStrip.RenderMode = ToolStripRenderMode.System;
		_search = new ToolStripTextBox();
		_search.AutoSize = false;
		_search.Width = 150;
		_search.TextBox.TextChanged += delegate
		{
			RebuildGrid();
		};
		ToolStripButton clearButton = new ToolStripButton("Очистить");
		clearButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
		clearButton.Click += delegate
		{
			_search.TextBox.Text = "";
		};
		_stageFilter = new ToolStripComboBox();
		_stageFilter.DropDownStyle = ComboBoxStyle.DropDownList;
		_stageFilter.Width = 130;
		_stageFilter.Items.Add("Все стадии");
		_stageFilter.SelectedIndex = 0;
		_stageFilter.SelectedIndexChanged += delegate
		{
			if (!_suppress)
			{
				SaveUiState();
				RebuildGrid();
			}
		};
		_groupButton = new ToolStripButton("Группировать");
		_groupButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
		_groupButton.CheckOnClick = true;
		_groupButton.ToolTipText = "Группировать листы по стадиям";
		_groupButton.CheckedChanged += delegate
		{
			if (!_suppress)
			{
				SaveUiState();
				RefreshNow();
			}
		};
		ToolStripButton refreshButton = new ToolStripButton("Обновить");
		refreshButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
		refreshButton.Click += delegate
		{
			Bridge.RequestRefresh();
			RefreshNow();
		};
		ToolStripButton colorsButton = new ToolStripButton("Цвета стадий...");
		colorsButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
		colorsButton.Click += OnColorsClick;
		_diagButton = new ToolStripButton("Диагностика");
		_diagButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
		_diagButton.Click += OnDiagClick;
		_autoButton = new ToolStripButton("Заполнить автоматически");
		_autoButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
		_autoButton.Click += delegate
		{
			Bridge.RequestAutoFill();
			Bridge.RequestRefresh();
		};
		_unsetButton = new ToolStripButton("Заполнить «Не задана»");
		_unsetButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
		_unsetButton.Click += delegate
		{
			Bridge.RequestFillUnset();
			Bridge.RequestRefresh();
		};
		_createParamButton = new ToolStripButton("Создать параметр");
		_createParamButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
		_createParamButton.Click += delegate
		{
			Bridge.RequestCreateParameter();
			Bridge.RequestRefresh();
		};
		_modeLabel = new ToolStripLabel("");
		_modeLabel.Font = new Font(_modeLabel.Font, FontStyle.Bold);
		_status = new ToolStripLabel("");
		_status.ForeColor = Color.FromArgb(150, 90, 0);
		_count = new ToolStripLabel("");
		_count.Font = new Font(_count.Font, FontStyle.Bold);
		toolStrip.Items.Add(new ToolStripLabel("Поиск: "));
		toolStrip.Items.Add(_search);
		toolStrip.Items.Add(clearButton);
		toolStrip.Items.Add(new ToolStripSeparator());
		toolStrip.Items.Add(new ToolStripLabel("Стадия: "));
		toolStrip.Items.Add(_stageFilter);
		toolStrip.Items.Add(_groupButton);
		toolStrip.Items.Add(new ToolStripSeparator());
		toolStrip.Items.Add(refreshButton);
		toolStrip.Items.Add(colorsButton);
		toolStrip.Items.Add(_autoButton);
		toolStrip.Items.Add(_unsetButton);
		toolStrip.Items.Add(_createParamButton);
		toolStrip.Items.Add(_diagButton);
		toolStrip.Items.Add(_modeLabel);
		toolStrip.Items.Add(_status);
		toolStrip.Items.Add(_count);
		_grid = new DataGridView();
		_grid.Dock = DockStyle.Fill;
		_grid.AllowUserToAddRows = false;
		_grid.AllowUserToDeleteRows = false;
		_grid.AllowUserToResizeRows = false;
		_grid.RowHeadersVisible = false;
		_grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
		_grid.MultiSelect = false;
		_grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
		_grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
		_grid.EnableHeadersVisualStyles = false;
		_grid.ColumnHeadersDefaultCellStyle.Font = new Font(_grid.Font, FontStyle.Bold);
		_grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(51, 153, 255);
		_grid.DefaultCellStyle.SelectionForeColor = Color.Black;
		_grid.CellBorderStyle = DataGridViewCellBorderStyle.Single;
		_grid.GridColor = Color.Black;
		_grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
		_grid.ReadOnly = false;
		_grid.CellValueChanged += OnCellValueChanged;
		_grid.CellPainting += OnCellPainting;
		_grid.DataError += OnGridDataError;
		DataGridViewTextBoxColumn numberColumn = new DataGridViewTextBoxColumn();
		numberColumn.HeaderText = "№ листа";
		numberColumn.Name = "Number";
		numberColumn.Width = 150;
		numberColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
		DataGridViewTextBoxColumn nameColumn = new DataGridViewTextBoxColumn();
		nameColumn.HeaderText = "Наименование";
		nameColumn.Name = "Name";
		nameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
		nameColumn.MinimumWidth = 140;
		nameColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
		DataGridViewComboBoxColumn stageColumn = new DataGridViewComboBoxColumn();
		stageColumn.HeaderText = "Стадия";
		stageColumn.Name = "Stage";
		stageColumn.Width = 130;
		stageColumn.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
		stageColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
		DataGridViewTextBoxColumn editedByColumn = new DataGridViewTextBoxColumn();
		editedByColumn.HeaderText = "Редактирует";
		editedByColumn.Name = "EditedBy";
		editedByColumn.Width = 110;
		editedByColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
		_grid.Columns.AddRange(numberColumn, nameColumn, editedByColumn, stageColumn);
		base.Controls.Add(_grid);
		base.Controls.Add(toolStrip);
	}

	private void OnDataChanged()
	{
		try
		{
			if (!base.IsDisposed && base.IsHandleCreated)
			{
				if (base.InvokeRequired)
				{
					BeginInvoke(new MethodInvoker(RefreshNow));
				}
				else
				{
					RefreshNow();
				}
			}
		}
		catch
		{
		}
	}

	public void RefreshNow()
	{
		if (_grid == null || _grid.IsCurrentCellInEditMode)
		{
			return;
		}
		try
		{
			if (_grid.EditingControl != null && _grid.EditingControl is DataGridViewComboBoxEditingControl)
			{
				return;
			}
		}
		catch
		{
		}
		_suppress = true;
		try
		{
			SyncStageFilterItems();
			_groupButton.Checked = StageSettings.Instance.GroupByStage;
			DataGridViewComboBoxColumn stageColumn = _grid.Columns[ColStage] as DataGridViewComboBoxColumn;
			List<string> names = StageSettings.Instance.Names;
			// К настроенным стадиям добавляем неизвестные значения из проекта —
			// иначе комбобокс бросает DataError на листах с «нестандартной» стадией
			List<string> comboNames = new List<string>(names);
			foreach (SheetRow row in Bridge.Rows)
			{
				if (!string.IsNullOrEmpty(row.Stage) && !comboNames.Contains(row.Stage))
				{
					comboNames.Add(row.Stage);
				}
			}
			if (stageColumn != null && !SameItems(stageColumn, comboNames))
			{
				stageColumn.DataSource = comboNames;
			}
			foreach (DataGridViewColumn column in _grid.Columns)
			{
				column.ReadOnly = column.Index != ColStage;
			}
			_grid.Columns[ColStage].ReadOnly = !Bridge.CanEditStage;
			_autoButton.Enabled = Bridge.CanEditStage;
			bool editable = !_grid.Columns[ColStage].ReadOnly;
			if (editable != _lastEditable)
			{
				_lastEditable = editable;
				Log.Write(string.Concat("Колонка «Стадия» редактируема: ", editable, " (CanEditStage=", Bridge.CanEditStage, ")"));
			}
			RebuildGrid();
			_status.Text = Bridge.Message;
			_status.ForeColor = (Bridge.CanEditStage ? Color.FromArgb(150, 90, 0) : Color.FromArgb(190, 30, 30));
			_diagButton.Enabled = Bridge.HasDocument;
			_unsetButton.Enabled = Bridge.CanEditStage;
			_createParamButton.Enabled = Bridge.HasDocument && !Bridge.CanEditStage;
			if (Bridge.PendingCount > 0)
			{
				_status.Text = "Не применено изменений: " + Bridge.PendingCount + ". " + _status.Text;
			}
			UpdateModeLabel();
		}
		finally
		{
			_suppress = false;
		}
	}

	private void SyncStageFilterItems()
	{
		StageSettings stageSettings = StageSettings.Instance;
		List<string> items = new List<string>
		{
			"Все стадии"
		};
		items.AddRange(stageSettings.Names);
		bool same = _stageFilter.Items.Count == items.Count;
		if (same)
		{
			for (int i = 0; i < items.Count; i++)
			{
				if (!string.Equals(_stageFilter.Items[i] as string, items[i], StringComparison.Ordinal))
				{
					same = false;
					break;
				}
			}
		}
		if (!same)
		{
			_stageFilter.Items.Clear();
			_stageFilter.Items.AddRange(items.ToArray());
		}
		string saved = stageSettings.FilterStage;
		int target = 0;
		if (!string.IsNullOrEmpty(saved))
		{
			int found = items.IndexOf(saved);
			if (found >= 0)
			{
				target = found;
			}
		}
		if (_stageFilter.SelectedIndex != target)
		{
			_stageFilter.SelectedIndex = target;
		}
	}

	private string CurrentStageFilter()
	{
		if (_stageFilter.SelectedIndex > 0)
		{
			return _stageFilter.Items[_stageFilter.SelectedIndex] as string;
		}
		return null;
	}

	private void SaveUiState()
	{
		try
		{
			StageSettings stageSettings = StageSettings.Instance;
			stageSettings.GroupByStage = _groupButton.Checked;
			stageSettings.FilterStage = CurrentStageFilter();
			stageSettings.Save();
		}
		catch (Exception ex)
		{
			Log.Write("Сохранение состояния UI: " + ex.Message);
		}
	}

	private bool SameItems(DataGridViewComboBoxColumn col, List<string> names)
	{
		if (col.DataSource == null)
		{
			return false;
		}
		if (!(col.DataSource is List<string> list) || list.Count != names.Count)
		{
			return false;
		}
		for (int i = 0; i < names.Count; i++)
		{
			if (!string.Equals(list[i], names[i], StringComparison.Ordinal))
			{
				return false;
			}
		}
		return true;
	}

	private static bool Matches(SheetRow row, string query, string stageFilter)
	{
		bool matchesQuery = query.Length == 0 || Contains(row.Number, query) || Contains(row.Name, query) || Contains(row.Stage, query);
		bool matchesStage = stageFilter == null || string.Equals(row.Stage, stageFilter, StringComparison.Ordinal);
		return matchesQuery && matchesStage;
	}

	/// <summary>
	/// Перестраивает строки таблицы: применяет поиск, фильтр по стадии и группировку.
	/// Фильтрация выполняется перестройкой (а не скрытием строк) — это исключает
	/// артефакты отрисовки DataGridView при переключении row.Visible.
	/// </summary>
	private void RebuildGrid()
	{
		if (_grid == null)
		{
			return;
		}
		List<SheetRow> all = Bridge.Rows;
		string query = (_search.TextBox.Text ?? "").Trim();
		string stageFilter = CurrentStageFilter();
		bool grouping = _groupButton.Checked;

		// Запоминаем выделенный лист, чтобы вернуть выделение после перестройки
		int selectedId = 0;
		if (_grid.CurrentRow?.Tag is SheetRow selected)
		{
			selectedId = selected.Id;
		}

		List<object> display = new List<object>();
		if (!grouping)
		{
			foreach (SheetRow row in all)
			{
				if (Matches(row, query, stageFilter))
				{
					display.Add(row);
				}
			}
		}
		else
		{
			List<SheetRow> rest = new List<SheetRow>(all);
			foreach (string name in StageSettings.Instance.Names)
			{
				List<SheetRow> group = rest.FindAll((SheetRow r) => string.Equals(r.Stage, name, StringComparison.Ordinal) && Matches(r, query, stageFilter));
				if (group.Count > 0)
				{
					display.Add(new GroupHeader
					{
						Title = name + " — " + group.Count + " " + PluralSheets(group.Count)
					});
					display.AddRange(group);
					foreach (SheetRow item in group)
					{
						rest.Remove(item);
					}
				}
			}
			List<SheetRow> others = rest.FindAll((SheetRow r) => Matches(r, query, stageFilter));
			if (others.Count > 0)
			{
				display.Add(new GroupHeader
				{
					Title = "Прочие — " + others.Count + " " + PluralSheets(others.Count)
				});
				display.AddRange(others);
			}
		}

		_grid.SuspendLayout();
		try
		{
			_grid.Rows.Clear();
			foreach (object item in display)
			{
				int index = _grid.Rows.Add();
				FillRow(_grid.Rows[index], item);
			}
		}
		finally
		{
			_grid.ResumeLayout();
		}

		// Восстанавливаем выделение
		if (selectedId != 0)
		{
			foreach (DataGridViewRow row in _grid.Rows)
			{
				if (row.Tag is SheetRow sheetRow && sheetRow.Id == selectedId)
				{
					try
					{
						_grid.CurrentCell = row.Cells[ColNumber];
					}
					catch
					{
					}
					break;
				}
			}
		}

		int visibleSheets = 0;
		foreach (object item in display)
		{
			if (item is SheetRow)
			{
				visibleSheets++;
			}
		}
		_count.Text = "Показано: " + visibleSheets + " из " + all.Count;
	}

	private void FillRow(DataGridViewRow gridRow, object data)
	{
		if (data is GroupHeader groupHeader)
		{
			gridRow.Tag = groupHeader;
			gridRow.Cells[ColNumber].Value = groupHeader.Title;
			gridRow.Cells[ColName].Value = "";
			gridRow.Cells[ColEditedBy].Value = "";
			gridRow.Cells[ColStage].Value = null;
			gridRow.ReadOnly = true;
			gridRow.MinimumHeight = HeaderRowHeight;
			gridRow.DefaultCellStyle.BackColor = Color.FromArgb(217, 217, 217);
			gridRow.DefaultCellStyle.ForeColor = Color.Black;
			gridRow.DefaultCellStyle.SelectionBackColor = Color.FromArgb(217, 217, 217);
			gridRow.DefaultCellStyle.SelectionForeColor = Color.Black;
			gridRow.DefaultCellStyle.Font = _headerFont;
			gridRow.Cells[ColNumber].ToolTipText = "";
			return;
		}
		SheetRow sheetRow = (SheetRow)data;
		gridRow.Tag = sheetRow;
		gridRow.ReadOnly = false;
		string text = sheetRow.Number;
		if (sheetRow.UpdatedInCentral)
		{
			text += " *";
		}
		if (!string.IsNullOrEmpty(sheetRow.LockedBy))
		{
			text = text + "  " + ShortOwner(sheetRow.LockedBy);
		}
		gridRow.Cells[ColNumber].Value = text;
		gridRow.Cells[ColName].Value = sheetRow.Name;
		gridRow.Cells[ColEditedBy].Value = sheetRow.EditedBy;
		if (!string.IsNullOrEmpty(sheetRow.LockedBy))
		{
			gridRow.Cells[ColNumber].ToolTipText = "Лист открыт другим пользователем: " + sheetRow.LockedBy + Environment.NewLine + "Стадию изменить нельзя, пока он его не закроет.";
		}
		else if (sheetRow.UpdatedInCentral)
		{
			gridRow.Cells[ColNumber].ToolTipText = "В центральной модели есть обновления — нажмите «Обновить».";
		}
		else
		{
			gridRow.Cells[ColNumber].ToolTipText = "";
		}
		DataGridViewCell stageCell = gridRow.Cells[ColStage];
		if (!string.Equals(Convert.ToString(stageCell.Value), sheetRow.Stage, StringComparison.Ordinal))
		{
			stageCell.Value = sheetRow.Stage;
		}
		Color backColor = StageSettings.Instance.ColorOf(sheetRow.Stage);
		gridRow.DefaultCellStyle.BackColor = backColor;
		gridRow.DefaultCellStyle.ForeColor = Color.Black;
		gridRow.DefaultCellStyle.Font = _grid.Font;
	}

	private void OnCellPainting(object sender, DataGridViewCellPaintingEventArgs e)
	{
		if (e.RowIndex == -1)
		{
			PaintHeaderCell(e);
			return;
		}
		if (e.ColumnIndex != ColStage || e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count)
		{
			return;
		}
		DataGridViewRow dataGridViewRow = _grid.Rows[e.RowIndex];
		if (dataGridViewRow.Tag is SheetRow sheetRow)
		{
			Color color = StageSettings.Instance.ColorOf(sheetRow.Stage);
			Color foreColor = Color.Black;
			if ((e.State & DataGridViewElementStates.Selected) != DataGridViewElementStates.None)
			{
				color = _grid.DefaultCellStyle.SelectionBackColor;
				foreColor = _grid.DefaultCellStyle.SelectionForeColor;
			}
			using (SolidBrush brush = new SolidBrush(color))
			{
				e.Graphics.FillRectangle(brush, e.CellBounds);
			}
			string value = Convert.ToString(dataGridViewRow.Cells[ColStage].Value);
			if (string.IsNullOrEmpty(value))
			{
				value = sheetRow.Stage;
			}
			Rectangle bounds = new Rectangle(e.CellBounds.X + 4, e.CellBounds.Y, e.CellBounds.Width - 8, e.CellBounds.Height);
			TextRenderer.DrawText(e.Graphics, value, e.CellStyle.Font, bounds, foreColor, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);
			using (Pen pen = new Pen(Color.Black, 1f))
			{
				e.Graphics.DrawRectangle(pen, e.CellBounds.X, e.CellBounds.Y, e.CellBounds.Width - 1, e.CellBounds.Height - 1);
			}
			e.Handled = true;
		}
		else if (dataGridViewRow.Tag is GroupHeader)
		{
			// Ячейка «Стадия» у заголовка группы: рисуем фон явно, иначе остаются артефакты
			Color headerBack = dataGridViewRow.DefaultCellStyle.BackColor;
			using (SolidBrush brush = new SolidBrush(headerBack))
			{
				e.Graphics.FillRectangle(brush, e.CellBounds);
			}
			using (Pen pen = new Pen(Color.Black, 1f))
			{
				e.Graphics.DrawRectangle(pen, e.CellBounds.X, e.CellBounds.Y, e.CellBounds.Width - 1, e.CellBounds.Height - 1);
			}
			e.Handled = true;
		}
	}

	private void PaintHeaderCell(DataGridViewCellPaintingEventArgs e)
	{
		if (e.ColumnIndex >= -1 && e.ColumnIndex < _grid.Columns.Count)
		{
			Color color = _grid.ColumnHeadersDefaultCellStyle.BackColor;
			Color foreColor = _grid.ColumnHeadersDefaultCellStyle.ForeColor;
			if (color.Name == "Control")
			{
				color = SystemColors.Control;
			}
			if (foreColor.Name == "ControlText")
			{
				foreColor = SystemColors.ControlText;
			}
			string text = "";
			if (e.ColumnIndex >= 0)
			{
				text = _grid.Columns[e.ColumnIndex].HeaderText;
			}
			using (SolidBrush brush = new SolidBrush(color))
			{
				e.Graphics.FillRectangle(brush, e.CellBounds);
			}
			if (text.Length > 0)
			{
				TextRenderer.DrawText(e.Graphics, text, e.CellStyle.Font, e.CellBounds, foreColor, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);
			}
			using (Pen pen = new Pen(Color.Black, 1f))
			{
				e.Graphics.DrawRectangle(pen, e.CellBounds.X, e.CellBounds.Y, e.CellBounds.Width - 1, e.CellBounds.Height - 1);
			}
			e.Handled = true;
		}
	}

	/// <summary>
	/// Подавляет стандартный диалог DataError: он появлялся, когда у листа стадия,
	/// которой нет в настроенном списке (кто-то вписал её вручную в параметр).
	/// Детали пишутся в лог, а неизвестные стадии добавляются в список комбобокса.
	/// </summary>
	private void OnGridDataError(object sender, DataGridViewDataErrorEventArgs e)
	{
		string value = "";
		try
		{
			if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
			{
				value = Convert.ToString(_grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
			}
		}
		catch
		{
		}
		Log.Write("DataGridView DataError: " + e.Exception.Message + " (строка " + e.RowIndex + ", колонка " + e.ColumnIndex + ", значение '" + value + "')");
		e.ThrowException = false;
	}

	private void OnCellValueChanged(object sender, DataGridViewCellEventArgs e)
	{
		if (_suppress || e.RowIndex < 0 || e.ColumnIndex != ColStage)
		{
			return;
		}
		DataGridViewRow dataGridViewRow = _grid.Rows[e.RowIndex];
		if (!(dataGridViewRow.Tag is SheetRow sheetRow))
		{
			return;
		}
		string text = Convert.ToString(dataGridViewRow.Cells[ColStage].Value);
		if (string.IsNullOrEmpty(text))
		{
			Log.Write("Выбор стадии: пустое значение");
			return;
		}
		if (string.Equals(text, sheetRow.Stage, StringComparison.Ordinal))
		{
			return;
		}
		Log.Write("Выбор стадии: лист " + sheetRow.Number + " (" + sheetRow.Id + ") -> " + text);
		if (Bridge.IsWorkshared)
		{
			CheckoutStatus checkoutStatus = Bridge.GetCheckoutStatus(sheetRow.Id);
			if ((int)checkoutStatus == 1)
			{
				string checkoutOwner = Bridge.GetCheckoutOwner(sheetRow.Id);
				Log.Write("Лист " + sheetRow.Number + " открыт другим пользователем (" + checkoutOwner + ") — запись отменена");
				Bridge.ShowSheetLocked(sheetRow.Number, sheetRow.Name, checkoutOwner);
				FillRow(dataGridViewRow, sheetRow);
				return;
			}
		}
		sheetRow.Stage = text;
		sheetRow.StageIsManual = true;
		dataGridViewRow.DefaultCellStyle.BackColor = StageSettings.Instance.ColorOf(text);
		Bridge.QueueStage(sheetRow.Id, text);
	}

	private static string PluralSheets(int count)
	{
		int mod100 = count % 100;
		if (mod100 >= 11 && mod100 <= 14)
		{
			return "листов";
		}
		switch (count % 10)
		{
		case 1:
			return "лист";
		case 2:
		case 3:
		case 4:
			return "листа";
		default:
			return "листов";
		}
	}

	private static string ShortOwner(string owner)
	{
		if (string.IsNullOrEmpty(owner))
		{
			return "";
		}
		if (owner.Length > 14)
		{
			return owner.Substring(0, 13) + "…";
		}
		return owner;
	}

	private static bool Contains(string source, string query)
	{
		if (string.IsNullOrEmpty(source))
		{
			return false;
		}
		return source.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private void UpdateModeLabel()
	{
		if (!Bridge.IsWorkshared)
		{
			_modeLabel.Text = "[локальная модель]";
			_modeLabel.ForeColor = Color.FromArgb(90, 90, 90);
			return;
		}
		if (Bridge.HasChangesFromCentral)
		{
			_modeLabel.Text = "[общая модель — есть изменения из центра]";
			_modeLabel.ForeColor = Color.FromArgb(190, 60, 0);
			return;
		}
		string text = "[общая модель";
		if (Bridge.LastSyncTime != DateTime.MinValue)
		{
			text = text + " — синхронизировано в " + Bridge.LastSyncTime.ToString("HH:mm:ss");
		}
		text += "]";
		_modeLabel.Text = text;
		_modeLabel.ForeColor = Color.FromArgb(0, 120, 60);
	}

	private void OnDiagClick(object sender, EventArgs e)
	{
		StageParamDiag diag = Bridge.Diag;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("Листов в проекте: " + diag.SheetsTotal);
		stringBuilder.AppendLine("Ищем параметр:      Стадия готовности");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("Результат: " + diag.Detail);
		if (!string.IsNullOrEmpty(diag.SheetNumber))
		{
			stringBuilder.AppendLine("Найден на листе: " + diag.SheetNumber);
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("Параметры листов, в названии которых есть «стадия»:");
		if (diag.Similar.Count == 0)
		{
			stringBuilder.AppendLine("  (не найдено ни одного)");
		}
		else
		{
			HashSet<string> hashSet = new HashSet<string>(diag.Similar);
			foreach (string item in hashSet)
			{
				stringBuilder.AppendLine("  " + item);
			}
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("Что требуется:");
		stringBuilder.AppendLine("  Управление → Параметры проекта → Общие → Листы");
		stringBuilder.AppendLine("  Имя:  Стадия готовности");
		stringBuilder.AppendLine("  Тип данных: Текст");
		stringBuilder.AppendLine("  Привязка: Экземпляр  (не «Тип» — иначе значение нельзя менять)");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("Совместная работа:");
		if (!Bridge.IsWorkshared)
		{
			stringBuilder.AppendLine("  Модель открыта локально, синхронизация не требуется.");
		}
		else
		{
			stringBuilder.AppendLine("  Модель в общем доступе.");
			stringBuilder.AppendLine("  Расхождения с центром: " + (Bridge.HasChangesFromCentral ? "есть, нажмите «Обновить»" : "нет"));
			stringBuilder.AppendLine("  Последняя синхронизация: " + ((Bridge.LastSyncTime == DateTime.MinValue) ? "не выполнялась" : Bridge.LastSyncTime.ToString("dd.MM.yyyy HH:mm:ss")));
			int num = 0;
			foreach (SheetRow row in Bridge.Rows)
			{
				if (row.UpdatedInCentral)
				{
					num++;
				}
			}
			stringBuilder.AppendLine("  Листов с обновлениями из центра: " + num);
		}
		string message = stringBuilder.ToString();
		Log.Write("=== ДИАГНОСТИКА ===");
		Log.Write(message);
		MessageBox.Show(message, "Цветные листики — диагностика параметра", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
	}

	private void OnColorsClick(object sender, EventArgs e)
	{
		using SheetColorsForm sheetColorsForm = new SheetColorsForm();
		if (sheetColorsForm.ShowDialog() == DialogResult.OK)
		{
			StageSettings.Instance.Save();
			RefreshNow();
		}
	}
}
