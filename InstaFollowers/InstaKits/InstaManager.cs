using InstaFollowers.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace InstaFollowers.InstaKits
{
    class InstaManager
    {
        private const string strApiURL = "https://i.instagram.com/api/v1/";

        private const string strApiLogin = strApiURL + "accounts/login/";
        private const string strApiSearchUser = strApiURL + "users/search?query={0}&is_typeahead=true&ig_sig_key_version=4";
        private const string strApiFollowerList = strApiURL + "friendships/{0}/followers/";
        private const string strApiNextFollowerList = strApiURL + "friendships/{0}/followers/?max_id={1}";
        private const string strApiUserInfo = strApiURL + "users/{0}/info/";
        private const string strApiSearchTaggedMedia = strApiURL + "feed/tag/{0}/?rank_token={1}";
        private const string strApiNextSearchTaggedMedia = strApiURL + "feed/tag/{0}/?rank_token={1}&max_id={2}";

        private const string strPageUserInfo = "https://instagram.com/{0}/";

        public static InstaManager Instance
        {
            get
            {
                return _instance ?? (_instance = new InstaManager());
            }
        }
        private static InstaManager _instance = null;

        public InstaManager()
        {
            // load data from Application settings
            _strUsername = Properties.Settings.Default.InstaUsername;
            _strPassword = Properties.Settings.Default.InstaPassword;
            _strUserAgent = Properties.Settings.Default.UserAgent;
            _strGUID = Properties.Settings.Default.GUID;
            _strAndroidDeviceID = Properties.Settings.Default.AndroidDeviceID;
            _strPhoneID = Properties.Settings.Default.PhoneID;

            // configure http client
            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = _cookies;
            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", _strUserAgent);
        }

        private HttpClient _httpClient;
        private CookieContainer _cookies = new CookieContainer();

        private string _strUserAgent;
        private string _strUsername;
        private string _strPassword;
        private string _strGUID;
        private string _strAndroidDeviceID;
        private string _strPhoneID;

        private string _strUserId;
        private string _strCSRFToken;
        private string _strDs_User_Id;

        private string rank_token
        {
            get
            {
                return string.Format("{0}_{1}", _strDs_User_Id, Guid.NewGuid().ToString());
            }
        }

        /// <summary>
        /// Do login asynchronously and return error message.
        /// </summary>
        /// <param name="user">Contains username and password</param>
        /// <returns>Error message. Null if OK.</returns>
        public async Task<string> LoginAccount()
        {
            InstaApiLoginPostData loginPostData = new InstaApiLoginPostData {
                username = _strUsername,
                password = _strPassword,
                device_id = _strAndroidDeviceID,
                guid = _strGUID,
                phone_id = _strPhoneID
            };

            string strPostData = JsonConvert.SerializeObject(loginPostData);
            string strSignature = InstaEncryption.getSignature(strPostData);

            var content = new FormUrlEncodedContent(new[] {
                new KeyValuePair<string, string>("signed_body", string.Format("{0}.{1}", strSignature, strPostData)),
                new KeyValuePair<string, string>("ig_sig_key_version", "4"),
            });

            HttpResponseMessage response = await _httpClient.PostAsync(strApiLogin, content);

            string strContent = await response.Content.ReadAsStringAsync();
            InstaApiLoginResult loginResult = JsonConvert.DeserializeObject<InstaApiLoginResult>(strContent);
            if(loginResult.status != "ok")
            {
                string strMessage = loginResult.message;
                return strMessage;
            }
            else
            {
                response.EnsureSuccessStatusCode();
                _strUserId = loginResult.logged_in_user.pk.ToString();

                CookieCollection cookieCollection = _cookies.GetCookies(new Uri("https://i.instagram.com"));
                foreach(Cookie cookie in cookieCollection)
                {
                    if(cookie.Name == "csrftoken")
                    {
                        _strCSRFToken = cookie.Value;
                        break;
                    }
                    else if(cookie.Name == "ds_user_id")
                    {
                        _strDs_User_Id = cookie.Value;
                    }
                }

                return null;
            }
        }
        private static int total = 0;
        /// <summary>
        /// search instagram user by username and return user_id
        /// </summary>
        /// <param name="username">username to be searched</param>
        /// <returns>user_id, -1 if not exists</returns>
        public async Task<long> SearchUser(string username)
        {
            var url = strApiSearchUser + "?query=" + username + "&is_typeahead=true&ig_sig_key_version=4";
            HttpResponseMessage response = await _httpClient.GetAsync(string.Format(strApiSearchUser, username));

            string strContent = await response.Content.ReadAsStringAsync();
            InstaApiSearchUsersResult searchResult = JsonConvert.DeserializeObject<InstaApiSearchUsersResult>(strContent);

            if(searchResult.status == "ok")
            {
                foreach(var user in searchResult.users)
                {
                    if(user.username == username)
                    {
                        return user.pk;
                    }
                }
            }

            return -1;
        }

        public List<string> UserEmails(string username)
        {
            List<string> emails = new List<string>();

            string htmlContent;

            try
            {
                htmlContent = new WebClient().DownloadString(string.Format(strPageUserInfo, username));
            }
            catch
            {
                return emails;
            }
            int pos1 = htmlContent.IndexOf("{\"biography\":\"");
            htmlContent = htmlContent.Substring(pos1 + 13);
            int pos2 = htmlContent.IndexOf('"', 1);
            var biography = htmlContent.Substring(0, pos2);

            Regex escapedChar = new Regex(@"\\u[0-9a-z]{4}");
            biography = escapedChar.Replace(biography, " ");

            Regex emailRegex = new Regex(@"\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*", RegexOptions.IgnoreCase);
            MatchCollection emailMatches = emailRegex.Matches(biography);
            foreach (Match match in emailMatches)
            {
                emails.Add(match.Value);
                total++;
            }

            return emails;
        }

        public async Task<int> FollowersList(ObservableCollection<InstaApiUser> followers, long user_id, long at_least = -1)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(string.Format(strApiFollowerList, user_id));

            string strContent = await response.Content.ReadAsStringAsync();
            InstaApiUserFollowersResult searchResult = JsonConvert.DeserializeObject<InstaApiUserFollowersResult>(strContent);

            while(true)
            {
                foreach(var user in searchResult.users)
                {
                    followers.Add(user);
                }

                if(searchResult.next_max_id == null)
                {
                    break;
                }

                if (followers.Count > at_least)
                {
                    break;
                }

                Thread.Sleep(1000);

                response = await _httpClient.GetAsync(string.Format(strApiNextFollowerList, user_id, searchResult.next_max_id));
                strContent = await response.Content.ReadAsStringAsync();
                searchResult = JsonConvert.DeserializeObject<InstaApiUserFollowersResult>(strContent);

                if(searchResult.status != "ok")
                {
                    int notused = 0;
                }
            }

            return 0;
        }

        public async Task<int> SearchTaggedMediaUploaders(ObservableCollection<InstaApiUser> uploaders, string tag_name, long at_least = -1)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(string.Format(strApiSearchTaggedMedia, tag_name, rank_token));

            string strContent = await response.Content.ReadAsStringAsync();
            InstaApiSearchTaggedMediaResult searchResult = JsonConvert.DeserializeObject<InstaApiSearchTaggedMediaResult>(strContent);

            while(true)
            {
                foreach(var item in searchResult.items)
                {
                    var isExist = false;
                    foreach(var old in uploaders)
                    {
                        if(old.username == item.user.username)
                        {
                            isExist = true;
                            break;
                        }
                    }
                    if (!isExist)
                    {
                        uploaders.Add(item.user);
                    }
                }

                if (!searchResult.more_available)
                {
                    break;
                }

                if(uploaders.Count > at_least)
                {
                    break;
                }

                Thread.Sleep(1000);

                response = await _httpClient.GetAsync(string.Format(strApiNextSearchTaggedMedia, tag_name, rank_token, searchResult.next_max_id));
                strContent = await response.Content.ReadAsStringAsync();
                searchResult = JsonConvert.DeserializeObject<InstaApiSearchTaggedMediaResult>(strContent);
            }

            return 0;
        }
    }
}
