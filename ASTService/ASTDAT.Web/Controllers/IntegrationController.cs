using ASTDAT.Data.Models;
using ASTDAT.Tools;
using ASTDAT.Web.Infrastructure;
using DATService;
using DATService.ServiceReference1;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using TruckStopRestfullService.Models;
//using TruckStopService.TruckStopServiceReference;

//using TruckStopService.TruckStopServiceReference;

namespace ASTDAT.Web.Controllers
{
    public class IntegrationController : Controller
    {
        public static DateTime? LastEmailParse = null;
        public static DateTime? LastDATLoadState = null;
        public static DateTime? LastTSLoadState = null;
        public static DateTime? LastDATRefreshState = null;
        public static DateTime? LastTSRefreshState = null;
        public static DateTime? LastMercuryGate = null;
        public static DateTime? LastBanyanTechnology = null;
		public static DateTime? LastUntilDate = null;

		// GET: Integration
		[Authorize]
        public ActionResult Index()
        {
            ViewBag.DATUrl = System.Configuration.ConfigurationManager.AppSettings["DATServiceURL"];
            ViewBag.TSUrl = System.Configuration.ConfigurationManager.AppSettings["TSServiceURL"];

            ViewBag.LastEmailParse = LastEmailParse;
            ViewBag.LastDATLoadState = LastDATLoadState;
            ViewBag.LastTSLoadState = LastTSLoadState;
            ViewBag.LastDATRefreshState = LastDATRefreshState;
            ViewBag.LastTSRefreshState = LastTSRefreshState;
            ViewBag.DATLastLogin = DATService.Session.LastLogin;
            ViewBag.DATLastLoginInfo = DATService.Session.LastLoginInfo;
            ViewBag.ApplicationStarted = MvcApplication.ApplicationStarted;
            ViewBag.LastMercuryGate = LastMercuryGate;
            ViewBag.LastBanyanTechnology = LastBanyanTechnology;
            ViewBag.LastUntilDate = LastUntilDate;

            return View();
        }

        /// <summary>
        /// parse emails
        /// add to DB
        /// upload to DAT
        /// upload to TS
        /// </summary>
        /// <param name="seccode"></param>
        /// <param name="limit"></param>
        /// <param name="removeOld"></param>
        /// <returns></returns>
        public async Task<ActionResult> EmailParse(string seccode = "")
        {
            //Logger.Write("Integration.EmailParse");
            try
            {
                LastEmailParse = DateTime.Now;

                //Please put it back on every 15 minutes
                if (LastEmailParse.Value.Minute % 15 == 0)
                {
                    RefreshDAT_NEW();
                    //Logger.Write("RefreshDAT_NEW disabled");
                }

                ViewBag.Started = DateTime.Now;
                //var mercuryGateGetLoads = await IntegrationService.Instance.MercuryGateGetLoads();
                ViewBag.Count = 0; //IntegrationService.Instance.LoadEmails(mercuryGateGetLoads);
                ViewBag.Finished = DateTime.Now;

                //LOGIC: DO NOT COMPARE those Loads that have been imported less than 5 minutes
                //if (LastEmailParse.Value.Minute % 1 == 0)
                //{
                //    await MercuryGate();
                //    await BanyanTechnology();
                //    DeleteOld();
                //}

                var untilDateHour = 17;

                if (LastEmailParse.Value.Hour == untilDateHour && (!LastUntilDate.HasValue || LastUntilDate.Value.Date != DateTime.Now.Date))
                {
                    LastUntilDate = DateTime.Now;
                    Logger.Write("Starting UntilDateProcessing: see UntilDateProcessing.txt");
                    UntilDateProcessing();
                }
            }
            catch (Exception exc)
            {
                Logger.WriteError("Integration.EmailParse: " + exc.Message);
            }
            return View();
        }

