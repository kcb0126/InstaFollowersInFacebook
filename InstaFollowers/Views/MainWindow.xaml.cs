using InstaFollowers.InstaKits;
using InstaFollowers.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
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

            dgFollowers.ItemsSource = followersViewModel;

            workerFollowerSearch.DoWork += WorkerFollowerSearch_DoWork;
            workerFollowerSearch.ProgressChanged += WorkerFollowerSearch_ProgressChanged;
            workerFollowerSearch.WorkerSupportsCancellation = true;
            workerFollowerSearch.WorkerReportsProgress = true;

            dgUploaders.ItemsSource = uploadersViewModel;

            workerUploaderSearch.DoWork += WorkerUploaderSearch_DoWork;
            workerUploaderSearch.ProgressChanged += WorkerUploaderSearch_ProgressChanged;
            workerUploaderSearch.WorkerSupportsCancellation = true;
            workerUploaderSearch.WorkerReportsProgress = true;
        }

        private ObservableCollection<UserWithEmailViewModel> followersViewModel = new ObservableCollection<UserWithEmailViewModel>();

        private BackgroundWorker workerFollowerSearch = new BackgroundWorker();

        private const int WORKER_USER_WITH_EMAIL_ADD_NEW = 0;
        private const int WORKER_USER_WITH_EMAIL_ENABLE_SEARCH_BUTTON = 1;
        private const int WORKER_USER_WITH_EMAIL_DISABLE_SEARCH_BUTTON = 2;
        private const int WORKER_UERS_WITH_EMAIL_TOTAL_SCRAPED = 3;
        private const int WORKER_USER_WITH_EMAIL_RANGE_CHANGED = 4;

        private int follower_range;
        private int follower_count;

        private ObservableCollection<UserWithEmailViewModel> uploadersViewModel = new ObservableCollection<UserWithEmailViewModel>();

        private BackgroundWorker workerUploaderSearch = new BackgroundWorker();

        private int uploader_range;
        private int uploader_count;

        private void WorkerFollowerSearch_DoWork(object sender, DoWorkEventArgs e)
        {
            workerFollowerSearch.ReportProgress(WORKER_USER_WITH_EMAIL_DISABLE_SEARCH_BUTTON);

            string username = e.Argument as string;
            var task_user_id = InstaManager.Instance.SearchUser(username);
            task_user_id.Wait();
            long user_id = task_user_id.Result;
            if (user_id == -1)
            {
                MessageBox.Show("There aren't any user who has that username.");
            }
            else
            {
                var followers = new ObservableCollection<InstaApiUser>();
                followers.CollectionChanged += Followers_CollectionChanged;
                var task_followers = InstaManager.Instance.FollowersList(followers, user_id, follower_count);
                task_followers.Wait();
                int total_scraped = 0;
                foreach (var follower in followers)
                {
                    var emails = InstaManager.Instance.UserEmails(follower.username);
                    if (emails.Count > 0)
                    {
                        var allEmails = string.Join(", ", emails.ToArray());
                        var followerViewModel = new UserWithEmailViewModel
                        {
                            No = -1, // temp index, will changed with UI code
                            Username = follower.username,
                            FullName = follower.full_name,
                            Email = allEmails
                        };
                        workerFollowerSearch.ReportProgress(WORKER_USER_WITH_EMAIL_ADD_NEW, followerViewModel);
                    }
                    workerFollowerSearch.ReportProgress(WORKER_UERS_WITH_EMAIL_TOTAL_SCRAPED, ++total_scraped);
                }
            }

            workerFollowerSearch.ReportProgress(WORKER_USER_WITH_EMAIL_ENABLE_SEARCH_BUTTON);
        }

        private void Followers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            workerFollowerSearch.ReportProgress(WORKER_USER_WITH_EMAIL_RANGE_CHANGED, (sender as ObservableCollection<InstaApiUser>).Count);
        }

        private void WorkerUploaderSearch_DoWork(object sender, DoWorkEventArgs e)
        {
            workerUploaderSearch.ReportProgress(WORKER_USER_WITH_EMAIL_DISABLE_SEARCH_BUTTON);

            string tagname = e.Argument as string;
            var uploaders = new ObservableCollection<InstaApiUser>();
            uploaders.CollectionChanged += Uploaders_CollectionChanged;
            var task_uploaders = InstaManager.Instance.SearchTaggedMediaUploaders(uploaders, tagname, uploader_count);
            task_uploaders.Wait();
            int total_scraped = 0;
            foreach(var uploader in uploaders)
            {
                var emails = InstaManager.Instance.UserEmails(uploader.username);
                if(emails.Count > 0)
                {
                    var allEmails = string.Join(", ", emails.ToArray());
                    var uploaderViewModel = new UserWithEmailViewModel
                    {
                        No = -1, // temp index, will changed with UI code
                        Username = uploader.username,
                        FullName = uploader.full_name,
                        Email = allEmails
                    };
                    workerUploaderSearch.ReportProgress(WORKER_USER_WITH_EMAIL_ADD_NEW, uploaderViewModel);
                }
                workerUploaderSearch.ReportProgress(WORKER_UERS_WITH_EMAIL_TOTAL_SCRAPED, ++total_scraped);
            }

            workerUploaderSearch.ReportProgress(WORKER_USER_WITH_EMAIL_ENABLE_SEARCH_BUTTON);
        }

        private void Uploaders_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            workerUploaderSearch.ReportProgress(WORKER_USER_WITH_EMAIL_RANGE_CHANGED, (sender as ObservableCollection<InstaApiUser>).Count);
        }

        private void WorkerFollowerSearch_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            switch(e.ProgressPercentage)
            {
                case WORKER_USER_WITH_EMAIL_ADD_NEW:
                    {
                        var followerViewModel = e.UserState as UserWithEmailViewModel;
                        followerViewModel.No = followersViewModel.Count + 1;
                        followersViewModel.Add(followerViewModel);
                    }
                    break;

                case WORKER_USER_WITH_EMAIL_DISABLE_SEARCH_BUTTON:
                    btnSearchFollowers.IsEnabled = false;
                    break;

                case WORKER_USER_WITH_EMAIL_ENABLE_SEARCH_BUTTON:
                    btnSearchFollowers.IsEnabled = true;
                    break;

                case WORKER_UERS_WITH_EMAIL_TOTAL_SCRAPED:
                    lblFollowersFound.Content = string.Format("{0}/{1} followers scraped", (int)e.UserState, follower_range);
                    break;

                case WORKER_USER_WITH_EMAIL_RANGE_CHANGED:
                    follower_range = (int)e.UserState;
                    lblFollowersFound.Content = string.Format("0/{0} followers scraped(preparing range...)", follower_range);
                    break;

                default:

                    break;
            }

        }

        private void WorkerUploaderSearch_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            switch(e.ProgressPercentage)
            {
                case WORKER_USER_WITH_EMAIL_ADD_NEW:
                    {
                        var uploaderViewModel = e.UserState as UserWithEmailViewModel;
                        uploaderViewModel.No = uploadersViewModel.Count + 1;
                        uploadersViewModel.Add(uploaderViewModel);
                    }
                    break;

                case WORKER_USER_WITH_EMAIL_DISABLE_SEARCH_BUTTON:
                    btnSearchByTag.IsEnabled = false;
                    break;

                case WORKER_USER_WITH_EMAIL_ENABLE_SEARCH_BUTTON:
                    btnSearchByTag.IsEnabled = true;
                    break;

                case WORKER_UERS_WITH_EMAIL_TOTAL_SCRAPED:
                    lblUploadersFound.Content = string.Format("{0}/{1} uploaders scraped", (int)e.UserState, uploader_range);
                    break;

                case WORKER_USER_WITH_EMAIL_RANGE_CHANGED:
                    uploader_range = (int)e.UserState;
                    lblUploadersFound.Content = string.Format("0/{0} uploaders scraped(preparing range...)", uploader_range);
                    break;

                default:

                    break;
            }
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
                tabControl.IsEnabled = true;
            }
        }

        private void btnSearchFollowers_Click(object sender, RoutedEventArgs e)
        {
            var username = txtUsername.Text;
            follower_count = int.Parse(txtFollowerCount.Text);
            workerFollowerSearch.RunWorkerAsync(username);
        }

        private void btnSearchByTag_Click(object sender, RoutedEventArgs e)
        {
            var tagname = txtHashTag.Text;
            uploader_count = int.Parse(txtUploaderCount.Text);
            workerUploaderSearch.RunWorkerAsync(tagname);
        }

        private void btnSaveFollowers_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.CheckPathExists = true;
            dlg.DefaultExt = ".csv";
            dlg.Filter = "CSV File(*.csv)|*.csv";
            dlg.Title = "Save";

            var result = dlg.ShowDialog();
            if(result ?? false)
            {
                var streamwriter = new System.IO.StreamWriter(dlg.FileName);

                CsvHelper.CsvWriter writer = new CsvHelper.CsvWriter(streamwriter);

                var records = new List<dynamic>();
                dynamic record = new ExpandoObject();

                writer.WriteRecords(followersViewModel.ToList());

                streamwriter.Close();
            }
        }

        private void btnSaveUploaders_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.CheckPathExists = true;
            dlg.DefaultExt = ".csv";
            dlg.Filter = "CSV File(*.csv)|*.csv";
            dlg.Title = "Save";

            var result = dlg.ShowDialog();
            if (result ?? false)
            {
                var streamwriter = new System.IO.StreamWriter(dlg.FileName);

                CsvHelper.CsvWriter writer = new CsvHelper.CsvWriter(streamwriter);

                var records = new List<dynamic>();
                dynamic record = new ExpandoObject();

                writer.WriteRecords(uploadersViewModel.ToList());

                streamwriter.Close();
            }
        }
    }
}
