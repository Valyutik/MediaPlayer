using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using File = TagLib.File;

namespace MediaPlayer;

public partial class ClientMainWindow
{
    public string UrlFile = string.Empty;
    private readonly DoubleAnimation _controlPanelAnimation;
    private readonly List<ItemPopupBox> _files = new();
    private readonly DispatcherTimer _timer = new();
    private readonly YoutubeClient _client;
    private string _allTime= string.Empty;
    private double _currentVolume = 1;
    private int _currentFile;
    private bool _isFullscreen;
    private bool _isMute;
    private bool _isPlay;
    private bool _fileUploaded;
    private bool _isFileMenuOpen;

    public ClientMainWindow()
    {
        InitializeComponent();
        _timer.Interval = new TimeSpan(200);
        _timer.Tick += Timer_Tick;
        _client = new YoutubeClient();
        _controlPanelAnimation = new DoubleAnimation
        {
            Duration = TimeSpan.FromSeconds(0.2f),
            EasingFunction = new QuadraticEase(),
        };
    }
    private void MainWindowPlayer_Closed(object sender, EventArgs e) => _timer.Tick -= Timer_Tick;
    private void SelectFile(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "MP4 files (*.mp4)|*.mp4|MP3 files (*.mp3)|*.mp3|All files()*.*|*.*",
            Multiselect = true,
        };
        if (openFileDialog.ShowDialog() != true) return;
        foreach (var file in openFileDialog.FileNames)
        {
            var fileExtension = Path.GetExtension(file);
            if (fileExtension is ".mp4" or ".avi" or ".mkv" or ".mp3" or ".flac" or ".wav")
            {
                InitializeItemPopupBox(file, Path.GetFileName(file), false);
            }
            else
                MessageBox.Show($"Данный формат не поддерживается: {fileExtension}");
        }
        SetLastFile();
    }
    private async void LoadUrlVideo()
    {
        try
        {
            var video = await _client.Videos.GetAsync(UrlFile);
            var streamManifest = await _client.Videos.Streams.GetManifestAsync(UrlFile);
            var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();
            InitializeItemPopupBox(streamInfo.Url, video.Title, true);
            SetLastFile();
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message);
        }
    }
    private void InitializeItemPopupBox(string filePath, string fileName, bool isUrl)
    {
        var itemPopupBox = new ItemPopupBox(filePath, fileName, isUrl, PopupBoxGrid);
        _files.Add(itemPopupBox);
        itemPopupBox.OnFileNameDoubleClick += FileNameOnMouseDoubleClick;
        itemPopupBox.OnButtonDoubleClick += ItemPopupBoxOnOnButtonDoubleClick;
    }

    private void ItemPopupBoxOnOnButtonDoubleClick(ItemPopupBox itemPopupBox)
    {
        if (_currentFile == _files.IndexOf(itemPopupBox))
        {
            _files.Remove(itemPopupBox);
            itemPopupBox.RemoveItemPopupBox();
            if (_files.Count != 0)
            {
                if (_currentFile - 1 >= 0)
                {
                    PreviousFile();
                }
                else
                {
                    _currentFile--;
                    NextFile();
                }
            }
            else
            {
                MediaElement.Source = null;
                ImageForMp3.Source = null;
                ClientMainWindowPlayer.Background = Brushes.White;
                _isFileMenuOpen = true;
                MenuButton_OnClick(this, null!);
                ShowControlPanels();
                _fileUploaded = false;
            }
        }
        else
        {
            _files.Remove(itemPopupBox);
            itemPopupBox.RemoveItemPopupBox();
            _currentFile--;
        }
    }

    private void FileNameOnMouseDoubleClick(ItemPopupBox itemPopupBox)
    {
        _currentFile = _files.IndexOf(itemPopupBox);
        SetCurrentFile();
        _isFileMenuOpen = true;
        MenuButton_OnClick(this, null!);
    }
    private void SetCurrentFile()
    {
        if (_currentFile < 0 || _currentFile >= _files.Count)
            return;
        MediaElement.Source = new Uri(_files[_currentFile].FilePath);
        ClientMainWindowPlayer.Background = Brushes.Black;
        FileNameTextBlock.Text = _files[_currentFile].FileName;
        SetImageFromMp3();
        PlayerPlay();
    }
    private void SetImageFromMp3()
    {
        if (_files[_currentFile].IsUrl)
            return;
        var tagFile = File.Create(_files[_currentFile].FilePath);
        var firstPicture = tagFile.Tag.Pictures.FirstOrDefault();
        if (firstPicture != null)
        {
            var ms = new MemoryStream(firstPicture.Data.Data);
            ms.Seek(0, SeekOrigin.Begin);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = ms;
            bitmap.EndInit();
            ImageForMp3.Source = bitmap;
        }
        else
        {
            ImageForMp3.Source = null;
        }
    }
    private void SetLastFile()
    {
        _currentFile = _files.Count - 1;
        SetCurrentFile();
        HideControlPanels();
        PlayerPlay();
    }
    private async void PlayerPlay()
    {
        MediaElement.Play();
        await Task.Delay(200);
        _timer.Start();
        _isPlay = true;
        ((PackIcon)PlayButton.Content).Kind = PackIconKind.Pause;
    }

    private void PlayerPause()
    {
        MediaElement.Pause();
        _timer.Stop();
        _isPlay = false;
        ((PackIcon)PlayButton.Content).Kind = PackIconKind.Play;
    }

    private void NextFile()
    {
        if (_currentFile >= _files.Count - 1)
            return;
        _currentFile++;
        SetCurrentFile();
    }

    private void PreviousFile()
    {
        if (_currentFile <= 0)
            return;
        _currentFile--;
        SetCurrentFile();
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isPlay)
        {
            PlayerPlay();
        }
        else
        {
            PlayerPause();
        }
    }

    private void Slider_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        MediaElement.Position = TimeSpan.FromSeconds(TimeLineSlider.Value);
        PlayerPlay();
    }

    private void Slider_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => PlayerPause();

    private void Timer_Tick(object? sender, EventArgs e)
    {
        TimeLineSlider.Value = MediaElement.Position.TotalSeconds;
        var ts= MediaElement.Position;
        TimeLabel.Content = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}" + $" / {_allTime}";
    }

    private void MediaPlayer_MediaOpened(object? sender, EventArgs e)
    {
        TimeLineSlider.Maximum = MediaElement.NaturalDuration.TimeSpan.TotalSeconds;
        var ts = MediaElement.NaturalDuration.TimeSpan;
        _allTime = string.Empty;
        _allTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}" + $"{_allTime}";
        TimeLabel.Content = $"00:00:00 / {_allTime}";
    }

    private void VolumeSlide_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        MediaElement.IsMuted = false;
        MediaElement.Volume = VolumeSlider.Value;
        if (VolumeSlider.Value == 0)
            VolumeIcon.Kind = PackIconKind.VolumeOff;
        else if (Math.Abs(VolumeSlider.Value - 1) < 0.01f)
            VolumeIcon.Kind = PackIconKind.VolumeHigh;
        else
            VolumeIcon.Kind = VolumeSlider.Value switch
            {
                > 0 and < 0.5f => PackIconKind.VolumeLow,
                > 0.5f and < 1 => PackIconKind.VolumeMedium,
                _ => VolumeIcon.Kind
            };
            
    }

    private void NextButton_Click(object sender, RoutedEventArgs e) => NextFile();

    private void PrevButton_Click(object sender, RoutedEventArgs e) => PreviousFile();

    private void MediaElement_MediaEnded(object sender, RoutedEventArgs e) => NextFile();
        
    private void OnOffFullscreen(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not Button button) return;
        var buttonContent = button.Content as PackIcon;
        if (!_isFullscreen)
        {
            WindowState = WindowState.Maximized;
            Visibility = Visibility.Collapsed;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Visibility = Visibility.Visible;
            Activate();
            _isFullscreen = !_isFullscreen;
            buttonContent!.Kind = PackIconKind.FullscreenExit;
        }
        else
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            _isFullscreen = !_isFullscreen;
            buttonContent!.Kind = PackIconKind.Fullscreen;
        }
    }

    private void OpenUrlFileButton_OnClick(object sender, RoutedEventArgs e)
    {
        var enterUrlWindow = new EnterUrlWindow(this);
        enterUrlWindow.Show();
        enterUrlWindow.OnAddButtonClick += LoadUrlVideo;
    }

    private void MediaElement_OnBufferingStarted(object sender, RoutedEventArgs e)
    {
        MainProgressBar.Width = 50;
        MainProgressBar.Height = 50;
    }

    private void MediaElement_OnBufferingEnded(object sender, RoutedEventArgs e)
    {
        MainProgressBar.Width = 0;
        MainProgressBar.Height = 0;
    }
    
    private void VolumeButtonClick(object sender, RoutedEventArgs e)
    {
        if (!MediaElement.IsMuted)
        {
            if (MediaElement.Volume != 0)
            {
                _currentVolume = MediaElement.Volume;
            }
            _isMute = true;
            VolumeSlider.Value = 0;
            VolumeIcon.Kind = PackIconKind.VolumeVariantOff;
        }
        else
        {
            _isMute = false;
            VolumeSlider.Value = _currentVolume;
        }
        MediaElement.IsMuted = _isMute;
    }

    private void MainWindow_OnKeyDown(object sender, KeyEventArgs e)
    {
        switch (e)
        {
            case {Key: Key.A}:
                SelectFile(sender, e);
                break;
            case {Key: Key.Space}:
                PlayButton_Click(sender, e);
                break;
            case {Key: Key.W}:
                OpenUrlFileButton_OnClick(sender, null!);
                break;
            case {Key: Key.Left}:
                MediaElement.Position -= TimeSpan.FromSeconds(10);
                break;
            case {Key: Key.Right}:
                MediaElement.Position += TimeSpan.FromSeconds(10);
                break;
            case {Key: Key.Up}:
                VolumeSlider.Value += 0.1f;
                break;
            case {Key: Key.Down}:
                VolumeSlider.Value -= 0.1f;
                break;
            case {Key: Key.F}:
                OnOffFullscreen(sender, new RoutedEventArgs(ButtonBase.ClickEvent, FullscreenButton));
                break;
            case {Key: Key.M}:
                VolumeButtonClick(sender, null!);
                break;
            case {Key: Key.OemComma}:
                PreviousFile();
                break;
            case {Key: Key.OemPeriod}:
                NextFile();
                break;
            case {Key: Key.Escape}:
                _isFullscreen = true;
                OnOffFullscreen(sender, new RoutedEventArgs(ButtonBase.ClickEvent, FullscreenButton));
                break;
        }
    }

    private void HideControlPanels()
    {
        _fileUploaded = true;
        HideTopControlPanel();
        HideBottomControlPanel();
    }
    private void ShowControlPanels()
    {
        _fileUploaded = true;
        ShowTopControlPanel();
        ShowBottomControlPanel();
    }

    private void ShowBottomControlPanel()
    {
        if (!_fileUploaded) return;
        _controlPanelAnimation.From = BottomControlPanel.ActualHeight;
        _controlPanelAnimation.To = BottomControlPanel.MaxHeight;
        BottomControlPanel.BeginAnimation(HeightProperty, _controlPanelAnimation);
    }

    private void ShowTopControlPanel()
    {
        if (!_fileUploaded) return;
        _controlPanelAnimation.From = TopControlPanel.ActualHeight;
        _controlPanelAnimation.To = TopControlPanel.MaxHeight;
        TopControlPanel.BeginAnimation(HeightProperty, _controlPanelAnimation);
    }

    private void HideTopControlPanel()
    {
        if (!_fileUploaded) return;
        if (_isFileMenuOpen) return;
        _controlPanelAnimation.From = TopControlPanel.ActualHeight;
        _controlPanelAnimation.To = TopControlPanel.MinHeight;
        TopControlPanel.BeginAnimation(HeightProperty, _controlPanelAnimation);
    }

    private void HideBottomControlPanel()
    {
        if (!_fileUploaded) return;
        _controlPanelAnimation.From = BottomControlPanel.ActualHeight;
        _controlPanelAnimation.To = BottomControlPanel.MinHeight;
        BottomControlPanel.BeginAnimation(HeightProperty, _controlPanelAnimation);
    }

    private void TopControlPanel_OnMouseEnter(object sender, MouseEventArgs e) => ShowTopControlPanel();

    private void TopControlPanel_OnMouseLeave(object sender, MouseEventArgs e) => HideTopControlPanel();

    private void BottomControlPanel_OnMouseEnter(object sender, MouseEventArgs e) => ShowBottomControlPanel();

    private void BottomControlPanel_OnMouseLeave(object sender, MouseEventArgs e) => HideBottomControlPanel();

    private void MenuButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_isFileMenuOpen)
        {
            _isFileMenuOpen = true;
            _controlPanelAnimation.From = 0;
            _controlPanelAnimation.To = 1;
            FilesBorder.BeginAnimation(OpacityProperty, _controlPanelAnimation);
            ((PackIcon)MenuButton.Content).Kind = PackIconKind.MenuDown;
        }
        else
        {
            _isFileMenuOpen = false;
            _controlPanelAnimation.From = 1;
            _controlPanelAnimation.To = 0;
            FilesBorder.BeginAnimation(OpacityProperty, _controlPanelAnimation);
            ((PackIcon)MenuButton.Content).Kind = PackIconKind.Menu;
        }
    }

    private void MediaElement_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_isFileMenuOpen)
        {
            MenuButton_OnClick(sender, null!);
        }
    }
}