        /// <summary>
        /// get loads from DAT
        /// remove from DB all removed from DAT
        /// </summary>
        /// <param name="removeCount"></param>
        /// <returns></returns>
        public ActionResult DATLoadState(int? removeCount, bool? skipUntilDate)
        {
			if (System.Configuration.ConfigurationManager.AppSettings["StopDATLoadState"] == "true")
			{
				return View();
			}
            Logger.Write("-------------- Started --------------", fileName: "DATLoadState.txt");
            try
            {
                LastDATLoadState = DateTime.Now;

                var currentDATLoads = IntegrationService.Instance.GetDATLoads();
                ViewBag.LastExceptions = IntegrationService.Instance.Session.LastExceptions;
                var removed = new List<string>();

                var tDeletedFromAST = 0;
                var tDeletedFromTS = 0;
                var tMarkDeletedFromTS = 0;
                var tAddedToAST = 0;
                var tAddedToTS = 0;

                if (currentDATLoads != null && currentDATLoads.assets != null && currentDATLoads.assets.Length > 0)
                {
                    //Logger.Write($"Assets.Length {currentDATLoads.assets.Length}", fileName: "DATLoadState.txt");
                    if (removeCount.HasValue) //DEBUG
                    {
                        currentDATLoads.assets = currentDATLoads.assets.Skip(removeCount.Value).ToArray();
                    }

                    using (var db = new DBContext())
                    {
                        var currentLoadsInDB = db.Loads.Where(x => x.AssetId != null && !x.DateDatDeleted.HasValue);

                        var counter = 0;
                        //mark as deleted in DB all removed from DAT
                        foreach (var item in currentLoadsInDB)
                        {
                            counter++;
                            //if (counter > 5) break;

                            var asset = currentDATLoads.assets.FirstOrDefault(x => x.assetId == item.AssetId);
                            if (asset == null) //Remove from DB
                            {
                                Logger.Write($"Delete Id:{item.Id}/AssetId:{item.AssetId}/TrackStopId:{item.TrackStopId}/ClientLoadNum:{item.ClientLoadNum}", fileName: "DATLoadState.txt");

                                item.DateDatDeleted = DateTime.Now;
                                tDeletedFromAST++;
								//changed 11/10/2019
								//item.AddComment($"Deteted from DAT by timer (DATLoadState)");
								item.AddComment($"This load was marked as deleted in DAT because it does not exist in DAT");
                                removed.Add(item.AssetId);
                                //Remove from TS too
                                if (item.CanDeletFromTS())
                                {
                                    Logger.Write($"Try to delete from TS {item.Id}", fileName: "DATLoadState.txt");
                                    var delResult = IntegrationService.Instance.TruckStopUtils.DeleteByIds(new string[] { item.Id.ToString(), item.ClientLoadNum });
                                    var delList = delResult.Data as List<string>;
                                    //if (delResult != null && delResult.LoadNumbers != null && delResult.LoadNumbers.Length == 1)
                                    if (delList != null && delList.Count == 1)
                                    {
                                        item.DateTSDeleted = DateTime.Now;
                                        tDeletedFromTS++;
                                        item.AddComment($"Delete from TS by timer (DATLoadState)");
                                        Logger.Write($"Deleted from TS {item.AssetId}", fileName: "DATLoadState.txt");
                                    }
                                    else if (delResult != null && delResult.StatusSet != null)
                                    {
                                        item.AddComment($"NOT Delete from TS by timer {String.Join(",", delResult.StatusSet.Select(x => x.Message))} (DATLoadState)");
                                        Logger.Write($"NOT Deleted from TS {item.AssetId} {String.Join(",", delResult.StatusSet.Select(x => x.Message))}", fileName: "DATLoadState.txt");
                                    }
                                    else
                                    {
                                        item.AddComment($"NOT Delete from TS by timer, no details (DATLoadState)");
                                        Logger.Write($"NOT Delete from TS {item.AssetId}, no details", fileName: "DATLoadState.txt");
                                    }
                                }
                                else
                                {
                                    //if not uploaded to TS mark delete too
                                    if (!item.TrackStopId.HasValue)
                                    {
                                        item.DateTSDeleted = DateTime.Now;
                                        tMarkDeletedFromTS++;
                                        item.AddComment($"Delete(mark) from TS by timer (DATLoadState)");
                                        Logger.Write($"Delete(mark) from TS {item.AssetId}, no details", fileName: "DATLoadState.txt");
                                    }
                                }
                            }
                            else
                            {
                                Logger.Write($"AssetId {item.Id}/{item.AssetId} exists in DAT", fileName: "DATLoadState.txt");
                            }
                        }
                        db.SaveChanges();

                        //Add in DB all from DAT
                        counter = 0;
                        foreach (var asset in currentDATLoads.assets)
                        {
							//do not import with "amst-" in "Reference ID" ?
							if ($"{asset.postersReferenceId}".ToLower().StartsWith("amst-"))
							{
								continue;
							}

							counter++;
                            //if (counter > 5) break;

                            var shipment = (asset.Item as Shipment);
                            if (shipment == null
                                || shipment.destination == null
                                || shipment.destination.Item == null
                                || !(shipment.destination.Item is NamedLatLon)
                                || shipment.origin == null
                                || shipment.origin.Item == null
                                || !(shipment.origin.Item is NamedLatLon)
                                )
                            {
                                Logger.Write($"{asset.assetId}, not valid shipment", fileName: "DATLoadState.txt");
                                continue;
                            }
                            var load = db.Loads.FirstOrDefault(x => x.AssetId == asset.assetId);
                            if (load == null)
                            {
                                Logger.Write($"{asset.assetId} try add to AST", fileName: "DATLoadState.txt");

                                var origin = (shipment.origin.Item as NamedLatLon);
                                var destination = (shipment.destination.Item as NamedLatLon);

                                load = new LoadModel
                                {
                                    AssetId = asset.assetId,
                                    AssetLength = asset.dimensions?.lengthFeet,
                                    //ClientLoadNum = asset.postersReferenceId
                                    PostersReferenceId = asset.postersReferenceId,
                                    Origin = IntegrationService.Instance.AddOriginDestination(db, $"{origin.city} {origin.stateProvince.ToString()}"),
                                    Destination = IntegrationService.Instance.AddOriginDestination(db, $"{destination.city} {destination.stateProvince.ToString()}"),
                                    EquipmentType = shipment.equipmentType.ToString(),
                                    DateLoaded = asset.status.created.date,
                                    PickUpDate = asset.availability.earliest,
                                    CreateDate = DateTime.Now,
                                    CreatedBy = $"DATLoadState",
                                    CreateLoc = "AutoApp",
                                };

								db.Loads.Add(load);
                                db.SaveChanges();
                                tAddedToAST++;

                                load.AddComment($"This load was refreshed from DAT (DATLoadState)");
                                Logger.Write($"{load.Id}/{asset.assetId} added to AST", fileName: "DATLoadState.txt");
                            }
                            else if (!load.PickUpDate.HasValue)
                            {
                                load.PickUpDate = asset.availability.earliest;
                                db.SaveChanges();
                                Logger.Write($"{load.Id}/{asset.assetId} updated PickUpDate", fileName: "DATLoadState.txt");
                            }

                            //Send to TS
                            if (!load.TrackStopId.HasValue)
                            {
                                Logger.Write($"{load.Id}/{asset.assetId} try send to TS", fileName: "DATLoadState.txt");
                                var resultTS = IntegrationService.Instance.TruckStopUtils.UploadToTruckStop(load.ToTSLoad() , source: "DATLoadState");
                                if (resultTS == null)
                                {
                                    Logger.Write($"{load.Id}/{asset.assetId} resultTS == null", fileName: "DATLoadState.txt");
                                }
                                else if (resultTS.StatusSet != null && resultTS.StatusSet.Count > 0)
                                {
                                    Logger.Write($"{load.Id}/{asset.assetId} resultTS.Errors {String.Join(", ", resultTS.StatusSet.Select(x => $"ErrorMessage: {x.Message}"))}", fileName: "DATLoadState.txt");
                                    //Logger.Write($"{load.Id}/{asset.assetId} resultTS.Errors {String.Join(", ", resultTS.StatusSet.Select(x => $"ErrorMessage: {x.Message}, Suggestions: {x.Suggestions}"))}", fileName: "DATLoadState.txt");
                                }
                                else if (resultTS.Data != null)
                                {
                                    var tsLoad = resultTS.Data as Load;
                                    tAddedToTS++;
                                    //load.AddComment($"DATLoadState added to TS {(resultTS.Loads.Length > 0 ? $"{resultTS.Loads[0]}" : "no id")}\r\n"); //11/10/2019
                                    load.AddComment($"This load was added in ITS because it exists in DAT {(tsLoad != null ? $"{tsLoad.Text}" : "no id")}\r\n");
                                    Logger.Write($"{load.Id}/{asset.assetId} added to TS {(tsLoad != null ? $"{tsLoad.Text}" : "no id")}", fileName: "DATLoadState.txt");
                                }
                                else
                                {
                                    Logger.Write($"DATLoadState strange state", fileName: "DATLoadState.txt");
                                }
                                db.SaveChanges();
                            }
                        }
                    }
                }

                if (!skipUntilDate.HasValue)
                {
                    //Logger.Write($"DATLoadState UntilDateProcessing", fileName: "DATLoadState.txt");
                    //IntegrationService.Instance.UntilDateProcessing();
                }

                Logger.Write($"[DAT TOTAL] DATLoads:{currentDATLoads?.assets?.Length}, DeletedFromAST:{tDeletedFromAST}, DeletedFromTS:{tDeletedFromTS}, MarkDeletedFromTS:{tMarkDeletedFromTS}, AddedToAST:{tAddedToAST}, AddedToTS:{tAddedToTS}", fileName: "DATLoadState.txt");

                ViewBag.Removed = removed;
                return View(currentDATLoads);
            }
            catch (Exception exc)
            {
                Logger.Write($"Exception", exc, fileName: "DATLoadState.txt");
            }

            return View();
        }

