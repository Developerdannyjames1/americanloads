using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using DATService;
using System.ServiceModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using ASTDAT.Data;
using ASTDAT.Tools;

namespace CheckDATLoads
{
    class Program
    {

        static void Main(string[] args)
        {

            var db = new ASTDAT.Data.Models.DBContext();

            //var token = db.DATLogins.Where(x => x.Expiration > DateTime.Now && x.TokenPrimary != null && x.TokenSecondary != null).OrderByDescending(x => x.DateTime).FirstOrDefault();

            //var ses = new Session(@"C:\Work\AST\ASTService\ASTService\bin\Debug");
            //var ses = new Session(@"C:\inetpub\wwwroot\AST\_AST-SERVICE");

            //var loads = ses.GetLoads();
            var date = DateTime.Now.AddMinutes(-5);
            var loads = db.Loads
    .Where(x => x.ClientName == "United Rentals" && (!x.DateDatDeleted.HasValue || !x.DateTSDeleted.HasValue)) //United Rentals and not deleted from DAT or TS
    .Where(x => x.CreateDate < date) // now 20:25, compare all < 20:20 
    .ToList()
    .Where(x => x.CreatedBy.StartsWith("ParseEmails")) //CreatedBy = "ParseEmails..."
    .ToList();

        }
    }
 
    //class TSView
    //{
    //    int IntegrID = 277319;
    //    string User = "AMERSPECWS";
    //    string passw = "8wZ3mU39";
    //    public void GetView()
    //    {
    //        LoadPostingClient client = Login();
    //        LoadViewsRequest req = new LoadViewsRequest
    //        {
    //            IntegrationId = IntegrID,
    //            UserName = User,
    //            Password = passw,
    //        };
    //        //LoadViewsByLoadNumberRequest
    //        var resp = client.GetLoadViews(req);
    //        client.Close();

    //    }
    //    private LoadPostingClient Login()
    //    {
    //        var remoteAddress = new EndpointAddress("http://testws.truckstop.com/V13/Posting/LoadPosting.svc");
    //        var binding = new BasicHttpBinding(BasicHttpSecurityMode.None) { MaxReceivedMessageSize = 2 << 20 };
    //        return new LoadPostingClient(binding, remoteAddress);

    //    }

    //}
}
