using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ColorLeaves.Core;

using Color = System.Drawing.Color;

namespace ColorLeaves.Revit.UI;

public class SheetColorsForm : Form
{
	private List<StageDef> _stages;

	private ListView _list;

	private Button _change;

	private Button _reset;

	public SheetColorsForm()
	{
		_stages = new List<StageDef>();
		foreach (StageDef stage in StageSettings.Instance.Stages)
		{
			_stages.Add(stage.Clone());
		}
		Text = "Цвета стадий готовности";
		base.StartPosition = FormStartPosition.CenterParent;
		base.FormBorderStyle = FormBorderStyle.FixedDialog;
		base.MinimizeBox = false;
		base.MaximizeBox = false;
		base.ClientSize = new Size(420, 330);
		_list = new ListView();
		_list.View = View.Details;
		_list.FullRowSelect = true;
		_list.HideSelection = false;
		_list.Dock = DockStyle.Fill;
		_list.Columns.Add("Стадия", 220);
		_list.Columns.Add("Цвет", 130);
		_list.SelectedIndexChanged += delegate
		{
			UpdateButtons();
		};
		FillList();
		_change = new Button();
		_change.Text = "Изменить цвет...";
		_change.Dock = DockStyle.Left;
		_change.Width = 140;
		_change.Click += OnChangeColor;
		_reset = new Button();
		_reset.Text = "Стандартные";
		_reset.Dock = DockStyle.Left;
		_reset.Width = 120;
		_reset.Click += OnReset;
		Button button = new Button();
		button.Text = "ОК";
		button.DialogResult = DialogResult.OK;
		button.Dock = DockStyle.Right;
		button.Width = 90;
		button.Click += OnOk;
		Button value = new Button
		{
			Text = "Отмена",
			DialogResult = DialogResult.Cancel,
			Dock = DockStyle.Right,
			Width = 90
		};
		Panel value2 = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = 40,
			Controls = 
			{
				(Control)value,
				(Control)button
			}
		};
		Panel value3 = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = 36,
			Controls = 
			{
				(Control)_reset,
				(Control)_change
			}
		};
		base.Controls.Add(_list);
		base.Controls.Add(value3);
		base.Controls.Add(value2);
		UpdateButtons();
	}

	private void FillList()
	{
		_list.BeginUpdate();
		_list.Items.Clear();
		foreach (StageDef stage in _stages)
		{
			ListViewItem listViewItem = new ListViewItem(stage.Name);
			listViewItem.SubItems.Add("#" + stage.ColorHex);
			listViewItem.Tag = stage;
			listViewItem.BackColor = stage.Color;
			listViewItem.ForeColor = Color.Black;
			_list.Items.Add(listViewItem);
		}
		_list.EndUpdate();
	}

	private void UpdateButtons()
	{
		bool enabled = _list.SelectedItems.Count > 0;
		_change.Enabled = enabled;
	}

	private void OnChangeColor(object sender, EventArgs e)
	{
		if (_list.SelectedItems.Count == 0 || !(_list.SelectedItems[0].Tag is StageDef stageDef))
		{
			return;
		}
		using (ColorDialog colorDialog = new ColorDialog())
		{
			colorDialog.Color = stageDef.Color;
			colorDialog.FullOpen = true;
			if (colorDialog.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}
			stageDef.Color = colorDialog.Color;
		}
		ListViewItem listViewItem = _list.SelectedItems[0];
		listViewItem.BackColor = stageDef.Color;
		listViewItem.SubItems[1].Text = "#" + stageDef.ColorHex;
	}

	private void OnReset(object sender, EventArgs e)
	{
		List<StageDef> list = StageSettings.CreateDefaults();
		if (list.Count != _stages.Count)
		{
			MessageBox.Show(this, "Стандартный набор содержит другое количество стадий.", "Цветные листики", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			return;
		}
		for (int i = 0; i < _stages.Count; i++)
		{
			_stages[i].ColorHex = list[i].ColorHex;
		}
		FillList();
	}

	private void OnOk(object sender, EventArgs e)
	{
		StageSettings instance = StageSettings.Instance;
		for (int i = 0; i < _stages.Count && i < instance.Stages.Count; i++)
		{
			instance.Stages[i].ColorHex = _stages[i].ColorHex;
			instance.Stages[i].Name = _stages[i].Name;
		}
	}
}
