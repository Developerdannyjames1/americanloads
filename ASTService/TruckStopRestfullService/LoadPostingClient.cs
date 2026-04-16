using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ASTDAT.Data;
using ASTDAT.Data.Models;
using ASTDAT.Tools;
using Newtonsoft.Json;
using TruckStopRestfullService.Models;

namespace TruckStopRestfullService
{
    public class LoadPostingClient : HttpClient
    {
        //string baseUrl = "https://api-int.truckstop.com";

        //string userName = "ValencioRobinson@mailinator.truckstop.com";
        //string passw = "24rvbUBm";
        //string clientId = "69E5B456-8701-4EC4-B151-C114B9BBBFA0";
        //string clientSecret = "1A0287A7-663F-4CF3-85A0-54FA33219CA9";
        //string base64 = "NjlFNUI0NTYtODcwMS00RUM0LUIxNTEtQzExNEI5QkJCRkEwOjFBMDI4N0E3LTY2M0YtNENGMy04NUEwLTU0RkEzMzIxOUNBOQ==";

        string baseUrl = "https://api.truckstop.com";

        string userName = "dispatch@americanspecialized.org";
        string passw = "Trucking1";
        string clientId = "DAE35F91-E6C6-49CC-8A81-6767964F1386";
        string clientSecret = "89A104B0-44C7-429E-BC33-63329145644A";
        string base64 = "REFFMzVGOTEtRTZDNi00OUNDLThBODEtNjc2Nzk2NEYxMzg2Ojg5QTEwNEIwLTQ0QzctNDI5RS1CQzMzLTYzMzI5MTQ1NjQ0QQ==";


        string access_token; // = "b0afea53e63a4105b98adffa4c7a19f7b58f08bb336f4e1c9118f8574db06c21";
        string refresh_token; // = "f08fc890b8484ead8c4fd68d882004f3f51d9d113540476ea7ace2b16ffa5d3c";
        private DateTime? token_Expired = null;

        public string Access_token { get => access_token; set => access_token = value; }
        public string Refresh_token { get => refresh_token; set => refresh_token = value; }

        public DateTime? Token_Expired { get => token_Expired; set => token_Expired = value; }
        public string UserName { get => userName; set => userName = value; }
        public string Password { get => passw; set => passw = value; }
        public string BaseUrl { get => baseUrl; set => baseUrl = value; }
        public string ClientId { get => clientId; set => clientId = value; }
        public string ClientSecret { get => clientSecret; set => clientSecret = value; }

        private object lockObject = new object();