        /// <summary>
        /// get loads from TS
        /// remove from DB all removed from TS
        /// </summary>
        /// <param name="removeCount"></param>
        /// <returns></returns>
        public ActionResult TSLoadState(int? removeCount, bool? skipUntilDate)
        {
			if (System.Configuration.ConfigurationManager.AppSettings["StopTSLoadState"] == "true")
			{
				return View();
			}
			Logger.Write("-------------- Started --------------", fileName: "TSLoadState.txt");
            try
            {
                LastTSLoadState = DateTime.Now;

                var currentTSloads = IntegrationService.Instance.GetTSLoads();

                if (!currentTSloads.Success)
                {
                    return View();
                }

                var tDeletedFromAST = 0;
                var tDeletedFromDAT = 0;
                var tMarkDeletedFromDAT = 0;
                var tAddedToAST = 0;
                var tAddedToDAT = 0;

                var removed = new List<int>();
                var currentLoads = currentTSloads.Data as List<Load>;
                if (currentTSloads != null && currentLoads != null)
                {
                    if (removeCount.HasValue) //DEBUG
                    {
                        currentLoads = currentLoads.Skip(removeCount.Value).ToList();
                    }

                    using (var db = new DBContext())
                    {
                        //mark as deleted in DB all removed from TS
                        var loadsInDB = db.Loads.Where(x => (x.TrackStopId.HasValue || x.TsLoadId.HasValue) && !x.DateTSDeleted.HasValue).ToList();
                        var counter = 0;
                        foreach (var item in loadsInDB)
                        {
                            counter++;
                            //if (counter > 5) break;

                            if (!currentLoads.Any(x => x.LegacyLoadId == item.TrackStopId || x.LoadId == (item.TsLoadId.HasValue ? item.TsLoadId.Value.ToString() : null) )) //No load in TS
                            {
                                Logger.Write($"Delete Id:{item.Id}/AssetId:{item.AssetId}/TrackStopId:{item.TrackStopId}/ClientLoadNum:{item.ClientLoadNum}", fileName: "TSLoadState.txt");
                                
								item.DateTSDeleted = DateTime.Now;
                                tDeletedFromAST++;

								//changed 11/10/2019
								//item.AddComment($"Deteted from TS by timer [TSLoadState]");
								item.AddComment($"This load was marked as deleted in TS because it does not exist in TS [TSLoadState]");
								removed.Add(item.TrackStopId.Value);
                                //Delete from DAT too
                                if (item.CanDeleteFromDAT())
                                {
                                    Logger.Write($"Try to delete from DAT {item.Id}", fileName: "TSLoadState.txt");
                                    var resp = IntegrationService.Instance.Session.DeleteAssetsByIds(new string[] { item.AssetId }); //TSLoadState
                                    if (resp != null && resp.deleteAssetResult != null && resp.deleteAssetResult.Item != null && resp.deleteAssetResult.Item is ServiceError)
                                    {
                                        var error = (resp.deleteAssetResult.Item as ServiceError);
                                        item.AddComment($"Deteted from DAT by timer ERROR {error.faultCode}|{error.message}|{error.detailedMessage} [TSLoadState]");
                                        Logger.Write($"Delete from DAT ERROR {error.faultCode}|{error.message}|{error.detailedMessage}", fileName: "TSLoadState.txt");
                                    }
                                    else
                                    {
                                        item.DateDatDeleted = DateTime.Now;
										tDeletedFromDAT++;

                                        item.AddComment($"Deteted from DAT by timer [TSLoadState]");
                                        Logger.Write($"Deleted from DAT", fileName: "TSLoadState.txt");
                                    }
                                }
                                else
                                {
                                    //if not uploaded to DAT mark delete too
                                    if (!String.IsNullOrEmpty(item.AssetId))
                                    {
                                        item.DateDatDeleted = DateTime.Now;
										tMarkDeletedFromDAT++;

                                        item.AddComment($"Detete(mark) from DAT by timer (DATLoadState)");
                                        Logger.Write($"Delete(mark) from DAT {item.AssetId}, no details", fileName: "TSLoadState.txt");
                                    }
                                }
                            }
                            else
                            {
                                Logger.Write($"Load {item.Id}/{item.TrackStopId} exists in TS", fileName: "TSLoadState.txt");
                            }

							db.SaveChanges();
						}

                        //Add in DB all from TS
                        counter = 0;
                        foreach (var tsLoad in currentLoads)
                        {
							//do not import with "amst-" in "Reference ID" ?
							if ($"{tsLoad.LoadNumber}".ToLower().StartsWith("amst-"))
							{
								continue;
							}

							counter++;
                            //if (counter > 5) break;
                            var loadId = new Guid(tsLoad.LoadId);
                            
                            var origin = tsLoad.LoadStops.FirstOrDefault(p => p.Type == 1);
                            var destination = tsLoad.LoadStops.FirstOrDefault(p => p.Type == 2);

                            if (String.IsNullOrEmpty(destination?.Location.City)
                                || String.IsNullOrEmpty(destination?.Location.State)
                                || String.IsNullOrEmpty(origin?.Location.City)
                                || String.IsNullOrEmpty(origin?.Location.State)
                                )
                            {
                                Logger.Write($"{tsLoad.Text}, not valid Destination or Origin", fileName: "TSLoadState.txt");
                                continue;
                            }
                            //CultureInfo enUS = new CultureInfo("en-US");
                            //DateTime pickupDate;
                            //if (!DateTime.TryParseExact(tsLoad.PickupDate, "M/d/yy", enUS, DateTimeStyles.None, out pickupDate))
                            //{
                            //    Logger.Write($"{tsLoad.Id}, not valid PickupDate {tsLoad.PickupDate}", fileName: "TSLoadState.txt");
                            //    continue;
                            //}
                            if (!origin.EarlyDateTime.HasValue)
                            {
                                Logger.Write($"{tsLoad.Text}, not valid PickupDate", fileName: "TSLoadState.txt");
                                continue;
                            }
                            decimal payment = tsLoad.RateAttributes?.PostedAllInRate?.Amount ?? 0;
                            //if (!decimal.TryParse(tsLoad.Payment, out payment))
                            //{
                            //    payment = 0;
                            //}

                            var load = db.Loads.FirstOrDefault(x => x.TsLoadId == loadId);
                            if (load == null)
                            {
                                if (tsLoad.LegacyLoadId.HasValue)
                                {
                                    load = db.Loads.FirstOrDefault(x => x.TrackStopId == tsLoad.LegacyLoadId);
                                }
                            }
                            
                            if (load == null)
                            {
                                Logger.Write($"{tsLoad.Text} try add to AST", fileName: "TSLoadState.txt");
                                var loadType = db.LoadTypes.FirstOrDefault(x => x.TsId == tsLoad.EquipmentAttributes.EquipmentTypeId);
                                load = new LoadModel
                                {
                                    TsLoadId = loadId,
                                    TrackStopId = tsLoad.LegacyLoadId,
                                    //AssetLength = tsLoad.le,
                                    ClientLoadNum = tsLoad.LoadNumber,
                                    //PostersReferenceId = asset.postersReferenceId,
                                    Origin = IntegrationService.Instance.AddOriginDestination(db, $"{origin.Location.City} {origin.Location.State}"),
                                    Destination = IntegrationService.Instance.AddOriginDestination(db, $"{destination.Location.City} {destination.Location.State}"),
                                    
                                    EquipmentType = $"{loadType.NameTS}",
                                    LoadType = loadType,
                                    
                                    ClientName = tsLoad.CreatedBy,
                                    DateLoaded = origin.EarlyDateTime,
                                    //Remove code to populate carrier amount
                                    //CarrierAmount = payment, 
                                    CarrierAmount = 0,
                                    IsLoadFull = tsLoad.EquipmentAttributes.TransportationModeId == 1,
                                    PickUpDate = origin.EarlyDateTime,
                                    Comments = "",
                                    CreateDate = DateTime.Now,
                                    CreatedBy = $"TSLoadState {tsLoad.CreatedBy}",
                                    CreateLoc = "AutoApp",
                                };
                                load.EquipmentType = load.EquipmentType.Length > 20 ? load.EquipmentType.Substring(0, 20) : load.EquipmentType;
                                //Please remove logic to get rate from LoadHistory
                                //load.CarrierAmount = IntegrationService.Instance.GetRateMate(load);

                                db.Loads.Add(load);
                                db.SaveChanges();
                                tAddedToAST++;
                                Logger.Write($"{tsLoad.Text} added to AST", fileName: "TSLoadState.txt");

                                load.AddComment($"This load was refreshed from Truckstop (TSLoadState)");
                                //Please remove logic to get rate from LoadHistory
                                /*if (load.CarrierAmount > 0)
                                {
                                    load.AddComment($"GetRateMate({load.CarrierAmount}) (TSLoadState)");
                                }*/
                            }

                            //Send to DAT
                            if (String.IsNullOrEmpty(load.AssetId))
                            {
                                Logger.Write($"{tsLoad.Text} try send to DAT", fileName: "TSLoadState.txt");
                                var result = IntegrationService.Instance.Session.UploadDAT(new List<PostAssetOperation> { load.ToDATLoad() });
                                if (result == null || result.Response == null || result.Response.postAssetResults == null)
                                {
                                    load.AddComment($"UploadDAT failed (TSLoadState)");
                                    Logger.Write($"{tsLoad.Text} failed no postAssetResults, ", fileName: "TSLoadState.txt");
                                }
                                /*foreach (var item in result.ToResults(load.ClientLoadNum ?? load.Id.ToString()))
                                {
                                    load.AddComment($"Upload to DAT. Is success:{item.IsSuccess}, Message:{item.Message} (TSLoadState)");
                                }*/
                                if (result != null && result.Response != null && result.Response.postAssetResults.Count() > 0 && result.Response.postAssetResults[0].Item is PostAssetSuccessData)
                                {
                                    var success = (result.Response.postAssetResults[0].Item as PostAssetSuccessData);
                                    load.AssetId = success.assetId;
                                    load.AddComment($"Uploaded to DAT {success.assetId}");
                                    tAddedToDAT++;
                                    Logger.Write($"Uploaded to DAT {success.assetId}", fileName: "TSLoadState.txt");
                                }

                                db.SaveChanges();
                            }
                        }
                    }
                }

                if (!skipUntilDate.HasValue)
                {
                    //Logger.Write($"UntilDateProcessing", fileName: "TSLoadState.txt");
                    //IntegrationService.Instance.UntilDateProcessing();
                }

                Logger.Write($"[TS TOTAL] TSLoads:{currentLoads.Count} ,DeletedFromAST:{tDeletedFromAST}, DeletedFromDAT:{tDeletedFromDAT}, MarkDeletedFromDAT:{tMarkDeletedFromDAT}, AddedToAST:{tAddedToAST}, AddedToDAT:{tAddedToDAT}", fileName: "TSLoadState.txt");
                ViewBag.Removed = removed;
                return View(currentTSloads);
            }
            catch (Exception exc)
            {
                Logger.Write("EXCEPTION", exc, fileName: "TSLoadState.txt");
            }

            return View();
        }

