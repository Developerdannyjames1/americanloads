using System;
using System.Data.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Threading.Tasks;

using ExcelDataReader;

//using TruckStopService;
//using TruckStopService.TruckStopServiceReference;

using ASTDAT.Data.Models;
using ASTDAT.Web.Infrastructure;
using DATService;
using DATService.ServiceReference1;
using System.Data;
using System.Web;
using ASTDAT.Data;
using ASTDAT.Tools;
using ASTDAT.Web.Models;
using Microsoft.AspNet.Identity.Owin;
using TruckStopRestfullService.Models;

namespace ASTDAT.Web.Controllers.API
{
    [Authorize]
    public class LoadsController : ApiController
    {
        public class ListModel
        {
            public int? SortDate { get; set; }
            public string Sort { get; set; }
            public DateTime? DateFrom { get; set; }
            public DateTime? DateTo { get; set; }
            public string OriginZip { get; set; }
            public string DestinZip { get; set; }
            public bool ShowDeleted { get; set; }
            public DateTime? LastRefreshDAT { get; set; }
            public string Id { get; set; }
			public string RefId { get; set; }

			public List<string> Companies { get; set; }
            public List<string> OriginCities { get; set; }
            public List<string> OriginStates { get; set; }
            public List<string> DestinationCities { get; set; }
            public List<string> DestinStates { get; set; }

            public int Page { get; set; }
            public int PerPage { get; set; }

            public bool FullLoad { get; set; }
        }

        [Route("api/Loads/List")]
        [HttpPost]
        public object List(ListModel model)
        {
            try
            {
                DateTime started = DateTime.Now;

                model.SortDate = model.SortDate ?? 1;
                model.Sort = model.Sort ?? "";
                model.DateTo = model.DateTo.HasValue ? (DateTime?)model.DateTo.Value.AddDays(1) : null;
                model.Page = model.Page == 0 ? 1 : model.Page;
                model.PerPage = model.PerPage == 0 ? 100 : model.PerPage;

                var db = new DBContext();

                var allLoadTypes = db.LoadTypes.OrderBy(x => x.Name).ToList();

                //Assign correct loadtype
                var timing = new List<string>();
                timing.Add($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}] Start assign correct load types");
                foreach (var load in db.Loads.Where(x => !x.LoadTypeId.HasValue).ToList())
                {
                    load.EquipmentType = load.EquipmentType ?? "";
                    var loadType = allLoadTypes.FirstOrDefault(x => x.IdDAT.ToLower() == load.EquipmentType.ToLower());
                    if (loadType != null)
                    {
                        load.LoadTypeId = loadType.Id;
                        db.SaveChanges();
                    }
                    else
                    {
                        loadType = allLoadTypes.FirstOrDefault(x => x.IdTS.ToLower() == load.EquipmentType.ToLower());
                        if (loadType != null)
                        {
                            load.LoadTypeId = loadType.Id;
                            db.SaveChanges();
                        }
                        else
                        {
                            loadType = allLoadTypes.FirstOrDefault(x => x.NameDAT.ToLower() == load.EquipmentType.ToLower());
                            if (loadType != null)
                            {
                                load.LoadTypeId = loadType.Id;
                                db.SaveChanges();
                            }
                            else
                            {
                                loadType = allLoadTypes.FirstOrDefault(x => x.NameTS.ToLower() == load.EquipmentType.ToLower());
                                if (loadType != null)
                                {
                                    load.LoadTypeId = loadType.Id;
                                    db.SaveChanges();
                                }
                            }
                        }
                    }
                }

                timing.Add($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}] Start query build");
                Expression<Func<LoadModel, bool>> filter1;
                if (model.ShowDeleted)
                {
                    //filter1 = (x => (x.DateDatDeleted.HasValue || x.AssetId == null) && (x.DateTSDeleted.HasValue || !x.TrackStopId.HasValue));
                    //filter1 = (x => x.DateDatDeleted.HasValue && x.DateTSDeleted.HasValue);
                    filter1 = (x => x.DateDatDeleted.HasValue && x.DateTSDeleted.HasValue && (x.ClientName != "UNITED RENTALS")); //HIDE UNITED RENTALS
				}
                else
                {
                    //filter1 = (x => (!x.DateDatDeleted.HasValue && x.AssetId != null) || (!x.DateTSDeleted.HasValue && x.TrackStopId.HasValue));
                    //show loads where not deleted from DAT or TS   
                    filter1 = (x => !x.DateDatDeleted.HasValue || !x.DateTSDeleted.HasValue);
                }

                model.Companies = model.Companies ?? new List<string>();
                model.OriginCities = model.OriginCities ?? new List<string>();
                model.OriginStates = model.OriginStates ?? new List<string>();
                model.DestinationCities = model.DestinationCities ?? new List<string>();
                model.DestinStates = model.DestinStates ?? new List<string>();

