using InstaFollowers.InstaKits;
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

namespace InstaFollowers
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

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            string errMsg = await InstaManager.Instance.LoginAccount();
            if(errMsg != null)
            {
                MessageBox.Show(errMsg);
                this.Close();
            }
            else
            {
                txtUsername.IsEnabled = true;
                btnSearch.IsEnabled = true;
            }
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
