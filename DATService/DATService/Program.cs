using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DATService.ServiceReference1;

namespace DATService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());
            Session session = null;
            try
            {
                session = new Session();
                if (session.IsConnected)
                {
                    session.SetOrigin("Corona", "CA");
                    session.SetDestination("Edgewood", "NY");
                    //session.BuildLoad(DateTime.Now);
                    //session.DeleteAllAssets();
                    //session.Post();

                }
            }
            catch (Exception ex)
            {
                var error = ex.Message;
            }
        }
    }
}
