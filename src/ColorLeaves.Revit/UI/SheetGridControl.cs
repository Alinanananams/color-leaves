using System;
using System.Collections;
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

	private const int ColStage = 2;

	private DataGridView _grid;

	private ToolStripTextBox _search;

	private ToolStripLabel _status;

	private ToolStripLabel _count;

	private ToolStripButton _autoButton;

	private ToolStripButton _diagButton;

	private ToolStripButton _unsetButton;

	private ToolStripButton _createParamButton;

	private ToolStripLabel _modeLabel;

	private bool _suppress;

	private bool _lastEditable;

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
		_search.Width = 180;
		_search.TextBox.TextChanged += delegate
		{
			ApplyFilter(_search.TextBox.Text);
		};
		ToolStripButton toolStripButton = new ToolStripButton("Очистить");
		toolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
		toolStripButton.Click += delegate
		{
			_search.TextBox.Text = "";
		};
		ToolStripButton toolStripButton2 = new ToolStripButton("Обновить");
		toolStripButton2.DisplayStyle = ToolStripItemDisplayStyle.Text;
		toolStripButton2.Click += delegate
		{
			Bridge.RequestRefresh();
			RefreshNow();
		};
		ToolStripButton toolStripButton3 = new ToolStripButton("Цвета стадий...");
		toolStripButton3.DisplayStyle = ToolStripItemDisplayStyle.Text;
		toolStripButton3.Click += OnColorsClick;
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
		toolStrip.Items.Add(toolStripButton);
		toolStrip.Items.Add(new ToolStripSeparator());
		toolStrip.Items.Add(toolStripButton2);
		toolStrip.Items.Add(toolStripButton3);
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
		DataGridViewTextBoxColumn dataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
		dataGridViewTextBoxColumn.HeaderText = "№ листа";
		dataGridViewTextBoxColumn.Name = "Number";
		dataGridViewTextBoxColumn.Width = 150;
		dataGridViewTextBoxColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
		DataGridViewTextBoxColumn dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
		dataGridViewTextBoxColumn2.HeaderText = "Наименование";
		dataGridViewTextBoxColumn2.Name = "Name";
		dataGridViewTextBoxColumn2.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
		dataGridViewTextBoxColumn2.MinimumWidth = 140;
		dataGridViewTextBoxColumn2.SortMode = DataGridViewColumnSortMode.NotSortable;
		DataGridViewComboBoxColumn dataGridViewComboBoxColumn = new DataGridViewComboBoxColumn();
		dataGridViewComboBoxColumn.HeaderText = "Стадия";
		dataGridViewComboBoxColumn.Name = "Stage";
		dataGridViewComboBoxColumn.Width = 130;
		dataGridViewComboBoxColumn.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
		dataGridViewComboBoxColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
		_grid.Columns.AddRange(dataGridViewTextBoxColumn, dataGridViewTextBoxColumn2, dataGridViewComboBoxColumn);
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
			DataGridViewComboBoxColumn dataGridViewComboBoxColumn = _grid.Columns[2] as DataGridViewComboBoxColumn;
			List<string> names = StageSettings.Instance.Names;
			if (dataGridViewComboBoxColumn != null && !SameItems(dataGridViewComboBoxColumn, names))
			{
				dataGridViewComboBoxColumn.DataSource = names;
			}
			foreach (DataGridViewColumn column in _grid.Columns)
			{
				column.ReadOnly = column.Index != 2;
			}
			_grid.Columns[2].ReadOnly = !Bridge.CanEditStage;
			_autoButton.Enabled = Bridge.CanEditStage;
			bool flag = !_grid.Columns[2].ReadOnly;
			if (flag != _lastEditable)
			{
				_lastEditable = flag;
				Log.Write(string.Concat("Колонка «Стадия» редактируема: ", flag, " (CanEditStage=", Bridge.CanEditStage, ", источник значений: ", dataGridViewComboBoxColumn.DataSource, ")"));
			}
			List<SheetRow> rows = Bridge.Rows;
			SyncRows(rows);
			ApplyFilter(_search.TextBox.Text);
			_count.Text = "Листов: " + rows.Count;
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

	private void SyncRows(List<SheetRow> data)
	{
		bool flag = data.Count == _grid.Rows.Count;
		if (flag)
		{
			for (int i = 0; i < data.Count; i++)
			{
				if (!(_grid.Rows[i].Tag is SheetRow sheetRow) || sheetRow.Id != data[i].Id)
				{
					flag = false;
					break;
				}
			}
		}
		if (flag)
		{
			for (int j = 0; j < data.Count; j++)
			{
				FillRow(_grid.Rows[j], data[j]);
			}
			return;
		}
		_grid.SuspendLayout();
		_grid.Rows.Clear();
		foreach (SheetRow datum in data)
		{
			int index = _grid.Rows.Add();
			FillRow(_grid.Rows[index], datum);
		}
		_grid.ResumeLayout();
	}

	private void FillRow(DataGridViewRow gridRow, SheetRow data)
	{
		if (!_grid.IsCurrentRowDirty || _grid.CurrentCell == null || _grid.CurrentCell.RowIndex != gridRow.Index || _grid.CurrentCell.ColumnIndex != 2)
		{
			gridRow.Tag = data;
			string text = data.Number;
			if (data.UpdatedInCentral)
			{
				text += " *";
			}
			if (!string.IsNullOrEmpty(data.LockedBy))
			{
				text = text + "  " + ShortOwner(data.LockedBy);
			}
			gridRow.Cells[0].Value = text;
			gridRow.Cells[1].Value = data.Name;
			if (!string.IsNullOrEmpty(data.LockedBy))
			{
				gridRow.Cells[0].ToolTipText = "Лист открыт другим пользователем: " + data.LockedBy + Environment.NewLine + "Стадию изменить нельзя, пока он его не закроет.";
			}
			else if (data.UpdatedInCentral)
			{
				gridRow.Cells[0].ToolTipText = "В центральной модели есть обновления — нажмите «Обновить».";
			}
			else
			{
				gridRow.Cells[0].ToolTipText = "";
			}
			DataGridViewCell dataGridViewCell = gridRow.Cells[2];
			if (!string.Equals(Convert.ToString(dataGridViewCell.Value), data.Stage, StringComparison.Ordinal))
			{
				dataGridViewCell.Value = data.Stage;
			}
			Color backColor = StageSettings.Instance.ColorOf(data.Stage);
			gridRow.DefaultCellStyle.BackColor = backColor;
			gridRow.DefaultCellStyle.ForeColor = Color.Black;
		}
	}

	private void OnCellPainting(object sender, DataGridViewCellPaintingEventArgs e)
	{
		if (e.RowIndex == -1)
		{
			PaintHeaderCell(e);
		}
		else
		{
			if (e.ColumnIndex != 2 || e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count)
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
				string value = Convert.ToString(dataGridViewRow.Cells[2].Value);
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

	private void OnCellValueChanged(object sender, DataGridViewCellEventArgs e)
	{
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Invalid comparison between Unknown and I4
		if (_suppress || e.RowIndex < 0 || e.ColumnIndex != 2)
		{
			return;
		}
		DataGridViewRow dataGridViewRow = _grid.Rows[e.RowIndex];
		if (!(dataGridViewRow.Tag is SheetRow sheetRow))
		{
			Log.Write("Выбор стадии: строка без данных (строка " + e.RowIndex + ")");
			return;
		}
		string text = Convert.ToString(dataGridViewRow.Cells[2].Value);
		if (string.IsNullOrEmpty(text))
		{
			Log.Write("Выбор стадии: пустое значение");
		}
		else
		{
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
	}

	private void ApplyFilter(string text)
	{
		string text2 = (text ?? "").Trim();
		foreach (DataGridViewRow item in (IEnumerable)_grid.Rows)
		{
			if (item.DataBoundItem != null || item.Tag != null)
			{
				if (!(item.Tag is SheetRow sheetRow))
				{
					item.Visible = false;
					continue;
				}
				bool visible = text2.Length == 0 || Contains(sheetRow.Number, text2) || Contains(sheetRow.Name, text2) || Contains(sheetRow.Stage, text2);
				item.Visible = visible;
			}
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
