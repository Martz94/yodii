﻿using System;
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

namespace Yodii.DemoApp.Examples.Plugins.Views
{
    /// <summary>
    /// Interaction logic for Client1.xaml
    /// </summary>
    public partial class Company3View : Window
    {
        Company3 _company;
        public Company3View(Company3 company)
        {
            _company = company;
            InitializeComponent();
        }
        private void Window_Closing( object sender, System.ComponentModel.CancelEventArgs e )
        {
            e.Cancel = !_company.WindowClosed();
        }
    }
}
