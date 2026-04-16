using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Threading.Tasks;
using ClosedXML.Excel;
using HtmlAgilityPack;
using TruckStopService;

namespace ASTService
{
    /*public class DataParser
    {
        public List<RowModel> ParseXLS_Insight(string inHTML)
        {
            List<RowModel> rowList = new List<RowModel>();
            try
            {

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(inHTML);
                HtmlNodeCollection cells;
                var tables = doc.DocumentNode.SelectNodes("//table");
                if (tables.Count > 1)
                {
                    
                   
                    var baseRow = new RowModel()
                    {
                        Asset = 1,
                        AssetLength = 53,
                        AssetType = "Van",
                        MoveType = "Drive Away",
                        Price = 0,
                        Company = "United Rentals"
                    };
                    if (tables.Count > 2)
                    {
                        foreach (var item in tables[0].SelectNodes("tr"))
                        {
                            if (item.InnerHtml.Contains("Load:"))
                            {
                                cells = item.SelectNodes("th|td");
                                baseRow.Load = cells[1].InnerText;
                            }
                            if (item.InnerHtml.Contains("Max Bid"))
                            {
                                cells = item.SelectNodes("th|td");
                                decimal price = 0;
                                decimal.TryParse(cells[1].InnerText.Replace("$", "").Trim(), out price);
                                if (price > 100000)
                                {
                                    price = 0;
                                }
                                baseRow.Price = price;
                            }
                        }

                        var rows2 = tables[2].SelectNodes("tr");
                        if (rows2.Count >= 3)
                        {
                            cells = rows2[rows2.Count - 1].SelectNodes("td");
                            if (cells.Count > 0)
                            {
                                var cell = cells[1].InnerText.Trim();
                                if (cell.ToUpper().Contains("FLATBED") || cell.ToUpper().Contains("STEPDECK") || cell.ToUpper().Contains("STEP DECK"))
                                {
                                    rowList.Add(new RowModel() { Load = baseRow.Load });
                                    return rowList;
                                }
                                if (!(cell.ToUpper().Contains("DRIVE AWAY") || cell.ToUpper().Contains("DRIVEAWAY") 
                                    || cell.ToUpper().Contains("BUMPER PULL") || cell.ToUpper().Contains("BUMPERPULL") 
                                    || cell.ToUpper().Contains("PINTLE HITCH") || cell.ToUpper().Contains("PINTLEHITCH")
                                    || cell.ToUpper().Contains("POWER ONLY") || cell.ToUpper().Contains("POWERONLY")))
                                {
                                    rowList.Add(new RowModel() { Load = baseRow.Load });
                                    return rowList;
                                }
                                if (cell.ToUpper().Contains("BUMPER PULL") || cell.ToUpper().Contains("BUMPERPULL"))
                                {
                                    baseRow.MoverNotes = "Bumper Pull";
                                }
                                if (cell.ToUpper().Contains("PINTLE HITCH") || cell.ToUpper().Contains("PINTLEHITCH"))
                                {
                                    baseRow.MoverNotes = "Pintle Hitch";
                                }
                                if (cell.ToUpper().Contains("POWER ONLY") || cell.ToUpper().Contains("POWERONLY"))
                                {
                                    baseRow.MoverNotes = "Power Only";
                                }

                                int typePos = -1;
                                string trackType = string.Empty;
                                baseRow.Instructions = HttpUtility.HtmlDecode(cell);
                                //if (cell.ToUpper().Contains("FLATBED"))
                                //{
                                //    baseRow.AssetType = "Flatbed";
                                //    typePos = cell.ToUpper().IndexOf("FLATBED");
                                //    trackType = "FLATBED";
                                //}
                                //else 
                                //if (cell.ToUpper().Contains("POWER ONLY") || cell.ToUpper().Contains("CHASSIS"))
                                //{
                                //    baseRow.AssetType = "Chassis";
                                //    if (cell.ToUpper().Contains("POWER ONLY"))
                                //    { 
                                //        typePos = cell.ToUpper().IndexOf("POWER ONLY");
                                //        trackType = "POWER ONLY";
                                //    }
                                //    else
                                //    {
                                //        typePos = cell.ToUpper().IndexOf("CHASSIS");
                                //        trackType = "CHASSIS";
                                //    }
                                //}
                                //else 
                                //if (cell.ToUpper().Contains("HOTSHOT"))
                                //{
                                //    baseRow.AssetType = "Hotshot";
                                //    typePos = cell.ToUpper().IndexOf("HOTSHOT");
                                //    trackType = "HOTSHOT";
                                //}
                                //else if (cell.ToUpper().Contains("STEPDECK") || cell.ToUpper().Contains("STEP DECK"))
                                //{
                                //    baseRow.AssetType = "Stepdeck";
                                //    if (cell.ToUpper().Contains("STEP DECK"))
                                //    {
                                //        typePos = cell.ToUpper().IndexOf("STEP DECK");
                                //        trackType = "STEP DECK";
                                //    }
                                //    else
                                //    {
                                //        typePos = cell.ToUpper().IndexOf("STEPDECK");
                                //        trackType = "STEPDECK";
                                //    }
                                //}
                                //else if (cell.ToUpper().Contains("PINTLE HITCH"))
                                //{
                                //    baseRow.AssetType = "Pintle Hitch";
                                //    typePos = cell.ToUpper().IndexOf("PINTLE HITCH");
                                //    trackType = "PINTLE HITCH";
                                //}
                                //else if (cell.ToUpper().Contains("BUMPER PULLS"))
                                //{
                                //    baseRow.AssetType = "Bumper Pulls";
                                //    typePos = cell.ToUpper().IndexOf("BUMPER PULLS");
                                //    trackType = "BUMPER PULLS";
                                //}
                                //if (typePos>=0 && !string.IsNullOrWhiteSpace(trackType))
                                //{
                                //    var subst = HttpUtility.HtmlDecode(cell.Substring(Math.Max(0, typePos - 10)));
                                //    subst = subst.Substring(0, Math.Max(subst.Length,trackType.Length + 20));
                                //    while (subst.IndexOf("'") >= 0)
                                //    {
                                //        var charInd = subst.IndexOf("'")-1;
                                //        while (charInd >= 0 && !Char.IsDigit(subst[charInd]))
                                //        {
                                //            charInd--;
                                //        }
                                //        var endPos = charInd + 1;
                                //        while (charInd >= 0 && Char.IsDigit(subst[charInd]))
                                //        {
                                //            charInd--;
                                //        }
                                //        var startPos = charInd + 1;
                                //        if (endPos - startPos > 0)
                                //        {
                                //            var sNumer = subst.Substring(startPos, endPos - startPos);
                                //            if (",28,32,36,40,42,43,45,48,51,53,57,".IndexOf("," + sNumer +",")>=0)
                                //            {
                                //                baseRow.AssetLength = Int32.Parse(sNumer);
                                //            }
                                //        }
                                //        subst = subst.Remove(startPos, subst.IndexOf("'") - startPos + 1);
                                //    }
                                //}
                            }

                        }
                    }

                    var lastType = "";
                    foreach (var item in tables[1].SelectNodes("tr"))
                    {
                        cells = item.SelectNodes("th|td");
                        var cellType = cells[1].InnerText.Trim().ToUpper();
                        string city, state, zip;
                        DateTime? date1 = null;
                        DateTime? date2 = null;
                        string cust = string.Empty;
                        if (cellType == "PICKUP" || cellType == "DROP") //PickUp
                        {
                            var strongs = cells[2].SelectNodes("strong");
                            var divs = cells[2].SelectNodes("div");
                            if (divs != null && divs.Count > 0)
                            {
                                cust = divs[0].InnerText.Replace(",", "");
                            }
                            else
                            {
                                var cArr = cells[2].InnerText.Replace("&nbsp;", " ").Replace("\t", "").Replace("\n", "").Split('\r');
                                cust = cArr[cArr.Length - 2].Trim().Replace(",", "");
                            }
                            //ws1.Cell(curRow, 11).Value = cust;


                            string alltxt = string.Empty;
                            if (strongs != null && strongs.Count > 0)
                            {
                                alltxt = strongs[0].InnerText.Replace("&nbsp;", " ").Replace("\t", "").Replace("\r", "").Replace("\n", "");
                            }
                            else
                            {
                                alltxt = cells[2].InnerText.Replace("&nbsp;", " ").Replace("\t", "").Replace("\r", "").Replace("\n", "");
                            }

                            var data = alltxt.Split(',');
                            if (data.Length > 4)
                            {
                                //ws1.Cell(curRow, 6).Value
                                city = data[data.Length - 2].Trim();
                                state = data[data.Length - 1].Split('.')[0].Trim();
                                zip = data[data.Length - 1].Split('.').Length<2 ? "" : data[data.Length - 1].Split('.')[1].Trim();
                                if (state.Length > 2)
                                {
                                    throw new Exception("Data parsing error: " + cellType + " data format is invalid - State is wrong");
                                }
                            }
                            else
                            {
                                throw new Exception("Data parsing error: " + alltxt);
                            }
                            var weights = cells[2].InnerText.Split(new string[] { "Weight:&nbsp;", "lb\r" }, StringSplitOptions.RemoveEmptyEntries);
                            int Weight = 0;
                            if (weights!=null)
                            {
                                foreach (var weight in weights)
                                {
                                    int lWeight = 0;
                                    if (Int32.TryParse(weight.Replace(",","").Trim(), out lWeight))
                                    {
                                        Weight = lWeight;
                                    }
                                }
                            }
                            var dates = cells[3].InnerText.Split('\r');

                            if (dates.Length >= 3)
                            {
                                DateTime date;
                                foreach (var lDate in dates)
                                {
                                    if (DateTime.TryParse(lDate.Trim(), new CultureInfo("en-US"), DateTimeStyles.AssumeLocal, out date))
                                    {
                                        if (date1!= null) date2 = date; else date1 = date;
                                    }
                                }
                                //if (dates.Length == 5)
                                //{

                                //    if (DateTime.TryParse(dates[2].Trim(), out date))
                                //        ws1.Cell(curRow, 14).Value = date;

                                //    if (DateTime.TryParse(dates[3].Trim(), out date))
                                //        ws1.Cell(curRow, 15).Value = date;
                                //}
                                //else
                                //{
                                //    if (DateTime.TryParse(dates[dates.Length - 2].Trim(), out date))
                                //        ws1.Cell(curRow, 14).Value = date;

                                //    if (DateTime.TryParse(dates[dates.Length - 1].Trim(), out date))
                                //        ws1.Cell(curRow, 15).Value = date;
                                //}
                            }
                            else
                            {
                                throw new Exception("Data parsing error: " + cells[3].InnerText);
                            }


                            RowModel curLine = null;
                            switch (cellType)
                            {
                                case "PICKUP":
                                    curLine = new RowModel()
                                    {
                                        Asset = baseRow.Asset,
                                        AssetLength = baseRow.AssetLength,
                                        AssetType = baseRow.AssetType,
                                        MoveType = baseRow.MoveType,
                                        Price = baseRow.Price,
                                        Load = baseRow.Load,
                                        Instructions = baseRow.Instructions,
                                        MoverNotes = baseRow.MoverNotes,
                                        Company = baseRow.Company,
                                        Weight = Weight,
                                    };
                                    //curLine.Origin = cust;
                                    curLine.OriginCity = city;
                                    curLine.OriginState = state;
                                    curLine.OriginZip = zip;
                                    curLine.PickUpStart = date1;
                                    if (date1.HasValue)
                                    {
                                        curLine.PickUpEnd = date1.Value.AddDays(10);
                                    }
                                    rowList.Add(curLine);
                                    lastType = "PICKUP";
                                    break;
                                case "DROP":
                                    if (lastType == "DROP")
                                    {
                                        curLine = new RowModel()
                                        {
                                            Asset = baseRow.Asset,
                                            AssetLength = baseRow.AssetLength,
                                            AssetType = baseRow.AssetType,
                                            MoveType = baseRow.MoveType,
                                            Price = baseRow.Price,
                                            Load = baseRow.Load,
                                            Instructions = baseRow.Instructions,
                                            MoverNotes = baseRow.MoverNotes,
                                            Company = baseRow.Company,
                                            Weight = Weight,
                                        };
                                        var prevRow = rowList[rowList.Count - 1];
                                        //curLine.Origin = prevRow.Origin;
                                        curLine.OriginCity = prevRow.DestinationCity; // OriginCity;
                                        curLine.OriginState = prevRow.DestinationState; // OriginState;
                                        curLine.OriginZip = prevRow.DestinationZip;
                                        curLine.PickUpStart = prevRow.PickUpStart;
                                        curLine.PickUpEnd = prevRow.PickUpEnd;
                                        rowList.Add(curLine);
                                    }
                                    var pickups = rowList.Where(p => string.IsNullOrWhiteSpace(p.DestinationState) && string.IsNullOrWhiteSpace(p.Destination)).ToList();
                                    foreach (var pk in pickups)
                                    {
                                        //pk.Destination = cust;
                                        pk.DestinationCity = city;
                                        pk.DestinationState = state;
                                        pk.DestinationZip = zip;
                                        pk.DeliveryDate = date2;

                                    }
                                    lastType = "DROP";
                                    break;
                            }
                        }
                        {
                            //if (cells[0].InnerText.Trim() == "1" || cells[1].InnerText.Trim().ToUpper() == "PICKUP") //PickUp
                            //{
                            //}
                            //if (cells[0].InnerText.Trim() == "2" || cells[1].InnerText.Trim().ToUpper() == "DROP") //Drop
                            //{
                            //    var strongs = cells[2].SelectNodes("strong");
                            //    var divs = cells[2].SelectNodes("div");
                            //    cust = string.Empty;
                            //    if (divs != null && divs.Count > 0)
                            //    {
                            //        cust = divs[0].InnerText.Replace(",", "");
                            //    }
                            //    else
                            //    {
                            //        var cArr = cells[2].InnerText.Replace("&nbsp;", " ").Replace("\t", "").Replace("\n", "").Split('\r');
                            //        cust = cArr[cArr.Length - 2].Trim().Replace(",", "");
                            //    }
                            //    ws1.Cell(curRow, 11).Value = cust;


                            //    string alltxt = string.Empty;
                            //    if (strongs != null && strongs.Count > 0)
                            //    {
                            //        alltxt = strongs[0].InnerText.Replace("&nbsp;", " ").Replace("\t", "").Replace("\r", "").Replace("\n", "");
                            //    }
                            //    else
                            //    {
                            //        alltxt = cells[2].InnerText.Replace("&nbsp;", " ").Replace("\t", "").Replace("\r", "").Replace("\n", "");
                            //    }
                            //    var data = alltxt.Split(',');
                            //    if (data.Length > 4)
                            //    {
                            //        ws1.Cell(curRow, 9).Value = data[data.Length - 2].Trim();
                            //        state = data[data.Length - 1].Split('.')[0].Trim();
                            //        if (state.Length > 2)
                            //        {
                            //            throw new Exception("Data parsing error: " + "Drop data format is invalid - State is wrong");
                            //        }
                            //        ws1.Cell(curRow, 10).Value = state;
                            //    }
                            //    else
                            //    {
                            //        throw new Exception("Data parsing error: " + alltxt);
                            //    }
                            //    var dates = cells[3].InnerText.Split('\r');
                            //    if (dates.Length >=3)
                            //    {
                            //        DateTime date;
                            //        int col = 36;
                            //        foreach (var lDate in dates)
                            //        {
                            //            if (DateTime.TryParse(lDate.Trim(), new CultureInfo("en-US"), DateTimeStyles.AssumeLocal, out date))
                            //            {
                            //                if (col == 36)
                            //                {
                            //                    col = 37;
                            //                }
                            //                else
                            //                {
                            //                    ws1.Cell(curRow, col).Value = date;
                            //                }
                            //            }
                            //        }

                            //        //DateTime date;
                            //        //if (DateTime.TryParse(dates[3].Trim(), out date))
                            //        //    ws1.Cell(curRow, 37).Value = date;
                            //    }
                            //    else
                            //    {
                            //        throw new Exception("Data parsing error: " + cells[3].InnerText);
                            //    }

                            //}
                        }
                    }
                }
                else //tables.Count
                {
                    throw new Exception("Data parsing error: ");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return rowList;

        }

    }
    public class RowModel
    {
        public string Load { get; set; }
        public int Asset { get; set; }
        public int AssetLength { get; set; }
        public string AssetType { get; set; }
        public string AssetSubType { get; set; }
        public string MoveType { get; set; }
        public decimal Price { get; set; }
        public string AssetNumber { get; set; }
        public string Origin { get; set; }
        public string OriginCity { get; set; }
        public string OriginState { get; set; }
        public string OriginZip { get; set; }
        public string Destination { get; set; }
        public string DestinationCity { get; set; }
        public string DestinationState { get; set; }
        public string DestinationZip { get; set; }
        public DateTime? PickUpStart { get; set; }
        public DateTime? PickUpEnd { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string Instructions { get; set; }
        public string MoverNotes { get; set; }
        public int Weight { get; set; }
        public string Company { get; set; }

        public DATService.RowModel ToDATRow()
        {
            return new DATService.RowModel()
            {
                Load = this.Load,
                Asset = this.Asset,
                AssetLength = this.AssetLength,
                AssetNumber = this.AssetNumber,
                AssetSubType = this.AssetSubType,
                AssetType = this.AssetType,
                MoverNotes = this.MoverNotes,
                MoveType = this.MoveType,
                Origin = this.Origin,
                OriginCity = this.OriginCity,
                OriginState = this.OriginState,
                OriginZip = this.OriginZip,
                Destination = this.Destination,
                DestinationCity = this.DestinationCity,
                DestinationState = this.DestinationState,
                DestinationZip = this.DestinationZip,
                PickUpStart = this.PickUpStart,
                PickUpEnd = this.PickUpEnd,
                DeliveryDate = this.DeliveryDate,
                Instructions = this.Instructions,
                Price = this.Price,
                Weight = this.Weight,
                Company = this.Company,
            };
        }

        public TruckLoadModel ToTruckLoadModel()
        {
            return new TruckLoadModel
            {
                DeliveryDate = this.DeliveryDate,

                DestinationCountry = this.Destination,
                DestinationCity = this.DestinationCity,
                DestinationState = this.DestinationState,

                OriginCountry = this.Origin,
                OriginCity = this.OriginCity,
                OriginState = this.OriginState,

                Load = this.Load,
                PickUpStart = this.PickUpStart,
                Weight = this.Weight,

                Instructions = this.Instructions,
            };
        }
    }*/
}
