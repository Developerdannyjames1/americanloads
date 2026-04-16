using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ASTDAT.Data.Models;
using ASTDAT.Tools;
using ASTDAT.Web.Infrastructure;
using Newtonsoft.Json;
using TruckStopRestfullService;
using TruckStopRestfullService.Models;

namespace TSRestFullTest
{
    class Program
    {
        static void Main(string[] args)
        {
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;


            var tsClient = new LoadPostingClient();
            var log = tsClient.Login().Result;

            //////var json1 = tsClient.GetDataAsync("loadmanagement/v2/load/searchfield").Result;
            ////var json1 = tsClient.GetDataAsync("freightmatching/v1/domaindata/transportationmodes").Result;
            ////var json1 = tsClient.GetDataAsync("freightmatching/v1/domaindata/equipmenttypes/dfm/load").Result;


            ////var json = File.ReadAllText(@"d:\Projects\AST\Json\SearchExampleResponse.json");
            var tsTool = new TruckStopRestUtils();
            TsResponse resp;

            //var respDel = tsTool.DeleteByLoadId("271e2a92-0c28-4d90-93d1-7895b5f104a0");
            //resp = tsTool.DeleteByLoadId("75c9b0b1-e531-4eb0-895a-b6fe90d7095c");

            //return;
            using (var db = new DBContext())
            {
                var load = db.Loads.Where(p => p.TsLoadId == null).OrderByDescending(o => o.Id).FirstOrDefault();
                load = db.Loads.FirstOrDefault(p => p.Id == 7727787);
                if (load != null)
                {
                    if (load.PickUpDate.HasValue && load.PickUpDate < DateTime.Today)
                    {
                        load.PickUpDate = DateTime.Today.AddDays(1);
                    }
                    //load.PickUpDate = DateTime.Today.AddDays(1);

                    if (load.DeliveryDate.HasValue && load.DeliveryDate < DateTime.Now)
                    {
                        load.DeliveryDate = DateTime.Today.AddDays(3);
                    }
                    //load.DeliveryDate = DateTime.Today.AddDays(3);

                    load.CarrierAmount = 548;
                    db.SaveChanges();
                }

                var tsLoad = load.ToTSLoad();

                //tsLoad.LoadId = Guid.NewGuid().ToString();
                var rrr = tsTool.DeleteByIds(new[] { "7727787" }); //FC6E6AEA-63F3-48C0-9EC8-C923A9983491
                var ttt = tsTool.GetLoadById(tsLoad.LoadId);
                resp = tsTool.UploadToTruckStop(tsLoad);
            }
            //var resp = tsClient.PostDataAsync("/loadmanagement/v2/load",load).Result;


            var sresp = tsTool.GetLoads();
        }
    }
}