        /// <summary>
        /// Update DAT (every 5 mins)
        /// </summary>
        /// <param name="skipUntilDate"></param>
        /// <returns></returns>
        public ActionResult RefreshDAT(bool? skipUntilDate)
        {
            //The integration team of DAT is saying that there are UPDATE calls.
            //I am saying: Remove them
            return View();
            /*using (var db = new DBContext())
            {
                var cLoads = db.Loads.Where(x => x.AssetId != null && !x.DateDatDeleted.HasValue).ToList();

                foreach (var item in cLoads)
                {
                    var result = IntegrationService.Instance.Session.UpdateAssets(item.AssetId, shipmentRate: new ShipmentRate
                    {
                        baseRateDollars = (float)item.CarrierAmount,
                        rateBasedOn = RateBasedOnType.Flat,
                    });
                    if (result != null && result.updateAssetResult != null && result.updateAssetResult.Item != null && result.updateAssetResult.Item is ServiceError)
                    {
                        if ($"{(result.updateAssetResult.Item as ServiceError).detailedMessage}".ToLower().Contains("is already expired"))
                        {
                            item.DateDatDeleted = DateTime.Now;
                            item.Comments = $"{item.Comments}\r\nRefreshDAT is already expired [RefreshDAT] {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                            db.SaveChanges();
                        }
                    }
                    else if (result != null && result.updateAssetResult != null && result.updateAssetResult.Item != null && result.updateAssetResult.Item is UpdateAssetSuccessData)
                    {
                    }
                }
            }

            if (!skipUntilDate.HasValue)
            {
                IntegrationService.Instance.UntilDateProcessing();
            }

            LastDATRefreshState = DateTime.Now;
            return View();*/
        }

