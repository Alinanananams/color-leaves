using System;
using System.Drawing;
using System.Windows.Forms;
using ColorLeaves.Core;

namespace ColorLeaves.Revit.UI;

/// <summary>
/// Компактное окно настроек: версия плагина и проверка обновлений.
/// Белый фон, пастельно-розовые кнопки — фирменная гамма плагина.
/// </summary>
public class SettingsForm : Form
{
	private Button _checkButton;

	private Button _updateButton;

	private Label _status;

	private ReleaseInfo _latest;

	public SettingsForm()
	{
		Text = "Color Leaves — Настройки";
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MaximizeBox = false;
		MinimizeBox = false;
		ShowInTaskbar = false;
		StartPosition = FormStartPosition.CenterScreen;
		ClientSize = new Size(410, 195);
		BackColor = Color.White;

		Label title = new Label();
		title.Text = "Color Leaves";
		title.Font = new Font("Segoe UI", 15f, FontStyle.Bold);
		title.Location = new Point(20, 15);
		title.AutoSize = true;
		Controls.Add(title);

		string currentText = typeof(SettingsForm).Assembly.GetName().Version.ToString(3);
		Label versionLabel = new Label();
		versionLabel.Text = "Версия плагина: v" + currentText + "   |   Revit 2021";
		versionLabel.ForeColor = Color.FromArgb(90, 90, 90);
		versionLabel.Location = new Point(22, 55);
		versionLabel.AutoSize = true;
		Controls.Add(versionLabel);

		_checkButton = MakeButton("Проверить обновления");
		_checkButton.Location = new Point(20, 85);
		_checkButton.Size = new Size(195, 34);
		_checkButton.Click += OnCheckClick;
		Controls.Add(_checkButton);

		_updateButton = MakeButton("Обновить");
		_updateButton.Location = new Point(225, 85);
		_updateButton.Size = new Size(115, 34);
		_updateButton.Visible = false;
		_updateButton.Click += OnUpdateClick;
		Controls.Add(_updateButton);

		_status = new Label();
		_status.ForeColor = Color.FromArgb(90, 90, 90);
		_status.Location = new Point(20, 128);
		_status.Size = new Size(375, 30);
		Controls.Add(_status);

		Button closeButton = MakeButton("Закрыть");
		closeButton.Location = new Point(295, 152);
		closeButton.Size = new Size(95, 30);
		closeButton.Click += delegate
		{
			Close();
		};
		Controls.Add(closeButton);

		AcceptButton = _checkButton;
		CancelButton = closeButton;
	}

	/// <summary>Плоская пастельно-розовая кнопка — фирменный стиль плагина.</summary>
	private static Button MakeButton(string text)
	{
		Button button = new Button();
		button.Text = text;
		button.FlatStyle = FlatStyle.Flat;
		button.BackColor = Color.FromArgb(248, 200, 220);
		button.FlatAppearance.BorderColor = Color.FromArgb(227, 158, 192);
		button.FlatAppearance.BorderSize = 1;
		button.FlatAppearance.MouseOverBackColor = Color.FromArgb(243, 181, 206);
		button.FlatAppearance.MouseDownBackColor = Color.FromArgb(236, 160, 193);
		button.ForeColor = Color.FromArgb(122, 59, 85);
		button.Font = new Font("Segoe UI", 9f);
		button.Cursor = Cursors.Hand;
		return button;
	}

	private void OnCheckClick(object sender, EventArgs e)
	{
		_checkButton.Enabled = false;
		_updateButton.Visible = false;
		_status.Text = "Проверяю обновления...";
		try
		{
			bool available;
			string message = UpdateFlow.Check(out _latest, out available);
			_status.Text = message;
			_updateButton.Visible = available;
		}
		catch (Exception ex)
		{
			_status.Text = "Ошибка проверки обновлений.";
			Log.Write("SettingsForm: " + ex.Message);
		}
		_checkButton.Enabled = true;
	}

	private void OnUpdateClick(object sender, EventArgs e)
	{
		if (_latest == null)
		{
			return;
		}
		_updateButton.Enabled = false;
		_checkButton.Enabled = false;
		_status.Text = "Скачиваю обновление...";
		try
		{
			_status.Text = UpdateFlow.DownloadAndPrepare(_latest);
		}
		catch (Exception ex)
		{
			_status.Text = "Не удалось скачать обновление.";
			Log.Write("SettingsForm: скачивание — " + ex.Message);
		}
		_updateButton.Enabled = true;
		_checkButton.Enabled = true;
	}
}