        public bool IsConnected
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Access_token) && Token_Expired > DateTime.Now; //CheckLogin();
            }
        }

        bool isConnected = false;

        public string LastError { get; set; }

        public LoadPostingClient() 
        {
            try
            {
                BaseUrl = ConfigurationManager.AppSettings["TSRestFullURL"];
                UserName = ConfigurationManager.AppSettings["TSRestFullUser"];
                Password = ConfigurationManager.AppSettings["TSRestFullPass"];
                ClientId = ConfigurationManager.AppSettings["TSRestFullClientId"];
                ClientSecret = ConfigurationManager.AppSettings["TSRestFullClientSecret"];

                base64 = Convert.ToBase64String(Encoding.Default.GetBytes($"{ClientId}:{ClientSecret}"));
            }
            catch (Exception exc)
            {
                Logger.WriteError($"Read data failed: {exc.Message}");
            }
            // ConnectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        }

        private async Task<bool> GetTokenAsync()
        {
            LastError = "";
            var actionUrl = "/auth/token";

            var url = BaseUrl + actionUrl;

            var httpContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("scope", "truckstop"),
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("username", UserName),
                new KeyValuePair<string, string>("password", Password),
            });
            DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await PostAsync(url, httpContent).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                Logger.WriteError($"TS GetToken error: {response.ReasonPhrase}");
                return false;
            }
            
            var tokenJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false); // ReadAsStreamAsync().Result;
            var respObj = JsonConvert.DeserializeObject<GetTokenResponse>(tokenJson);
            if (respObj == null) return false;
            using (var db = new DBContext())
            {
                var lastToken = new TSLoginModel
                {
                    DateTime = DateTime.Now,
                    AccessToken = respObj.Access_Token,
                    RefreshToken = respObj.Refresh_Token,
                    Expiration = DateTime.Now.AddSeconds(respObj.Expires_In)
                };
                db.TSLogins.Add(lastToken);
                db.SaveChanges();
                Access_token = lastToken.AccessToken;
                Refresh_token = lastToken.RefreshToken;
                Token_Expired = lastToken.Expiration;
            }
            return true;
        }

        private async Task RefreshTokenAsync(string refreshToken)
        {
            LastError = "";
            var actionUrl = "/auth/token";

            var url = BaseUrl + actionUrl;

            var httpContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("scope", "truckstop"),
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
            });
            //DefaultRequestHeaders.Authorization = null;
            DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await PostAsync(url, httpContent).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return;
            
            var tokenJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false); // ReadAsStreamAsync().Result;
            var respObj = JsonConvert.DeserializeObject<GetTokenResponse>(tokenJson);
            if (respObj == null) return;
            using (var db = new DBContext())
            {
                var lastToken = new TSLoginModel
                {
                    DateTime = DateTime.Now,
                    AccessToken = respObj.Access_Token,
                    RefreshToken = respObj.Refresh_Token,
                    Expiration = DateTime.Now.AddSeconds(respObj.Expires_In),
                    Message = "Refreshed"
                };
                db.TSLogins.Add(lastToken);
                db.SaveChanges();
                Access_token = lastToken.AccessToken;
                Refresh_token = lastToken.RefreshToken;
                Token_Expired = lastToken.Expiration;
            }
            return;

        }
        public async Task<bool> Login(bool ignoreConnection = false)
        {
            LastError = "";
            if (ignoreConnection)
            {
                return await GetTokenAsync().ConfigureAwait(false);
            }
            if (IsConnected) return true;
            try
            {
                TSLoginModel connInfo = null;
                using (var db = new DBContext())
                {
                    connInfo = db.TSLogins
                        .Where(x => x.Expiration > DateTime.Now && x.AccessToken != null && x.RefreshToken != null)
                        .OrderByDescending(x => x.DateTime)
                        .FirstOrDefault();

                    if (connInfo != null)
                    {
                        Access_token = connInfo.AccessToken;
                        Refresh_token = connInfo.RefreshToken;
                        Token_Expired = connInfo.Expiration;
                    }
                    else
                    {
                        connInfo = db.TSLogins.OrderByDescending(x => x.DateTime).FirstOrDefault();
                        if (connInfo != null && !string.IsNullOrWhiteSpace(connInfo.RefreshToken))
                        {
                            var rToken = connInfo.RefreshToken;
                            connInfo.RefreshToken = null;
                            db.SaveChanges();
                            await RefreshTokenAsync(rToken).ConfigureAwait(false);
                        }
                    }
                }

                if (IsConnected) return true;

                return await GetTokenAsync().ConfigureAwait(false);
            }
            catch (Exception exc)
            {
                LastError = exc.Message;
                return false;
            }
        }

        public async Task<string> GetDataAsync(string url)
        {
            LastError = "";
            
            if (!IsConnected && !(await Login().ConfigureAwait(false))) return "";
            DefaultRequestHeaders.Clear();
            DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",Access_token);
            DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")); 

            if (!url.StartsWith("/")) url = "/" + url;

            var response = await GetAsync(BaseUrl + url).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (json.StartsWith("[") || json.StartsWith("{["))
                {
                    var respObj = JsonConvert.DeserializeObject<List<TsResponse>>(json);
                    LastError = $"{response.ReasonPhrase} - {string.Join(", ", respObj.SelectMany(s => s.StatusSet.Select(p => p.Message)))}";
                }
                else
                {
                    var respObj = JsonConvert.DeserializeObject<TsResponse>(json);
                    LastError = $"{response.ReasonPhrase} - {string.Join(", ", respObj.StatusSet.Select(p => p.Message))}";
                }
                Logger.WriteError($"TS GetData error: {response.ReasonPhrase} - {LastError}");
                return json;
            }

            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        public async Task<string> PostDataAsync(string url, TsRequest request)
        {
            LastError = "";
            if (!IsConnected && !(await Login().ConfigureAwait(false))) return "";
            DefaultRequestHeaders.Clear();
            DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Access_token);
            DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!url.StartsWith("/")) url = "/" + url;

            var rJson = request.ToJson();
            var content = new StringContent(rJson, Encoding.UTF8, "application/json");

            var response = await PostAsync(BaseUrl + url, content).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                try
                {
                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (json.StartsWith("[") || json.StartsWith("{["))
                    {
                        var respObj = JsonConvert.DeserializeObject<List<TsResponse>>(json);
                        LastError = $"{response.ReasonPhrase} - {string.Join(", ", respObj.SelectMany(s => s.StatusSet.Select(p => p.Message)))}";
                    }
                    else
                    {
                        var respObj = JsonConvert.DeserializeObject<TsResponse>(json);
                        LastError = $"{response.ReasonPhrase} - {string.Join(", ", respObj.StatusSet.Select(p => p.Message))}";
                    }
                    Logger.WriteError($"TS PostData error: {LastError}");
                    Logger.Write($"TS PostData error: {LastError}\r{BaseUrl + url}\r{rJson}", fileName: "TSrequests.txt");
                    return json;
                }
                catch (Exception exc)
                {
                    Logger.WriteError($"Data parsing error: {exc.Message}");
                    LastError = exc.Message;
                    return "";
                }
            }

            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }       
        public async Task<string> PutDataAsync(string url, TsRequest request)
        {
            if (!IsConnected && !(await Login().ConfigureAwait(false))) return "";
            LastError = "";
            DefaultRequestHeaders.Clear();
            DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Access_token);
            DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!url.StartsWith("/")) url = "/" + url;

            var rJson = request.ToJson();
            var content = new StringContent(rJson, Encoding.UTF8, "application/json");

            var response = await PutAsync(BaseUrl + url, content).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                try
                {
                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (json.StartsWith("[") || json.StartsWith("{["))
                    {
                        var respObj = JsonConvert.DeserializeObject<List<TsResponse>>(json);
                        LastError = $"{response.ReasonPhrase} - {string.Join(", ", respObj.SelectMany(s=>s.StatusSet.Select(p=>p.Message)))}";
                    }
                    else
                    {
                        var respObj = JsonConvert.DeserializeObject<TsResponse>(json);
                        LastError = $"{response.ReasonPhrase} - {string.Join(", ", respObj.StatusSet.Select(p => p.Message))}";
                    }
                    Logger.WriteError($"TS PutData error: {LastError}");
                    Logger.Write($"TS PutData error: {LastError}\r{BaseUrl + url}\r{rJson}",fileName: "TSrequests.txt");
                    return json;
                }
                catch (Exception exc)
                {
                    Logger.WriteError($"Data parsing error: {exc.Message}");
                    LastError = exc.Message;
                    return "";
                }
            }

            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
        public async Task<bool> DeleteLoadAsync(string loadId, int reason = 0)
        {
            if (!IsConnected && !(await Login().ConfigureAwait(false))) return false;
            LastError = "";
            DefaultRequestHeaders.Clear();
            DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Access_token);
            DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var url = $"/loadmanagement/v3/load/{loadId}";

            var request = new DeleteRequest {Reason = reason};

            var rJson = request.ToJson();
            var content = new StringContent(rJson, Encoding.UTF8, "application/json");

            HttpRequestMessage httpRequest = new HttpRequestMessage
            {
                Content = content,
                Method = HttpMethod.Delete,
                RequestUri = new Uri(BaseUrl + url)
            };

            var response = await SendAsync(httpRequest).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.NoContent)
            {
                try
                {
                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (json.StartsWith("[") || json.StartsWith("{["))
                    {
                        var respObj = JsonConvert.DeserializeObject<List<TsResponse>>(json);
                        LastError = $"{response.ReasonPhrase} - {string.Join(", ", respObj.SelectMany(s=>s.StatusSet.Select(p=>p.Message)))}";
                    }
                    else
                    {
                        var respObj = JsonConvert.DeserializeObject<TsResponse>(json);
                        LastError = $"{response.ReasonPhrase} - {string.Join(", ", respObj.StatusSet.Select(p => p.Message))}";
                    }
                    Logger.WriteError($"Delete Load error: {LastError}");
                    Logger.Write($"TS Delete error: {LastError}\r{BaseUrl + url}\r{rJson}", fileName: "TSrequests.txt");
                    return false;
                }
                catch (Exception exc)
                {
                    Logger.WriteError($"Data parsing error: {exc.Message}");
                    LastError = exc.Message;
                    return false;
                }
            }

            return true;
        }

        public string GetLoads(int page = 1)
        {
            var req = new SearchRequest
            {
                Pagination = new Pagination
                {
                    PageSize = 100, 
                    PageNumber = page
                }
            };

            //req.SearchCriteria.Add(
            //    new SearchCriteria
            //    {
            //        Name = "OriginEarlyDateTime",
            //        Value = "01/01/2021",
            //        Operator = "gte"
            //    });

            return PostDataAsync("loadmanagement/v2/load/search", req).Result;
        }
        public string GetLoadByNumber(string loadNumber)
        {
            var req = new SearchRequest
            {
                Pagination = new Pagination
                {
                    PageSize = 100, 
                    PageNumber = 1
                }
            };

            req.SearchCriteria.Add(
                new SearchCriteria
                {
                    Name = "LoadNumber",
                    Value = loadNumber,
                    Operator = "Eq"
                });

            return PostDataAsync("loadmanagement/v2/load/search", req).Result;
        }
        public string GetLoadById(Guid loadId)
        {
            return GetLoadById(loadId.ToString());
        }
        public string GetLoadById(string loadId)
        {
            var req = new SearchRequest
            {
                Pagination = new Pagination
                {
                    PageSize = 100, 
                    PageNumber = 1
                }
            };

            req.SearchCriteria.Add(
                new SearchCriteria
                {
                    Name = "LoadId",
                    Value = loadId,
                    Operator = "Eq"
                });

            return PostDataAsync("loadmanagement/v2/load/search", req).Result;
        }
    }
}
