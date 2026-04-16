using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

using ASTDAT.Data.Models;
using ASTDAT.Tools;
using DATService;
//using TruckStopService.TruckStopServiceReference;

namespace ASTDAT.Web.Infrastructure
{
   // public class TruckLoadModel
   // {
   //     public DateTime? DeliveryDate { get; set; }
   //     public DateTime? PickUpStart { get; set; }

   //     public string DestinationCountry { get; set; }
   //     public string DestinationCity { get; set; }
   //     public string DestinationState { get; set; }
   //     //public string DestinationZip { get; set; }

   //     public string OriginCountry { get; set; }
   //     public string OriginCity { get; set; }
   //     public string OriginState { get; set; }
   //     //public string OriginZip { get; set; }

   //     public string Load { get; set; }
   //     //public int Asset { get; set; }
   //     //public int AssetLength { get; set; }
   //     //public string AssetType { get; set; }
   //     //public string AssetSubType { get; set; }
   //     //public string MoveType { get; set; }
   //     //public decimal Price { get; set; }
   //     //public string AssetNumber { get; set; }
   //     //public DateTime? PickUpEnd { get; set; }
   //     public string Instructions { get; set; }
   //     //public string MoverNotes { get; set; }
   //     public int Weight { get; set; }
   //     public bool IsLoadFull { get; set; }

   //     public int RealLoadId { get; set; }
   // }

   // public class TruckStopUtils
   // {
   //     //private List<TruckLoadModel> truckLoads = new List<TruckLoadModel>();
   //     object lockObject = new object();

   //     static TruckStopUtils()
   //     {
   //         Database.SetInitializer<DBContext>(null);
   //     }

   //     public int IntegrationId { get; set; }
   //     public string UserName { get; set; }
   //     public string Password { get; set; }
   //     public string Host { get; set; }

   //     public TruckStopUtils()
   //     {
   //         try
   //         {
   //             IntegrationId = int.Parse(ConfigurationManager.AppSettings["IntegrationId"]);
   //             UserName = ConfigurationManager.AppSettings["TSServiceUser"];
   //             Password = ConfigurationManager.AppSettings["TSServicePass"];
   //             Host = ConfigurationManager.AppSettings["TSServiceURL"];
   //         }
   //         catch//(Exception exc)
   //         {

   //         }
   //     }

   //     List<Tuple<DateTime, Exception>> lastExceptions = new List<Tuple<DateTime, Exception>>();
   //     public List<Tuple<DateTime, Exception>> LastExceptions
   //     {
   //         get
   //         {
   //             var r = lastExceptions.ToList();
   //             lastExceptions.Clear();
   //             return r;
   //         }
   //     }

   //     /*public void AddToTruckStop(TruckLoadModel truckLoad)
   //     {
   //         truckLoads.Add(truckLoad);
   //     }*/

   //     public string SeekTypeOfEquipment(string instructions)
   //     {
   //         if (instructions.ToUpper().Contains("DRIVE AWAY")) return "DA";
   //         if (instructions.ToUpper().Contains("POWER ONLY")) return "PO";
   //         if (instructions.ToUpper().Contains("FLATBED")) return "F";
   //         if (instructions.ToUpper().Contains("DOUBLE-DROP")) return "DD";
   //         if (instructions.ToUpper().Contains("STEP DECK")) return "FSD";
   //         if (instructions.ToUpper().Contains("RGN")) return "RGN";

   //         return "";
   //     }

   //     private LoadPostingClient Login()
   //     {
   //         var remoteAddress = new EndpointAddress(Host);
   //         var binding = new BasicHttpBinding(BasicHttpSecurityMode.None) { MaxReceivedMessageSize = 2 << 20 };
   //         return new LoadPostingClient(binding, remoteAddress);
   //     }

   //     public LoadPostingReturn UploadToTruckStop(Load[] items = null, bool fullImport = false, string source = "", string logfile = "", int attempts = 3)
   //     {
			//if (attempts <= 0)
			//{
			//	return null;
			//}
			//lock (lockObject)
   //         {
   //             try
   //             {
   //                 LoadPostingClient client = Login();

   //                 var loadsArray = items;

   //                 var loads = new LoadPostingRequest
   //                 {
   //                     IntegrationId = IntegrationId,
   //                     UserName = UserName,
   //                     Password = Password,
   //                     Loads = loadsArray,
   //                     FullImport = fullImport,
   //                 };

   //                 Logger.Write($"UploadToTruckStop Before PostLoads: {String.Join(",", loads.Loads.Select(x => x.LoadNumber).ToList())}", fileName: logfile);
   //                 var result = client.PostLoads(loads);
   //                 Logger.Write($"UploadToTruckStop After PostLoads: result == null {result == null}", fileName: logfile);
   //                 if (result != null && result.Errors != null)
   //                 {
   //                     Logger.Write($"UploadToTruckStop result.Errors:{String.Join(",", result.Errors.Select(x => $"ErrorMessage:{x.ErrorMessage},Suggestions:{(x.Suggestions == null ? "" : String.Join(",", x.Suggestions))}").ToList())}", fileName: logfile);
   //                 }
   //                 if (result != null && result.LoadNumbers != null)
   //                 {
   //                     Logger.Write($"UploadToTruckStop result.LoadNumbers: {String.Join(",", result.LoadNumbers)}", fileName: logfile);
   //                 }
   //                 if (result != null && result.Loads != null)
   //                 {
   //                     Logger.Write($"UploadToTruckStop result.Loads: {String.Join(",", result.Loads)}", fileName: logfile);
   //                 }

