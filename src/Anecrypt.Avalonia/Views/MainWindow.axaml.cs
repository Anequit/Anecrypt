using Anecrypt.Avalonia.ViewModels;
using Avalonia.Input;
using System.Linq;
using System.Collections.Generic;
using Avalonia.Platform.Storage;
using Avalonia.Controls;

namespace Anecrypt.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        FilePathGrid.AddHandler(DragDrop.DropEvent, Drop);
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        IEnumerable<IStorageItem>? files = e.Data.GetFiles();

        if (files is null)
            return;

        (DataContext as MainWindowViewModel)!.DropCommand.Execute(files.First());
    }
}