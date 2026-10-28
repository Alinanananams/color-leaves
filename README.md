# Color Leaves

C#-плагин для **Autodesk Revit 2021**, автоматизирующий раскраску листов по сборкам.

Плагин добавляет на ленту Revit вкладку **«Цветные листики»** с панелью **«Листы»**.

## Возможности

- 🎨 Раскраска листов по сборкам (`AssemblyInstance`)
- 📄 Группировка и сортировка листов
- ⚙️ Сохранение настроек между запусками

> Раздел в развитии — список возможностей будет дополняться по мере реализации модулей.

## Требования

- Autodesk Revit **2021**
- Windows 10/11 x64
- [.NET SDK 8.0](https://dotnet.microsoft.com/download) — только для сборки из исходников

## Установка

**Способ 1 — из исходников (для разработчиков):**

```sh
git clone https://github.com/Alinanananams/color-leaves.git
cd color-leaves
build-and-deploy.bat
```

Скрипт соберёт плагин и установит его в Revit:
- DLL — в `%APPDATA%\Autodesk\Revit\Addins\2021\`
- манифест `ColorLeaves.addin` — там же

**Способ 2 — вручную:**

Соберите проект (см. ниже) и скопируйте `ColorLeaves.Revit.dll` и `addins/2021/ColorLeaves.addin` в `%APPDATA%\Autodesk\Revit\Addins\2021\`.

## Сборка

Плагин ссылается на Revit API через NuGet-пакеты [`Nice3point.Revit.Api.*`](https://www.nuget.org/packages/Nice3point.Revit.Api), поэтому локально установленный Revit для сборки **не требуется**.

```sh
dotnet build build/ColorLeaves.sln -c Debug
```

Выходные файлы — в `bin/Debug/2021/`.

Быстрая сборка и установка в Revit: `build-and-deploy.bat`.

## Отладка

1. Выполните `build-and-deploy.bat`
2. Запустите Revit 2021 и откройте любую команду плагина с вкладки «Цветные листики»
3. В Visual Studio: **Debug → Attach to Process → `Revit.exe`**

## Структура проекта

```
color-leaves
├── src/
│   ├── ColorLeaves.Core/        # Общие модели, константы, утилиты
│   └── ColorLeaves.Revit/       # Точка входа, команды, сервисы, UI
├── build/ColorLeaves.sln        # Решение Visual Studio
├── addins/2021/                 # Манифест .addin для Revit 2021
├── DOCS/                        # Планы фич
├── AGENTS.md                    # Инструкции для AI-ассистентов
├── build-and-deploy.bat         # Сборка и установка в Revit
└── Version.txt                  # Версия плагина (единый источник)
```

## Важно

- `RevitAPI.dll` и `RevitAPIUI.dll` не входят в репозиторий — приходят через NuGet
- Для коммерческого использования замените `VendorId` в `addins/2021/ColorLeaves.addin`
- `AddInId` задан уникальным GUID. При конфликте с другим плагином замените его
