using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using ClosedXML.Excel;
using DATService;
//using TruckStopService;
using System.Net.Http;
using System.Net;
using ASTDAT.Tools;

namespace ASTService
{
    public partial class ASTService : ServiceBase
    {
        public static Configuration Settings
        {
            get { return settings; }
            set { settings = value; }
        }
        static Configuration settings;
        public List<ThreadModel> ImportList
        {
            get { return importList; }
        }
        public List<ThreadModel> importList = new List<ThreadModel>();

        public int IntervalCheckEmail { get; set; }
        public int IntervalRefreshLoads { get; set; }
        public int IntervalRefreshDAT { get; set; }

        public string TemplateXLS = "";
        public string OutputFolder = "";
        System.Timers.Timer myTimer = null;
        public int CurrentID = 0;

        public bool IsImportProcess = false;
        string FileToImport = "";
        bool noImport
        {
            get { return Environment.MachineName.ToLower() == "workroom"; }
        }

        internal void TestStartupAndStop(string[] args)
        {
            Console.WriteLine("OnStart() Begin");
            this.OnStart(args);
            Console.WriteLine("OnStart() Finish, Press Any Key");
            Console.ReadLine();
            this.OnStop();
        }

        public ASTService()
        {
            IntervalCheckEmail = 60;
            IntervalRefreshLoads = 60 * 60;
            IntervalRefreshDAT = 60 * 5;
            //IntervalRefreshDAT = 60 * 2;

            InitializeComponent();
            this.CanStop = true;
            this.CanPauseAndContinue = true;

            //Setup logging
            this.AutoLog = false;

            ((ISupportInitialize)this.EventLog).BeginInit();
            if (!EventLog.SourceExists(this.ServiceName))
            {
                EventLog.CreateEventSource(this.ServiceName, "Application");
            }
            ((ISupportInitialize)this.EventLog).EndInit();

            this.EventLog.Source = this.ServiceName;
            this.EventLog.Log = "Application";
        }

