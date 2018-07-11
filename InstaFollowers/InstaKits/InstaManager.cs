using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace InstaFollowers.InstaKits
{
    class InstaManager
    {
        private const string strApiLogin = "https://i.instagram.com/api/v1/accounts/login/";

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

                return null;
            }
        }
    }
}
