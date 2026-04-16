using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Security.Cryptography;
using System.IO;
using ASTDAT.Data.Models;
using ASTDAT.Tools;
//using Newtonsoft.Json;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            //var t = GetBids();
            //t.Wait();
            //var html = t.Result;
            //Parse(html);
            //Parse(System.IO.File.ReadAllText("Bids.html"));

            //var t = GetLoads();
            //t.Wait();
            //var pages = t.Result;

            BanyanTechnologyUtils banyanTechnologyUtils = new BanyanTechnologyUtils();
            var t = banyanTechnologyUtils.GetLoads("Dabundis", "RainforRent");
            t.Wait();
            var r = t.Result;

            var t2 = banyanTechnologyUtils.ParseList(r["_list_"]); ;
            t2.Wait();
            var pages = t2.Result;

            //var pages = new Dictionary<string, string> { { "qq", System.IO.File.ReadAllText("BT_Load.html") } };
            //banyanTechnologyUtils.MULTI_TEST = System.IO.File.ReadAllText("BT_Multi.html");
            //var pages = banyanTechnologyUtils.ParseList(System.IO.File.ReadAllText("BT_Loads.html"));

            foreach (var page in pages)
            {
                //var m = banyanTechnologyUtils.ParsePage(page.Value);
            }
        }

        private static async Task<Dictionary<string, string>> GetLoads()
        {
            var pages = new Dictionary<string, string>();

            CookieContainer cookieContainer = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = cookieContainer;
            var httpClient = new HttpClient(handler);

            var url = "http://logistics.banyantechnology.com/index.aspx?strBrand=";
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var loginPage = await response.Content.ReadAsStringAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(loginPage);

            var inputs = doc.DocumentNode.SelectNodes("//input");
            var __VIEWSTATE = "";
            var __VIEWSTATEGENERATOR = "";
            var __EVENTVALIDATION = "";
            foreach (var input in inputs)
            {
                if (input.Id == "__VIEWSTATE") __VIEWSTATE = input.GetAttributeValue("value", "");
                if (input.Id == "__VIEWSTATEGENERATOR") __VIEWSTATEGENERATOR = input.GetAttributeValue("value", "");
                if (input.Id == "__EVENTVALIDATION") __EVENTVALIDATION = input.GetAttributeValue("value", "");
            }
            ///__LASTFOCUS
            //__VIEWSTATE
            //__VIEWSTATEGENERATOR
            ///__EVENTTARGET
            ///__EVENTARGUMENT
            //__EVENTVALIDATION
            var httpContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("__VIEWSTATE", __VIEWSTATE),
                new KeyValuePair<string, string>("__VIEWSTATEGENERATOR", __VIEWSTATEGENERATOR),
                new KeyValuePair<string, string>("__EVENTVALIDATION", __EVENTVALIDATION),
                new KeyValuePair<string, string>("ctl00$strbrand", "BanyanTechnology"),
                new KeyValuePair<string, string>("ctl00$cphBody$txtUsername", "Dabundis"),
                new KeyValuePair<string, string>("ctl00$cphBody$txtPassword", "RainforRent"),
                new KeyValuePair<string, string>("ctl00$cphBody$cmdClientLogin", "Login"),
            });

            try
            {
                response = await httpClient.PostAsync(url, httpContent);
                response.EnsureSuccessStatusCode();
                var cookies = cookieContainer.GetCookies(new Uri(url));
                foreach (var cookie in cookies)
                {

                }

                //url = "http://logistics.banyantechnology.com/Cobrand/MainMenu.aspx";
                url = "http://logistics.banyantechnology.com/cobrand/viewloads.aspx?searchtype=carrier&show=QTND";
                response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                //return await response.Content.ReadAsStringAsync();
                doc.LoadHtml(await response.Content.ReadAsStringAsync());
                var tables = doc.DocumentNode.SelectNodes("//table");
                foreach (var table in tables)
                {
                    foreach (var row in table.SelectNodes("tr"))
                    {
                        foreach (var col in row.SelectNodes("td"))
                        {
                            var href = col.SelectSingleNode("a");
                            if (href != null && href.GetAttributeValue("href", "").IndexOf("extendedload.aspx") == 0)
                            {
                                url = "http://logistics.banyantechnology.com/cobrand/" + href.GetAttributeValue("href", "");
                                response = await httpClient.GetAsync(url);
                                response.EnsureSuccessStatusCode();
                                var load = await response.Content.ReadAsStringAsync();
                                pages.Add(href.GetAttributeValue("href", ""), load);
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {

            }
            return pages;
        }

        private static void Parse(string html)
        {
            try
            {
                using (var db = new DBContext())
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    var tables = doc.DocumentNode.SelectNodes("//table");
                    foreach (var table in tables)
                    {
                        foreach (var row in table.SelectNodes("tr"))
                        {
                            foreach (var col in row.SelectNodes("td"))
                            {
                                if (col.InnerHtml.IndexOf("displayTransport") > 0)
                                {
                                    var href = col.SelectSingleNode("a");
                                    var load = db.Loads.FirstOrDefault(x => x.ClientLoadNum == href.InnerHtml);
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception exc)
            {

            }
        }

        private static async Task<string> GetBids()
        { 
            CookieContainer cookieContainer = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = cookieContainer;
            var httpClient = new HttpClient(handler);

            var httpContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("UserId", "dispatch@americanspecialized.org"),
                new KeyValuePair<string, string>("Password", "Trucking1"),
                new KeyValuePair<string, string>("RememberMe", "true"),
                new KeyValuePair<string, string>("submitbutton", "Увійти"),
                new KeyValuePair<string, string>("NoAutoLogin", "true"),
                new KeyValuePair<string, string>("menus", "top"),
                new KeyValuePair<string, string>("inline", "true"),
            });

            var url = "https://t-insight.mercurygate.net/MercuryGate/login/LoginProcess.jsp";
            try
            {
                var response = await httpClient.PostAsync(url, httpContent);
                response.EnsureSuccessStatusCode();
                var cookies = cookieContainer.GetCookies(new Uri(url));
                foreach (var cookie in cookies)
                {

                }

                url = "https://t-insight.mercurygate.net/MercuryGate/newmenu/PortalFrame.jsp?returnUrl=..%2Fmainframe%2FMainFrame.jsp&bCompaySelected=false";
                url = "https://t-insight.mercurygate.net/MercuryGate/transport/portletBidView.jsp?ListCacheKey=portletFrameportletBidViewCarrierManageBids";
                url = "https://t-insight.mercurygate.net/MercuryGate/transport/portletBidView.jsp?norefresh=&ListCacheKey=portletFrameportletBidViewCarrierManageBids";
                response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch(Exception exc)
            {

            }
            return "";
        }
    }
}
