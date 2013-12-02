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
        isPlaying, isPaused, isStopped
    }

    public partial class MainWindow : Window
    {
        DispatcherTimer Timer;
        DispatcherTimer BufferTimer;
        string _fileName = "";

        //フラグ
        bool _isRepeat = false;
        bool _isDrag = false;
        PlayerState CurrentState = PlayerState.isStopped;

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
                }
                else
                {
                    this.WindowState = WindowState.Normal;
                }
            };

            //キー操作
            //キーダウンでウィンドウを最前面化
            this.KeyDown += (s, e) =>
            {
                if (this.Topmost == false)
                {
                    this.Topmost = true;
                }
                else
                {
                    this.Topmost = false;
                }
            };

            //マウス操作
            //マウスが画面に載ったらパネルを表示
            this.MouseEnter += (s, e) =>
            {
                InfoTextPanel.Visibility = Visibility.Visible;
                ControlPanel.Visibility = Visibility.Visible;
            };

            //マウスが画面から離れたらパネルを非表示
            this.MouseLeave += (s, e) =>
            {
                InfoTextPanel.Visibility = Visibility.Collapsed;
                ControlPanel.Visibility = Visibility.Collapsed;
            };

            //ウィンドウの終了操作
            this.Closed += (s, e) => { Stop(); };

            //シークバーと再生時間を動かすDispatherTimerをセット
            Timer = new DispatcherTimer();
            Timer.Tick += TimerEvent;
            Timer.Interval = new TimeSpan(0, 0, 1);

            //コマンドライン引数を確認
            if (App.CommandLineArgs != null)
            {
                _fileName = App.CommandLineArgs[0];

                Play();
            }
        }

        #endregion

        #region Commands

        //ファイルを開く
        void DoOpenCommand(object sender, RoutedEventArgs e)
        {

            //OpenFileDialogの設定
            var openDlg = new OpenFileDialog();
            openDlg.DefaultExt = "mp4; wmv; avi";
            openDlg.Filter = "Movie File (*.mp4; *.wmv; *.avi;)|*.mp4; *.wmv; *.avi| all file (*.*)|*.*";

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
            if (CurrentState == PlayerState.isStopped) { return; }

            this.Height = MainPlayer.NaturalVideoHeight / 2;
            this.Width = MainPlayer.NaturalVideoWidth / 2;
        }

        void DoSetNaturalVideoHeightWidthCommand(object sender, RoutedEventArgs e)
        {
            if (CurrentState == PlayerState.isStopped) { return; }

            this.Height = MainPlayer.NaturalVideoHeight;
            this.Width = MainPlayer.NaturalVideoWidth;
        }

        void DoSetOneAndHerfNaturalVideoHeightWidthCommand(object sender, RoutedEventArgs e)
        {
            if (CurrentState == PlayerState.isStopped) { return; }

            this.Height = MainPlayer.NaturalVideoHeight * 1.5;
            this.Width = MainPlayer.NaturalVideoWidth * 1.5;
        }

        void DoSetDoubledNaturalVideoHeightWidthCommand(object sender, RoutedEventArgs e)
        {
            if (CurrentState == PlayerState.isStopped) { return; }

            this.Height = MainPlayer.NaturalVideoHeight * 2;
            this.Width = MainPlayer.NaturalVideoWidth * 2;
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

                //再生時間を書き込む
                NaturalDurationText.Text = String.Format("{0:00}:{1:00}:{2:00}", MainPlayer.NaturalDuration.TimeSpan.Hours,
                                                                                 MainPlayer.NaturalDuration.TimeSpan.Minutes,
                                                                                 MainPlayer.NaturalDuration.TimeSpan.Seconds);

                Timer.Start();
            }
            else
            { 
            
            }
        }

        //動画終了時の処理
        private void MainPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            Timer.Stop();
            Stop();

            if (_isRepeat) { Play(); }
        }

        private void MainPlayer_BufferingStarted(object sender, RoutedEventArgs e)
        {
            InfoText.Text = "Buffring...";

            BufferTimer = new DispatcherTimer();
            BufferTimer.Tick += (s, ev) => { InfoText.Text = String.Format("{0}%", MainPlayer.BufferingProgress * 100).ToString(); };
            BufferTimer.Interval = new TimeSpan(0, 0, 1);

            BufferTimer.Start();
        }

        private void MainPlayer_BufferingEnded(object sender, RoutedEventArgs e)
        {
            BufferTimer.Stop();
            InfoText.Text = "Conmplete Conectiong!";
        }

        #endregion

        #region Methods

        //再生
        public void Play()
        {
            if (_fileName == "") { return; }

            InfoText.Text = System.IO.Path.GetFileNameWithoutExtension(_fileName);

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
                RepeatButton.Content = "OFF";
            }
            else
            {
                //リピートオン
                _isRepeat = true;
                RepeatButton.Content = "ON";
            }
        }

        #endregion

        #region SeekBar Methods

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
            if (_isDrag)
            {
                Point ClickPoint = e.GetPosition(this.SeekBarControl);
                SeekBar.Width = ClickPoint.X;
            }
        }

        private void SeekBarControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point ClickPoint = e.GetPosition(this.SeekBarControl);

            MainPlayer.Position = new TimeSpan(0, 0, (int)(MainPlayer.NaturalDuration.TimeSpan.TotalSeconds * (ClickPoint.X / SeekBarControl.ActualWidth)));
            SeekBar.Width = ClickPoint.X;

            _isDrag = false;
            Timer.Start();

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
        }

        #endregion

        #region VolumeBar Methods

        //ボリュームボタンとバーのマウス操作
        private void VolumeButton_Click(object sender, RoutedEventArgs e)
        {
            MainPlayer.Volume = 0;
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

            MainPlayer.Volume = 1 * (ClickPoint.X / VolumeBarControl.Width);
            VolumeBar.Width = ClickPoint.X;

            _isDrag = false;
        }

        private void VolumeBarControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrag)
            {
                Point ClickPoint = e.GetPosition(this.VolumeBarPanel);
                MainPlayer.Volume = 1 * (ClickPoint.X / VolumeBarControl.Width);

                VolumeBar.Width = ClickPoint.X;
            }
        }

        private void VolumeBarControl_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isDrag)
            {
                Point ClickPoint = e.GetPosition(this.VolumeBarPanel);
                if (ClickPoint.X < 0)
                {
                    MainPlayer.Volume = 0;
                    VolumeBar.Width = 0;
                }
                else
                {
                    MainPlayer.Volume = 1 * (ClickPoint.X / VolumeBarControl.Width);
                    VolumeBar.Width = ClickPoint.X;
                }

                _isDrag = false;
            }
        }

        #endregion




    }
}