        void GetSettings()
        {
            if (!File.Exists(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "ASTService.cfg")))
            {
                this.EventLog.WriteEntry("File does not exist: " + Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "ASTService.cfg"), EventLogEntryType.Error);

            }
            ExeConfigurationFileMap map = new ExeConfigurationFileMap()
            {
                ExeConfigFilename = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "ASTService.cfg")
            };
            Settings = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);

            if (Settings.AppSettings.Settings["MailCheckInterval"] != null)
            {
                int interval = 0;
                if (Int32.TryParse(Settings.AppSettings.Settings["MailCheckInterval"].Value, out interval))
                {
                    IntervalCheckEmail = interval;
                    Console.WriteLine($"IntervalCheckEmail: {IntervalCheckEmail}");
                }
            }
            if (Settings.AppSettings.Settings["LoadsRefreshInterval"] != null)
            {
                int interval = 0;
                if (Int32.TryParse(Settings.AppSettings.Settings["LoadsRefreshInterval"].Value, out interval))
                {
                    IntervalRefreshLoads = interval;
                    Console.WriteLine($"IntervalRefreshLoads: {IntervalRefreshLoads}");
                }
            }
            if (Settings.AppSettings.Settings["IntervalRefreshDAT"] != null)
            {
                int interval = 0;
                if (Int32.TryParse(Settings.AppSettings.Settings["IntervalRefreshDAT"].Value, out interval))
                {
                    IntervalRefreshDAT = interval;
                    Console.WriteLine($"IntervalRefreshDAT: {IntervalRefreshDAT}");
                }
            }
            //
            if (Settings.AppSettings.Settings["TemplateFile"] != null)
            {
                TemplateXLS = Settings.AppSettings.Settings["TemplateFile"].Value;
            }
            if (Settings.AppSettings.Settings["OutputFolder"] != null)
            {
                OutputFolder = Settings.AppSettings.Settings["OutputFolder"].Value;
            }

            if (!Directory.Exists(OutputFolder))
            {
                var nPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), OutputFolder);

                if (!Directory.Exists(nPath))
                {
                    Directory.CreateDirectory(nPath);
                }
                if (Directory.Exists(nPath))
                {
                    OutputFolder = nPath;
                }

            }
            if (!Directory.Exists(OutputFolder))
            {
                this.EventLog.WriteEntry("Output folder does not exist and can not be created: " + OutputFolder, EventLogEntryType.Error);
                OutputFolder = "";
            }
            if (!File.Exists(TemplateXLS))
            {
                if (File.Exists(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), TemplateXLS)))
                {
                    TemplateXLS = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), TemplateXLS);
                }
                else
                {
                    this.EventLog.WriteEntry("Template file does not exist: " + TemplateXLS, EventLogEntryType.Error);
                    TemplateXLS = "";
                }
            }
        }

        //Session session = null;
        //TruckStopUtils truckStopUtils = null;

        protected override void OnStart(string[] args)
        {
            Logger.Enabled = true;
            Logger.Write("Started");
            Logger.Write($"{Logger.Folder}\\{Logger.FileName}");

            try
            {
                GetSettings();
                this.EventLog.WriteEntry("Service Started");

                myTimer = new System.Timers.Timer(1000);
                myTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimedEvent);
                myTimer.Interval = 1 * 1000; //every 1 second
                myTimer.Enabled = true;
            }
            catch (Exception exc)
            {
                Logger.Write("OnStart.Exception");
                Logger.Write("OnStart.Exception", exc);
            }
        }

        protected override void OnStop()
        {
            myTimer.Enabled = false;
            this.EventLog.WriteEntry("Service Stopped");
        }

        public void TestOnStartAndOnStop(string[] args)
        {
            OnStart(args);
            //System.Threading.Thread.Sleep(10000);
            //FileToImport = @"d:\Projects\AST\ASTService\ASTService\bin\Debug\Out\Insight180926053253.xlsx";
            //StartImport();
            OnStop();
        }

        private bool IsStopedFlag { get; set; }
        private bool StopedFlagNotFound { get; set; }
		private bool DATPostersReferenceIdUpdateRunned { get; set; } = false;
		private bool DATLoginCheckRunned { get; set; } = false;

        private void OnTimedEvent(object source, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                var now = DateTime.Now;
                if (now.Second % 5 != 0)
                {
                    return;
                }

				//At about 12:02 AM, run a process that will check DAT login
				var hour = 0; var minute = 2;
				if (now.Hour == hour && now.Minute == minute && !DATLoginCheckRunned)
				{
                    DATLoginCheckRunned = true;
					DATLoginCheck();
				}
				//At about 12:16 AM, run a process that will report all the DAT loads with the following logic
				hour = 0; minute = 16;
				if (now.Hour == hour && now.Minute == minute && !DATPostersReferenceIdUpdateRunned)
				{
                    DATLoginCheckRunned = false;
                    DATPostersReferenceIdUpdateRunned = true;
					DATPostersReferenceIdUpdate();
				}
				if (now.Hour == 1 && DATPostersReferenceIdUpdateRunned) //clear flag
				{
                    DATLoginCheckRunned = false;
                    DATPostersReferenceIdUpdateRunned = false;
				}

				if (now.Hour < 4 || now.Hour > 19)
                {
                    if (!Environment.UserInteractive)
                    {
                        return;
                    }
                }

                if (Settings.AppSettings.Settings["WebAppFolder"] != null && !String.IsNullOrEmpty(Settings.AppSettings.Settings["WebAppFolder"].Value)) //Flag set in config
                {
                    if (IsStopedFlag) //Is stopped
                    {
                        if (!System.IO.File.Exists(Settings.AppSettings.Settings["WebAppFolder"].Value + "ServicePause.flg")) //File was removed
                        {
                            IsStopedFlag = false;
                            Logger.Write($"{Settings.AppSettings.Settings["WebAppFolder"].Value + "ServicePause.flg"} file NOT found: Turned on the service");
                        }
                    }
                    else
                    {
                        if (System.IO.File.Exists(Settings.AppSettings.Settings["WebAppFolder"].Value + "ServicePause.flg")) //File was added
                        {
                            IsStopedFlag = true;
                            Logger.Write($"{Settings.AppSettings.Settings["WebAppFolder"].Value + "ServicePause.flg"} file found: Turned off the service");
                        }
                    }
                    if (IsStopedFlag)
                    {
                        return;
                    }
                }
                else
                {
                    GetSettings();
                    if (!StopedFlagNotFound)
                    {
                        StopedFlagNotFound = true;
                        Logger.Write($"Flag folder not specified: Turned off the service. ");
                    }
                    return;
                }

                ((System.Timers.Timer)source).Enabled = false;

                var seconds = (((now.Hour * 60) + now.Minute) * 60) + now.Second;

                if (IntervalCheckEmail>0 && seconds % IntervalCheckEmail == 0)
                {
                    Console.WriteLine($"OnTimedEvent.CheckMail [{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}]");
                    try
                    {
                        Logger.Write("CheckMail");
                        CheckMail();
                    }
                    catch (Exception exc)
                    {
                        Logger.Write("EXC.CheckMail", exc);
                    }
                }

                if (seconds % IntervalRefreshLoads == 0) //87000 - never start
                {
                    Console.WriteLine($"OnTimedEvent.RefreshLoads [{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}]");
                    try
                    {
                        Logger.Write("RefreshDATLoads");
                        RefreshDATLoads();
                    }
                    catch (Exception exc)
                    {
                        Logger.Write("EXC.RefreshDATLoads", exc);
                    }
                    try
                    {
                        Logger.Write("RefreshTSLoads");
                        RefreshTSLoads();
                    }
                    catch (Exception exc)
                    {
                        Logger.Write("EXC.RefreshTSLoads", exc);
                    }
                }

                if (seconds % IntervalRefreshDAT == 0)
                {
                    Console.WriteLine($"OnTimedEvent.RefreshDAT [{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}]");
                    try
                    {
                        Logger.Write("RefreshDAT");
                        RefreshDAT();
                    }
                    catch (Exception exc)
                    {
                        Logger.Write("EXC.RefreshDAT", exc);
                    }
                }
                if (seconds % IntervalRefreshDAT == 0)
                {
                    Console.WriteLine($"OnTimedEvent.RefreshTS [{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}]");
                    try
                    {
                        Logger.Write("RefreshTS");
                        RefreshTS();
                    }
                    catch (Exception exc)
                    {
                        Logger.Write("EXC.RefreshTS", exc);
                    }
                }

                ((System.Timers.Timer)source).Enabled = true;
            }
            catch(Exception exc)
            {
                Logger.Write("EXC.Timer", exc);
            }
        }

        void RefreshDATLoads()
        {
            var wr = HttpWebRequest.Create($"{settings.AppSettings.Settings["WebSiteUrl"].Value}/Integration/DATLoadState");
            var resp = wr.GetResponse();
        }

        void RefreshTSLoads()
        {
            var wr = HttpWebRequest.Create($"{settings.AppSettings.Settings["WebSiteUrl"].Value}/Integration/TSLoadState");
            var resp = wr.GetResponse();
        }

        void RefreshDAT()
        {
            var wr = HttpWebRequest.Create($"{settings.AppSettings.Settings["WebSiteUrl"].Value}/Integration/RefreshDAT");
            var resp = wr.GetResponse();
        }

        void DATPostersReferenceIdUpdate()
        {
            var wr = HttpWebRequest.Create($"{settings.AppSettings.Settings["WebSiteUrl"].Value}/Integration/DATPostersReferenceIdUpdate");
            var resp = wr.GetResponse();
        }

        void DATLoginCheck()
        {
            var wr = HttpWebRequest.Create($"{settings.AppSettings.Settings["WebSiteUrl"].Value}/Integration/GetDATLoadsCount");
            var resp = wr.GetResponse();
        }

        void RefreshTS()
        {
            var wr = HttpWebRequest.Create($"{settings.AppSettings.Settings["WebSiteUrl"].Value}/Integration/RefreshTS");
            var resp = wr.GetResponse();
        }

        void CheckMail()
        {
            var wr = HttpWebRequest.Create($"{settings.AppSettings.Settings["WebSiteUrl"].Value}/Integration/EmailParse");
            var resp = wr.GetResponse();
        }

        void Import()
        {
            if (string.IsNullOrWhiteSpace(FileToImport))
            {
                return;
            }
            using (var browser = new ASTImport(FileToImport))
            {
                FileToImport = "";
                browser.StartImport();
            }
        }

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hwnd, int cmd);
        private void StartImport()
        {
            return;
            if (noImport) return;
            //Process pr = Process.Start(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "importing.exe"), FileToImport);
            // Thread thr = new Thread(delegate ()
            //{
            //    Import();
            //});
            // thr.SetApartmentState(ApartmentState.STA);
            // thr.Start();

            // ImportList.Add(new ThreadModel(){ Thread = thr, Started = DateTime.Now });
            var shell = new ShellAdd();
            var processID = shell.RunApplication(null, Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "importing.exe") + " \"" + FileToImport + "\"");
            Process process = Process.GetProcessById(processID);
            process.EnableRaisingEvents = true;
            process.Exited += Process_Exited;
            IsImportProcess = true;
            while (IsImportProcess)
            {
                Thread.Sleep(500);
            }
            process.Dispose();
            //File.AppendAllText("import.log", string.Format("{0}, {1}\n    {2}", DateTime.Now, FileToImport, process.Handle));
            //process.EnableRaisingEvents = true;
            //process.Exited += Process_Exited;

            //process.StartInfo = new ProcessStartInfo(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "importing.exe"));
            //process.StartInfo.Arguments = FileToImport;
            ////if (!string.IsNullOrWhiteSpace(lParam.Path))
            ////{
            ////    process.StartInfo.WorkingDirectory = lParam.Path;
            ////}
            //process.StartInfo.CreateNoWindow = false;
            //process.StartInfo.ErrorDialog = false;
            //process.StartInfo.RedirectStandardError = false;
            //process.StartInfo.RedirectStandardInput = false;
            //process.StartInfo.RedirectStandardOutput = false;
            //process.StartInfo.UseShellExecute = false;
            //process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            //bool res = process.Start();
            //if (!res)
            //{
            //    EventLog.WriteEntry("Importing does not started", EventLogEntryType.Error);
            //}
        }
        public void Process_Exited (object sender, EventArgs eventArgs)
        {
            IsImportProcess = false;
            //Process process = (Process)sender;
            //File.AppendAllText(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "import.log"), 
            //    string.Format("{0}, {1}\n    {2}", DateTime.Now, FileToImport, process.Handle));

        }
    }
    public class ThreadModel
    {
        public Thread Thread{ get; set; }
        public DateTime Started{ get; set; }
    }

    public class FinalModel
    {
        public string Load { get; set; }
        public int isConverted { get; set; }
        public int isUploaded { get; set; }
        public string FileName { get; set; }
        public string ConvertError { get; set; }
    }
    public enum EventLogType
    {
        Start,
        MailError,
        ParseError,
        UploadError,
        SQLError,
        SystemError,
        FinishConvert,
        FinishUpload
    }
    public enum Customers
    {
        Unknown,
        Insight
    }
}
