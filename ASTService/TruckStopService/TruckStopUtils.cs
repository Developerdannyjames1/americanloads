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
using TruckStopService.TruckStopServiceReference;

namespace TruckStopService
{
    public class TruckLoadModel
    {
        public DateTime? DeliveryDate { get; set; }
        public DateTime? PickUpStart { get; set; }

        public string DestinationCountry { get; set; }
        public string DestinationCity { get; set; }
        public string DestinationState { get; set; }
        //public string DestinationZip { get; set; }

        public string OriginCountry { get; set; }
        public string OriginCity { get; set; }
        public string OriginState { get; set; }
        //public string OriginZip { get; set; }

        public string Load { get; set; }
        //public int Asset { get; set; }
        //public int AssetLength { get; set; }
        //public string AssetType { get; set; }
        //public string AssetSubType { get; set; }
        //public string MoveType { get; set; }
        //public decimal Price { get; set; }
        //public string AssetNumber { get; set; }
        //public DateTime? PickUpEnd { get; set; }
        public string Instructions { get; set; }
        //public string MoverNotes { get; set; }
        public int Weight { get; set; }
        public bool IsLoadFull { get; set; }
        //public string Company { get; set; }
    }

    public class TruckStopUtils
    {
        private List<TruckLoadModel> truckLoads = new List<TruckLoadModel>();

        static TruckStopUtils()
        {
            Database.SetInitializer<DBContext>(null);
        }

        Configuration settings;
        public TruckStopUtils(Configuration settings)
        {
            this.settings = settings;
        }

        public TruckStopUtils(string configPath)
        {
            if (!File.Exists(Path.Combine(configPath, "ASTService.cfg")))
            {
                throw new Exception("File does not exist: " + Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "ASTService.cfg"));
            }
            ExeConfigurationFileMap map = new ExeConfigurationFileMap()
            {
                ExeConfigFilename = Path.Combine(configPath, "ASTService.cfg")
            };
            settings = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
        }

        public void AddToTruckStop(TruckLoadModel truckLoad)
        {
            truckLoads.Add(truckLoad);
        }

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

        private LoadPostingClient Login()
        {
            var remoteAddress = new EndpointAddress(settings.AppSettings.Settings["TSServiceURL"].Value);
            var binding = new BasicHttpBinding(BasicHttpSecurityMode.None) { MaxReceivedMessageSize = 2 << 20 };
            return new LoadPostingClient(binding, remoteAddress);
        }

        public void UploadToTruckStop()
        {
            LoadPostingClient client = Login();

            var loadsArray = truckLoads.Select(x => new Load
            {
                PickUpDate = x.PickUpStart.HasValue && x.PickUpStart.Value > DateTime.Now ? x.PickUpStart.Value : DateTime.Now.AddDays(1),
                DeliveryDate = x.DeliveryDate.HasValue && x.DeliveryDate.Value > DateTime.Now ? x.DeliveryDate.Value : DateTime.Now.AddDays(2),

                LoadNumber = x.Load,

                DestinationCountry = x.DestinationCountry ?? "USA",
                DestinationCity = x.DestinationCity,
                DestinationState = x.DestinationState,

                OriginCountry = x.OriginCountry ?? "USA",
                OriginCity = x.OriginCity,
                OriginState = x.OriginState,

                Weight = x.Weight.ToString(),

                TypeOfEquipment = SeekTypeOfEquipment(x.Instructions),
            })
            .Where(x => x.TypeOfEquipment != "")
            .ToArray();

            var loads = new LoadPostingRequest
            {
                IntegrationId = int.Parse(settings.AppSettings.Settings["IntegrationId"].Value),
                UserName = settings.AppSettings.Settings["TSServiceUser"].Value,
                Password = settings.AppSettings.Settings["TSServicePass"].Value,
                Loads = loadsArray,
                FullImport = true,
            };

            var result = client.PostLoads(loads);

            using (var db = new DBContext(settings.ConnectionStrings.ConnectionStrings["Default"].ConnectionString))
            {
                var ll = db.Loads.ToList();
                for (var i = 0; i < result.Loads.Length; i++) {
                    var ln = result.LoadNumbers[i];
                    var load = db.Loads.FirstOrDefault(x => x.ClientLoadNum == ln);
                    if (load != null)
                    {
                        load.TrackStopId = result.Loads[i];
                        load.DateLoaded = load.DateLoaded.HasValue ? load.DateLoaded : DateTime.Now;
                    }
                }
                db.SaveChanges();
            }

            client.Close();
        }

        public LoadPostingReturn DeleteByIds(string[] loadNumbers)
        {
            LoadPostingClient client = Login();
            LoadDeleteByLoadNumberRequest req = new LoadDeleteByLoadNumberRequest
            {
                LoadNumbers = loadNumbers,
                IntegrationId = int.Parse(settings.AppSettings.Settings["IntegrationId"].Value),
                UserName = settings.AppSettings.Settings["TSServiceUser"].Value,
                Password = settings.AppSettings.Settings["TSServicePass"].Value,
            };
            var resp = client.DeleteLoadsByLoadNumber(req);
            return resp;
        }
    }
}