		//Please put it back on every 15 minutes
		public ActionResult RefreshDAT_NEW()
		{
			using (var db = new DBContext())
			{
				var cLoads = db.Loads.Where(x => x.AssetId != null && !x.DateDatDeleted.HasValue).ToList();

				foreach (var item in cLoads)
				{
					try
					{
						var updateResult = IntegrationService.Instance.Session.UpdateAssets(item.ToUpdateAssetOperation());

						if (updateResult != null &&
                            !updateResult.IsException
							&& updateResult.UpdateAssetResponse.updateAssetResult != null
							&& updateResult.UpdateAssetResponse.updateAssetResult.Item != null
							&& updateResult.UpdateAssetResponse.updateAssetResult.Item is UpdateAssetSuccessData
							)
						{
							item.AddComment($"Updated the load in DAT");
						}
						else if (updateResult != null &&
                            !updateResult.IsException
							&& updateResult.UpdateAssetResponse.updateAssetResult != null
							&& updateResult.UpdateAssetResponse.updateAssetResult.Item != null
							&& updateResult.UpdateAssetResponse.updateAssetResult.Item is ServiceError
							)
						{
							var error = updateResult.UpdateAssetResponse.updateAssetResult.Item as ServiceError;
							if (error != null)
							{
								item.AddComment($"Update to DAT failed, error: {error.message} | {error.detailedMessage}");
							}
						}
						else if (updateResult != null && updateResult.IsException)
						{
							item.AddComment($"Update to DAT failed, details: {updateResult.Message}");
						}
						else
						{
							item.AddComment($"Update to DAT failed, no details");
						}
					}
					catch(Exception exc)
					{
						Logger.Write($"Exception! Load: {item.Id}", exc, "RefreshDAT_NEW.log");
					}
				}
			}

			LastDATRefreshState = DateTime.Now;
			return View("RefreshDAT");
		}

		public ActionResult RefreshTS()
        {
            //Important: DO NOT REFRESH TO Truckstop, NEVER
            return View();
            /*var result = new List<LoadPostingReturn>();
            using (var db = new DBContext())
            {
                var loads = db.Loads.Where(x => x.TrackStopId.HasValue && !x.DateTSDeleted.HasValue).ToList();
                foreach (var load in loads)
                {
                    var item = new TruckStopService.TruckStopServiceReference.Load
                    {
                        PickUpDate = load.DateLoaded.HasValue ? load.DateLoaded.Value : DateTime.Now,
                        DeliveryDate = load.DateLoaded.HasValue ? load.DateLoaded.Value : DateTime.Now,

                        LoadNumber = $"{load.ClientLoadNum}",

                        DestinationCity = load.Destination.City,
                        DestinationState = load.Destination.State.Code,

                        OriginCity = load.Origin.City,
                        OriginState = load.Origin.State.Code,

                        TypeOfEquipment = load.LoadTypeId.HasValue ? load.LoadType.IdTS : "DA",
                    };
                    result.Add(IntegrationService.Instance.TruckStopUtils.UploadToTruckStop(new TruckStopService.TruckStopServiceReference.Load[] { item }));
                }
            }

            LastTSRefreshState = DateTime.Now;
            return View(result);*/
        }

