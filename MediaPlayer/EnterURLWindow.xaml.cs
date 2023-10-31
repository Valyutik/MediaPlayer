using System;
using System.Windows;

namespace MediaPlayer;

public partial class EnterUrlWindow
{
    public event Action? OnAddButtonClick;
    private readonly ClientMainWindow _mainWindow;
    public EnterUrlWindow(ClientMainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
    }

    private void AddButtonClick(object sender, RoutedEventArgs e)
    {
        _mainWindow.UrlFile = MainTextBox.Text;
        OnAddButtonClick?.Invoke();
        this.Close();
    }
}