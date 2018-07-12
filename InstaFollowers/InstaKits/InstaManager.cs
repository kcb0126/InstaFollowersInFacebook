using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        public static InstaManager Instance
        {
            get
            {
                return _instance ?? (_instance = new InstaManager());
            }
        }
        private static InstaManager _instance = null;

        private static Mutex _mutex = new Mutex();

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

        /// <summary>
        /// Do login asynchronously and return error message.
        /// </summary>
        /// <param name="user">Contains username and password</param>
        /// <returns>Error message. Null if OK.</returns>
        public async Task<string> LoginAccount()
        {
            _mutex.WaitOne();

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
                _mutex.ReleaseMutex();
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
                }

                _mutex.ReleaseMutex();
                return null;
            }
        }

        /// <summary>
        /// search instagram user by username and return user_id
        /// </summary>
        /// <param name="username">username to be searched</param>
        /// <returns>user_id, -1 if not exists</returns>
        public async Task<long> SearchUser(string username)
        {
            _mutex.WaitOne();

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
                        _mutex.ReleaseMutex();
                        return user.pk;
                    }
                }
            }

            _mutex.ReleaseMutex();
            return -1;
        }

        public async Task<List<string>> UserEmails(long user_id)
        {
            _mutex.WaitOne();

            HttpResponseMessage response = await _httpClient.GetAsync(string.Format(strApiUserInfo, user_id));

            string strContent = await response.Content.ReadAsStringAsync();
            InstaApiUserInfoResult userInfoResult = JsonConvert.DeserializeObject<InstaApiUserInfoResult>(strContent);

            List<string> emails = new List<string>();
            Regex emailRegex = new Regex(@"\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*", RegexOptions.IgnoreCase);
            MatchCollection emailMatches = emailRegex.Matches(userInfoResult.user.biography);
            foreach (Match match in emailMatches)
            {
                emails.Add(match.Value);
                System.Diagnostics.Debug.WriteLine(match.Value);
            }

            _mutex.ReleaseMutex();
            return emails;
        }

        public async Task<List<long>> FollowersList(long user_id, long at_least = -1)
        {
            _mutex.WaitOne();

            HttpResponseMessage response = await _httpClient.GetAsync(string.Format(strApiFollowerList, user_id));

            string strContent = await response.Content.ReadAsStringAsync();
            InstaApiUserFollowersResult searchResult = JsonConvert.DeserializeObject<InstaApiUserFollowersResult>(strContent);

            List<long> follower_ids = new List<long>();
            while(true)
            {
                foreach(var user in searchResult.users)
                {
                    follower_ids.Add(user.pk);
                }

                if(searchResult.next_max_id == null)
                {
                    break;
                }

                if (follower_ids.Count > at_least)
                {
                    break;
                }

                Thread.Sleep(1000);

                response = await _httpClient.GetAsync(string.Format(strApiNextFollowerList, user_id, searchResult.next_max_id));
                strContent = await response.Content.ReadAsStringAsync();
                searchResult = JsonConvert.DeserializeObject<InstaApiUserFollowersResult>(strContent);
            }

            _mutex.ReleaseMutex();
            return follower_ids;
        }
    }
}
