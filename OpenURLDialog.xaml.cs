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
using System.Windows.Shapes;

namespace WpfMediaPlayer
{
    /// <summary>
    /// Window1.xaml の相互作用ロジック
    /// </summary>
    public partial class OpenURLDialog : Window
    {
        //ファイル名もしくはURL
        string _filename = "";
        public string FileName
        {
            get
            {
                return _filename;
            }
            set
            {
                _filename = value;
            }
        }

        //コンストラクタ
        public OpenURLDialog()
        {
            InitializeComponent();
        }

        //OKボタンのイベント
        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            FileName = URLBox.Text;
            this.Close();
        }

        //Cancelボタンのイベント
        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
