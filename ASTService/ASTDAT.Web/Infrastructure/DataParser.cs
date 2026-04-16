using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Threading.Tasks;
using ClosedXML.Excel;
using HtmlAgilityPack;
//using TruckStopService;
using DATService;
using ASTDAT.Tools;
using ASTDAT.Data.Models;

namespace ASTDAT.Web.Infrastructure
{
    public class DataParser
    {
        public List<LoadModel> ParseXLS_Insight(string inHTML)
        {
            List<LoadModel> rowList = new List<LoadModel>();
            try
            {

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(inHTML);
                HtmlNodeCollection cells;
                var tables = doc.DocumentNode.SelectNodes("//table");
                if (tables.Count > 1)
                {
                    var baseRow = new LoadModel()
                    {
                        //Asset = 1,
                        AssetLength = 53,
                        //AssetType = "Van",
                        EquipmentType = "Van",
                        LoadTypeId = 21, //Van
                        //MoveType = "Drive Away",
                        //Price = 0,
                        CarrierAmount = 0,
                        ClientName = "United Rentals"
                    };
                    if (tables.Count > 2)
                    {
                        var trs = tables[0].SelectNodes("tr");
                        trs = trs != null ? trs : tables[0].SelectSingleNode("tbody").SelectNodes("tr");
                        foreach (var item in trs)
                        {
                            if (item.InnerHtml.Contains("Load:"))
                            {
                                cells = item.SelectNodes("th|td");
                                //baseRow.Load = cells[1].InnerText;
                                baseRow.ClientLoadNum = cells[1].InnerText;
                            }
                            if (item.InnerHtml.Contains("Max Bid"))
                            {
                                cells = item.SelectNodes("th|td");
                                decimal price = 0;
                                decimal.TryParse(cells[1].InnerText.Replace("$", "").Trim(), out price);
                                if (price > 999999)
                                {
                                    price = 0;
                                }
                                //baseRow.Price = price;
                                baseRow.CarrierAmount = price;
                            }
                        }

                        var rows2 = tables[2].SelectNodes("tr");
                        rows2 = rows2 != null ? rows2 : tables[2].SelectSingleNode("tbody").SelectNodes("tr");
                        if (rows2.Count >= 3)
                        {
                            cells = rows2[rows2.Count - 1].SelectNodes("td");
                            if (cells.Count > 0)
                            {
                                var cell = cells[1].InnerText.Trim();
                                //When there is STEPDECK in the email, make type: Step Deck
                                //When there is FLATBED in the email, make type: Flatbed
                                /*if (cell.ToUpper().Contains("FLATBED") || cell.ToUpper().Contains("STEPDECK") || cell.ToUpper().Contains("STEP DECK"))
                                {
                                    rowList.Add(new RowModel() { Load = baseRow.Load });
                                    return rowList;
                                }*/
                                if (!
                                        (
                                        cell.ToUpper().Contains("DRIVE AWAY") || cell.ToUpper().Contains("DRIVEAWAY")
                                        || cell.ToUpper().Contains("BUMPER PULL") || cell.ToUpper().Contains("BUMPERPULL")
                                        || cell.ToUpper().Contains("PINTLE HITCH") || cell.ToUpper().Contains("PINTLEHITCH")
                                        || cell.ToUpper().Contains("POWER ONLY") || cell.ToUpper().Contains("POWERONLY")
                                        || cell.ToUpper().Contains("FLATBED") || cell.ToUpper().Contains("STEP DECK") || cell.ToUpper().Contains("STEPDECK")
                                        )
                                    )
                                {
                                    //rowList.Add(new RowModel() { Load = baseRow.Load });
                                    rowList.Add(new LoadModel() { ClientLoadNum = baseRow.ClientLoadNum });
                                    return rowList;
                                }
                                if (cell.ToUpper().Contains("BUMPER PULL") || cell.ToUpper().Contains("BUMPERPULL"))
                                {
									//baseRow.MoverNotes = "Bumper Pull";
									//rowList.Add(new RowModel() { Load = baseRow.Load });
									//rowList.Add(new LoadModel() { ClientLoadNum = baseRow.ClientLoadNum });
									baseRow.EquipmentType = "DriveTowaway";
									baseRow.LoadTypeId = 35;
									//return rowList;
                                }
                                if (cell.ToUpper().Contains("PINTLE HITCH") || cell.ToUpper().Contains("PINTLEHITCH"))
                                {
									//baseRow.MoverNotes = "Pintle Hitch";
									//rowList.Add(new RowModel() { Load = baseRow.Load });
									//rowList.Add(new LoadModel() { ClientLoadNum = baseRow.ClientLoadNum });
									baseRow.EquipmentType = "DriveTowaway";
									baseRow.LoadTypeId = 35;
									//return rowList;
                                }
                                if (cell.ToUpper().Contains("POWER ONLY") || cell.ToUpper().Contains("POWERONLY"))
                                {
                                    //baseRow.MoverNotes = "Power Only";
                                    //baseRow.AssetType = "PowerOnly";
                                    baseRow.EquipmentType = "PowerOnly";
                                    baseRow.LoadTypeId = 15;
                                }
                                //When there is FLATBED in the email, make type: Flatbed
                                if (cell.ToUpper().Contains("FLATBED"))
                                {
                                    //baseRow.MoverNotes = "Flatbed";
                                    //baseRow.AssetType = "Flatbed";
                                    baseRow.EquipmentType = "Flatbed";
                                    baseRow.LoadTypeId = 5;
                                }
                                //When there is STEPDECK in the email, make type: Step Deck
                                if (cell.ToUpper().Contains("STEPDECK") || cell.ToUpper().Contains("STEP DECK"))
                                {
                                    //baseRow.MoverNotes = "StepDeck";
                                    //baseRow.AssetType = "Step Deck";
                                    baseRow.EquipmentType = "StepDeck";
                                    baseRow.LoadTypeId = 19;
                                }

                                //int typePos = -1;
                                string trackType = string.Empty;
                                //baseRow.Instructions = HttpUtility.HtmlDecode(cell);
                                baseRow.Description = HttpUtility.HtmlDecode(cell);
                            }
                        }
                    }

					//Import from Rain for Rent and United Rentals POWER ONLY, but NOT Flatbed
					//When there is FLATBED in the email, make type: Flatbed
					//When there is STEPDECK in the email, make type: Step Deck
					//if (baseRow.EquipmentType != "PowerOnly" && baseRow.EquipmentType != "Flatbed" && baseRow.EquipmentType != "StepDeck")
					//if (baseRow.EquipmentType != "PowerOnly")
					//When in "Special Instructions" "POWER ONLY" or "BUMPER PULL" or "PINTLE HITCH" make "Equipment Type" "Drive Away"
					if (baseRow.EquipmentType != "PowerOnly" && baseRow.EquipmentType != "DriveTowaway")
					{
                        rowList.Add(new LoadModel() { ClientLoadNum = baseRow.ClientLoadNum });
                        return rowList;
                    }

                    var lastType = "";
                    var rows = tables[1].SelectNodes("tr");
                    rows = rows != null ? rows : tables[1].SelectSingleNode("tbody").SelectNodes("tr");
                    foreach (var item in rows)
                    {
                        cells = item.SelectNodes("th|td");
                        var cellType = cells[1].InnerText.Trim().ToUpper();
                        string city = "";
                        var state = "";
                        var zip = "";
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
                                zip = data[data.Length - 1].Split('.').Length < 2 ? "" : data[data.Length - 1].Split('.')[1].Trim();
                                if (state.Length > 2)
                                {
                                    //throw new Exception("Data parsing error: " + cellType + " data format is invalid - State is wrong");
                                    Logger.Write($"Data parsing error: {cellType} data format is invalid - State is wrong");
                                }
                            }
                            else
                            {
                                Logger.Write($"Data parsing error: {alltxt}");
                            }
                            var weights = cells[2].InnerText.Split(new string[] { "Weight:&nbsp;", "lb\r" }, StringSplitOptions.RemoveEmptyEntries);
                            int Weight = 0;
                            if (weights != null)
                            {
                                foreach (var weight in weights)
                                {
                                    int lWeight = 0;
                                    if (Int32.TryParse(weight.Replace(",", "").Trim(), out lWeight))
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
                                        if (date1 != null)
                                        {
                                            date2 = date;
                                        }
                                        else
                                        {
                                            date1 = date;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Logger.Write($"Data parsing error: {cells[3].InnerText}");
                            }

                            LoadModel curLine = null;
                            switch (cellType)
                            {
                                case "PICKUP":
                                    curLine = new LoadModel()
                                    {
                                        //Asset = baseRow.Asset,
                                        AssetLength = baseRow.AssetLength,
                                        //AssetType = baseRow.AssetType,
                                        //MoveType = baseRow.MoveType,
                                        EquipmentType = baseRow.EquipmentType,
                                        LoadTypeId = baseRow.LoadTypeId,
                                        //Price = baseRow.Price,
                                        CustomerAmount = baseRow.CustomerAmount,
                                        //Load = baseRow.Load,
                                        ClientLoadNum = baseRow.ClientLoadNum,
                                        //Instructions = baseRow.Instructions,
                                        Description = baseRow.Description,
                                        //MoverNotes = baseRow.MoverNotes,
                                        //Company = baseRow.Company,
                                        ClientName = baseRow.ClientName,
                                        Weight = Weight,
                                    };
                                    //2019-06-06
                                    //curLine.Origin = new OriginDestinationModel { City = $"{city} {state}"  };
                                    curLine.Origin = IntegrationService.Instance.AddOriginDestination(null, city, state);
                                    curLine.PickUpDate = date1;
                                    if (date2.HasValue)
                                    {
                                        //curLine.PickUpEnd = date2.Value;
                                    }
                                    else if (date1.HasValue)
                                    {
                                        //curLine.PickUpEnd = date1.Value.AddDays(10);
                                    }
                                    rowList.Add(curLine);
                                    lastType = "PICKUP";
                                    break;
                                case "DROP":
                                    if (lastType == "DROP")
                                    {
                                        curLine = new LoadModel()
                                        {
                                            //Asset = baseRow.Asset,
                                            AssetLength = baseRow.AssetLength,
                                            //AssetType = baseRow.AssetType,
                                            //MoveType = baseRow.MoveType,
                                            EquipmentType = baseRow.EquipmentType,
                                            LoadTypeId = baseRow.LoadTypeId,
                                            //Price = baseRow.Price,
                                            CustomerAmount = baseRow.CustomerAmount,
                                            //Load = baseRow.Load,
                                            ClientLoadNum = baseRow.ClientLoadNum,
                                            //Instructions = baseRow.Instructions,
                                            Description = baseRow.Description,
                                            //MoverNotes = baseRow.MoverNotes,
                                            //Company = baseRow.Company,
                                            ClientName = baseRow.ClientName,
                                            Weight = Weight,
                                        };
                                        var prevRow = rowList[rowList.Count - 1];
                                        curLine.Origin = prevRow.Origin;
                                        curLine.PickUpDate = prevRow.PickUpDate;
                                        rowList.Add(curLine);
                                    }
                                    var pickups = rowList.Where(p => p.Destination == null).ToList();
                                    foreach (var pk in pickups)
                                    {
                                        //pk.Destination = new OriginDestinationModel { City = $"{city} {state}" };
                                        pk.Destination = IntegrationService.Instance.AddOriginDestination(null, city, state);

                                        if (pk.PickUpDate.HasValue)
                                        {
                                            if ((pk.PickUpDate.Value - date1.Value).TotalMinutes == 0)
                                            {
                                                pk.DeliveryDate = date2;
                                            }
                                            else
                                            {
                                                pk.DeliveryDate = date1;
                                            }
                                        }

                                    }
                                    lastType = "DROP";
                                    break;
                            }
                        }
                    }
                }
                else //tables.Count
                {
                    //throw new Exception("Data parsing error: ");
                    Logger.Write("ParseEmail Data parsing error");
                }
            }
            catch (Exception exc)
            {
                Logger.Write("Exception.ParseEmail", exc);
            }

            return rowList;
        }
    }
}
