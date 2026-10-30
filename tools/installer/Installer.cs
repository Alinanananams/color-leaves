// Color Leaves — простой установщик плагина для Autodesk Revit 2021.
// Файлы плагина зашиты в exe как ресурсы.
// Раскладка: в Addins\2021\ только ColorLeaves.addin, остальные файлы — в папке ColorLeaves\.
// Поддерживает установку, удаление и тихие режимы /silent и /uninstall.
// Внимание: файл компилируется csc.exe (C# 5) — без интерполяции строк и новых возможностей языка.
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace ColorLeaves.Installer
{
    internal static class Program
    {
        private static Label _status;

        private static Button _installButton;

        private static Button _uninstallButton;

        // Корневая папка надстроек Revit 2021: в ней лежит только манифест .addin
        private static readonly string TargetDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Autodesk", "Revit", "Addins", "2021");

        // Папка с файлами плагина (DLL и иконки) внутри Addins
        private static readonly string LibDir = Path.Combine(TargetDir, "ColorLeaves");

        // Файлы плагина, зашитые в установщик как ресурсы
        private static readonly string[] LibFiles =
        {
            "ColorLeaves.Revit.dll",
            "ColorLeaves.Core.dll",
            "sheet.png",
            "refresh.png",
            "colors.png",
            "parameter.png"
        };

        // Файлы старой плоской раскладки (до версии 1.4.1) в корне Addins — вычищаются
        private static readonly string[] LegacyFiles =
        {
            "ColorLeaves.Revit.dll",
            "ColorLeaves.Core.dll",
            "sheet.png",
            "refresh.png",
            "colors.png",
            "parameter.png"
        };

        [STAThread]
        private static void Main(string[] args)
        {
            foreach (string arg in args)
            {
                if (string.Equals(arg, "/uninstall", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "-uninstall", StringComparison.OrdinalIgnoreCase))
                {
                    Environment.Exit(UninstallSilently());
                    return;
                }
                if (string.Equals(arg, "/silent", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "-silent", StringComparison.OrdinalIgnoreCase))
                {
                    Environment.Exit(InstallSilently());
                    return;
                }
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(BuildForm());
        }

        private static int InstallSilently()
        {
            try
            {
                if (IsRevitRunning())
                {
                    return 2;
                }
                Install();
                return 0;
            }
            catch
            {
                return 1;
            }
        }

        private static int UninstallSilently()
        {
            try
            {
                if (IsRevitRunning())
                {
                    return 2;
                }
                Uninstall();
                return 0;
            }
            catch
            {
                return 1;
            }
        }

        /// <summary>Версия из зашитого Version.txt (пустая строка, если ресурса нет).</summary>
        private static string ReadVersion()
        {
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Version.txt"))
                {
                    if (stream == null)
                    {
                        return "";
                    }
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd().Trim();
                    }
                }
            }
            catch
            {
                return "";
            }
        }

        private static bool IsRevitRunning()
        {
            return Process.GetProcessesByName("Revit").Length > 0;
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

            string version = ReadVersion();
            Label title = new Label();
            title.Text = (version.Length > 0) ? ("Color Leaves v" + version) : "Color Leaves";
            title.Font = new System.Drawing.Font("Segoe UI", 14f, System.Drawing.FontStyle.Bold);
            title.Location = new System.Drawing.Point(20, 15);
            title.AutoSize = true;
            form.Controls.Add(title);

            Label info = new Label();
            info.Text = "Плагин стадий готовности листов для Autodesk Revit 2021.\r\n\r\n" +
                "Устанавливается в:\r\n%APPDATA%\\Autodesk\\Revit\\Addins\\2021\\\r\n" +
                "(манифест .addin в корне, файлы плагина — в папке ColorLeaves\\)\r\n\r\n" +
                "Перед установкой или удалением закройте Revit.";
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
            _installButton.Size = new System.Drawing.Size(150, 32);
            _installButton.Click += OnInstallClick;
            form.Controls.Add(_installButton);

            _uninstallButton = new Button();
            _uninstallButton.Text = "Удалить";
            _uninstallButton.Location = new System.Drawing.Point(190, 185);
            _uninstallButton.Size = new System.Drawing.Size(150, 32);
            _uninstallButton.Click += OnUninstallClick;
            form.Controls.Add(_uninstallButton);

            return form;
        }

        private static bool _installDone;

        private static bool _uninstallDone;

        private static void OnInstallClick(object sender, EventArgs e)
        {
            if (_installDone)
            {
                Application.Exit();
                return;
            }
            InstallWithUi();
        }

        private static void OnUninstallClick(object sender, EventArgs e)
        {
            if (_uninstallDone)
            {
                Application.Exit();
                return;
            }
            UninstallWithUi();
        }

        private static void Install()
        {
            if (IsRevitRunning())
            {
                throw new InvalidOperationException("Autodesk Revit запущен — закройте его и повторите.");
            }
            if (!Directory.Exists(LibDir))
            {
                Directory.CreateDirectory(LibDir);
            }
            // Манифест — в корень Addins, файлы плагина — в папку ColorLeaves\
            ExtractResource("ColorLeaves.addin", TargetDir);
            string[] files = LibFiles;
            for (int i = 0; i < files.Length; i++)
            {
                ExtractResource(files[i], LibDir);
            }
            // Миграция со старой плоской раскладки: чистим корень Addins
            DeleteLegacyFiles();
        }

        private static void ExtractResource(string resourceName, string targetDir)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException("Ресурс не найден в установщике: " + resourceName);
                }
                string path = Path.Combine(targetDir, resourceName);
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

        private static void DeleteLegacyFiles()
        {
            string[] files = LegacyFiles;
            for (int i = 0; i < files.Length; i++)
            {
                string path = Path.Combine(TargetDir, files[i]);
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch
                {
                    // Не критично: лишний файл в корне не мешает работе плагина
                }
            }
        }

        private static void Uninstall()
        {
            if (IsRevitRunning())
            {
                throw new InvalidOperationException("Autodesk Revit запущен — закройте его и повторите.");
            }
            // Манифест, папка плагина, остатки старой плоской раскладки.
            // Настройки стадий (%APPDATA%\Autodesk\Revit\ColorSheets\) не трогаем —
            // при повторной установке конфигурация сохранится.
            string addinPath = Path.Combine(TargetDir, "ColorLeaves.addin");
            try
            {
                if (File.Exists(addinPath))
                {
                    File.Delete(addinPath);
                }
            }
            catch
            {
            }
            try
            {
                if (Directory.Exists(LibDir))
                {
                    Directory.Delete(LibDir, true);
                }
            }
            catch
            {
            }
            DeleteLegacyFiles();
        }

        private static void InstallWithUi()
        {
            _installButton.Enabled = false;
            _uninstallButton.Enabled = false;
            _status.ForeColor = System.Drawing.Color.Black;
            _status.Text = "Установка...";
            try
            {
                Install();
                _status.ForeColor = System.Drawing.Color.FromArgb(0, 120, 60);
                _status.Text = "Готово! Запустите Revit 2021 — вкладка «Цветные листики».";
                _installButton.Text = "Закрыть";
                _installButton.Enabled = true;
                _installDone = true;
            }
            catch (Exception ex)
            {
                _status.ForeColor = System.Drawing.Color.FromArgb(190, 30, 30);
                _status.Text = "Ошибка установки.";
                MessageBox.Show("Не удалось скопировать файлы в:\r\n" + TargetDir + "\r\n\r\n" +
                    "Убедитесь, что Revit закрыт, и повторите установку.\r\n\r\nДетали: " + ex.Message,
                    "Color Leaves — ошибка установки", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                _installButton.Enabled = true;
                _uninstallButton.Enabled = true;
            }
        }

        private static void UninstallWithUi()
        {
            DialogResult confirm = MessageBox.Show(
                "Удалить плагин Color Leaves из Revit 2021?\r\n\r\n" +
                "Настройки стадий сохранятся — при повторной установке они подтянутся.",
                "Color Leaves — удаление", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
            {
                return;
            }
            _installButton.Enabled = false;
            _uninstallButton.Enabled = false;
            _status.ForeColor = System.Drawing.Color.Black;
            _status.Text = "Удаление...";
            try
            {
                Uninstall();
                _status.ForeColor = System.Drawing.Color.FromArgb(0, 120, 60);
                _status.Text = "Плагин удалён. Настройки стадий сохранены.";
                _uninstallButton.Text = "Закрыть";
                _uninstallButton.Enabled = true;
                _uninstallDone = true;
            }
            catch (Exception ex)
            {
                _status.ForeColor = System.Drawing.Color.FromArgb(190, 30, 30);
                _status.Text = "Ошибка удаления.";
                MessageBox.Show("Не удалось удалить файлы из:\r\n" + TargetDir + "\r\n\r\n" +
                    "Убедитесь, что Revit закрыт, и повторите удаление.\r\n\r\nДетали: " + ex.Message,
                    "Color Leaves — ошибка удаления", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                _installButton.Enabled = true;
                _uninstallButton.Enabled = true;
            }
        }
    }
}
