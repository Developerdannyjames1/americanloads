using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Configuration;

namespace ASTService
{
    public class ASTImport : WebBrowser
    {
        public Configuration Settings
        {
            get { return settings; }
            set { settings = value; }
        }
        Configuration settings;
        int Attempt = 0;
        string FileName;
        bool completed = false;
        public ASTImport(string fileName):base()
        {
            SetBrowserFeatureControl();
            this.ScriptErrorsSuppressed = true;
            if (!File.Exists(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "ASTService.cfg")))
            {
                throw new Exception("File does not exist: " + Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "ASTService.cfg"));
            }
            ExeConfigurationFileMap map = new ExeConfigurationFileMap()
            {
                ExeConfigFilename = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "ASTService.cfg")
            };
            Settings = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
            FileName = fileName;
            DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(onDocumentCompleted);
        }
        void onDocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            WebBrowser webBrowser1 = (WebBrowser)sender;
            if (webBrowser1.ReadyState == WebBrowserReadyState.Complete)
            {
                completed = true;
                //MessageBox.Show("Completed: "+e.Url);
            }
            HtmlDocument doc = webBrowser1.Document;
            for (int i = 0; i < doc.Forms.Count; i++)
            {
                HtmlElement form = doc.Forms[i]; // must be declared inside the loop because there's a closure
                if (form.GetAttribute("enctype").ToLower() != "multipart/form-data" || form.GetAttribute("action") != "/ControlPanel/ListingImport")
                { continue; }

                form.AttachEventHandler("onsubmit", delegate (object o, EventArgs arg)
                {
                    FormToMultipartPostData.FormToMultipartPostData postData = new FormToMultipartPostData.FormToMultipartPostData(webBrowser1, form);
                    postData.SetFile("File", FileName);
                    postData.Submit();
                });
                form.SetAttribute("hasBrowserHandler", "1"); // expose that we have a handler to JS
            }

        }
        public async void StartImport()
        {
            if (string.IsNullOrWhiteSpace(FileName))
            {
                return;
            }
            if (FileName == "CheckNotSent")
            {
                var files = GetFileList();
                foreach (var item in files)
                {
                    FileName = item.FileName;
                    Attempt = item.Attempt;
                    SaveResult(await Import());
                }
            }
            else
            {
                if (!File.Exists(FileName))
                {
                    SaveResult(string.Format("File '{0}' does not exist or access is denied",FileName));
                }
                else
                {
                    SaveResult(await Import());
                }
            }
        }
        public async Task<string> Import()
        {
            //var url = "https://usetrt.com/Account/Login";

            //var url = "https://usetrt.com/Account/Login?ReturnUrl=/ControlPanel/ListingImport";
            //var username = "valencio@rsoft.net";
            //var password = "tydL4ma2h!";

            //var url = "https://stage.usetrt.com/Account/Login?ReturnUrl=/ControlPanel/ListingImport";

            try
            {
                var url = "https://stage.usetrt.com/ControlPanel/ListingImport";
                var username = "rsoft@rsoft.net";
                var password = "lmuinyadJ!";

                var inUri = new Uri(url);
                var webBrowser1 = this;
                webBrowser1.Navigate(inUri);
                //timer1.Interval = 60 * 1000;
                while (completed == false)
                {
                    Application.DoEvents();
                    await Task.Delay(100);
                }
                if (webBrowser1.Document.GetElementById("Email") != null)
                {
                    webBrowser1.Document.GetElementById("Email").InnerText = username;
                    webBrowser1.Document.GetElementById("Password").InnerText = password;
                    webBrowser1.Document.Forms[0].InvokeMember("Submit");
                    //completed = false;
                    while (completed == false)
                    {
                        Application.DoEvents();
                        Thread.Sleep(100);
                    }

                }

                //timer1.Interval = 60 * 1000;

                var form = webBrowser1.Document.Forms[0];
                completed = false;
                form.InvokeMember("Submit");
                var curTick = DateTime.Now;
                while (completed == false && DateTime.Now < curTick.AddSeconds(20))
                {
                    Application.DoEvents();
                    Thread.Sleep(100);
                }
                completed = false;
                if (webBrowser1.Document.GetElementById("File") != null)
                {
                    var doc = webBrowser1.Document;
                    var buttons = doc.GetElementsByTagName("button");
                    foreach (HtmlElement but in buttons)
                    {
                        if (but.GetAttribute("type").ToLower() == "submit")
                        {
                            but.InvokeMember("Click");
                            break;
                        }
                    }
                }

                //timer1.Interval = 60 * 1000;
                while (completed == false && DateTime.Now < curTick.AddSeconds(20))
                {
                    Application.DoEvents();
                    Thread.Sleep(100);
                }
                if (webBrowser1.Document.GetElementById("File") != null)
                {
                    throw new Exception("Selecting file error");
                }

                if (webBrowser1.Document.GetElementById("TrailerNumber") == null)
                    throw new Exception("Mapping Page error");

                webBrowser1.Document.GetElementById("WeightPreloaded").SetAttribute("selectedIndex", "26");
                webBrowser1.Document.GetElementById("HasLiftPads").SetAttribute("selectedIndex", "27");
                webBrowser1.Document.GetElementById("FoodGradeCargoOnly").SetAttribute("selectedIndex", "29");
                webBrowser1.Document.GetElementById("TrailerNumber").SetAttribute("selectedIndex", "12");
                webBrowser1.Document.GetElementById("TrailerWeight").SetAttribute("selectedIndex", "24");
                webBrowser1.Document.GetElementById("CompanyPolicy").SetAttribute("selectedIndex", "38");

                webBrowser1.Document.Forms[0].InvokeMember("Submit");
                //timer1.Interval = 120 * 1000;
                curTick = DateTime.Now;
                while (webBrowser1.Document.GetElementById("form0") == null && DateTime.Now < curTick.AddSeconds(80))
                {
                    Application.DoEvents();
                    Thread.Sleep(100);
                }

                if (webBrowser1.Document.GetElementById("form0") == null)
                {
                    throw new Exception("Confirm mapping Page error");
                }

                var btns = webBrowser1.Document.GetElementsByTagName("button");

                foreach (HtmlElement but in btns)
                {
                    if (but.GetAttribute("type").ToLower() == "submit")
                    {
                        but.InvokeMember("Click");
                        break;
                    }
                }
                //timer1.Interval = 120 * 1000;
                curTick = DateTime.Now;
                while (webBrowser1.Document.GetElementById("import-result-tabs") == null && DateTime.Now < curTick.AddSeconds(80))
                {
                    Application.DoEvents();
                    Thread.Sleep(100);
                }

                if (webBrowser1.Document.GetElementById("import-result-tabs") == null)
                {
                    throw new Exception("Preview Page error");
                }
                while (completed == false && DateTime.Now < curTick.AddSeconds(80))
                {
                    Application.DoEvents();
                    Thread.Sleep(100);
                }
                webBrowser1.Document.GetElementById("form0").InvokeMember("Submit");
                curTick = DateTime.Now;

                while (completed == false && DateTime.Now < curTick.AddSeconds(80))
                {
                    Application.DoEvents();
                    Thread.Sleep(100);
                }
                return "Ok";
            }
            catch (Exception ex)
            {
                var err = ex.Message;
                File.AppendAllText("import.log", string.Format("{0}, {1}\n    {2}", DateTime.Now, FileName, err));
                return err;
            }
        }
        bool WaitForComplete(int timeout = 40000)
        {
            int timeStep = 1;
            while (this.ReadyState != WebBrowserReadyState.Complete) // && timeStep * 100 <= timeout)
            {
                Application.DoEvents();
                Thread.Sleep(100);
                timeStep++;
            }

            return (this.ReadyState == WebBrowserReadyState.Complete);
        }
        bool WaitForElement(string element, int timeout = 40000)
        {
            int timeStep = 1;
            while (this.ReadyState != WebBrowserReadyState.Complete || this.Document.GetElementById(element) == null) //timeStep * 100 <= timeout)
            {
                Application.DoEvents();
                Thread.Sleep(100);
                timeStep++;
            }

            return (this.Document.GetElementById(element) != null);
        }
        void SaveResult(string result)
        {
            var conString = Settings.ConnectionStrings.ConnectionStrings["Default"].ConnectionString;
            var conn = new SqlConnection(conString);

            conn.Open();
            if (conn.State == ConnectionState.Open)
            {

                var impResult = (result == "Ok") ? 1 : 0;
                var errorMessage = impResult == 1 ? "" : result;
                if (!string.IsNullOrWhiteSpace(errorMessage) && Attempt > 0)
                {
                    errorMessage = string.Format("Attepmpt {0}: {1}", Attempt, errorMessage);
                }
                using (SqlCommand comm = new SqlCommand("", conn))
                {
                    comm.CommandText = string.Format("update [UploadLog] set Uploaded = {0}, UploadError = isnull([UploadError]+char(13),'')+nullif('{1}',''),[SendAttempts] = isnull([SendAttempts],0) + 1 where [FileName]='{2}'", impResult, errorMessage, Path.GetFileName(FileName));
                    comm.ExecuteNonQuery();
                }
                conn.Close();
            }
        }
        List<FileData> GetFileList()
        {
            var retVal = new List<FileData>();
            var conString = Settings.ConnectionStrings.ConnectionStrings["Default"].ConnectionString;
            var conn = new SqlConnection(conString);
            var OutputFolder = "";
            if (Settings.AppSettings.Settings["OutputFolder"] != null)
            {
                OutputFolder = Settings.AppSettings.Settings["OutputFolder"].Value;
            }
            if (!Directory.Exists(OutputFolder))
            {
                var nPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), OutputFolder);
                if (Directory.Exists(nPath))
                {
                    OutputFolder = nPath;
                }
            }
            if (!Directory.Exists(OutputFolder))
            {
                return retVal;
            }
            conn.Open();
            DataTable tb = new DataTable();
            if (conn.State == ConnectionState.Open)
            {
                var csql = "select [FileName], max(isnull([SendAttempts],0)) [SendAttempts] from [UploadLog] where isnull([FileName],'')<>'' and isnull([Uploaded],0) = 0 and isnull([SendAttempts],0)<3 group by [FileName]";
                using (SqlCommand comm = new SqlCommand(csql, conn))
                {
                    using (SqlDataAdapter da = new SqlDataAdapter(comm))
                    {
                        da.Fill(tb);
                    }
                }
            }
            conn.Close();
            foreach (DataRow item in tb.Rows)
            {
                var flName = Path.Combine(OutputFolder, item[0].ToString());
                if (File.Exists(flName))
                {
                    retVal.Add(new FileData() { FileName = flName, Attempt = (int)item[1] + 2 });
                }
                else
                {
                    FileName = item[0].ToString();
                    Attempt = (int)item[1] + 2;
                    SaveResult("File does not exist");
                }
            }

            return retVal;
        }

        #region Browser feature conntrol
        void SetBrowserFeatureControl()
        {
            // http://msdn.microsoft.com/en-us/library/ee330720(v=vs.85).aspx

            // FeatureControl settings are per-process
            var fileName = System.IO.Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);

            // make the control is not running inside Visual Studio Designer
            if (String.Compare(fileName, "devenv.exe", true) == 0 || String.Compare(fileName, "XDesProc.exe", true) == 0)
                return;

            SetBrowserFeatureControlKey("FEATURE_BROWSER_EMULATION", fileName, GetBrowserEmulationMode()); // Webpages containing standards-based !DOCTYPE directives are displayed in IE10 Standards mode.
        }

        void SetBrowserFeatureControlKey(string feature, string appName, uint value)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(
                String.Concat(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\", feature),
                RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
                key.SetValue(appName, (UInt32)value, RegistryValueKind.DWord);
            }
        }

        UInt32 GetBrowserEmulationMode()
        {
            int browserVersion = 7;
            using (var ieKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer",
                RegistryKeyPermissionCheck.ReadSubTree,
                System.Security.AccessControl.RegistryRights.QueryValues))
            {
                var version = ieKey.GetValue("svcVersion");
                if (null == version)
                {
                    version = ieKey.GetValue("Version");
                    if (null == version)
                        throw new ApplicationException("Microsoft Internet Explorer is required!");
                }
                int.TryParse(version.ToString().Split('.')[0], out browserVersion);
            }

            UInt32 mode = 10000; // Internet Explorer 10. Webpages containing standards-based !DOCTYPE directives are displayed in IE10 Standards mode. Default value for Internet Explorer 10.
            switch (browserVersion)
            {
                case 7:
                    mode = 7000; // Webpages containing standards-based !DOCTYPE directives are displayed in IE7 Standards mode. Default value for applications hosting the WebBrowser Control.
                    break;
                case 8:
                    mode = 8000; // Webpages containing standards-based !DOCTYPE directives are displayed in IE8 mode. Default value for Internet Explorer 8
                    break;
                case 9:
                    mode = 9000; // Internet Explorer 9. Webpages containing standards-based !DOCTYPE directives are displayed in IE9 mode. Default value for Internet Explorer 9.
                    break;
                default:
                    // use IE10 mode by default
                    break;
            }

            return mode;
        }
        #endregion
    }
    public class FileData
    {
        public string FileName { get; set; }
        public int Attempt { get; set; }
    }

}
