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
using Newtonsoft.Json;
using TruckStopRestfullService.Models;
using Load = TruckStopRestfullService.Models.Load;
using LoadPostingClient = TruckStopRestfullService.LoadPostingClient;
//using LoadPostingReturn = TruckStopRestfullService.LoadPostingReturn;

//using TruckStopService.TruckStopServiceReference;

namespace ASTDAT.Web.Infrastructure
{
 
    public class TruckStopRestUtils
    {
        //private List<TruckLoadModel> truckLoads = new List<TruckLoadModel>();
        object lockObject = new object();

        static TruckStopRestUtils()
        {
            Database.SetInitializer<DBContext>(null);
        }

        private LoadPostingClient client { get; set; }

        public TruckStopRestUtils()
        {
            try
            {
                client = new LoadPostingClient();
            }
            catch//(Exception exc)
            {

            }
        }

        List<Tuple<DateTime, Exception>> lastExceptions = new List<Tuple<DateTime, Exception>>();
        public List<Tuple<DateTime, Exception>> LastExceptions
        {
            get
            {
                var r = lastExceptions.ToList();
                lastExceptions.Clear();
                return r;
            }
        }

        /*public void AddToTruckStop(TruckLoadModel truckLoad)
        {
            truckLoads.Add(truckLoad);
        }*/

        public string SeekTypeOfEquipment(string instructions)
        {
            if (instructions.ToUpper().Contains("DRIVE AWAY")) return "DA";
            if (instructions.ToUpper().Contains("POWER ONLY")) return "PO";
            if (instructions.ToUpper().Contains("FLATBED")) return "F";
            if (instructions.ToUpper().Contains("DOUBLE-DROP")) return "DD";
            if (instructions.ToUpper().Contains("STEP DECK")) return "FSD";
            if (instructions.ToUpper().Contains("RGN")) return "RGN";

            return "";
        }

        //private LoadPostingClient Login()
        //{
        //    var remoteAddress = new EndpointAddress(Host);
        //    var binding = new BasicHttpBinding(BasicHttpSecurityMode.None) { MaxReceivedMessageSize = 2 << 20 };
        //    return new LoadPostingClient(binding, remoteAddress);
        //}

