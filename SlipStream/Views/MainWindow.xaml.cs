﻿using Accord.Extensions.Imaging;
using SlipStream.ViewModels;
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

namespace SlipStream.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

        }

        private void NextFrame_Click(object sender, RoutedEventArgs e)
        {
            MainWindowViewModel m = this.DataContext as MainWindowViewModel;

            m.NextFrame();
        }

        private void PlayStream_Click(object sender, RoutedEventArgs e)
        {
            MainWindowViewModel m = this.DataContext as MainWindowViewModel;

            m.PlayStream();
        }
    }
}
