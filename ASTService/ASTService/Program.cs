using System;
using System.Globalization;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Configuration;
using System.IO;
using System.Data.Entity;

namespace ASTService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            if (Environment.UserInteractive)
            {
                var file = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Log.txt";
                File.WriteAllText(file, "");

                //MailChecker.DEBUG_Email_Limit = 300;

                var service1 = new ASTService();
                service1.TestStartupAndStop(args);
            }
            else
            {
                ServiceBase[] ServicesToRun;

                var srv = new ASTService();

                ServicesToRun = new ServiceBase[]
                {
                srv
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