                var q = db.Loads
                    .Where(filter1)
                    .Where(x => !model.DateFrom.HasValue || x.CreateDate >= model.DateFrom)
                    .Where(x => !model.DateTo.HasValue || x.CreateDate < model.DateTo)
                    .Where(x => model.Id == null || x.Id.ToString().Contains(model.Id))
					.Where(x => model.RefId == null || x.ClientLoadNum.Contains(model.RefId))
					.Where(x => model.Companies.Count == 0 || model.Companies.Contains(x.ClientName))
                    .Where(x => model.OriginCities.Count == 0 || model.OriginCities.Contains(x.Origin.City))
                    .Where(x => model.OriginStates.Count == 0 || model.OriginStates.Contains(x.Origin.State.Code))
                    .Where(x => model.DestinationCities.Count == 0 || model.DestinationCities.Contains(x.Destination.City))
                    .Where(x => model.DestinStates.Count == 0 || model.DestinStates.Contains(x.Destination.State.Code))
                    ;

                if (model.Sort == "")
                {
                    //qs = model.SortDate == 1 ? q.OrderBy(x => x.DateLoaded) : q.OrderByDescending(x => x.DateLoaded);
                    q = model.SortDate == 1 ? q.OrderBy(x => x.PickUpDate) : q.OrderByDescending(x => x.PickUpDate);
                }
                else
                {
                    if (model.Sort.StartsWith("OriginCity"))
                    {
                        q = model.Sort.Contains("asc") ? q.OrderBy(x => x.Origin.City) : q.OrderByDescending(x => x.Origin.City);
                    }
                    if (model.Sort.StartsWith("OriginState"))
                    {
                        q = model.Sort.Contains("asc") ? q.OrderBy(x => x.Origin.State.Code) : q.OrderByDescending(x => x.Origin.State.Code);
                    }
                    if (model.Sort.StartsWith("OriginZip"))
                    {
                        q = model.Sort.Contains("asc") ? q.OrderBy(x => x.Origin.PostalCode) : q.OrderByDescending(x => x.Origin.PostalCode);
                    }
                    if (model.Sort.StartsWith("DestinCity"))
                    {
                        q = model.Sort.Contains("asc") ? q.OrderBy(x => x.Destination.City) : q.OrderByDescending(x => x.Destination.City);
                    }
                    if (model.Sort.StartsWith("DestinState"))
                    {
                        q = model.Sort.Contains("asc") ? q.OrderBy(x => x.Destination.State.Code) : q.OrderByDescending(x => x.Destination.State.Code);
                    }
                    if (model.Sort.StartsWith("DestinZip"))
                    {
                        q = model.Sort.Contains("asc") ? q.OrderBy(x => x.Destination.PostalCode) : q.OrderByDescending(x => x.Destination.PostalCode);
                    }
                    if (model.Sort.StartsWith("CompanyName"))
                    {
                        q = model.Sort.Contains("asc") ? q.OrderBy(x => x.ClientName) : q.OrderByDescending(x => x.ClientName);
                    }
					if (model.Sort.StartsWith("RefId"))
					{
						q = model.Sort.Contains("asc") ? q.OrderBy(x => x.ClientLoadNum) : q.OrderByDescending(x => x.ClientLoadNum);
					}
					if (model.Sort.StartsWith("LoadId"))
					{
						q = model.Sort.Contains("asc") ? q.OrderBy(x => x.Id) : q.OrderByDescending(x => x.Id);
					}					
					//q = model.SortDate == 1 ? q.AsQueryable().ThenBy(x => x.DateLoaded) : q.ThenByDescending(x => x.DateLoaded);
				}

				timing.Add($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}] Start query run");

                var q2 = q
                    .ToList()
                    .Select(x => new LoadModel
                    {
                        AssetId = x.AssetId,
                        AvailabilityEarliest = x.AvailabilityEarliest,
                        AvailabilityLatest = x.AvailabilityLatest,
                        ClientLoadNum = x.ClientLoadNum,
                        ClientName = x.ClientName,
                        Count = x.Count,
                        DateDatDeleted = x.DateDatDeleted,
                        DateDatLoaded = x.DateDatLoaded,
                        DateDatRefreshed = x.DateDatRefreshed,
                        DateLoaded = x.DateLoaded,
                        DateRefreshed = x.DateRefreshed,
                        DateRTFLoaded = x.DateRTFLoaded,
                        DateTRTLoaded = x.DateTRTLoaded,
                        Destination = x.Destination,
                        DestinationId = x.DestinationId,
                        DimensionsHeightInches = x.DimensionsHeightInches,
                        DimensionsLengthFeet = x.DimensionsLengthFeet,
                        DimensionsVolumeCubic = x.DimensionsVolumeCubic,
                        DimensionsWeightPounds = x.DimensionsWeightPounds,
                        EmailID = x.EmailID,
                        EquipmentType = x.EquipmentType,
                        Id = x.Id,
                        IncludeAsset = x.IncludeAsset,
                        Ltl = x.Ltl,
                        OriginId = x.OriginId,
                        Origin = x.Origin,
                        PostersReferenceId = x.PostersReferenceId,
                        PostToExtendedNetwork = x.PostToExtendedNetwork,
                        CarrierAmount = x.CarrierAmount,
                        RateEateBasedOn = x.RateEateBasedOn,
                        RateRateMiles = x.RateRateMiles,
                        Stops = x.Stops,
                        TruckStopsEnhancements = x.TruckStopsEnhancements,
                        TruckStopsPosterDisplayName = x.TruckStopsPosterDisplayName,
                        AssetLength = x.AssetLength,
                        TrackStopId = x.TrackStopId,
                        TsLoadId = x.TsLoadId,
                        DateTSDeleted = x.DateTSDeleted,
                        LoadType = x.LoadType,
                        LoadTypeId = x.LoadTypeId,
                        UntilDate = x.UntilDate,
                        Description = x.Description,
                        IsLoadFull = x.IsLoadFull,
                        PickUpDate = x.PickUpDate,
                        DeliveryDate = x.DeliveryDate,
                        CreateDate = x.CreateDate,
                        CreatedBy = x.CreatedBy,
                        CreateLoc = x.CreateLoc,
                        UpdateDate = x.UpdateDate,
                        UpdatedBy = x.UpdatedBy,
                        UpdateLoc = x.UpdateLoc,
                        CustomerAmount = x.CustomerAmount,
                        Weight = x.Weight,
						UserNotes = x.UserNotes,
						AllowUntilSat = x.AllowUntilSat,
						AllowUntilSun = x.AllowUntilSun,
                    });

