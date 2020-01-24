using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DemoWebService
{
    public class Utils
    {

        #region Fields
        private static volatile Utils _instance;
        private static readonly object _syncLock = new object();
        private bool BpmSessionId = false;
        private string domain;
        private string userName;
        private string password;
        private bool _IsLoginSuccess = false;
        #endregion
        private Utils() { }

        #region Properties
        public static Utils Instance
        {
            get
            {
                if (_instance != null) return _instance;

                lock (_syncLock)
                {
                    if (_instance == null)
                    {
                        _instance = new Utils();
                    }
                }
                return _instance;
            }
        }
        public CookieContainer Auth { get; set; }
        public bool IsLoginSuccess { get { return _IsLoginSuccess; } }
        public CurrentUser CurrentUser { get; private set; } = null;
        #endregion


        #region Events
        public event EventHandler<WebSocketMessageReceivedEventArgs> WebSocketMessageReceived;
        #endregion

        #region Methods
        public void SetCredentials(string UserName, string Password, string Domain)
        {
            Instance.userName = (!string.IsNullOrEmpty(UserName)) ? UserName : "Supervisor";
            Instance.password = (!string.IsNullOrEmpty(Password)) ? Password : "Supervisor";
            Instance.domain = (!string.IsNullOrEmpty(Domain)) ? Domain : "http://k_krylov_nb:9010";
        }
        private CookieContainer AuthRequest(string userName, string password, string domain)
        {
            {
                if (domain.EndsWith("/", StringComparison.Ordinal))
                    domain = domain.Remove(domain.Length - 1, 1);

                CookieContainer AuthCookie = new CookieContainer();
                string authServiceUri = domain + @"/ServiceModel/AuthService.svc/Login";

                HttpWebRequest authRequest = (HttpWebRequest)WebRequest.Create(authServiceUri);
                authRequest.Method = "POST";
                authRequest.ContentType = "application/json";
                authRequest.CookieContainer = AuthCookie;
                using (var requestStream = authRequest.GetRequestStream())
                {
                    using StreamWriter writer = new StreamWriter(requestStream);
                    writer.Write(@"{
                    ""UserName"":""" + userName + @""",
                    ""UserPassword"":""" + password + @"""
                    }");
                }
                using (HttpWebResponse myHttpWebResponse = (HttpWebResponse)authRequest.GetResponse())
                {
                    HttpStatusCode status = myHttpWebResponse.StatusCode;
                    using (StreamReader MyStreamReader = new StreamReader(myHttpWebResponse.GetResponseStream(), true))
                    {
                        string response = MyStreamReader.ReadToEnd();
                        JObject jsonResponse = JObject.Parse(response);
                        Dictionary<string, string> results = new Dictionary<string, string>();

                        foreach (KeyValuePair<string, JToken> item in jsonResponse)
                        {
                            results.Add(item.Key, item.Value.ToString());
                        }
                        if (results["Code"] == "0")
                        {
                            AuthCookie.Add(myHttpWebResponse.Cookies);
                            Instance._IsLoginSuccess = true;
                            return AuthCookie;
                        }
                        else
                        {
                            throw new Exception("Login Failed");
                        }
                    }
                }
            }
        }
        public async Task<bool> LoginAsync()
        {
            Auth = AuthRequest(Instance.userName, Instance.password, Instance.domain);
            if (Instance._IsLoginSuccess)
            {
                Instance.CurrentUser = await GetSysValuesAsync().ConfigureAwait(false);
                Instance.ConectWebSocket();
            }
            return Instance._IsLoginSuccess;
        }
        public async Task<bool> LogoutAsync()
        {
            var logout = await GetResponseAsync("{}", ActionEnum.LOGOUT).ConfigureAwait(false);
            if (logout.HttpStatusCode == HttpStatusCode.OK)
            {
                Auth = null;
                Instance.BpmSessionId = false;
                Instance.CurrentUser = null;
                Instance._IsLoginSuccess = false;
                return true;
            }
            else
            {
                return false;
            }
        }
        public async Task<RequestResponse> ExecuteRequest(string jsonRequestBody, ActionEnum requestType) {
            RequestResponse requestResponse = await GetResponseAsync(jsonRequestBody, requestType);
            return requestResponse;
        }
        private async Task<RequestResponse> GetResponseAsync(string json, ActionEnum method)
        {
            string transportUrl = Url.TransportUrl(method, Instance.domain);
            HttpStatusCode Code;
            RequestResponse result = new RequestResponse();
            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(transportUrl);
            myHttpWebRequest.Method = "POST";
            myHttpWebRequest.ContentType = "application/json";
            myHttpWebRequest.CookieContainer = Instance.Auth;

            Uri siteUri = new Uri(transportUrl);
            foreach (Cookie cookie in Instance.Auth.GetCookies(siteUri))
            {
                if (cookie.Name == "BPMCSRF" || cookie.Name == "BPMSESSIONID")
                {
                    myHttpWebRequest.Headers.Add(cookie.Name, cookie.Value);
                }
            }
            //encode json
            byte[] postBytes = Encoding.UTF8.GetBytes(json);
            //Prepare Request Stream
            using (Stream requestStream = myHttpWebRequest.GetRequestStream())
            {
                requestStream.Write(postBytes, 0, postBytes.Length);
            }
            //Send Request
            try
            {
                using (HttpWebResponse myHttpWebResponse = (HttpWebResponse)await myHttpWebRequest.GetResponseAsync().ConfigureAwait(false))
                {
                    Code = myHttpWebResponse.StatusCode;
                    /***
                    * !!! VERY IMPORTANT !!!
                    * READ: https://academy.bpmonline.com/documents/technic-sdk/7-14/executing-odata-queries-using-fiddler
                    * User session is created only upon the first request to the EntityDataService.svc, after which the BPMSESSIONID cookie will be returned in the response. 
                    * Therefore, there is no need to add BPMSESSIONID cookie to the title of the first request.
                    * If you do not add BPMSESSIONID cookie to each subseqnent request, then each request will create a new user session. 
                    * Significant frequency of requests (several or more requests a minute) will increase the RAM consumption which will decrease performance.
                    */
                    if (Instance.BpmSessionId == false)
                    {
                        string val = myHttpWebResponse.Cookies["BPMSESSIONID"].Value;
                        Cookie C = new Cookie("BPMSESSIONID", val);
                        Instance.Auth.Add(new Uri(Instance.domain), C);
                        Instance.BpmSessionId = true;
                    }
                    using (StreamReader MyStreamReader = new StreamReader(myHttpWebResponse.GetResponseStream(), true))
                    {
                        result.HttpStatusCode = Code;
                        result.Result = MyStreamReader.ReadToEnd();
                        result.ErrorMessage = null;
                    }
                }
            }
            catch (WebException we)
            {
                Code = ((HttpWebResponse)(we).Response).StatusCode;
                using (StreamReader MyStreamReader = new StreamReader(((HttpWebResponse)(we).Response).GetResponseStream(), true))
                {
                    result.HttpStatusCode = Code;
                    result.Result = null;
                    result.ErrorMessage = MyStreamReader.ReadToEnd();
                }
            }
            return result;
        }
        private async Task<CurrentUser> GetSysValuesAsync()
        {
            string transportResponse;
            string transportUrl = Instance.domain + @"/0/Nui/ViewModule.aspx";
            HttpStatusCode Code;
            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(transportUrl);
            myHttpWebRequest.Method = "GET";
            myHttpWebRequest.ContentType = "text/html; charset=utf-8";
            myHttpWebRequest.CookieContainer = Instance.Auth;

            CurrentUser currentUser = new CurrentUser();
            try
            {
                using (HttpWebResponse myHttpWebResponse = (HttpWebResponse)await myHttpWebRequest.GetResponseAsync().ConfigureAwait(false))
                {
                    Code = myHttpWebResponse.StatusCode;
                    if (Instance.BpmSessionId == false)
                    {
                        string val = myHttpWebResponse.Cookies["BPMSESSIONID"].Value;
                        Cookie C = new Cookie("BPMSESSIONID", val);
                        Instance.Auth.Add(new Uri(Instance.domain), C);
                        Instance.BpmSessionId = true;
                    }
                    using (StreamReader MyStreamReader = new StreamReader(myHttpWebResponse.GetResponseStream(), true))
                    {
                        transportResponse = MyStreamReader.ReadToEnd();

                        HtmlDocument doc = new HtmlDocument();
                        doc.LoadHtml(transportResponse);

                        HtmlNodeCollection head = doc.DocumentNode.ChildNodes["html"].ChildNodes["head"].ChildNodes;
                        for (int i = 0; i < head.Count; i++)
                        {
                            HtmlNode node = head[i];
                            if (node.Name == "script")
                            {
                                if (node.InnerText.StartsWith("\r\nvar sysValues = {CURRENT_USER", StringComparison.Ordinal))
                                {
                                    string myText = node.InnerText;
                                    string[] values = myText.Split(';');
                                    for (int v = 0; v < values.Length; v++)
                                    {
                                        if (values[v].StartsWith("\r\nvar sysValues", StringComparison.Ordinal))
                                        {
                                            string settings = values[v].Split('=')[1];
                                            currentUser = JsonConvert.DeserializeObject<CurrentUser>(settings);
                                            currentUser.Code = Code;
                                            currentUser.ErrorMessage = "";
                                            return currentUser;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (WebException we)
            {
                Code = ((HttpWebResponse)(we).Response).StatusCode;
                using (StreamReader MyStreamReader = new StreamReader(((HttpWebResponse)(we).Response).GetResponseStream(), true))
                {
                    transportResponse = MyStreamReader.ReadToEnd();
                    currentUser = new CurrentUser()
                    {
                        ErrorMessage = transportResponse,
                        Code = Code
                    };
                }
            }
            return currentUser;
        }
        private async void ConectWebSocket()
        {

            ClientWebSocket wss = new ClientWebSocket();
            wss.Options.Cookies = Instance.Auth;
            foreach (Cookie c in Instance.Auth.GetCookies(new Uri(domain)))
            {
                if (c.Name == "BPMCSRF" && !String.IsNullOrEmpty(c.Value))
                {
                    wss.Options.SetRequestHeader(c.Name, c.Value);
                    wss.Options.KeepAliveInterval = TimeSpan.FromSeconds(60);
                }
            }

            string socketDomain;
            if (domain.StartsWith("https://", StringComparison.Ordinal))
            {
                socketDomain = domain.Replace("https://", "wss://");
            }
            else
            {
                socketDomain = domain.Replace("http://", "ws://");
            }

            await wss.ConnectAsync(new Uri($"{socketDomain}/0/Nui/ViewModule.aspx.ashx"), CancellationToken.None).ConfigureAwait(false);
            while (wss.State == WebSocketState.Open)
            {
                ArraySegment<Byte> buffer = new ArraySegment<byte>(new Byte[8192]);
                WebSocketReceiveResult result;
                using (var ms = new MemoryStream())
                {
                    do
                    {
                        result = await wss.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                    }
                    while (!result.EndOfMessage);

                    ms.Seek(0, SeekOrigin.Begin);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        using (var reader = new StreamReader(ms, Encoding.UTF8))
                        {
                            string txt = reader.ReadToEnd();
                            WebSocketMessageReceivedEventArgs e = JsonConvert.DeserializeObject<WebSocketMessageReceivedEventArgs>(txt);
                            Instance.OnWebSocketMessageReceived(e);
                        }
                    }
                }
            }
            wss.Dispose();
        }
        private void OnWebSocketMessageReceived(WebSocketMessageReceivedEventArgs e)
        {
            EventHandler<WebSocketMessageReceivedEventArgs> handler = WebSocketMessageReceived;
            handler?.Invoke(this, e);
        }
        #endregion
    }
}
