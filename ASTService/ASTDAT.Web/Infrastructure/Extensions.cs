using ASTDAT.Data.Models;
using ASTDAT.Web.Infrastructure;
using DATService;
using DATService.ServiceReference1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using TruckStopRestfullService.Models;
//using TruckStopService.TruckStopServiceReference;
using Load = TruckStopRestfullService.Models.Load;
using Location = TruckStopRestfullService.Models.Location;

//using TruckStopService.TruckStopServiceReference;

namespace ASTDAT.Tools
{
    public static class Extensions
    {
        //public static TruckStopService.TruckStopServiceReference.Load ToTSLoad(this LoadModel load)
        //{
        //    var item = new TruckStopService.TruckStopServiceReference.Load
        //    {
        //        //TODO: change LoadNumber == load.Id or use RealLoadId
        //        LoadNumber = $"{load.ClientLoadNum ?? load.Id.ToString()}",

        //        DestinationCity = load.Destination.City,
        //        DestinationState = load.Destination.State.Code,

        //        OriginCity = load.Origin.City,
        //        OriginState = load.Origin.State.Code,

        //        TypeOfEquipment = load.LoadTypeId.HasValue ? load.LoadType.IdTS : "DA",

        //        Length = load.AssetLength > 0 ? (load.AssetLength ?? 0).ToString() : null,
        //        Weight = load.Weight.HasValue ? load.Weight.Value.ToString() : null,

        //        PaymentAmount = load.CarrierAmount.ToString().Replace(",", "."),
        //        SpecInfo = load.Description,
        //        IsLoadFull = load.IsLoadFull,
        //    };

        //    if (load.PickUpDate.HasValue)
        //    {
        //        //item.PickUpDate = load.PickUpDate.Value.EndOfDay();
        //        item.PickUpDate = load.PickUpDate.Value;
        //    }
        //    if (load.DeliveryDate.HasValue)
        //    {
        //        //item.DeliveryDate = load.DeliveryDate.Value.EndOfDay();
        //        item.DeliveryDate = load.DeliveryDate.Value;
        //    }

        //    return item;
        //}

        public static Load ToTSLoad(this LoadModel load)
        {
            var item = new Load
            {
                LoadId = load.TsLoadId.HasValue ? load.TsLoadId.Value.ToString() : null,
                LoadNumber = $"{load.ClientLoadNum ?? load.Id.ToString()}",
                LoadStops = new List<LoadStop>
                {
                    new LoadStop
                    {
                        Id  = Guid.NewGuid().ToString(),
                        Type =  1,
                        Sequence = 1,
                        Location = new Location()
                        {
                            City = load.Origin.City,
                            State = load.Origin.State.Code,
                        }
                    },
                    new LoadStop
                    {
                        Id  = Guid.NewGuid().ToString(),
                        Type =  2,
                        Sequence = 2,
                        Location = new Location()
                        {
                            City = load.Destination.City,
                            State = load.Destination.State.Code,
                        }

                    }
                },
                RateAttributes = new RateAttributes
                {
                    PostedAllInRate = new Rate
                    {
                        Amount = load.CarrierAmount,
                        CurrencyCode = "USD"
                    }
                },
                Note = load.Description,

                Dimensional = new Dimensional()
                {
                    Weight = load.Weight,
                    Length = load.AssetLength > 0 ? load.AssetLength : null,
                    Cube = null
                },
                EquipmentAttributes = new EquipmentAttributes
                {
                    EquipmentTypeId = load.LoadTypeId.HasValue ? (load.LoadType.TsId ?? 74) : 74,
                    TransportationModeId = load.IsLoadFull ? 1 : 2,
                },

            };

            if (load.PickUpDate.HasValue)
            {
                item.LoadStops.FirstOrDefault(p => p.Type == 1).EarlyDateTime = load.PickUpDate.Value;
            }
            if (load.DeliveryDate.HasValue)
            {
                item.LoadStops.FirstOrDefault(p => p.Type == 2).EarlyDateTime = load.DeliveryDate.Value;
            }
            var ls1 = item.LoadStops.FirstOrDefault(p => p.Type == 1);
            if (!ls1.EarlyDateTime.HasValue) ls1.EarlyDateTime = DateTime.Today;
            while (ls1.EarlyDateTime.Value.Date < DateTime.Now.ToUniversalTime().Date)
            {
                ls1.EarlyDateTime = ls1.EarlyDateTime.Value.AddDays(1);
            }
            ls1 = item.LoadStops.FirstOrDefault(p => p.Type == 2);
            if (ls1.EarlyDateTime.HasValue)
            while (ls1.EarlyDateTime.Value.Date < DateTime.Now.ToUniversalTime().Date)
            {
                ls1.EarlyDateTime = ls1.EarlyDateTime.Value.AddDays(1);
            }

            return item;
        }