        /*public ActionResult DeleteDAT(string assetId)
        {
            IntegrationService.Instance.Session.DeleteAssetsByIds(new string[] { assetId });
            return View();
        }

        public ActionResult DeleteTS(string assetId)
        {
            IntegrationService.Instance.Session.DeleteAssetsByIds(new string[] { assetId });
            return View();
        }*/
        [AllowAnonymous]
        public ActionResult UntilDateProcessing(bool test = false)
        {
			LastUntilDate = DateTime.Now;
            //System.IO.File.AppendAllText($"C:\\Work\\test.txt", $"Test {DateTime.Now}\r\n");  // access denied 


            IntegrationService.Instance.UntilDateProcessing(test);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public ActionResult Restore(string date)
        {
            using (var db = new DBContext())
            {
                DateTime restoreDate;
                if (!DateTime.TryParse(date, out restoreDate))
                {
                    restoreDate = DateTime.Now.Date;
                }

                var loads = db.Loads.Where(x => x.DateDatDeleted >= restoreDate || x.DateTSDeleted >= restoreDate).ToList();
                ViewBag.RestoreDate = restoreDate;
                ViewBag.DATId = String.Join(",", loads.Where(x => x.DateDatDeleted.HasValue).Select(x => x.Id.ToString()));
                ViewBag.TSId = String.Join(",", loads.Where(x => x.DateTSDeleted.HasValue).Select(x => x.Id.ToString()));

                return View(loads);
            }
        }

        /*[HttpPost]
        public ActionResult Restore(string DatIds, string TSIds)
        {
            using (var db = new DBContext())
            {
                var loads = new List<LoadModel>();
                var idsDAT = $"{DatIds}".Split(',').Select(x => int.Parse(x)).ToList();
                var idsTS = $"{TSIds}".Split(',').Select(x => int.Parse(x)).ToList();
                var messages = new Dictionary<int, string>();
                foreach (var load in db.Loads.Where(x => idsDAT.Contains(x.Id)).ToList())
                {
                    IntegrationService.Instance.Session.UploadDAT(new List<PostAssetOperation>() { load.ToDATLoad() });
                    load.DateDatDeleted = null;
                    load.AddComment($"Restored DAT [Restore]");
                    db.SaveChanges();
                    loads.Add(load);
                }
                foreach (var load in db.Loads.Where(x => idsTS.Contains(x.Id)).ToList())
                {
                    var r = IntegrationService.Instance.TruckStopUtils.UploadToTruckStop(new Load[] { load.ToTSLoad() });
                    messages.Add(load.Id, Newtonsoft.Json.JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented));
                    load.DateTSDeleted = null;
                    load.AddComment($"Restored TS [Restore]");
                    db.SaveChanges();
                    loads.Add(load);
                }

                ViewBag.DATId = DatIds;
                ViewBag.TSId = TSIds;
                ViewBag.Messages = messages;

                return View(loads);
            }
        }*/

        /*public ActionResult ParseLast()
        {
            using (var db = new DBContext())
            {
                foreach (var item in db.UploadLogs.OrderByDescending(x => x.Id))
                {
                    var msg = new EMessage
                    {
                        //Customer = Customers
                    };

                    IntegrationService.Instance.ParseEmails(new List<EMessage>() { msg });
                }
            }

            return View();
        }*/

        //Mercury Gate has only united Rentals
        //Active loads, YES. REMOVE from Loadboards, including AST Loads.
        //Once a driver has been assigned to a load, then it MUST be removed from the loadboards.
        //The new logic is:
        //Delete UNITED RENTALS loads if they are NOT in Mercury Gate Insight TMS
        public async Task<ActionResult> MercuryGate()
        {
            LastMercuryGate = DateTime.Now;

            var mgData = await IntegrationService.Instance.MercuryGateGetLoads();//Open Mercury Gate
            var removed = new List<LoadModel>();

			using (var db = new DBContext())
			{
				//Only uploaded and not deleted to DAT or TS
				//&& ((x.AssetId != null && !x.DateDatDeleted.HasValue) || (x.TrackStopId.HasValue && !x.DateTSDeleted.HasValue))
				//LOGIC: DO NOT COMPARE those Loads that have been imported less than 5 minutes
				var date = DateTime.Now.AddMinutes(-5);
				var loads = db.Loads
					.Where(x => x.ClientName == "United Rentals" && (!x.DateDatDeleted.HasValue || !x.DateTSDeleted.HasValue)) //United Rentals and not deleted from DAT or TS
					.Where(x => x.CreateDate < date) // now 20:25, compare all < 20:20 
					.ToList()
					.Where(x => x.CreatedBy.StartsWith("ParseEmails")) //CreatedBy = "ParseEmails..."
					.ToList();
				//Get from the Loads table all the loads that are of "United Rentals"
				//not deleted from DAT or TS

				if (mgData.Exceptions.Any())
				{
					foreach (var exc in mgData.Exceptions)
					{
						Logger.Write("MercuryGate", exc);
					}
				}

				foreach (var load in loads)
                {
					//If it cannot open, then DO NOT DELETE
					if (mgData.Exceptions.Any())
					{
						var msg = "Cannot open United Rentals in Mercury Gate / T-Insight";
						if (!db.LoadComments.Any(x => x.LoadId == load.Id && x.Comment == msg))
						{
							load.AddComment(msg);
						}
						continue;
					}

					var exists = mgData.Loads
                        .FirstOrDefault(x =>
                            x.LoadClientId == load.ClientLoadNum
                            || x.LoadId == load.ClientLoadNum
                            || x.LoadId == load.PostersReferenceId
                            || x.LoadClientId == load.PostersReferenceId);

					if (exists == null) //Not exists in MG - DELETE
                    {
                        //item.AdvancedData = load;
                        load.AddComment($"Deleted because United Rentals load does NOT exist at Insight TMS"); //Add comment
                        IntegrationService.Instance.DeleteFromSQLAndBoards(load, "MercuryGate"); //Delete from AST / DAT /TS
                        db.SaveChanges();
                        removed.Add(load);
                    }
                    else //exists - add debug info
                    {
                        exists.AdvancedData = load;
                    }
                }
            }

            ViewBag.Removed = removed;
            return View(mgData);
        }

        public async Task<ActionResult> MercuryGateTest()
        {
            var model = await IntegrationService.Instance.MercuryGateGetLoads();
            return View("MercuryGate", model);
        }

        public async Task<ActionResult> BanyanTechnology()
        {
            return View();
            LastBanyanTechnology = DateTime.Now;

            var userName = System.Configuration.ConfigurationManager.AppSettings["BanyanTechnologyUserName"] ?? "Dabundis";
            var password = System.Configuration.ConfigurationManager.AppSettings["BanyanTechnologyPassword"] ?? "RainforRent";

            BanyanTechnologyUtils banyanTechnologyUtils = new BanyanTechnologyUtils();
            var pages = await banyanTechnologyUtils.GetLoads(userName, password);

            var pagesLoads = new List<ASTDAT.Tools.BanyanTechnologyUtils.LoadModel>();

            using (var db = new DBContext())
            {
                var loads = db.Loads.Where(x => x.BanyanTechBOL != null).ToList();

                if (pages.ContainsKey("_list_"))
                {
                    pagesLoads = await banyanTechnologyUtils.ParseList(pages["_list_"]);
                    foreach (var pagesLoad in pagesLoads)
                    {
                        if (loads.Any(x => x.BanyanTechBOL == pagesLoad.BOLNum))
                        {
                            continue;
                        }

                        DateTime? pickupDate = null;
                        DateTime? deliveryDate = null;
                        DateTime dt;
                        if (DateTime.TryParseExact(pagesLoad.Origin.PickupDate, "M'/'d'/'yyyy h:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                        {
                            pickupDate = dt;
                        }
                        else if (DateTime.TryParseExact(pagesLoad.Origin.PickupDate, "M/d/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                        {
                            pickupDate = dt;
                        }
                        if (DateTime.TryParseExact(pagesLoad.Destination.DeliveryDate, "M'/'d'/'yyyy h:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                        {
                            deliveryDate = dt;
                        }
                        else if (DateTime.TryParseExact(pagesLoad.Destination.DeliveryDate, "M/d/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                        {
                            deliveryDate = dt;
                        }

                        var load = new LoadModel
                        {
                            DestinationId = IntegrationService.Instance.AddOriginDestination(db, pagesLoad.Destination.City, pagesLoad.Destination.State, pagesLoad.Destination.Country).Id,
                            OriginId = IntegrationService.Instance.AddOriginDestination(db, pagesLoad.Origin.City, pagesLoad.Origin.State, pagesLoad.Origin.Country).Id,
                            PickUpDate = pickupDate,
                            DeliveryDate = deliveryDate,
                            //LoadTypeId = 15, //Power Only
                            LoadTypeId = 35, //DRIVEAWAY
                            ClientName = "Rain for Rent",
                            AssetLength = 1,
                            //BanyanTechBOL = m.LoadId,
                            BanyanTechBOL = pagesLoad.BOLNum,
                            Comments = "",
                            CreateDate = DateTime.Now,
                            CreatedBy = $"BanyanTechnology Rain for Rent",
                            CreateLoc = "AutoApp",
                        };
                        load.CarrierAmount = IntegrationService.Instance.GetRateMate(load);
                        if (load.CarrierAmount > 0)
                        {
                            load.AddComment($"GetRateMate({load.CarrierAmount})");
                        }

                        if (pagesLoad.LoadType == "FTL")
                        {
                            load.LoadTypeId = 21;
                            load.IsLoadFull = true;
                        }
                        else if (pagesLoad.LoadType == "PTL")
                        {
                            load.LoadTypeId = 21;
                            load.IsLoadFull = false;
                        }
                        if ($"{pagesLoad.IsFull}".ToLower().Contains("full"))
                        {
                            load.IsLoadFull = true;
                        }
                        var eq = db.LoadTypes.FirstOrDefault(x =>
                            x.IdDAT.ToLower() == pagesLoad.LoadType.ToLower()
                            || x.IdTS.ToLower() == pagesLoad.LoadType.ToLower()
                            || x.IdDAT.ToLower() == pagesLoad.LoadType.ToLower()
                            || x.Name.ToLower() == pagesLoad.LoadType.ToLower()
                            || x.NameDAT.ToLower() == pagesLoad.LoadType.ToLower()
                            || x.NameTS.ToLower() == pagesLoad.LoadType.ToLower());
                        if (eq != null)
                        {
                            load.LoadTypeId = eq.Id;
                        }

                        db.Loads.Add(load);
                        db.SaveChanges();

                        var results = IntegrationService.Instance.DoUploadLoad(load, "BanyanTechnology");
                        foreach (var result in results)
                        {
                            load.AddComment($"BanyanTechnology IsDAT:{result.IsDAT},IsTS:{result.IsTS}={result.IsSuccess}={result.Message}");
                        }
                        db.SaveChanges();
                    }
                }

                foreach (var load in loads)
                {
                    IntegrationService.Instance.DeleteFromSQLAndBoards(load, "IntegrationController.BanyanTechnology");
                    db.SaveChanges();
                }
            }

            return View(pagesLoads);
        }

        public ActionResult RateMate()
        {
            return View();
        }

        public ActionResult UpdateAsset(int id, float amount, int lengthFeet = 0, string comment = "")
        {
            using (var db = new DBContext())
            {
                var load = db.Loads.FirstOrDefault(x => x.Id == id);

                var dimensions = new Dimensions
                {
                    lengthFeet = lengthFeet,
                };

                var updateAssetOperation = new UpdateAssetOperation
                {
                    Item1 = new ShipmentUpdate
                    {
                        comments = new string[] { comment },
                        rate = new ShipmentRate
                        {
                            baseRateDollars = amount,
                        },
                        dimensions = lengthFeet == 0 ? null : dimensions,
                    },
                    ItemElementName = ItemChoiceType.assetId,
                    Item = load.AssetId,
                };
                IntegrationService.Instance.Session.UpdateAssets(updateAssetOperation);
            }

            return View();
        }

        public ActionResult _DeleteFromDATAndTS(string pwd)
        {
            ViewBag.Complete = false;
            ViewBag.DATCountB = 0;
            ViewBag.TSCountB = 0;
            ViewBag.DATCountA = 0;
            ViewBag.TSCountA = 0;

            if (pwd == DateTime.Now.Hour.ToString())
            {
                ViewBag.Complete = true;

                var datLoads = IntegrationService.Instance.Session.GetLoads();
                if (datLoads != null && datLoads.assets != null)
                {
                    ViewBag.DATCountB = datLoads.assets.Length;
                }
                IntegrationService.Instance.Session.DeleteAllAssets();
                datLoads = IntegrationService.Instance.Session.GetLoads();
                if (datLoads != null && datLoads.assets != null)
                {
                    ViewBag.DATCountA = datLoads.assets.Length;
                }

                var tsLoads = IntegrationService.Instance.TruckStopUtils.GetLoads();
                if (tsLoads != null && tsLoads.Data is List<Load> loads)
                {
                    ViewBag.TSCountB = loads.Count;
                    IntegrationService.Instance.TruckStopUtils.DeleteByIds(loads.Select(x => x.LoadNumber).ToArray());
                }

                tsLoads = IntegrationService.Instance.TruckStopUtils.GetLoads();
                if (tsLoads != null && tsLoads.Data is List<Load> tsloads)
                {
                    ViewBag.TSCountA = tsloads.Count;
                }
            }

            return View();
        }


		//Every 4 minutes, eliminate TO DELETE all the loads that are in AST Loads prior to today
		public ActionResult DeleteOld()
        {
            using (var db = new DBContext())
            {
                var dayStart = DateTime.Now.Date;

				/*var loads = db.Loads.Where(x =>
                    (!x.UntilDate.HasValue || x.UntilDate < today) //no UntilDate or UntilDate < today
                    &&
                    ((x.PickUpDate.HasValue && x.PickUpDate < today) || (x.DeliveryDate.HasValue && x.DeliveryDate < today)) //PickUpDate or DeliveryDate < today
                    &&
                    (!x.DateDatDeleted.HasValue || !x.DateTSDeleted.HasValue) //in TS or DAT
                    ).ToList();*/

				var loads = db.Loads
					.Where(x => !x.CreateDate.HasValue || x.CreateDate < dayStart) //prior day (no CreateDate or less start of day)
					.Where(x => !x.DateDatDeleted.HasValue || !x.DateTSDeleted.HasValue) //not deleted
					.Where(x => !x.UntilDate.HasValue || x.UntilDate < dayStart) //no UntilDate or UntilDate < start of day
					.ToList();

				foreach (var load in loads)
                {
                    IntegrationService.Instance.DeleteFromSQLAndBoards(load, "DeleteOld");
					db.SaveChanges();
                }
            }

            return View();
        }

        public ActionResult GetDATLoads()
        {
            var currentDATLoads = IntegrationService.Instance.GetDATLoads();
            ViewBag.LastExceptions = IntegrationService.Instance.Session.LastExceptions;
            return View("DATLoadState", currentDATLoads);
        }

        public ActionResult GetDATLoadsCount()
        {
            var currentDATLoadsCount = IntegrationService.Instance.GetDatLoadsCount();
            ViewBag.LastExceptions = IntegrationService.Instance.Session.LastExceptions;
            ViewBag.Count = currentDATLoadsCount;
            return View("DATLoadsCount");
        }

        public ActionResult GetTSLoads()
        {
            var currentTSloads = IntegrationService.Instance.GetTSLoads();
            ViewBag.LastExceptions = IntegrationService.Instance.TruckStopUtils.LastExceptions;
            return View("TSLoadState", currentTSloads);
        }

        public ActionResult DeleteOlder30()
        {
            IntegrationService.Instance.DeleteOlder30();
            return View();
        }

        [Authorize]
        public ActionResult ServiceManage(string mode)
        {
            if (mode == "stop")
            {
                System.IO.File.WriteAllText(Server.MapPath("~/App_Data/ServicePause.flg"), "");
                return RedirectToAction("ServiceManage");
            }
            if (mode == "start")
            {
                if (System.IO.File.Exists(Server.MapPath("~/App_Data/ServicePause.flg")))
                {
                    System.IO.File.Delete(Server.MapPath("~/App_Data/ServicePause.flg"));
                }
                return RedirectToAction("ServiceManage");
            }

            ViewBag.ServiceStopped = System.IO.File.Exists(Server.MapPath("~/App_Data/ServicePause.flg"));
            ViewBag.ServiceStatus = System.IO.File.Exists(Server.MapPath("~/App_Data/ServicePause.flg")) ? "Stopped" : "Started";
            return View();
        }

		public async Task<ActionResult> ReimportEmail(string id)
		{
			using (var db = new DBContext())
			{
				var email = db.UploadLogs.FirstOrDefault(x => x.MessageID == id);
				var messages = new List<Tuple<string, string>>();
				messages.Add(new Tuple<string, string>(email.EmailBody, email.MessageID));

				//Remove from UploadLogs
				db.UploadLogs.Remove(email);
				//Remove from LoadComments
				var load = db.Loads.FirstOrDefault(x => x.EmailID == id);
				if (load != null)
				{
					var comments = db.LoadComments.Where(x => x.LoadId == load.Id).ToList();
					db.LoadComments.RemoveRange(comments);
					//Remove from Loads
					Logger.Write($"ReimportEmail {load.Id} {load.CreateDate}", fileName: "DeleteLoads.txt");
					db.Loads.Remove(load); //ReimportEmail
					db.SaveChanges();

					var mercuryGateGetLoads = await IntegrationService.Instance.MercuryGateGetLoads();
					IntegrationService.Instance.ParseEmails(messages, mercuryGateGetLoads);
				}
			}

			return View();
		}

		public ActionResult TextReplacer()
		{
			ViewBag.Text = @"MUSTTT DELIVER BY 10/09/2019 0800!* Special Instructions:
POC: CHARLES LACKIE 480-335-4687
Item: DIMENSIONS: 239&quot; X 96&quot; X 150&quot; - 28,000 LBS THIS IS A 5TH WHEEL - POWER ONLY UNIT";

			ViewBag.Text += @"
(0)a@b
(1)a@b.
(2)a@b.c
(3)a@b.ce
(4)a-b@c.de
(5)a.b@c.de
(6)a@2-b.cd
(7)a@2b.cd
(8)a@b-c.de
(9)a@bc-d-2.e-f.gh ";

			ViewBag.Text += @"
Look for 123*123*1234, where * can be any charater and N is always a number
Look for 1231231234, where there are no separators
Look for (123)*1231234, or
Look for (123)*123*1234";

			return View();
		}

		[HttpPost]
		public ActionResult TextReplacer(string text)
		{
			var lines = text.Split('\r');
			var result = "";
			foreach(var line in lines)
			{
				//phone
				var regex = new Regex(@"\(?([0-9]{3})\)?.?([0-9]{3}).?([0-9]{4})");
				if (regex.IsMatch(line))
					continue;

				//email
				regex = new Regex(@"\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*", RegexOptions.IgnoreCase);
				if (regex.IsMatch(line))
					continue;

				result += line;
			}

			ViewBag.Text = text;
			ViewBag.Result = result;

			return View();
		}

		public ActionResult DATPostersReferenceIdUpdate()
		{
			var loads = IntegrationService.Instance.DATPostersReferenceIdUpdate();

			return View(loads);
		}

		[HttpGet]
		//[HttpPost]
		public ActionResult RestoreLoads()
		{
			ViewBag.State = "Seek";

			using (var db = new DBContext())
			{
				var today = DateTime.Now.Date;
                var yesterday = DateTime.Now.Date.AddDays(-1);

                var loads = db.Loads
					.Where(x =>
						(x.DateDatDeleted.HasValue && x.DateTSDeleted.HasValue)
                        && x.PickUpDate.HasValue
                        && x.CreateDate.HasValue
						&& 
						((x.UntilDate >= today && x.PickUpDate >= today) || (!x.UntilDate.HasValue && x.PickUpDate >= today))
						)
                    .OrderBy(x => x.CreateDate)
					.ToList();

				return View(loads);
			}
		}
		
		[HttpPost]
		public ActionResult RestoreLoads(List<int> ids)
		{
			ViewBag.State = "Restored";

			using (var db = new DBContext())
			{
				var today = DateTime.Now.Date;

				var loads = db.Loads
					.Where(x => ids.Contains(x.Id))
                    .OrderBy(x => x.CreateDate)
                    .ToList();

				foreach(var load in loads)
				{
					load.DateDatDeleted = null;
					load.AssetId = null;

					load.DateTSDeleted = null;
					load.TrackStopId = null;

					db.SaveChanges();
                    load.AddComment("the load are restored");

					IntegrationService.Instance.DoUploadLoad(load, "RestoreLoads");
				}

                return RedirectToAction("RestoreLoads");
			}
		}

		public ActionResult DATConnection(bool? disable = null)
		{
			if (disable.HasValue)
			{
				DATService.Session.LockInternet = disable.Value;
				if (disable.Value)
				{
					IntegrationService.Instance.Session.ClearConnected("DATDisconnect");
				}

				return RedirectToAction("DATConnection");
			}

			ViewBag.LockInternet = DATService.Session.LockInternet;

			string lines = "";
			using (var reader = new StreamReader(Server.MapPath("~/App_Data/Log.txt")))
			{
				if (reader.BaseStream.Length > 50000)
				{
					reader.BaseStream.Seek(-50000, SeekOrigin.End);
				}
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					lines += line + "\r\n";
				}
			}

			ViewBag.Lines = lines;

			return View();
		}

		public ActionResult TSConnection(bool? disable = null)
		{
			if (disable.HasValue)
			{
				DATService.Session.LockInternet = disable.Value;
				if (disable.Value)
				{
					IntegrationService.Instance.Session.ClearConnected("DATDisconnect");
				}

				return RedirectToAction("DATConnection");
			}

			ViewBag.LockInternet = DATService.Session.LockInternet;

			string lines = "";
			using (var reader = new StreamReader(Server.MapPath("~/App_Data/Log.txt")))
			{
				if (reader.BaseStream.Length > 50000)
				{
					reader.BaseStream.Seek(-50000, SeekOrigin.End);
				}
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					lines += line + "\r\n";
				}
			}

			ViewBag.Lines = lines;

			return View();
		}

		public async Task<ActionResult> NL()
		{
			var baseUri = new Uri("https://api-test.nextload.com/");
			var encodedConsumerKey = HttpUtility.UrlEncode("valencio@rsoft.net");
			var encodedConsumerKeySecret = HttpUtility.UrlEncode("ty4Ydpr!");
			var encodedPair = String.Format("{0}:{1}", encodedConsumerKey, encodedConsumerKeySecret);

			var requestToken = new HttpRequestMessage
			{
				Method = HttpMethod.Post,
				RequestUri = new Uri(baseUri, "oauth2/token"),
				Content = new StringContent("grant_type=client_credentials")
			};

			requestToken.Content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded") { CharSet = "UTF-8" };
			requestToken.Headers.TryAddWithoutValidation("Authorization", String.Format("Basic {0}", encodedPair));

			var httpClient = new HttpClient();
			var bearerResult = await httpClient.SendAsync(requestToken);
			var bearerData = await bearerResult.Content.ReadAsStringAsync();
			/*var bearerToken = JObject.Parse(bearerData)["access_token"].ToString();

			var requestData = new HttpRequestMessage
			{
				Method = HttpMethod.Get,
				RequestUri = new Uri(baseUri, apiPath),
			};
			requestData.Headers.TryAddWithoutValidation("Authorization", String.Format("Bearer {0}", bearerToken));

			var results = await HttpClient.SendAsync(requestData);
			return await results.Content.ReadAsStringAsync();*/

			return View();
		}

		public ActionResult NextLoadRedirect()
		{
			return View();
		}
	}
}