        public TsResponse UploadToTruckStop(Load tsload = null, bool fullImport = false, string source = "", string logfile = "", int attempts = 3, DBContext db = null)
        {
            if (tsload == null || attempts <= 0)
            {
                return null;
            }
            //lock (lockObject) 
            //{
                var method = string.IsNullOrWhiteSpace(tsload.LoadId) ? "UploadToTruckStop" : "UpdateInTruckStop";
                try
                {
                    if (!client.Login().Result)
                    {
                        Logger.Write($"{method}: login error", fileName: logfile);
                        return new TsResponse
                        {
                            Success = false,
                            StatusSet = new List<StatusSet>
                            {
                                new StatusSet
                                {
                                    Code = -1,
                                    Message = "Login error"
                                }
                            }
                        };
                    }
                    //LoadPostingClient client = Login();

                    Logger.Write($"{method} Before PostLoads: {tsload.LoadNumber}", fileName: logfile);

                    string json;
                    var errorMessage = "";
                    if (string.IsNullOrWhiteSpace(tsload.LoadId))
                    {
                        json = client.PostDataAsync("/loadmanagement/v2/load", tsload).Result;
                        errorMessage = client.LastError;
                    }
                    else
                    {
                        json = client.PutDataAsync($"/loadmanagement/v2/load/{tsload.LoadId}", tsload).Result;
                        errorMessage = client.LastError;
                        //if (!string.IsNullOrWhiteSpace(errorMessage) && errorMessage.ToLower().Contains("not found"))
                        //{
                        //    json = client.PostDataAsync("/loadmanagement/v2/load", tsload).Result;
                        //    errorMessage = client.LastError;
                        //} 
                        //else 
                        if (!string.IsNullOrWhiteSpace(errorMessage)) // && client.LastError.ToLower().Contains("updating load not allowed"))
                        {
                            var tsr = GetLoadById(tsload.LoadId);
                            if (tsr.Success && tsr.Data is List<Load> tsrList && tsrList.Count == 0) // load does not exist in the TS
                            {
                                json = client.PostDataAsync("/loadmanagement/v2/load", tsload).Result;
                                errorMessage = client.LastError;
                            }
                        }
                    }
                        

                    if (!string.IsNullOrWhiteSpace(errorMessage))
                    {
                        Logger.Write($"{method} result.Errors:{errorMessage}", fileName: logfile);
                        if (json.StartsWith("[") || json.StartsWith("{["))
                        {
                            var respObj = JsonConvert.DeserializeObject<List<TsResponse>>(json);
                            return respObj[0];
                        }
                        return JsonConvert.DeserializeObject<TsResponse>(json);
                    }

                    var respLoad = JsonConvert.DeserializeObject<Load>(json);
                    if (db == null) db = new DBContext(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);

                    var ln = respLoad.LoadNumber;
                    int intId;
                    var load = db.Loads.OrderByDescending(x => x.Id).FirstOrDefault(x => x.ClientLoadNum == ln); //Get last loads by ClientLoadNum
                    if (!int.TryParse(ln, out intId)) //Get load by int ID (internal id)
                    {
                        intId = -1;
                    }
                    else
                    {
                        load = db.Loads.FirstOrDefault(x => x.Id == intId);
                    }
                    //Lookup not deleted first
                    load = load != null ? load : db.Loads.OrderByDescending(x => x.Id).FirstOrDefault(x => x.ClientLoadNum == ln && !x.DateTSDeleted.HasValue);

                    if (load != null)
                    {
                        load.TsLoadId = new Guid(respLoad.LoadId);
                        load.TrackStopId = respLoad.LegacyLoadId;
                        load.AddComment($"Uploaded to TS, TruckStopLoadId:{load.TsLoadId},ClientLoadNum:{ln},Source:{source}");
                        load.DateTSDeleted = null;
                        load.DateLoaded = load.DateLoaded.HasValue ? load.DateLoaded : DateTime.Now;
                    }

                    db.SaveChanges();

                    //truckLoads.Clear();

                    return new TsResponse
                    {
                        Success = true,
                        Data = respLoad
                    };
                }
                catch (Exception exc)
                {
                    Logger.Write(method, exc, fileName: logfile);
                    return UploadToTruckStop(tsload, fullImport, source, logfile, attempts - 1);
                }
            //}
        }
       public TsResponse UpdateInTruckStop(Load tsload = null, bool fullImport = false, string source = "", string logfile = "", int attempts = 3)
        {
            if (tsload == null || attempts <= 0)
            {
                return null;
            }
            //lock (lockObject)
            //{
                try
                {
                    if (!client.Login().Result)
                    {
                        Logger.Write("UpdateInTruckStop: login error", fileName: logfile);
                        return new TsResponse
                        {
                            Success = false,
                            StatusSet = new List<StatusSet>
                            {
                                new StatusSet
                                {
                                    Code = -1,
                                    Message = "Login error"
                                }
                            }
                        };
                    }
                    //LoadPostingClient client = Login();
                    Logger.Write($"UpdateInTruckStop Before Put Load: {tsload.LoadNumber}", fileName: logfile);

                    var json = client.PutDataAsync("/loadmanagement/v2/load", tsload).Result;

                    if (!string.IsNullOrWhiteSpace(client.LastError))
                    {
                        Logger.Write($"UpdateInTruckStop result.Errors:{client.LastError}", fileName: logfile);
                        return JsonConvert.DeserializeObject<TsResponse>(json);
                    }

                    var respLoad = JsonConvert.DeserializeObject<Load>(json);
                    using (var db = new DBContext(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                    {
                        var ln = respLoad.LoadNumber;
                        int intId;
                        var load = db.Loads.OrderByDescending(x => x.Id).FirstOrDefault(x => x.ClientLoadNum == ln); //Get last loads by ClientLoadNum
                        if (!int.TryParse(ln, out intId)) //Get load by int ID (internal id)
                        {
                            intId = -1;
                        }
                        else
                        {
                            load = db.Loads.FirstOrDefault(x => x.Id == intId);
                        }
                        //Lookup not deleted first
                        load = load != null ? load : db.Loads.OrderByDescending(x => x.Id).FirstOrDefault(x => x.ClientLoadNum == ln && !x.DateTSDeleted.HasValue);

                        if (load != null)
                        {
                            load.TsLoadId = new Guid(respLoad.LoadId);
                            load.TrackStopId = respLoad.LegacyLoadId;
                            load.AddComment($"Uploaded to TS, TruckStopLoadId:{load.TsLoadId},ClientLoadNum:{ln},Source:{source}");
                            load.DateTSDeleted = null;
                            load.DateLoaded = load.DateLoaded.HasValue ? load.DateLoaded : DateTime.Now;
                        }

                        db.SaveChanges();
                    }

                    //truckLoads.Clear();

                    return new TsResponse
                    {
                        Success = true,
                        Data = respLoad
                    };
                }
                catch (Exception exc)
                {
                    Logger.Write("UploadToTruckStop", exc, fileName: logfile);
                    return UploadToTruckStop(tsload, fullImport, source, logfile, attempts - 1);
                }
            //}
        }

        public TsResponse DeleteByIds(string[] loadNumbers, int attempts = 3)
        {
            if (attempts <= 0)
            {
                return null;
            }
            //lock (lockObject)
            //{
                try
                {
                    if (!client.Login().Result)
                    {
                        Logger.Write("TruckStop, DeleteById : login error");
                        return new TsResponse
                        {
                            Success = false,
                            StatusSet = new List<StatusSet>
                            {
                                new StatusSet
                                {
                                    Code = -1,
                                    Message = "Login error"
                                }
                            }
                        };
                    }

                    using (var db = new DBContext(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                    {
                        var deletedList = new List<string>();
                        var errorList = new List<string>();

                        foreach (var loadNumber in loadNumbers)
                        {
                            var ln = loadNumber;
                            int intId;
                            var load = db.Loads.OrderByDescending(x => x.Id)
                                .FirstOrDefault(x => x.ClientLoadNum == ln); //Get last loads by ClientLoadNum
                            if (!int.TryParse(ln, out intId)) //Get load by int ID (internal id)
                            {
                                intId = -1;
                            }
                            else
                            {
                                load = db.Loads.FirstOrDefault(x => x.Id == intId);
                            }

                            //Lookup not deleted first
                            load = load != null
                                ? load
                                : db.Loads.OrderByDescending(x => x.Id).FirstOrDefault(x =>
                                    x.ClientLoadNum == ln && !x.DateTSDeleted.HasValue);

                            if (load != null)
                            {
                                string loadId = "";
                                if (load.TsLoadId.HasValue)
                                {
                                    loadId = load.TsLoadId.Value.ToString();
                                }
                                else
                                {
                                    var respJson = client.GetLoadByNumber(ln);
                                    var loads = JsonConvert.DeserializeObject<SearchResponse>(respJson);
                                    if (loads.Loads.Count > 0)
                                    {
                                        loadId = loads.Loads[0].LoadId;
                                    }
                                }

                                if (!string.IsNullOrWhiteSpace(loadId) && client.DeleteLoadAsync(loadId).Result)
                                {
                                    deletedList.Add(loadNumber);
                                }
                                else
                                {
                                    if (string.IsNullOrWhiteSpace(loadId))
                                    {
                                        errorList.Add($"Load with number {loadNumber} not found");
                                    }
                                    else 
                                    {
                                        errorList.Add($"Delete error: {client.LastError}");
                                    }
                                }
                            }
                        }

                        if (errorList.Count == 0)
                        {
                            return new TsResponse
                            {
                                Success = true,
                                Data = deletedList
                            };

                        }
                        return new TsResponse
                        {
                            Success = deletedList.Count > 0,
                            Data = deletedList,
                            StatusSet = errorList.Select(s=> 
                                new StatusSet
                                {
                                    Message = s
                                }).ToList(),
                        };

                    }

                }
                catch (Exception exc)
                {
                    Logger.Write("DeleteByIds", exc);
                    return DeleteByIds(loadNumbers, attempts - 1);
                }
            //}
        }
       public TsResponse DeleteByLoadId(string loadId)
        {
            try
            {
                if (!client.Login().Result)
                {
                    Logger.Write("TruckStop, DeleteById : login error");
                    return new TsResponse
                    {
                        Success = false,
                        StatusSet = new List<StatusSet>
                        {
                            new StatusSet
                            {
                                Code = -1,
                                Message = "Login error"
                            }
                        }
                    };
                }
                var deletedList = new List<string>();
                var errorList = new List<string>();


                if (!string.IsNullOrWhiteSpace(loadId) && client.DeleteLoadAsync(loadId).Result)
                {
                    deletedList.Add(loadId);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(loadId))
                    {
                        errorList.Add($"Load with Id {loadId} not found");
                    }
                    else 
                    {
                        errorList.Add($"Delete error: {client.LastError}");
                    }
                }

                if (errorList.Count == 0)
                {
                    return new TsResponse
                    {
                        Success = true,
                        Data = deletedList
                    };

                }
                return new TsResponse
                {
                    Success = deletedList.Count > 0,
                    Data = deletedList,
                    StatusSet = errorList.Select(s=> 
                        new StatusSet
                        {
                            Message = s
                        }).ToList(),
                };

            }
            catch (Exception exc)
            {
                Logger.Write("DeleteByIds", exc);
                return new TsResponse
                {
                    Success = false,
                    StatusSet = new List<StatusSet>
                    {
                        new StatusSet
                        {
                            Code = -1,
                            Message = exc.Message
                        }
                    }
                };
            }
        }

       public TsResponse GetLoadById(string loadId)
       {
            try
            {
                if (!client.Login().Result)
                {
                    Logger.Write("GetLoads from TruckStop: login error");
                    return new TsResponse
                    {
                        Success = false,
                        StatusSet = new List<StatusSet>
                            {
                                new StatusSet
                                {
                                    Code = -1,
                                    Message = "Login error"
                                }
                            }
                    };
                }

                var respJson = client.GetLoadById(loadId);
                if (string.IsNullOrWhiteSpace(client.LastError) && !string.IsNullOrWhiteSpace(respJson))
                {
                    var loads = JsonConvert.DeserializeObject<SearchResponse>(respJson);
                    return new TsResponse
                    {
                        Success = true,
                        Data = loads.Loads
                    };
                }
                if (!string.IsNullOrWhiteSpace(respJson))
                {
                    return JsonConvert.DeserializeObject<TsResponse>(respJson);
                }
                return new TsResponse
                {
                    Success = false,
                    StatusSet = new List<StatusSet>
                        {
                            new StatusSet
                            {
                                Code = -1,
                                Message = client.LastError
                            }
                        }
                };

            }
            catch (Exception exc)
            {
                Logger.Write("GetLoadById", exc);
                lastExceptions.Add(new Tuple<DateTime, Exception>(DateTime.Now, exc));
                return new TsResponse
                {
                    Success = false,
                    StatusSet = new List<StatusSet>
                    {
                        new StatusSet
                        {
                            Code = -1,
                            Message = exc.Message
                        }
                    }
                };
            }
        }
        public TsResponse GetLoads(int attempts = 3)
        {
            if (attempts <= 0)
            {
                return null;
            }
            //lock (lockObject)
            //{
                try
                {
                    if (!client.Login().Result)
                    {
                        Logger.Write("GetLoads from TruckStop: login error");
                        return new TsResponse
                        {
                            Success = false,
                            StatusSet = new List<StatusSet>
                            {
                                new StatusSet
                                {
                                    Code = -1,
                                    Message = "Login error"
                                }
                            }
                        };
                    }

                    var loadList = new List<Load>();
                    var cond = true;
                    var page = 1;
                    string respJson = null;
                    while (cond)
                    {
                        respJson = client.GetLoads(page);
                        if (string.IsNullOrWhiteSpace(client.LastError) && !string.IsNullOrWhiteSpace(respJson))
                        {
                            var loads = JsonConvert.DeserializeObject<SearchResponse>(respJson);
                            loadList.AddRange(loads.Loads);
                            if (loads.Pagination.TotalPages > page)
                            {
                                page++;
                            }
                            else
                            {
                                return new TsResponse
                                {
                                    Success = true,
                                    Data = loadList
                                };
                            }
                        }
                        else
                        {
                            cond = false;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(respJson))
                    {
                        return JsonConvert.DeserializeObject<TsResponse>(respJson);
                    }
                    return new TsResponse
                    {
                        Success = false,
                        StatusSet = new List<StatusSet>
                        {
                            new StatusSet
                            {
                                Code = -1,
                                Message = client.LastError
                            }
                        }
                    };

                }
                catch (Exception exc)
                {
                    Logger.Write("GetLoads", exc);
                    lastExceptions.Add(new Tuple<DateTime, Exception>(DateTime.Now, exc));
                    return GetLoads(attempts - 1);
                }
            //}
        }
    }

}