                timing.Add($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}] Start pagination 1");
                //var pageCount = (int)Math.Ceiling(q2.Count() / (double)model.PerPage);
                //var pageCount = (int)Math.Ceiling(db.Loads.Count(filter1) / (double)model.PerPage);
                var pc = db.Loads.Where(filter1)
                    .Where(x => !model.DateFrom.HasValue || x.CreateDate >= model.DateFrom)
                    .Where(x => !model.DateTo.HasValue || x.CreateDate < model.DateTo)
                    .Where(x => model.Id == null || x.Id.ToString().Contains(model.Id))
                    .Where(x => model.Companies.Count == 0 || model.Companies.Contains(x.ClientName))
                    .Where(x => model.OriginCities.Count == 0 || model.OriginCities.Contains(x.Origin.City))
                    .Where(x => model.OriginStates.Count == 0 || model.OriginStates.Contains(x.Origin.State.Code))
                    .Where(x => model.DestinationCities.Count == 0 || model.DestinationCities.Contains(x.Destination.City))
                    .Where(x => model.DestinStates.Count == 0 || model.DestinStates.Contains(x.Destination.State.Code))
                    .Count();
                var pageCount = (int)Math.Ceiling(pc / (double)model.PerPage);

                timing.Add($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}] Start pagination 2");
                var list = q2.Skip(model.Page * model.PerPage - model.PerPage).Take(model.PerPage).ToList();

                timing.Add($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}] Start build result");
                var compNames = list.Select(s => s.ClientName).Distinct().ToList();
                var result = new
                {
                    //List = list,
                    //List = model.ShowDeleted ? list.Skip(model.Page * model.PerPage - model.PerPage).Take(model.PerPage).ToList() : list,
                    //List = list.Skip(model.Page * model.PerPage - model.PerPage).Take(model.PerPage).ToList(),
                    List = list,
                    PageCount = pageCount,
                    Page = model.Page,
                    PerPage = model.PerPage,
                    SortDate = model.SortDate,
                    Sort = model.Sort,

                    //Companies = list.Select(x => x.ClientName).OrderBy(x => x).Distinct(),
                    //LoadDates = list.Select(x => x.DateLoaded).OrderBy(x => x).Distinct(),
                    //OriginStates = list.Select(x => x.Origin.State.Code).OrderBy(x => x).Distinct(),
                    //DestinStates = list.Select(x => x.Destination.State.Code).OrderBy(x => x).Distinct(),
                    //OriginCities = list.Select(x => x.Origin.City).OrderBy(x => x).Distinct(),
                    //DestinationCities = list.Select(x => x.Destination.City).OrderBy(x => x).Distinct(),
                    //AllLoadTypes = db.Loads.Select(x => x.EquipmentType).OrderBy(x => x).Distinct(),
                    //AllCompanies = db.Loads.Select(x => x.ClientName).OrderBy(x => x).Distinct(),

                    ShowDeleted = model.ShowDeleted,
                    //TotalLoads = db.Loads.Count(x => (!x.DateDatDeleted.HasValue && x.AssetId != null) || (!x.DateTSDeleted.HasValue && x.TrackStopId.HasValue)),
                    //TotalLoads = db.Loads.Count(x => !x.DateDatDeleted.HasValue && !x.DateTSDeleted.HasValue),
                    TotalLoads = db.Loads.Count(filter1),
                    LastRefreshDAT = IntegrationController.LastDATRefreshState.HasValue ? IntegrationController.LastDATRefreshState.Value.ToString("HH:mm") : "--:--",

                    AllCities = db.OriginDestinations.Select(x => x.City).OrderBy(x => x).Distinct(),
                    AllStates = db.States.Select(x => x.Code).OrderBy(x => x).Distinct(),
                    OriginDestinations = db.OriginDestinations.Include(x => x.State).OrderBy(x => x.City).ToList(),
                    AllLoadTypes = allLoadTypes,
                    AllCompanies = db.Companies.Where(p=> compNames.Contains(p.Name)).OrderBy(x => x.Name).Select(x => x.Name).Distinct().ToList(),

                    Timing = timing,
                };
                result.Timing.Add($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}] Complete");
                var done = DateTime.Now;
                result.Timing.Add($"{started.ToString("HH:mm:ss.fff")}-{done.ToString("HH:mm:ss.fff")} {(done - started).TotalMilliseconds}");

                return result;
            }
            catch (Exception exc)
            {
                return new
                {
                    Exception1 = exc.Message,
                    Exception2 = exc.InnerException == null ? "" : exc.InnerException.Message,
                    StackTrace = exc.StackTrace,
                    TotalLoads = 0,
                };
            }
        }

        public class DeleteModel
        {
            public List<int> Ids { get; set; }
            public bool? DAT { get; set; }
            public bool? TS { get; set; }
            public bool? Eliminate { get; set; }
        }

        [Route("api/Loads/Delete")]
        [HttpPost]
        public object Delete(DeleteModel model)
        {
            Logger.Write("Delete");
            var db = new DBContext();

            var loads = db.Loads
                .Include(x => x.Destination)
                .Include(x => x.Origin)
                .Include(x => x.Destination.State)
                .Include(x => x.Origin.State)
                .Where(x => model.Ids.Contains(x.Id))
                .ToList();

            //Eliminate
            if (model.Eliminate == true)
            {
                foreach (var item in loads)
                {
                    if (item.AssetId != null && !item.DateDatDeleted.HasValue)
                    {
                        continue;
                    }
                    if (item.TrackStopId.HasValue && item.TrackStopId>0 && !item.DateTSDeleted.HasValue)
                    {
                        continue;
                    }

                    IntegrationService.Instance.LoadHistoryDelete(item);

                    db.LoadComments.RemoveRange(db.LoadComments.Where(x => x.LoadId == item.Id));
                    db.SaveChanges();
                    db.Loads.Remove(item); //Eliminate
					Logger.Write($"Eliminate {item.Id}", fileName: "DeleteLoads.txt");
					db.SaveChanges();
                }
                return new
                {
                    Ok = true,
                };
            }

            if (!model.DAT.HasValue && !model.TS.HasValue)
            {
                model.DAT = true;
                model.TS = true;
            }

            foreach (var load in loads)
            {
				try
				{
					IntegrationService.Instance.DeleteFromSQLAndBoards(load, "API.LoadsController.Delete");
					if (load.ClientName?.ToUpper() == "UNITED RENTALS")
					{
						db.LoadComments.RemoveRange(db.LoadComments.Where(x => x.LoadId == load.Id));
						db.SaveChanges();
						Logger.Write($"Delete UNITED RENTALS {load.Id}", fileName: "DeleteLoads.txt");
						db.Loads.Remove(load); //Delete UNITED RENTALS
					}
					db.SaveChanges();
				}
				catch
				{

				}
			}

			return new
            {
                Ok = true,
            };
        }

        [Route("api/Loads/UpdateDAT")]
        [HttpPost]
        public async Task<object> UpdateDAT(LoadModel model)
        {
            return await AddLoadInt(model, true);
        }

        [Route("api/Loads/AddLoad")]
        [HttpPost]
        public async Task<object> AddLoad(LoadModel model)
        {
            return await AddLoadInt(model, false);
        }

        private async Task<object> AddLoadInt(LoadModel model, bool updateDAT)
        {
            model.PickUpDate = (new DateTimeOffset(model.PickUpDate.Value)).DateTime;
            if (model.DeliveryDate.HasValue)
            {
                model.DeliveryDate = (new DateTimeOffset(model.DeliveryDate.Value)).DateTime;
            }
            using (var db = new DBContext())
            {
                var userManager = HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>();
                var user = await userManager.FindByNameAsync(User.Identity.Name);
                
                //Create Destination
                if (model.Destination.Id == 0)
                {
                    model.Destination = IntegrationService.Instance.AddOriginDestination(db, model.Destination.City, model.Destination.State?.Code);
                }
                //Create Origin
                if (model.Origin.Id == 0)
                {
                    model.Origin = IntegrationService.Instance.AddOriginDestination(db, model.Origin.City, model.Origin.State?.Code);
                }
				//Create Company
				model.ClientName = model.ClientName.ToUpper();
				if (!db.Companies.Any(x => x.Name == model.ClientName))
                {
                    db.Companies.Add(new Data.CompanyModel { Name = model.ClientName });
                }
                db.SaveChanges();

                if (model.Id == 0) //NEW
                {
                    model.DestinationId = model.Destination.Id;
                    model.Destination = null;
                    model.OriginId = model.Origin.Id;
                    model.Origin = null;
                    model.EquipmentType = "Van";
                    model.LoadTypeId = model.LoadType?.Id;
                    model.LoadType = null;
                    model.UntilDate = model.UntilDate;
                    model.AssetLength = model.AssetLength;
                    model.Description = model.Description;
                    model.IsLoadFull = model.IsLoadFull;
                    model.DeliveryDate = model.DeliveryDate;
                    model.PickUpDate = model.PickUpDate;
                    model.CarrierAmount = model.CarrierAmount;
                    model.CustomerAmount = model.CustomerAmount;
                    model.Weight = model.Weight;
                    model.UserNotes = model.UserNotes;
                    model.AllowUntilSat = model.AllowUntilSat;
                    model.AllowUntilSun = model.AllowUntilSun;

                    model.CreateDate = DateTime.Now;
                    model.CreatedBy = user.UserName;
                    model.CreateLoc = user.Location;
                    model.UpdateDate = DateTime.Now;
                    model.UpdatedBy = user.UserName;
                    model.UpdateLoc = user.Location;

                    db.Loads.Add(model);
                    //ADD
                    db.SaveChanges();
                    //model.AddComment($"Load added from WEB"); 11/10/2019
					model.AddComment($"Added by user: {model.CreatedBy}, IP: {HttpContext.Current.Request.UserHostAddress}");

					model.ClientLoadNum = String.IsNullOrEmpty(model.ClientLoadNum) ? $"{model.Id}" : model.ClientLoadNum;
                    db.SaveChanges();
                    db.Entry(model).State = EntityState.Detached;

                    //UPLOAD
                    model = db.Loads
                            .AsNoTracking()
                            .Include(x => x.Destination)
                            .Include(x => x.Destination.State)
                            .Include(x => x.Origin)
                            .Include(x => x.Origin.State)
                            .Include(x => x.LoadType)
                            .FirstOrDefault(x => x.Id == model.Id);

                    var errors = IntegrationService.Instance.DoUploadLoad(model, "AddUploadInt.Add");

                    model = db.Loads
                            .Include(x => x.Destination)
                            .Include(x => x.Destination.State)
                            .Include(x => x.Origin)
                            .Include(x => x.Origin.State)
                            .Include(x => x.LoadType)
                            .Include(x => x.AllComments)
                            .FirstOrDefault(x => x.Id == model.Id);

                    model.Comments = (model.Comments == null ? "" : $"{model.Comments}\r\n") + String.Join("\r\n", model.AllComments.OrderBy(x => x.DateTime).Select(z => $"[{z.DateTime.ToString("yyyy/MM/dd HH:mm:ss")}] {z.Comment}"));
                    model.AllComments.ForEach(x =>
                    {
                        x.Load = null;
                    });

                    return new
                    {
                        Ok = true,
                        model = model,
                        errors = errors,
                    };
                }
                else
                {
                    var canUseUpdateAsset = true;

                    var currentLoad = db.Loads.FirstOrDefault(x => x.Id == model.Id);

                    if (currentLoad.DestinationId != model.Destination.Id)  canUseUpdateAsset = false;//1
                    if (currentLoad.OriginId != model.Origin.Id)            canUseUpdateAsset = false;//2
                    if (currentLoad.LoadTypeId != model.LoadType?.Id)       canUseUpdateAsset = false;//3
                    if (currentLoad.IsLoadFull != model.IsLoadFull)         canUseUpdateAsset = false;//4
                    if (currentLoad.DeliveryDate != model.DeliveryDate)     canUseUpdateAsset = false;//5
                    if (currentLoad.PickUpDate != model.PickUpDate)         canUseUpdateAsset = false;//6

                    if (!currentLoad.DateLoaded.EqualByDate(model.DateLoaded))         canUseUpdateAsset = false;//7
                    if (!currentLoad.UntilDate.EqualByDate(model.UntilDate))           canUseUpdateAsset = false;//8

                    currentLoad.DestinationId = model.Destination.Id; //1
                    currentLoad.OriginId = model.Origin.Id;//2
                    currentLoad.LoadTypeId = model.LoadType?.Id;//3
                    currentLoad.DateLoaded = model.DateLoaded;//4
                    currentLoad.UntilDate = model.UntilDate;//5
                    currentLoad.IsLoadFull = model.IsLoadFull;//6
                    currentLoad.DeliveryDate = model.DeliveryDate;//7
                    currentLoad.PickUpDate = model.PickUpDate;//8

					currentLoad.ClientLoadNum = model.ClientLoadNum;//8

					currentLoad.UpdateDate = DateTime.Now;
                    currentLoad.UpdatedBy = user.UserName;
                    currentLoad.UpdateLoc = user.Location;

                    currentLoad.AssetLength = model.AssetLength;
                    currentLoad.CarrierAmount = model.CarrierAmount;
                    currentLoad.Description = model.Description;
                    currentLoad.Weight = model.Weight;

					currentLoad.ClientName = model.ClientName;
					currentLoad.CustomerAmount = model.CustomerAmount;
					currentLoad.UserNotes = model.UserNotes;
					currentLoad.AllowUntilSat = model.AllowUntilSat;
					currentLoad.AllowUntilSun = model.AllowUntilSun;

					db.SaveChanges();
                    currentLoad.AddComment($"Updated by user: {currentLoad.UpdatedBy}, IP: {HttpContext.Current.Request.UserHostAddress}");

                    var DATComment = "";
					var TSComment = "";
                    var errors = new List<ResultInfo>();

					//UPDATE ASSET
					if (canUseUpdateAsset == true && !String.IsNullOrEmpty(currentLoad.AssetId))
                    {
                        var updateResult = IntegrationService.Instance.Session.UpdateAssets(currentLoad.ToUpdateAssetOperation());
						//currentLoad.AddComment($"Used UpdateAssets"); 11/10/2019

						if (updateResult != null &&
                            !updateResult.IsException
							&& updateResult.UpdateAssetResponse.updateAssetResult != null
							&& updateResult.UpdateAssetResponse.updateAssetResult.Item != null
							&& updateResult.UpdateAssetResponse.updateAssetResult.Item is UpdateAssetSuccessData
							)
						{
							DATComment = $"Updated the load in DAT";
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
								DATComment = $"Update to DAT failed, error: {error.message} | {error.detailedMessage}";
							}
						}
						else if (updateResult != null && updateResult.IsException)
						{
							DATComment = $"Update to DAT failed, details: {updateResult.Message}";
							errors.Add(ResultInfo.DATError($"Update to DAT failed, details: {updateResult.Message}"));
						}
						else
						{
							DATComment = $"Update to DAT failed, no details";
							errors.Add(ResultInfo.DATError($"Update to DAT failed, no details"));
						}
					}

					//UPLOAD TO TS
					var resultTS = IntegrationService.Instance.TruckStopUtils.UploadToTruckStop(currentLoad.ToTSLoad() , source: "AddLoadInt.Edit", db: db); //Edit loads

                    TSComment = $"Updated the load in TS";
					if (resultTS == null)
                    {
                        errors.Add(ResultInfo.TSError($"No response from TS"));
						TSComment = $"Not posted to TS, No response from TS";
					}
                    else if (resultTS.StatusSet != null && resultTS.StatusSet.Count > 0)
                    {
						foreach (var error in resultTS.StatusSet)
                        {
                            errors.Add(ResultInfo.TSError($"TS error: { error.Message }"));
                        }
						TSComment = $"Update to TS failed, details: {string.Join(", ", resultTS.StatusSet.Select(x => x.Message))}";
					}
                    //else
                    //{
                    //    var tsLoad = resultTS.Data as Load;
                    //    if (tsLoad != null && !string.IsNullOrWhiteSpace(tsLoad.LoadId))
                    //    {
                    //        model.TsLoadId = new Guid(tsLoad.LoadId);
                    //    }
                    //}

					//UPLOAD TO DAT
                    if (canUseUpdateAsset == false || String.IsNullOrEmpty(currentLoad.AssetId))
                    {
						if (!String.IsNullOrEmpty(currentLoad.AssetId))
                        {
                            var resultDAT = IntegrationService.Instance.Session.DeleteAssetsByIds(new string[] { currentLoad.AssetId }); //AddLoadInt
                            //currentLoad.AddComment($"Deleted from DAT"); //11/10/2019
                            //currentLoad.AddComment($"Recreated the load in DAT");
                            /*if (resultDAT == null || resultDAT.deleteAssetResult == null || resultDAT.deleteAssetResult.Item == null || !(resultDAT.deleteAssetResult.Item is DeleteAssetSuccessData))
                            {

                            }*/
                        }
                        var result = IntegrationService.Instance.Session.UploadDAT(new List<PostAssetOperation> { currentLoad.ToDATLoad() }); //api Edit Load
                        if (result == null || result.Response == null || result.Response.postAssetResults == null || result.Response.postAssetResults.Length != 1)
                        {
                            errors.Add(ResultInfo.DATError($"No response from DAT"));
							DATComment = $"Not posted to DAT, No response from DAT";
						}
                        else if (result.Response.postAssetResults[0].Item == null || result.Response.postAssetResults[0].Item is ServiceError)
                        {
                            var error = (result.Response.postAssetResults[0].Item as ServiceError);
                            errors.Add(ResultInfo.DATError($"DAT error: {error.message} ({error.detailedMessage})"));
							DATComment = $"Not posted to DAT, DAT error: {error.message} ({error.detailedMessage})";
						}
                        else
                        {
							DATComment = $"Recreated the load in DAT";
							var success = (result.Response.postAssetResults[0].Item as PostAssetSuccessData);
                            currentLoad.AssetId = success.assetId;
                            db.SaveChanges();
                            errors.Add(ResultInfo.DATSuccess($"Posted to DAT new id: {currentLoad.AssetId}"));
                        }
                    }
                    db.SaveChanges();

                    model = db.Loads
                            .Include(x => x.Destination)
                            .Include(x => x.Destination.State)
                            .Include(x => x.Origin)
                            .Include(x => x.Origin.State)
                            .Include(x => x.LoadType)
                            .FirstOrDefault(x => x.Id == model.Id);

					/*if (errors.Any(x => !x.IsSuccess && x.IsTS))
					{
						TSComment = $"Upload to TS failed. Sources:{string.Join(", ", errors.Select(x => x.Source))}, Messages:{string.Join(", ", errors.Select(x => x.Message))}";
					}*/

					model.AddComment(DATComment);
					model.AddComment(TSComment);

					return new
                    {
                        Ok = true,
                        model = model,
                        errors = errors,
                    };
                }
            }
        }

        public class UploadLoadModel
        {
            public LoadModel Load { get; set; }
            //public bool? DAT { get; set; }
            //public bool? TS { get; set; }
        }

        [Route("api/Loads/UploadLoad")]
        [HttpPost]
        public object UploadLoad(UploadLoadModel model)
        {
            var errors = IntegrationService.Instance.DoUploadLoad(model.Load, "UploadLoad");

            using (var db = new DBContext())
            {
                var load = db.Loads
                    .Include(x => x.Destination)
                    .Include(x => x.Destination.State)
                    .Include(x => x.Origin)
                    .Include(x => x.Origin.State)
                    .Include(x => x.LoadType)
                    .FirstOrDefault(x => x.Id == model.Load.Id);

                return new
                {
                    Ok = true,
                    model = load,
                    errors = errors,
                };
            }
        }

        public class ImportModel
        {
            public DateTime? LastModifiedDate { get; set; }
            public string Name { get; set; }
            public long? Size { get; set; }
            public HttpPostedFileBase File { get; set; }
        }

        [Route("api/Loads/Import")]
        [HttpPost]
        public async Task<object> Import()
        {
            HttpRequestMessage request = this.Request;
            if (!request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            string root = System.Web.HttpContext.Current.Server.MapPath("~/App_Data/Uploads");
            if (!System.IO.Directory.Exists(root))
            {
                System.IO.Directory.CreateDirectory(root);
            }

            var files = System.IO.Directory.GetFiles(root);
            foreach (var file in files)
            {
                var fi = new System.IO.FileInfo(file);
                if (fi.CreationTime < DateTime.Now.AddMinutes(-1))
                {
                    System.IO.File.Delete(file);
                }
            }

            var provider = new MultipartFormDataStreamProvider(root);
            var formData = await request.Content.ReadAsMultipartAsync(provider);
            var fileDateTime = DateTime.Parse(await formData.Contents[1].ReadAsStringAsync());
            var fileName = await formData.Contents[2].ReadAsStringAsync();
            var fileSize = long.Parse(await formData.Contents[3].ReadAsStringAsync());

            var messages = new List<ResultInfo>();

            using (var db = new DBContext())
            {
                var importLogModel = db.ImportLogs.FirstOrDefault(x => x.FileName.ToLower() == fileName.ToLower() && x.FileDateTime == fileDateTime && x.FileSize == fileSize);
                if (importLogModel != null)
                {
                    messages.Add(ResultInfo.Error($"That file has already been imported on {importLogModel.Created.ToString()}, cannot re-import."));
                    return new
                    {
                        Messages = messages,
                    };
                }

                importLogModel = new ImportLogModel
                {
                    FileName = fileName,
                    FileSize = fileSize,
                    FileDateTime = fileDateTime,
                    Created = DateTime.Now,
                };
                db.ImportLogs.Add(importLogModel);
                db.SaveChanges();
            }

            try
            {
                using (var stream = System.IO.File.Open(formData.FileData.FirstOrDefault().LocalFileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    using (var reader = ExcelReaderFactory.CreateOpenXmlReader(stream))
                    {
                        var ds = reader.AsDataSet();
                        if (ds != null && ds.Tables.Count != 0)
                        {
                            using (var db = new DBContext())
                            {
                                for (var i = 1; i < ds.Tables[0].Rows.Count; i++)
                                {
                                    try
                                    {
                                        var orig = IntegrationService.Instance.AddOriginDestination(db, $"{ds.Tables[0].Rows.GetCell(i, 0)} {ds.Tables[0].Rows.GetCell(i, 1)}");
                                        var dest = IntegrationService.Instance.AddOriginDestination(db, $"{ds.Tables[0].Rows.GetCell(i, 2)} {ds.Tables[0].Rows.GetCell(i, 3)}");
                                        var loadTypeId = ds.Tables[0].Rows.GetCell(i, 4);

                                        var load = new LoadModel
                                        {
                                            DestinationId = dest.Id,
                                            OriginId = orig.Id,
                                            LoadTypeId = db.LoadTypes.FirstOrDefault(x => x.Name == loadTypeId || x.IdDAT == loadTypeId || x.IdTS == loadTypeId)?.Id,
                                            AssetLength = ds.Tables[0].Rows.GetCell(i, 5).ToNullableInt(),
                                            CarrierAmount = ds.Tables[0].Rows.GetCell(i, 6).ToNullableDecimal() ?? 0,
                                            DateLoaded = ds.Tables[0].Rows.GetCell(i, 7).ToNullableDateTime(),
                                            //ClientLoadNum =             ds.Tables[0].Rows.GetCell(i, 8),
                                            ClientName = ds.Tables[0].Rows.GetCell(i, 8),
                                            //AvailabilityEarliest =      ds.Tables[0].Rows.GetCell(i, 1).ToNullableDateTime(),
                                            //AvailabilityLatest =        ds.Tables[0].Rows.GetCell(i, 2).ToNullableDateTime(),
                                            //Comments =                  ds.Tables[0].Rows.GetCell(i, 5),
                                            //Count =                     ds.Tables[0].Rows.GetCell(i, 6).ToNullableInt(),
                                            //DateDatDeleted =            ds.Tables[0].Rows.GetCell(i, 7).ToNullableDateTime(),
                                            //DateDatLoaded =             ds.Tables[0].Rows.GetCell(i, 8).ToNullableDateTime(),
                                            //DateDatRefreshed =          ds.Tables[0].Rows.GetCell(i, 9).ToNullableDateTime(),
                                            //DateRefreshed =             ds.Tables[0].Rows.GetCell(i, 11).ToNullableDateTime(),
                                            //DateRTFLoaded =             ds.Tables[0].Rows.GetCell(i, 12).ToNullableDateTime(),
                                            //DateTRTLoaded =             ds.Tables[0].Rows.GetCell(i, 13).ToNullableDateTime(),
                                            //DateTSDeleted =             ds.Tables[0].Rows.GetCell(i, 14).ToNullableDateTime(),
                                            //DimensionsHeightInches =    ds.Tables[0].Rows.GetCell(i, 19).ToNullableInt(),
                                            //DimensionsLengthFeet =      ds.Tables[0].Rows.GetCell(i, 20).ToNullableInt(),
                                            //DimensionsVolumeCubic =     ds.Tables[0].Rows.GetCell(i, 21).ToNullableInt(),
                                            //DimensionsWeightPounds =    ds.Tables[0].Rows.GetCell(i, 22).ToNullableInt(),
                                            //EmailID =                   ds.Tables[0].Rows.GetCell(i, 23),
                                            //EquipmentType =             ds.Tables[0].Rows.GetCell(i, 24),
                                            //IncludeAsset =              ds.Tables[0].Rows.GetCell(i, 25).ToNullableInt() == 1 ? true : false,
                                            //Ltl =                       ds.Tables[0].Rows.GetCell(i, 27).ToNullableInt() == 1 ? true : false,
                                            //PostersReferenceId =        ds.Tables[0].Rows.GetCell(i, 28),
                                            //PostToExtendedNetwork =     ds.Tables[0].Rows.GetCell(i, 29).ToNullableInt() == 1 ? true : false,
                                            //RateEateBasedOn =           ds.Tables[0].Rows.GetCell(i, 31).ToNullableShort() ?? 0,
                                            //RateRateMiles =             ds.Tables[0].Rows.GetCell(i, 32).ToNullableInt(),
                                            //Stops =                     ds.Tables[0].Rows.GetCell(i, 33).ToNullableInt(),
                                            //TruckStopsEnhancements =    ds.Tables[0].Rows.GetCell(i, 34),
                                            //TruckStopsPosterDisplayName = ds.Tables[0].Rows.GetCell(i, 35),
                                            CreateDate = DateTime.Now,
                                            CreatedBy = $"ImportFile {ds.Tables[0].Rows.GetCell(i, 8)}",
                                            CreateLoc = "AutoApp",
                                        };

                                        if (!db.Companies.Any(x => x.Name == load.ClientName))
                                        {
                                            db.Companies.Add(new Data.CompanyModel { Name = load.ClientName });
                                        }
                                        db.Loads.Add(load);
                                        db.SaveChanges();
                                        load.ClientLoadNum = load.Id.ToString();
                                        db.SaveChanges();

                                        var result = IntegrationService.Instance.DoUploadLoad(load, "Import");
                                        messages.AddRange(result);
                                    }
                                    catch (Exception exc)
                                    {
                                        messages.Add(ResultInfo.Error($"Row {i} can not be read {exc.Message} {(exc.InnerException == null ? "" : exc.InnerException.Message)}"));
                                    }
                                }
                                return new
                                {
                                    //Messages = messages.Where(x => x.Item1 >= 0),
                                    Messages = messages,
                                    LoadsCount = ds.Tables[0].Rows.Count - 1,
                                    LoadsImported = messages.Count(x => x.IsSuccess)
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                messages.Add(ResultInfo.Error($"File can not be read {exc.Message} {(exc.InnerException == null ? "" : exc.InnerException.Message)}"));
            }

            return new
            {
                Messages = messages,
            };
        }

        [Route("api/Loads/ImportLog")]
        [HttpPost]
        public async Task<object> ImportLog()
        {
            using (var db = new DBContext())
            {
                return new
                {
                    List = db.ImportLogs.OrderByDescending(x => x.Created).ToList(),
                };
            }
        }

        [Route("api/Loads/GetComments")]
        [HttpGet]
        public object GetComments(int id)
        {
            using (var db = new DBContext())
            {
                var load = db.Loads
                    .Include(x => x.AllComments)
                    .FirstOrDefault(x => x.Id == id);

                return new
                {
                    Comments = (load.Comments == null ? "" : $"{load.Comments}\r\n") + String.Join("\r\n", load.AllComments.OrderBy(z => z.DateTime).Select(z => $"[{z.DateTime.ToString("yyyy/MM/dd HH:mm:ss")}] {z.Comment}")),
                };
            }
        }

        [Route("api/Loads/GetCities")]
        [HttpGet]
        public object GetCities(string str)
        {
            using (var db = new DBContext())
            {
                var cities = db.OriginDestinations
                    .Where(x => x.City.Contains(str))
                    .Select(x => x.City)
                    .OrderBy(x => x).Distinct()
                    .ToList();

                return new
                {
                    Cities = cities
                };
            }
        }

		[Route("api/Loads/Recover")]
		[HttpPost]
		public object Recover(List<int> ids)
		{
			if (ids == null)
			{
				return new
				{
				};
			}
			using (var db = new DBContext())
			{
				foreach(var id in ids)
				{
					var load = db.Loads.FirstOrDefault(x => x.Id == id);
					if (load != null)
					{
						load.DateDatDeleted = null;
						load.DateTSDeleted = null;
						db.SaveChanges();
					}
				}
				return new
				{
				};
			}
		}
	}
}