        public static PostAssetOperation ToDATLoad(this LoadModel load, DateTime? pickUpDate = null, DateTime? deliveryDate = null)
        {
            //PostRequest resultDAT = null;
            EquipmentType eqType;
            var postAssetOperation = IntegrationService.Instance.Session.CreatePostAssetOperation(new RowModel
            {
                AssetType = load.LoadTypeId.HasValue ? load.LoadType.IdDAT : load.EquipmentType,
                DestinationCity = load.Destination.City,
                DestinationState = load.Destination.State.Code,
                OriginCity = load.Origin.City,
                OriginState = load.Origin.State.Code,
                AssetLength = load.AssetLength ?? 0,
                Price = load.CarrierAmount,
                DeliveryDate = load.DeliveryDate,
                PickUpStart = load.PickUpDate,                
                Instructions = load.Description,
                Weight = load.Weight.HasValue ? load.Weight.Value : 0,

            }, "", out eqType, load.PostersReferenceId);

            return postAssetOperation;
        }

        public static List<ResultInfo> ToResults(this TsResponse resultTS, string clientLoadNum)
        {
            var results = new List<ResultInfo>();

            if (resultTS != null && resultTS.StatusSet != null)
            {
                foreach (var item in resultTS.StatusSet)
                {
                    results.Add(ResultInfo.TSError($"{clientLoadNum} not imported to TS: {item.Message}"));
                }
            }
            if (resultTS != null && resultTS.Data != null)
            {
                try
                {
                    var load = JsonConvert.DeserializeObject<Load>(resultTS.Data as string);
                    if (load != null) results.Add(ResultInfo.TSSuccess($"{clientLoadNum} imported to TS, id: {load.LegacyLoadId}"));
                }
                catch
                {
                }
            }

            return results;
        }
        //public static List<ResultInfo> ToResults(this LoadPostingReturn resultTS, string clientLoadNum)
        //{
        //    var results = new List<ResultInfo>();

        //    if (resultTS != null && resultTS.Errors != null)
        //    {
        //        foreach (var item in resultTS.Errors)
        //        {
        //            results.Add(ResultInfo.TSError($"{clientLoadNum} not imported to TS: {item.ErrorMessage}"));
        //        }
        //    }
        //    if (resultTS != null && resultTS.Loads != null)
        //    {
        //        foreach (var item in resultTS.Loads)
        //        {
        //            if (item > 0)
        //            {
        //                results.Add(ResultInfo.TSSuccess($"{ clientLoadNum } imported to TS, id: { item }"));
        //            }
        //        }
        //    }

        //    return results;
        //}

        public static List<ResultInfo> ToResults(this PostRequest resultDAT, string clientLoadNum)
        {
            var results = new List<ResultInfo>();

            if (resultDAT != null && resultDAT.Response != null && resultDAT.Response.postAssetResults != null)
            {
                foreach (PostAssetResult postAssetResult in resultDAT.Response.postAssetResults)
                {
                    var postAssetSuccessData = postAssetResult.Item as PostAssetSuccessData;
                    if (postAssetSuccessData == null)
                    {
                        var serviceError = postAssetResult.Item as ServiceError;
                        results.Add(ResultInfo.DATError($"{clientLoadNum} not imported to DAT: {serviceError.message}"));
                    }
                    else
                    {
                        results.Add(ResultInfo.DATSuccess($"{ clientLoadNum } imported to DAT, id: { postAssetSuccessData.assetId }"));
                    }
                }
            }
            /*else if (IntegrationService.Instance.Session.LastException != null)
            {
                results.Add(ResultInfo.DATError($"{clientLoadNum} not imported to DAT: {IntegrationService.Instance.Session.LastException.Message}"));
            }*/
            else
            {
                results.Add(ResultInfo.DATError($"{clientLoadNum} not imported to DAT: no response"));
            }

            return results;
        }