   //                 using (var db = new DBContext(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
   //                 {
   //                     //var ll = db.Loads.ToList();
   //                     for (var i = 0; i < result.Loads.Length; i++)
   //                     {
   //                         var ln = result.LoadNumbers[i];
   //                         int intId;
   //                         var load = db.Loads.OrderByDescending(x => x.Id).FirstOrDefault(x => x.ClientLoadNum == ln); //Get last loads by ClientLoadNum
   //                         if (!int.TryParse(ln, out intId)) //Get load by int ID (internal id)
   //                         {
   //                             intId = -1;
   //                         }
   //                         else
   //                         {
   //                             load = db.Loads.FirstOrDefault(x => x.Id == intId);
   //                         }
   //                         //Lookup not deleted first
   //                         load = load != null ? load : db.Loads.OrderByDescending(x => x.Id).FirstOrDefault(x => x.ClientLoadNum == ln && !x.DateTSDeleted.HasValue);

   //                         if (load != null)
   //                         {
   //                             load.TrackStopId = result.Loads[i];
   //                             load.AddComment($"Uploaded to TS, TruckStopId:{load.TrackStopId},ClientLoadNum:{ln},Source:{source}");
   //                             load.DateTSDeleted = null;
   //                             load.DateLoaded = load.DateLoaded.HasValue ? load.DateLoaded : DateTime.Now;
   //                         }
   //                     }
   //                     db.SaveChanges();
   //                     if (result.Errors != null && result.Errors.Count() > 0)
   //                     {
   //                         Logger.WriteError($"result.Errors");
   //                         foreach (var error in result.Errors)
   //                         {
   //                             Logger.WriteError($"ErrorMessage:{error.ErrorMessage}, Suggestions:{String.Join(",", error.Suggestions == null ? new string[0]: error.Suggestions)}");
   //                         }
   //                     }
   //                 }

   //                 client.Close();
   //                 //truckLoads.Clear();

   //                 return result;
   //             }
   //             catch(Exception exc)
   //             {
   //                 Logger.Write("UploadToTruckStop", exc, fileName: logfile);
			//		return UploadToTruckStop(items, fullImport, source, logfile, attempts - 1);
			//	}
   //         }
   //     }

   //     public LoadPostingReturn DeleteByIds(string[] loadNumbers, int attempts = 3)
   //     {
			//if (attempts <= 0)
			//{
			//	return null;
			//}
			//lock (lockObject)
   //         {
   //             try
   //             {
   //                 LoadPostingClient client = Login();
   //                 LoadDeleteByLoadNumberRequest req = new LoadDeleteByLoadNumberRequest
   //                 {
   //                     LoadNumbers = loadNumbers,
   //                     IntegrationId = int.Parse(ConfigurationManager.AppSettings["IntegrationId"]),
   //                     UserName = ConfigurationManager.AppSettings["TSServiceUser"],
   //                     Password = ConfigurationManager.AppSettings["TSServicePass"],
   //                 };
   //                 var resp = client.DeleteLoadsByLoadNumber(req);
   //                 client.Close();

   //                 return resp;
   //             }
   //             catch(Exception exc)
   //             {
   //                 Logger.Write("DeleteByIds", exc);
			//		return DeleteByIds(loadNumbers, attempts - 1);
			//	}
   //         }
   //     }

   //     public LoadPostingListReturn GetLoads(int attempts = 3)
   //     {
			//if (attempts <= 0)
			//{
			//	return null;
			//}
   //         lock (lockObject)
   //         {
   //             try
   //             {
   //                 LoadPostingClient client = Login();
   //                 LoadListRequest loadListRequest = new LoadListRequest
   //                 {
   //                     IntegrationId = IntegrationId,
   //                     UserName = UserName,
   //                     Password = Password,
   //                 };
   //                 var resp = client.GetLoads(loadListRequest);
   //                 client.Close();

   //                 return resp;
   //             }
   //             catch (Exception exc)
   //             {
   //                 Logger.Write("GetLoads", exc);
   //                 lastExceptions.Add(new Tuple<DateTime, Exception>(DateTime.Now, exc));
			//		return GetLoads(attempts - 1);
   //             }
   //         }
   //     }
   // }

   // public static class Extenstions
   // {
   //     public static TruckLoadModel ToTruckLoadModel(this RowModel row, int loadId)
   //     {
   //         return new TruckLoadModel
   //         {
   //             DeliveryDate = row.DeliveryDate,
   //             PickUpStart = row.PickUpStart,

   //             DestinationCountry = row.Destination,
   //             DestinationCity = row.DestinationCity,
   //             DestinationState = row.DestinationState,

   //             OriginCountry = row.Origin,
   //             OriginCity = row.OriginCity,
   //             OriginState = row.OriginState,

   //             Load = row.Load,
   //             Weight = row.Weight,
   //             RealLoadId = loadId,

   //             Instructions = row.Instructions,
   //             IsLoadFull = true
   //         };
   //     }
   // }
}
