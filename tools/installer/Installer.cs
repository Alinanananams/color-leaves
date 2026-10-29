// Color Leaves — простой установщик плагина для Autodesk Revit 2021.
// Файлы плагина зашиты в exe как ресурсы и копируются в %APPDATA%\Autodesk\Revit\Addins\2021\.
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace ColorLeaves.Installer
{
    internal static class Program
    {
        private static Label _status;

        private static Button _installButton;

        private static readonly string TargetDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Autodesk", "Revit", "Addins", "2021");

        private static readonly string[] Files =
        {
            "ColorLeaves.addin",
            "ColorLeaves.Revit.dll",
            "ColorLeaves.Core.dll",
            "sheet.png",
            "refresh.png",
            "colors.png",
            "parameter.png"
        };

        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(BuildForm());
        }

        private static Form BuildForm()
        {
            Form form = new Form();
            form.Text = "Color Leaves — установка плагина";
            form.FormBorderStyle = FormBorderStyle.FixedSingle;
            form.MaximizeBox = false;
            form.MinimizeBox = false;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.ClientSize = new System.Drawing.Size(460, 230);

            Label title = new Label();
            title.Text = "Color Leaves v1.1.0";
            title.Font = new System.Drawing.Font("Segoe UI", 14f, System.Drawing.FontStyle.Bold);
            title.Location = new System.Drawing.Point(20, 15);
            title.AutoSize = true;
            form.Controls.Add(title);

            Label info = new Label();
            info.Text = "Плагин стадий готовности листов для Autodesk Revit 2021.\r\n\r\n" +
                "Устанавливается в:\r\n%APPDATA%\\Autodesk\\Revit\\Addins\\2021\\\r\n\r\n" +
                "Перед установкой закройте Revit.";
            info.Location = new System.Drawing.Point(20, 55);
            info.Size = new System.Drawing.Size(420, 100);
            form.Controls.Add(info);

            _status = new Label();
            _status.Text = "";
            _status.ForeColor = System.Drawing.Color.FromArgb(0, 120, 60);
            _status.Location = new System.Drawing.Point(20, 158);
            _status.AutoSize = true;
            form.Controls.Add(_status);

            _installButton = new Button();
            _installButton.Text = "Установить";
            _installButton.Location = new System.Drawing.Point(20, 185);
            _installButton.Size = new System.Drawing.Size(160, 32);
            _installButton.Click += OnInstallClick;
            form.Controls.Add(_installButton);

            return form;
        }

        private static bool _done;

        private static void OnInstallClick(object sender, EventArgs e)
        {
            if (_done)
            {
                Application.Exit();
                return;
            }
            Install();
        }

        private static void Install()
        {
            _installButton.Enabled = false;
            _status.ForeColor = System.Drawing.Color.Black;
            _status.Text = "Установка...";
            try
            {
                if (!Directory.Exists(TargetDir))
                {
                    Directory.CreateDirectory(TargetDir);
                }
                string[] files = Files;
                for (int i = 0; i < files.Length; i++)
                {
                    using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(files[i]))
                    {
                        if (stream == null)
                        {
                            throw new FileNotFoundException("Ресурс не найден в установщике: " + files[i]);
                        }
                        string path = Path.Combine(TargetDir, files[i]);
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                        using (FileStream fileStream = File.Create(path))
                        {
                            stream.CopyTo(fileStream);
                        }
                    }
                }
                _status.ForeColor = System.Drawing.Color.FromArgb(0, 120, 60);
                _status.Text = "Готово! Запустите Revit 2021 — вкладка «Цветные листики».";
                _installButton.Text = "Закрыть";
                _installButton.Enabled = true;
                _done = true;
            }
            catch (Exception ex)
            {
                _status.ForeColor = System.Drawing.Color.FromArgb(190, 30, 30);
                _status.Text = "Ошибка установки.";
                MessageBox.Show("Не удалось скопировать файлы в:\r\n" + TargetDir + "\r\n\r\n" +
                    "Убедитесь, что Revit закрыт, и повторите установку.\r\n\r\nДетали: " + ex.Message,
                    "Color Leaves — ошибка установки", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                _installButton.Enabled = true;
            }
        }
    }
}