        /*public static LoadModel ToLoadModel(this RowModel row, DBContext db, string messageID)
        {
            var org = IntegrationService.Instance.AddOriginDestination(db, row.OriginCity, row.OriginState);
            var dest = IntegrationService.Instance.AddOriginDestination(db, row.OriginCity, row.OriginState);

            var load = new LoadModel
            {
                Ltl = false,
                IncludeAsset = true,
                PostToExtendedNetwork = false,
                DimensionsWeightPounds = row.Weight,
                CarrierAmount = row.Price,
                RateEateBasedOn = 0,
                ClientName = row.Company,
                ClientLoadNum = row.Load,
                AssetLength = row.AssetLength,
                Description = row.Instructions,
                PickUpDate = row.PickUpStart,
                DeliveryDate = row.DeliveryDate,
                CreateDate = DateTime.Now,
                CreatedBy = $"ParseEmails {row.Company}",
                CreateLoc = "AutoApp",
                IsLoadFull = true,
                Count = 1,
                Stops = 0,
                OriginId = org.Id,
                DestinationId = dest.Id,
                EmailID = messageID,
                EquipmentType = row.AssetType,
            };

            return load;
        }*/

        public static bool UploadedToDAT(this LoadModel load)
        {
            return !String.IsNullOrEmpty(load.AssetId);
        }

        public static bool DeletedFromDAT(this LoadModel load)
        {
            return UploadedToDAT(load) && load.DateDatDeleted.HasValue;
        }

        public static bool CanDeleteFromDAT(this LoadModel load)
        {
            return UploadedToDAT(load) && !load.DateDatDeleted.HasValue;
        }

        public static bool UploadedToTS(this LoadModel load)
        {
            //return load.TrackStopId.HasValue && load.TrackStopId>0;
            return load.TsLoadId.HasValue; // && load.TrackStopId>0;
        }

        public static bool DeletedFromTS(this LoadModel load)
        {
            return UploadedToTS(load) && load.DateTSDeleted.HasValue;
        }

        public static bool CanDeletFromTS(this LoadModel load)
        {
            return UploadedToTS(load) && !load.DateTSDeleted.HasValue;
        }

        public static void AddComment(this LoadModel load, string comment)
        {
            if (load == null)
            {
                return;
            }
            var db = new DBContext();

            //comment = $"[{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}] {comment}";
            var userId = "";
            if (HttpContext.Current != null && HttpContext.Current.User != null && HttpContext.Current.User.Identity != null)
            {
                userId = HttpContext.Current.User.Identity.Name;
            }

            var model = new LoadCommentModel
            {
                Comment = comment,
                DateTime = DateTime.Now,
                LoadId = load.Id,
                UserId = userId,
            };
            db.LoadComments.Add(model);
            db.SaveChanges();
        }

        public static bool EqualByDate(this DateTime? val1, DateTime? val2)
        {
            if (val1 == null && val2 == null)       return true;
            if (val1 == null && val2 != null)       return false;
            if (val1 != null && val2 == null)       return false;
            if (val1.HasValue != val2.HasValue)     return false;
            if (val1.Value.Date != val2.Value.Date) return false;

            return true;
        }

		public static DateTime EndOfDay(this DateTime dt)
		{
			return dt.Date.AddDays(1).AddSeconds(-1);
		}

		public static UpdateAssetOperation ToUpdateAssetOperation(this LoadModel load)
		{
			load.Description = load.Description ?? "";
			string[] comments = null;
			try
			{
				if (load.Description.Length < 70)
				{
					comments = new string[1] { load.Description };
				}
				else if (load.Description.Length < 140)
				{
					comments = new string[2] { load.Description.Substring(0, 70), load.Description.Substring(70, load.Description.Length - 70) };
				}
				else
				{
					comments = new string[2] { load.Description.Substring(0, 70), load.Description.Substring(70, 70) };
				}
			}
			catch
			{

			}

			var updateAssetOperation = new UpdateAssetOperation
			{
				Item1 = new ShipmentUpdate
				{
					comments = comments,
					rate = new ShipmentRate
					{
						baseRateDollars = (float)load.CarrierAmount,
					},
					dimensions = !load.AssetLength.HasValue ? null : new Dimensions
					{
						lengthFeet = load.AssetLength.HasValue ? load.AssetLength.Value : 0,
						lengthFeetSpecified = load.AssetLength.HasValue,
						weightPounds = load.Weight.HasValue ? load.Weight.Value : 0,
						weightPoundsSpecified = load.Weight.HasValue,
					},
				},
				ItemElementName = ItemChoiceType.assetId,
				Item = load.AssetId,
			};

			return updateAssetOperation;
		}
	}
}
