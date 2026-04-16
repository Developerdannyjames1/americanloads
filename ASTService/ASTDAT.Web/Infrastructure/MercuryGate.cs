using ASTDAT.Data.Models;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace ASTDAT.Web.Infrastructure
{
    public class MercuryGate
    {
        public class LoadModel<T>
        {
            public string RespondBy { get; set; }
            public string LoadId { get; set; }
            public string LoadClientId { get; set; }
            public string OriginCity { get; set; }
            public string OriginState { get; set; }
            public string PickupRange { get; set; }
            public string DestinationCity { get; set; }
            public string DestinationState { get; set; }
            public string DeliveryRange { get; set; }
            public string CurrentBid { get; set; }
            public string LowestBid { get; set; }
            public string BookItNow { get; set; }
            public string NumberOfStops { get; set; }
            public string LoadOwner { get; set; }
            public string Equipment { get; set; }
            public string LoadPlanner { get; set; }
            public string Weight { get; set; }
            public string LengthWidthHeight { get; set; }
            public string Description { get; set; }
            public string HazMat { get; set; }


            public T AdvancedData { get; set; }
        }

        public class BidsResult<T>
        {
            public List<LoadModel<T>> Loads { get; set; }
            public List<Exception> Exceptions { get; set; }
        }

        public async Task<BidsResult<T>> GetBids<T>(string userId, string password)
        {
            var result = new BidsResult<T>
            {
                Loads = new List<LoadModel<T>>(),
                Exceptions = new List<Exception>(),
            };

            try
            {
                CookieContainer cookieContainer = new CookieContainer();
                HttpClientHandler handler = new HttpClientHandler();
                handler.CookieContainer = cookieContainer;
                var httpClient = new HttpClient(handler);

                var httpContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("UserId", userId),
                    new KeyValuePair<string, string>("Password", password),
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

                    //url = "https://t-insight.mercurygate.net/MercuryGate/newmenu/PortalFrame.jsp?returnUrl=..%2Fmainframe%2FMainFrame.jsp&bCompaySelected=false";
                    //url = "https://t-insight.mercurygate.net/MercuryGate/transport/portletBidView.jsp?ListCacheKey=portletFrameportletBidViewCarrierManageBids";
                    url = "https://t-insight.mercurygate.net/MercuryGate/transport/portletBidView.jsp?norefresh=&ListCacheKey=portletFrameportletBidViewCarrierManageBids";
                    response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    var html = await response.Content.ReadAsStringAsync();

                    using (var db = new DBContext())
                    {
                        var doc = new HtmlDocument();
                        doc.LoadHtml(html);
                        var tables = doc.DocumentNode.SelectNodes("//table");
                        foreach (var table in tables)
                        {
                            foreach (var row in table.SelectNodes("tr").Skip(1))
                            {
                                try
                                {
                                    var rows = row.SelectNodes("td");
                                    var href = rows[1].SelectSingleNode("a");
                                    var load = new LoadModel<T>
                                    {
                                        RespondBy = rows[0].InnerHtml,
                                        LoadId = href.Attributes["href"].Value.Replace("javascript:displayTransport(", "").Replace(");", ""),
                                        LoadClientId = href.InnerHtml,
                                        OriginCity = rows[2].InnerHtml,
                                        OriginState = rows[3].InnerHtml,
                                        PickupRange = rows[4].InnerHtml,
                                        DestinationCity = rows[5].InnerHtml,
                                        DestinationState = rows[6].InnerHtml,
                                        DeliveryRange = rows[7].InnerHtml,
                                        CurrentBid = "",
                                        LowestBid = rows[9].InnerHtml,
                                        BookItNow = rows[10].InnerHtml,
                                        NumberOfStops = rows[11].InnerHtml,
                                        LoadOwner = rows[12].InnerHtml,
                                        Equipment = rows[13].InnerHtml,
                                        LoadPlanner = rows[14].InnerHtml,
                                    };
                                    result.Loads.Add(load);

                                    /*
                                    Get 
                                    - Weight, 
                                    - Len x Width x Hght
                                    - and Description 
                                    from the link in the United Rentals email. Example of output:
                                    Van NO STEP DECKS PPE REQUIRED Deliver 5-17 by 1200. Weight: 46,000 Dimensions: 48.0 x 8.0 X 8.0 ft  Item: (33)40'KD SHEETS
                                    */
                                    try
                                    {
                                        url = $"https://t-insight.mercurygate.net/MercuryGate/transport/portletBidTransportDetail.jsp?norefresh=&sOid={load.LoadId}&bShowGL=false&bAllowUpdate=false";
                                        response = await httpClient.GetAsync(url);
                                        response.EnsureSuccessStatusCode();
                                        var details = await response.Content.ReadAsStringAsync();

                                        var docDetails = new HtmlDocument();
                                        docDetails.LoadHtml(details);
                                        var tablesDetails = docDetails.DocumentNode.SelectNodes("//table");
                                        var subTables = tablesDetails[0]?
                                            .SelectNodes("tr")?
                                            .FirstOrDefault()?
                                            .SelectNodes("td")?
                                            .FirstOrDefault()?
                                            .SelectNodes("//table");

                                        if (subTables != null)
                                        {
                                            foreach (var subTable in subTables)
                                            {
                                                var td1 = subTable
                                                    .SelectNodes("tr")?
                                                    .FirstOrDefault()?
                                                    .SelectNodes("td")?
                                                    .FirstOrDefault();
                                                if (td1?.InnerHtml == "Item ID")
                                                {
                                                    var items = subTable
                                                        .SelectNodes("tr")?
                                                        .Skip(1)?
                                                        .FirstOrDefault()?
                                                        .SelectNodes("td");

                                                    if (items != null && items.Count > 7)
                                                    {
                                                        load.Weight = items[4].InnerHtml;
                                                        load.LengthWidthHeight = items[6].InnerHtml;
                                                        load.Description = items[7].InnerHtml;
                                                        load.HazMat = items[1].InnerHtml;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception exc)
                                    {
                                        result.Exceptions.Add(exc);
                                    }
                                }
                                catch (Exception exc)
                                {
                                    result.Exceptions.Add(exc);
                                }
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    result.Exceptions.Add(exc);
                }
            }
            catch (Exception exc)
            {
                result.Exceptions.Add(exc);
            }

            return result;
        }
    }
}