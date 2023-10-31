using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace MediaPlayer;

public class ItemPopupBox
{
    public event Action<ItemPopupBox>? OnFileNameDoubleClick;
    public event Action<ItemPopupBox>? OnButtonDoubleClick;
    public readonly string FilePath;
    public readonly string FileName;
    public readonly bool IsUrl;
    private readonly Button _button = new()
    {
        VerticalAlignment = VerticalAlignment.Center,
        HorizontalAlignment= HorizontalAlignment.Right,
        Content = new PackIcon()
        {
            Kind= PackIconKind.CloseOutline, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center
        },
        Focusable = false, Style = (Style)Application.Current.Resources["MaterialDesignFlatButton"]
    };
    private readonly Label _label = new()
    {
        FontSize = 18,
        VerticalAlignment = VerticalAlignment.Center,
        HorizontalAlignment = HorizontalAlignment.Left,
        HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center,
        BorderThickness = new Thickness(0,0,0,1), BorderBrush = Brushes.Gray
    };
    private readonly Grid _grid;

    public ItemPopupBox(string filePath,string fileName, bool isUrl, Grid grid)
    {
        FilePath = filePath;
        FileName = fileName;
        IsUrl = isUrl;
        _grid = grid;
        grid.RowDefinitions.Add(new RowDefinition());
        _label.Content = FileName;
        var idGridRow = grid.RowDefinitions.Count - 2;
        Grid.SetRow(_label, idGridRow + 1);
        Grid.SetRow(_button, idGridRow + 1);
        Grid.SetColumn(_label, 0);
        Grid.SetColumn(_button, grid.ColumnDefinitions.Count - 1);
        grid.Children.Add(_label);
        grid.Children.Add(_button);
        _label.MouseDoubleClick += (_, _) => OnFileNameDoubleClick?.Invoke(this);
        _button.MouseDoubleClick += (_, _) => OnButtonDoubleClick?.Invoke(this);
    }

    public void RemoveItemPopupBox()
    {
        _grid.Children.Remove(this._button);
        _grid.Children.Remove(this._label);
    }
}