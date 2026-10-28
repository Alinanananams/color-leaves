using Autodesk.Revit.DB;

namespace ColorLeaves.Revit.Services;

public class SheetColorService
{
    private readonly Document _document;

    public SheetColorService(Document document)
    {
        _document = document;
    }

    public void ProcessSheets()
    {
        // Логика обработки листов: фильтрация по сборкам, назначение цветов
        // Здесь будет основная бизнес-логика плагина
    }
}
