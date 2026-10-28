using System;
using System.Drawing;
using System.Windows.Forms;

using Color = System.Drawing.Color;

namespace ColorLeaves.Revit.UI;

public class SheetLockedForm : Form
{
	private readonly string _owner;

	public SheetLockedForm(string sheetNumber, string sheetName, string owner)
	{
		_owner = owner ?? "";
		Text = "Лист недоступен";
		base.StartPosition = FormStartPosition.CenterParent;
		base.FormBorderStyle = FormBorderStyle.FixedDialog;
		base.MinimizeBox = false;
		base.MaximizeBox = false;
		base.ClientSize = new Size(470, 230);
		Label value = new Label
		{
			Text = "!",
			Font = new Font("Segoe UI", 22f, FontStyle.Bold),
			ForeColor = Color.FromArgb(190, 40, 40),
			AutoSize = true,
			Location = new Point(20, 22)
		};
		Label value2 = new Label
		{
			Text = "Изменить стадию нельзя",
			Font = new Font("Segoe UI", 11f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(58, 22)
		};
		Label value3 = new Label
		{
			Text = BuildText(sheetNumber, sheetName, _owner),
			AutoSize = false,
			Location = new Point(58, 52),
			Size = new Size(390, 90),
			ForeColor = Color.FromArgb(45, 45, 45)
		};
		Button button = new Button();
		button.Text = "Скопировать имя";
		button.Location = new Point(58, 172);
		button.Size = new Size(150, 32);
		button.Click += OnCopy;
		Button button2 = new Button
		{
			Text = "Понятно",
			Location = new Point(288, 172),
			Size = new Size(140, 32),
			DialogResult = DialogResult.OK
		};
		base.Controls.Add(value);
		base.Controls.Add(value2);
		base.Controls.Add(value3);
		base.Controls.Add(button);
		base.Controls.Add(button2);
		base.AcceptButton = button2;
		base.CancelButton = button2;
	}

	private static string BuildText(string number, string name, string owner)
	{
		string text = (string.IsNullOrEmpty(owner) ? "другим пользователем" : owner);
		string text2 = "Лист " + number;
		if (!string.IsNullOrEmpty(name))
		{
			text2 = text2 + " «" + name + "»";
		}
		text2 = text2 + " открыт " + text + ".";
		text2 = text2 + Environment.NewLine + Environment.NewLine;
		text2 += "Пока с листом работает другой человек, стадию изменить";
		text2 += " нельзя — Revit не даёт редактировать занятые элементы.";
		return text2 + Environment.NewLine + "Попробуйте позже или договоритесь о снятии листа.";
	}

	private void OnCopy(object sender, EventArgs e)
	{
		try
		{
			Clipboard.SetText(_owner);
			MessageBox.Show(this, "Имя скопировано в буфер обмена.", "Цветные листики", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}
		catch
		{
			MessageBox.Show(this, "Не удалось скопировать: " + _owner, "Цветные листики", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}
	}
}
