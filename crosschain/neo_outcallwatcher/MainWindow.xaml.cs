using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace neo_outcallwatcher
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            lib_neo_outcall_s.watcher.StartParse(int.Parse(txtBlockHeight.Text));
            //list1.Items.Add("height=" + n);
        }
        System.Windows.Threading.DispatcherTimer timer;
        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            lib_neo_outcall_s.watcher.AddWatchContract("0x24192c2a72e0ce8d069232f345aea4db032faf72");
            lib_neo_outcall_s.watcher.StartWatcherThread();


            timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += (s, ee) =>
              {
                  var n = lib_neo_outcall_s.watcher.GetHeight();
                  var np = lib_neo_outcall_s.watcher.GetParseHeight();
                  this.label01.Content = "height=" + n + "   parse height=" + np;
                  if (lib_neo_outcall_s.watcher.GetCallItemCount() > 0)
                  {
                      var item = lib_neo_outcall_s.watcher.PickCall();
                      this.list1.Items.Add(item);
                  }
              };
            timer.Start();
        }
    }
}
