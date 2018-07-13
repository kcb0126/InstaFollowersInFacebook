using InstaFollowers.InstaKits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
                btnStart.IsEnabled = false;
                txtUsername.IsEnabled = true;
                btnSearch.IsEnabled = true;
            }
        }

        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text;
            long user_id = await InstaManager.Instance.SearchUser(username);
            if(user_id == -1)
            {
                MessageBox.Show("There aren't any user who has that username.");
            }
            else
            {
                var followers = await InstaManager.Instance.FollowersList(user_id, 500);
                var emails = new List<string>();
                foreach(var follower in followers)
                {
                    emails.AddRange(InstaManager.Instance.UserEmails(follower));
                }
            }
        }
    }
}
