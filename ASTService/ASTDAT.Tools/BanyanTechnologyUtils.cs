using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ASTDAT.Tools
{
    public class BanyanTechnologyUtils
    {
        CookieContainer cookieContainer = new CookieContainer();
        HttpClientHandler handler = new HttpClientHandler();
        HttpClient httpClient;

        public async Task<Dictionary<string, string>> GetLoads(string userName, string userPass)
        {
            handler.CookieContainer = cookieContainer;
            httpClient = new HttpClient(handler);

            var pages = new Dictionary<string, string>();

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

            var httpContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("__VIEWSTATE", __VIEWSTATE),
                new KeyValuePair<string, string>("__VIEWSTATEGENERATOR", __VIEWSTATEGENERATOR),
                new KeyValuePair<string, string>("__EVENTVALIDATION", __EVENTVALIDATION),
                new KeyValuePair<string, string>("ctl00$strbrand", "BanyanTechnology"),
                new KeyValuePair<string, string>("ctl00$cphBody$txtUsername", userName),
                new KeyValuePair<string, string>("ctl00$cphBody$txtPassword", userPass),
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

                url = "http://logistics.banyantechnology.com/cobrand/viewloads.aspx?searchtype=carrier&show=QTND";
                response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var html = await response.Content.ReadAsStringAsync();
                pages.Add("_list_", html);
                doc.LoadHtml(html);
                var tables = doc.DocumentNode.SelectNodes("//table");
                foreach (var table in tables)
                {
                    foreach (var row in table.SelectNodes("tr"))
                    {
                        foreach (var col in row.SelectNodes("td"))
                        {
                            var href = col.SelectSingleNode("a");
                            if (href != null && (href.GetAttributeValue("href", "").IndexOf("extendedload.aspx") == 0/* || href.GetAttributeValue("href", "").IndexOf("multistop.aspx") == 0*/))
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

        private HtmlDocument doc = new HtmlDocument();
        public string MULTI_TEST = "";

        public async Task<List<LoadModel>> ParseList(string page)
        {
            var result = new List<LoadModel>();

            var doc = new HtmlDocument();
            doc.LoadHtml(page);

            var tables = doc.DocumentNode.SelectNodes("//table");
            foreach (var table in tables)
            {
                foreach (var row in table.SelectNodes("tr"))
                {
                    var cols = row.SelectNodes("td");
                    if (cols.Count > 12)
                    {
                        var href = cols[0].SelectSingleNode("a");
                        if (href == null || !href.GetAttributeValue("href", "").Contains("LoadID="))
                        {
                            continue;
                        }
                        var loadId = href.GetAttributeValue("href", "").ToLower();
                        if (loadId.Contains("multistop.aspx"))
                        {
                            await ParseMulti(result, loadId);
                            continue;
                        }
                        loadId = loadId.Substring(loadId.IndexOf("loadid=") + 7);


                        var origin = cols[6].InnerHtml.Replace("<br />", "|").Replace("<br>", "|").Replace(",", "|").Split('|');
                        var dest = cols[7].InnerHtml.Replace("<br />", "|").Replace("<br>", "|").Replace(",", "|").Split('|');
                        if (origin.Length != 3 || dest.Length != 3)
                        {
                            continue;
                        }
                        var pickup = cols[8].InnerHtml.Replace("<br>", " ").Replace("<br />", " ");
                        var delivery = cols[9].InnerHtml.Replace("<br>", " ").Replace("<br />", " ");

                        result.Add(new LoadModel
                        {
                            LoadType = cols[1].InnerHtml,
                            Client = cols[2].InnerHtml,
                            BOLNum = cols[3].GetAttributeValue("title", ""),
                            LoadId = loadId,
                            Origin = new MerchantModel
                            {
                                City = origin[0],
                                State = origin[1],
                                ZipCode = origin[2],
                                PickupDate = pickup,
                            },
                            Destination = new MerchantModel
                            {
                                City = dest[0],
                                State = dest[1],
                                ZipCode = dest[2],
                                DeliveryDate = delivery,
                            },
                            Status = cols[12].InnerHtml,
                            Wt = cols[10].InnerHtml,
                            Qty = cols[11].InnerHtml,
                        });
                    }
                }
            }
            return result;
        }

        public async Task<List<LoadModel>> ParseMulti(List<LoadModel> result, string url)
        {
            var html = MULTI_TEST;
            if (html == "")
            {
                url = "http://logistics.banyantechnology.com/cobrand/" + url;
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                html = await response.Content.ReadAsStringAsync();
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var tables = doc.DocumentNode.SelectNodes("//table");
            foreach (var table in tables)
            {
                if (table.Id != "tStops")
                {
                    continue;
                }
                foreach(var rows in table.SelectNodes("tr").Skip(1))
                {
                    foreach(var col in rows.SelectNodes("td"))
                    {
                        var href = col.SelectSingleNode("a");
                        if (href == null || !href.GetAttributeValue("href", "").Contains("LoadID="))
                        {
                            continue;
                        }
                        var loadId = href.GetAttributeValue("href", "").ToLower();
                        if (!loadId.Contains("extendedload.aspx"))
                        {
                            continue;
                        }

                        if (httpClient != null)
                        {
                            url = "http://logistics.banyantechnology.com/" + loadId.Substring(3);
                            var response = await httpClient.GetAsync(url);
                            response.EnsureSuccessStatusCode();

                            html = await response.Content.ReadAsStringAsync();
                        }
                        else
                        {
                            html = System.IO.File.ReadAllText($"BT_{loadId.Substring(loadId.IndexOf("loadid=") + 7)}.html");
                        }

                        result.Add(ParsePage(html));
                    }
                }
            }

            return result;
        }

        public LoadModel ParsePage(string page)
        {
            doc.LoadHtml(page);

            var status = doc.GetElementbyId("ctl00_cphBody_lblStatus");
            if (status.SelectSingleNode("b") != null)
            {
                status = status == null ? status : status.SelectSingleNode("b");
            }
            var loadId = doc.GetElementbyId("ctl00_cphBody_lblLoadID");
            var BOLNum = doc.GetElementbyId("ctl00_cphBody_lblBOLNum");
            if (BOLNum.SelectSingleNode("b") != null)
            {
                BOLNum = BOLNum == null ? BOLNum : BOLNum.SelectSingleNode("b");
            }

            var load = new LoadModel();

            load.Status = status == null ? "" : status.InnerHtml.Replace("Status: ", "");
            load.LoadId = loadId == null ? "" : loadId.InnerHtml.Replace("LoadID: ", "");
            load.BOLNum = BOLNum == null ? "" : BOLNum.InnerHtml.Replace("BOL #: ", "");

            load.Origin = ParseMerchant("ctl00_cphBody_lblShipper");
            load.Destination = ParseMerchant("ctl00_cphBody_lblConsignee");

            var full = doc.GetElementbyId("ddlNameOfService1");
            foreach(var item in full.SelectNodes("option"))
            {
                if (item.OuterHtml.Contains("selected"))
                {
                    load.IsFull = item.InnerHtml;
                }
            }
            var equip = doc.GetElementbyId("ddlServiceEquipmentType1");
            foreach (var item in equip.SelectNodes("option"))
            {
                if (item.OuterHtml.Contains("selected"))
                {
                    load.LoadType = item.InnerHtml;
                }
            }

            return load;
        }

        private MerchantModel ParseMerchant(string id)
        {
            var table = doc
                .GetElementbyId(id)
                .SelectSingleNode("table")
                .SelectSingleNode("tr")
                .SelectSingleNode("td")
                .SelectSingleNode("table");
            var address = new MerchantModel();

            foreach (var tr in table.SelectNodes("tr"))
            {
                var cols = tr.SelectNodes("td");
                if (cols.Count > 1 && $"{cols[0].InnerHtml}".ToLower().Trim() == "city:")           address.City = $"{cols[1].InnerHtml}".Trim();
                if (cols.Count > 1 && $"{cols[0].InnerHtml}".ToLower().Trim() == "state:")          address.State = $"{cols[1].InnerHtml}".Trim();
                if (cols.Count > 1 && $"{cols[0].InnerHtml}".ToLower().Trim() == "zip code:")       address.ZipCode = $"{cols[1].InnerHtml}".Trim();
                if (cols.Count > 1 && $"{cols[0].InnerHtml}".ToLower().Trim() == "country:")        address.Country = $"{cols[1].InnerHtml}".Trim();
                if (cols.Count > 1 && $"{cols[0].InnerHtml}".ToLower().Trim() == "location name:")  address.LocationName = $"{cols[1].InnerHtml}".Trim();
                if (cols.Count > 1 && $"{cols[0].InnerHtml}".ToLower().Trim() == "address:")        address.Address = $"{cols[1].InnerHtml}".Trim();

                if (cols.Count > 1 && $"{cols[0].InnerHtml}".ToLower().Trim() == "name:")           address.Name = $"{cols[1].InnerHtml}".Trim();
                if (cols.Count > 1 && $"{cols[0].InnerHtml}".ToLower().Trim() == "phone:")          address.Phone = $"{cols[1].InnerHtml}".Trim();
                if (cols.Count > 1 && $"{cols[0].InnerHtml}".ToLower().Trim() == "fax:")            address.Fax = $"{cols[1].InnerHtml}".Trim();
                if (cols.Count > 1 && $"{cols[0].InnerHtml}".ToLower().Trim() == "email:")          address.Email = $"{cols[1].InnerHtml}".Trim();

                if (cols.Count > 1 && $"{cols[0].InnerHtml}".ToLower().Trim() == "dock name:") address.DockName = $"{cols[1].InnerHtml}".Trim();
                if (cols.Count > 1 && $"{cols[0].InnerHtml}".ToLower().Trim() == "note:") address.Note = $"{cols[1].InnerHtml}".Trim();
                if (cols.Count > 1 && $"{cols[0].InnerHtml}".ToLower().Trim() == "dock open:") address.DockOpen = $"{cols[1].InnerHtml}".Trim();
                if (cols.Count > 1 && $"{cols[0].InnerHtml}".ToLower().Trim() == "dock close:") address.DockClose = $"{cols[1].InnerHtml}".Trim();
                if (cols.Count > 1 && $"{cols[0].InnerHtml}".ToLower().Trim() == "pickup date:") address.PickupDate = $"{cols[1].InnerHtml}".Trim();
                if (cols.Count > 1 && $"{cols[0].InnerHtml}".ToLower().Trim() == "pickup time:") address.PickupTime = $"{cols[1].InnerHtml}".Trim();
                if (cols.Count > 1 && $"{cols[0].InnerHtml}".ToLower().Trim() == "pickup num:") address.PickupNum = $"{cols[1].InnerHtml}".Trim();
                if (cols.Count > 1 && $"{cols[0].InnerHtml}".ToLower().Trim() == "delivery date:") address.DeliveryDate = $"{cols[1].InnerHtml}".Trim();
                if (cols.Count > 1 && $"{cols[0].InnerHtml}".ToLower().Trim() == "delivery time:") address.DeliveryTime = $"{cols[1].InnerHtml}".Trim();
                if (cols.Count > 1 && $"{cols[0].InnerHtml}".ToLower().Trim() == "delivery num:") address.DeliveryNum = $"{cols[1].InnerHtml}".Trim();
            }

            return address;
        }

        public class LoadModel
        {
            public string Status { get; set; }
            public string LoadId { get; set; }
            public string BOLNum { get; set; }
            public string LoadType { get; set; }
            public string Client { get; set; }
            public string Wt { get; set; }
            public string Qty { get; set; }
            public string IsFull { get; set; }
            public MerchantModel Origin { get; set; }
            public MerchantModel Destination { get; set; }
        }

        public class MerchantModel
        {
            //Address Data
            public string LocationName { get; set; }
            public string Address { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string ZipCode { get; set; }
            public string Country { get; set; }

            //Contact Data
            public string Name { get; set; }
            public string Phone { get; set; }
            public string Fax { get; set; }
            public string Email { get; set; }

            //Dock Data
            public string DockName { get; set; }
            public string Note { get; set; }
            public string DockOpen { get; set; }
            public string DockClose { get; set; }

            public string PickupDate { get; set; }
            public string PickupTime { get; set; }
            public string PickupNum { get; set; }

            public string DeliveryDate { get; set; }
            public string DeliveryTime { get; set; }
            public string DeliveryNum { get; set; }
        }
    }
}
