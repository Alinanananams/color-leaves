using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ColorLeaves.Core.Models;

namespace ColorLeaves.Revit.UI.ViewModels;

public class MainViewModel
{
    public ObservableCollection<SheetInfo> Sheets { get; } = new();

    public ICommand ApplyColorsCommand { get; }

    public MainViewModel()
    {
        ApplyColorsCommand = new RelayCommand(_ => ExecuteApply());
    }

    private void ExecuteApply()
    {
        // Применение цветов к листам
    }
}

public class RelayCommand : ICommand
{
    private readonly Action<object> _execute;

    public RelayCommand(Action<object> execute)
    {
        _execute = execute;
    }

    public bool CanExecute(object parameter) => true;

    public event EventHandler CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public void Execute(object parameter) => _execute(parameter);
}
