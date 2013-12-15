using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using System.Windows.Threading;
using System.IO;

namespace WpfMediaPlayer
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    /// 

    public enum PlayerState
    {
        isPlaying, isPaused, isStopped, isStream
    }

    public partial class MainWindow : Window
    {
        //フラグ
        bool _isRepeat = false;
        bool _isDrag = false;
        bool _autoResize = true;
        PlayerState CurrentState = PlayerState.isStopped;

        DispatcherTimer Timer;
        DispatcherTimer BufferTimer;
        DispatcherTimer TipTextClearTimer;
        DoubleAnimation fadeautoAnime;
        string _fileName = "";
        double volumeTemp;

        double Volume
        {
            get
            {
                return MainPlayer.Volume;
            }

            set
            {
                if (value >= 1)
                {
                    MainPlayer.Volume = 1;
                }
                else if (value <= 0)
                {
                    MainPlayer.Volume = 0;
                    VolumeButtonImage.Source = new BitmapImage(new Uri("image/mute.png", UriKind.Relative));
                }
                else
                {
                    MainPlayer.Volume = value;
                    VolumeButtonImage.Source = new BitmapImage(new Uri("image/volume_button.png", UriKind.Relative));
                }

                VolumeBar.Width = VolumeBarControl.Width * Volume;
                TipText = String.Format("Volume:{0:00}", Volume * 100);
            }
        }

        string TipText
        {
            get
            {
                return TipTextBlock.Text;
            }
            
            set
            {
                TipTextBlock.Text = value;
                ShowTip();
            }
        }

        #region Constructor

        //コンストラクタ
        public MainWindow()
        {
            InitializeComponent();

            //マウス操作
            //ドラッグでウィンドウ移動
            this.MouseLeftButtonDown += (sender, e) => { if (!_isDrag) { this.DragMove(); } };

            //ダブルクリックで最大化
            this.MouseDoubleClick += (s, e) =>
            {
                if (this.WindowState == WindowState.Normal)
                {
                    this.WindowState = WindowState.Maximized;
                    TipText = "Maximized";
                }
                else
                {
                    this.WindowState = WindowState.Normal;
                    TipText = "Normal";
                }
            };

            //マウスホイールでボリュームを変化させる
            this.MouseWheel += (s, e) =>
            {
                if (e.Delta > 0)
                {
                    Volume += 0.02;
                }
                else
                {
                    Volume -= 0.02;
                }
            };

            //マウスが画面に載ったらパネルを表示
            this.MouseEnter += (s, e) =>
            {
                if (fadeautoAnime == null)
                {
                    GridControlPanel.Visibility = Visibility.Visible;
                }
            };

            //マウスが画面から離れたらパネルを非表示
            this.MouseLeave += (s, e) =>
            {
                PanelFadeout();
            };

            //キー操作
            //キーダウンでウィンドウを最前面化
            this.KeyDown += (s, e) =>
            {
                if (this.Topmost == false)
                {
                    this.Topmost = true;
                    TipText = "Topmost";
                }
                else
                {
                    this.Topmost = false;
                    TipText = "Normal";
                }
            };

            //シークバーと再生時間を動かすDispatherTimerをセット
            Timer = new DispatcherTimer();
            Timer.Tick += TimerEvent;
            Timer.Interval = new TimeSpan(0, 0, 1);

            //ボリュームをセット
            MainPlayer.Volume = Properties.Settings.Default.Volume;

            //autoResizeの設定
            _autoResize = Properties.Settings.Default.AutoResize;
            MenuItemAutoResize.IsChecked = _autoResize;

            //コマンドライン引数を確認
            if (App.CommandLineArgs != null)
            {
                _fileName = App.CommandLineArgs[0];

                Play();
            }
        }

        #endregion

        #region Events

        private void Window_Closed(object sender, EventArgs e)
        {
            Stop();

            Properties.Settings.Default.AutoResize = _autoResize;
            Properties.Settings.Default.Volume = Volume;

            Properties.Settings.Default.Save();
        }

        #endregion

        #region Commands

        //ファイルを開く
        void DoOpenCommand(object sender, RoutedEventArgs e)
        {
            //OpenFileDialogの設定
            var openDlg = new OpenFileDialog();
            openDlg.DefaultExt = "mp4; flv; wmv; avi";
            openDlg.Filter = "Movie File (*.mp4; *.flv; *.wmv; *.avi;)|*.mp4; *.flv; *.wmv; *.avi| all file (*.*)|*.*";

            //Dialogを開く
            Nullable<bool> result = openDlg.ShowDialog();
            if (result == false) { return; }

            if (CurrentState != PlayerState.isStopped) { Stop(); }

            //ファイル名を保存
            _fileName = openDlg.FileName;

            //自動再生
            Play();
        }

        //ファイルもしくはURLを開く
        void DoOpenURLCommand(object sender, RoutedEventArgs e)
        {
            //OpenFileDialogの設定
            var openURLDlg = new OpenURLDialog();

            //Dialogを開く
            Nullable<bool> result = openURLDlg.ShowDialog();

            //ファイル名を保存
            _fileName = openURLDlg.FileName;

            //自動再生
            Play();
        }

        //ウィンドウを閉じる
        void DoCloseCommand(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        //再生コマンド
        void DoPlayCommand(object sender, RoutedEventArgs e)
        {
            if (_fileName == "") { return; }

            switch (CurrentState)
            {
                case PlayerState.isPaused:
                    Resume();
                    break;

                case PlayerState.isPlaying:
                    Pause(); ;
                    break;

                case PlayerState.isStopped:
                    Play();
                    break;
            }
        }

        //一時停止
        void DoPauseCommand(object sender, RoutedEventArgs e)
        {
            Pause();
        }

        //ストップコマンド
        void DoStopCommand(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        //リピートコマンド
        void DoRepeatCommand(object sender, RoutedEventArgs e)
        {
            Repeat();
        }

        //ウィンドウサイズを調整
        void DoSetHalfNaturalVideoHeightWidthCommand(object sender, RoutedEventArgs e)
        {
            Resize(0.5);
        }

        void DoSetNaturalVideoHeightWidthCommand(object sender, RoutedEventArgs e)
        {
            Resize();
        }

        void DoSetOneAndHerfNaturalVideoHeightWidthCommand(object sender, RoutedEventArgs e)
        {
            Resize(1.5);
        }

        void DoSetDoubledNaturalVideoHeightWidthCommand(object sender, RoutedEventArgs e)
        {
            Resize(2);
        }

        //メディアを開いたら自動的にウィンドウサイズを更新
        void DoAutoResizeCommand(object sender, RoutedEventArgs e)
        {
            MenuItem item = (MenuItem)sender;

            if (_autoResize)
            {
                _autoResize = false;
                item.IsChecked = false;
            }
            else
            {
                _autoResize = true;
                item.IsChecked = true;
            }
        }

        #endregion

        #region MediaElement Events

        //動画読み込み時の処理
        private void MainPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (MainPlayer.NaturalDuration != Duration.Automatic)
            {
                //コントロールの有効化
                PlayButton.IsEnabled = true;
                RepeatButton.IsEnabled = true;
                SeekBarControl.IsEnabled = true;

                

                if (_autoResize) { Resize(); }

                //再生時間を書き込む
                NaturalDurationText.Text = String.Format("{0:00}:{1:00}:{2:00}", MainPlayer.NaturalDuration.TimeSpan.Hours,
                                                                                 MainPlayer.NaturalDuration.TimeSpan.Minutes,
                                                                                 MainPlayer.NaturalDuration.TimeSpan.Seconds);

                Timer.Start();
            }
        }

        //動画終了時の処理
        private void MainPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            Timer.Stop();
            Stop();

            if (_isRepeat) { Play(); }
        }

        //バッファ開始時の処理
        private void MainPlayer_BufferingStarted(object sender, RoutedEventArgs e)
        {
            InfoTextBlock.Text = "Buffring...";

            BufferTimer = new DispatcherTimer();
            BufferTimer.Tick += (s, ev) => { TipText = String.Format("{0}%", MainPlayer.BufferingProgress * 100).ToString(); };
            BufferTimer.Interval = new TimeSpan(0, 0, 1);

            BufferTimer.Start();
            CurrentState = PlayerState.isStream;
        }

        //バッファ終了時の処理
        private void MainPlayer_BufferingEnded(object sender, RoutedEventArgs e)
        {
            BufferTimer.Stop();
            InfoTextBlock.Text = "Conectiong...";
        }

        #endregion

        #region Methods

        //再生
        public void Play()
        {
            if (_fileName == "") { return; }

            InfoTextBlock.Text = System.IO.Path.GetFileNameWithoutExtension(_fileName);
            this.Title = System.IO.Path.GetFileNameWithoutExtension(_fileName);

            MainPlayer.Source = new Uri(_fileName);
            MainPlayer.Play();

            PlayButtonImage.Source = new BitmapImage(new Uri("image/pause_button.png", UriKind.Relative));
            CurrentState = PlayerState.isPlaying;           
        }

        //停止
        public void Stop()
        {
            if (CurrentState == PlayerState.isStopped) { return; }

            SeekBarControl.IsEnabled = false;

            Timer.Stop();
            MainPlayer.Stop();
            MainPlayer.Close();

            PlayButtonImage.Source = new BitmapImage(new Uri("image/play_button.png", UriKind.Relative));

            if (CurrentState == PlayerState.isStream) { InfoTextBlock.Text = "Disconnected..."; }

            CurrentState = PlayerState.isStopped;
        }
 
        //一時停止
        public void Pause()
        {
            if (CurrentState == PlayerState.isPlaying)
            {
                Timer.Stop();
                MainPlayer.Pause();
                PlayButtonImage.Source = new BitmapImage(new Uri("image/play_button.png", UriKind.Relative));
                CurrentState = PlayerState.isPaused;
            }
        }

        //再開
        public void Resume()
        {
            if (CurrentState == PlayerState.isPaused)
            {
                MainPlayer.Play();
                Timer.Start();
                PlayButtonImage.Source = new BitmapImage(new Uri("image/pause_button.png", UriKind.Relative));
                CurrentState = PlayerState.isPlaying;
            }
        }

        //繰り返し
        public void Repeat()
        {
            if (_isRepeat)
            {
                //リピートオフ
                _isRepeat = false;
                RepeatButtonImage.Source = new BitmapImage(new Uri("image/repeat_button_off.png", UriKind.Relative));
            }
            else
            {
                //リピートオン
                _isRepeat = true;             
                RepeatButtonImage.Source = new BitmapImage(new Uri("image/repeat_button_on.png", UriKind.Relative));
            }
        }

        //ウィンドウサイズを変更
        public void Resize(double rate = 1)
        {
            if (CurrentState == PlayerState.isStopped) { return; }

            this.Height = MainPlayer.NaturalVideoHeight * rate;
            this.Width = MainPlayer.NaturalVideoWidth * rate;
        }

        //パネルをフェードアウトさせる
        public void PanelFadeout()
        {
            if (fadeautoAnime == null)
            {
                Dispatcher.Invoke(new Action(() =>
                {

                    fadeautoAnime = new DoubleAnimation();
                    fadeautoAnime.From = 0.7;
                    fadeautoAnime.To = 0;
                    fadeautoAnime.Duration = TimeSpan.FromSeconds(0.5);
                    fadeautoAnime.FillBehavior = FillBehavior.Stop;

                    fadeautoAnime.Completed += (e, s) =>
                    {
                        GridControlPanel.Visibility = Visibility.Collapsed;
                        fadeautoAnime = null;   
                    };

                    GridControlPanel.BeginAnimation(Grid.OpacityProperty, fadeautoAnime);
                    
                }));
            }
        }

        //Tipを一定時間表示
        public void ShowTip(System.Windows.HorizontalAlignment Aligment = System.Windows.HorizontalAlignment.Right)
        {
            if (TipTextClearTimer == null)
            {
                TipPanel.Visibility = Visibility.Visible;
                TipPanel.Margin = new Thickness(1, InfoTextGridPanel.ActualHeight, 1, 0);
                TipPanel.HorizontalAlignment = Aligment;

                TipTextClearTimer = new DispatcherTimer();
                TipTextClearTimer.Tick += (e, s) =>
                {
                    TipPanel.Visibility = Visibility.Collapsed;
                    TipTextClearTimer.Stop();
                    TipTextClearTimer = null;
                };
                TipTextClearTimer.Interval = TimeSpan.FromSeconds(5);
                TipTextClearTimer.Start();
            }
        }

        #endregion

        #region SeekBar Methods and Events

        //1秒毎にシークバーと再生時間を更新
        public void TimerEvent(object sender, EventArgs e)
        {
            SeekBar.Width = SeekBarControl.ActualWidth * (MainPlayer.Position.TotalSeconds / MainPlayer.NaturalDuration.TimeSpan.TotalSeconds);

            PotisionText.Text = String.Format("{0:00}:{1:00}:{2:00}", MainPlayer.Position.Hours,
                                                                      MainPlayer.Position.Minutes,
                                                                      MainPlayer.Position.Seconds);
        }

        //シークバーのマウス操作
        private void SeekBarControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Timer.Stop();
            _isDrag = true;
        }

        private void SeekBarControl_MouseMove(object sender, MouseEventArgs e)
        {
            Point ClickPoint = e.GetPosition(this.SeekBarControl);

            if (_isDrag)
            {             
                SeekBar.Width = ClickPoint.X;
            }

            SeekTimeTip(ClickPoint.X);
        }

        private void SeekBarControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point ClickPoint = e.GetPosition(this.SeekBarControl);

            MainPlayer.Position = new TimeSpan(0, 0, (int)(MainPlayer.NaturalDuration.TimeSpan.TotalSeconds * (ClickPoint.X / SeekBarControl.ActualWidth)));
            SeekBar.Width = ClickPoint.X;

            _isDrag = false;
            Timer.Start();

        }

        private void SeekBarControl_MouseEnter(object sender, MouseEventArgs e)
        {
            Point ClickPoint = e.GetPosition(this.SeekBarControl);
            SeekTimeTip(ClickPoint.X);
        }

        private void SeekBarControl_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isDrag)
            {
                Point ClickPoint = e.GetPosition(this.SeekBarControl);

                MainPlayer.Position = new TimeSpan(0, 0, (int)(MainPlayer.NaturalDuration.TimeSpan.TotalSeconds * (ClickPoint.X / SeekBarControl.ActualWidth)));
                SeekBar.Width = ClickPoint.X;

                _isDrag = false;
                Timer.Start();
            }

            SeekTimePanel.Visibility = Visibility.Collapsed;
        }

        //シークバー上の時間を表示
        private void SeekTimeTip(double MousePositionX)
        {
            int PositionToSec = (int)(MainPlayer.NaturalDuration.TimeSpan.TotalSeconds * (MousePositionX / SeekBarControl.ActualWidth));
            int hours = PositionToSec / 3600;
            int miniutes = (PositionToSec - hours * 3600) / 60;
            int seconds = (PositionToSec - hours * 3600) % 60;

            SeekTimePanel.Visibility = Visibility.Visible;

            if (MousePositionX <= SeekTimePanel.ActualWidth / 2)
            {
                SeekTimePanel.Margin = new Thickness(0, 0, 0, ControlPanel.ActualHeight);
            }
            else if (MousePositionX >= SeekBarControl.ActualWidth - SeekTimePanel.ActualWidth / 2)
            {
                SeekTimePanel.Margin = new Thickness(SeekBarControl.ActualWidth - SeekTimePanel.ActualWidth, 0, 0, ControlPanel.ActualHeight);
            }
            else
            {
                SeekTimePanel.Margin = new Thickness(MousePositionX - SeekTimePanel.ActualWidth / 2, 0, 0, ControlPanel.ActualHeight);
            }

            SeekTimeText.Text = String.Format("{0:00}:{1:00}:{2:00}", hours, miniutes, seconds);
        }

        #endregion

        #region VolumeBar Methods and Events

        //ボリュームボタンとバーのマウス操作
        private void VolumeButton_Click(object sender, RoutedEventArgs e)
        {
            if (Volume == 0)
            {
                Volume = volumeTemp;
            }
            else
            {
                volumeTemp = Volume;
                Volume = 0;
            }
        }

        private void VolumeButton_MouseEnter(object sender, MouseEventArgs e)
        {
            VolumeBarPanel.Width = VolumeBarControl.Width;
            VolumeBarPanel.Visibility = Visibility.Visible;
        }

        //バーを閉じる時のアニメーション
        public void VolumeBarPanelAnimation()
        {
            DoubleAnimation BarAnimation = new DoubleAnimation();
            BarAnimation.From = VolumeBarControl.Width;
            BarAnimation.To = VolumeBarControl.Width / 2;
            BarAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(200));

            Storyboard.SetTargetName(BarAnimation, "VolumeBarPanel");
            Storyboard.SetTargetProperty(BarAnimation, new PropertyPath(Grid.WidthProperty));
            Storyboard BarStoryBoard = new Storyboard();
            BarStoryBoard.Children.Add(BarAnimation);
            BarStoryBoard.FillBehavior = FillBehavior.Stop;
            BarStoryBoard.Completed += (s, ev) =>
            {
                BarStoryBoard.Stop();
                VolumeBarPanel.Width = 0;
                VolumeBarPanel.Visibility = Visibility.Collapsed;
            };

            BarStoryBoard.Begin(VolumeBarPanel);
        }

        private void VolumeControlPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            VolumeBarPanelAnimation();
        }

        private void VolumeBarControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDrag = true;
        }

        private void VolumeBarControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point ClickPoint = e.GetPosition(this.VolumeBarPanel);

            Volume = 1 * (ClickPoint.X / VolumeBarControl.Width);
            

            _isDrag = false;
        }

        private void VolumeBarControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrag)
            {
                Point ClickPoint = e.GetPosition(this.VolumeBarPanel);

                Volume = 1 * (ClickPoint.X / VolumeBarControl.Width);
                
            }
        }

        private void VolumeBarControl_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isDrag)
            {
                Point ClickPoint = e.GetPosition(this.VolumeBarPanel);

                Volume = 1 * (ClickPoint.X / VolumeBarControl.Width);
                

                _isDrag = false;
            }
        }

        #endregion

    }
}
