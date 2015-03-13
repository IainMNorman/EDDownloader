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
using Normsco.EDDownloader;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Windows.Threading;
using System.Diagnostics;
using System.IO;

namespace Normsco.EDDClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            button.IsEnabled = true;
        }

        public EDDEngine d = new EDDEngine();
        
        ProgressBar mainBar = new ProgressBar();

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            button.IsEnabled = false;
            await d.Init(@"d:\edtempdl\");
            await d.Start();
        }
    }
}
