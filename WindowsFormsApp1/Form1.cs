using System;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;
using System.Configuration;
using System.Collections;
//using System.Threading;
using FormToMultipartPostData;

namespace Importing
{
    public partial class Form1 : Form
    {
        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }
        string fileName;
        public Configuration Settings
        {
            get { return settings; }
            set { settings = value; }
        }
        Configuration settings;
        public int Attempt = 0;
        public bool completed = false;
        public Form1()
        {
            InitializeComponent();
            SetBrowserFeatureControl();
            if (!File.Exists(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "ASTService.cfg")))
            {
                throw new Exception("File does not exist: " + Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "ASTService.cfg"));

            }
            ExeConfigurationFileMap map = new ExeConfigurationFileMap()
            {
                ExeConfigFilename = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "ASTService.cfg")
            };
            Settings = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
            //webBrowser1.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(onDocumentCompleted);
        }
        public async Task<string> Import()
        {

            var url = Settings.AppSettings.Settings["TRTUrl"].Value;
            var username = Settings.AppSettings.Settings["TRTUsername"].Value;
            var password = Settings.AppSettings.Settings["TRTPassword"].Value;

            var inUri = new Uri(url);

            webBrowser1.Navigate(inUri);
            timer1.Interval = 60 * 1000;
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
                timer1.Interval = 60 * 1000;
                while (completed == false)
                {
                    Application.DoEvents();
                    await Task.Delay(100);
                }

            }

            timer1.Interval = 60 * 1000;

            var form = webBrowser1.Document.Forms[0];
            completed = false;
            form.InvokeMember("Submit");
            while (completed == false)
            {
                Application.DoEvents();
                await Task.Delay(100);
            }
            completed = false;
            if (webBrowser1.Document.GetElementById("File") != null)
            {
                var doc = webBrowser1.Document;
                var buttons = doc.GetElementsByTagName("button");
                foreach (HtmlElement but in buttons)
                {
                    if (but.GetAttribute("type").ToLower()=="submit")
                    {
                        but.InvokeMember("Click");
                        break;
                    }
                }
            }
            timer1.Interval = 60 * 1000;
            while (completed == false)
            {
                Application.DoEvents();
                await Task.Delay(100);
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
            timer1.Interval = 120 * 1000;
            var curTick = DateTime.Now;
            while (webBrowser1.Document.GetElementById("form0")==null && DateTime.Now < curTick.AddSeconds(80))
            {
                Application.DoEvents();
                await Task.Delay(100);
            }

            if (webBrowser1.Document.GetElementById("form0")==null)
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
            timer1.Interval = 120 * 1000;
            curTick = DateTime.Now;
            while (webBrowser1.Document.GetElementById("import-result-tabs") == null && DateTime.Now < curTick.AddSeconds(80))
            {
                Application.DoEvents();
                await Task.Delay(100);
            }
            
            if (webBrowser1.Document.GetElementById("import-result-tabs") == null)
            {
                throw new Exception("Preview Page error");
            }
            while (completed == false)
            {
                Application.DoEvents();
                await Task.Delay(100);
            }
            webBrowser1.Document.GetElementById("form0").InvokeMember("Submit");
            curTick = DateTime.Now;

            while (completed == false)
            {
                Application.DoEvents();
                await Task.Delay(100);
            }
            return "Ok";

        }
        private void webBrowser1_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            completed = false;
            //MessageBox.Show("Navigating: "+e.Url);
            string url = e.Url.ToString();
            if (url.StartsWith("submit:"))
            {
                string formId = url.Substring(7);
                HtmlElement form = webBrowser1.Document.GetElementById(formId);
                if (form != null) form.RaiseEvent("onsubmit");
                e.Cancel = true;
            }
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (webBrowser1.ReadyState == WebBrowserReadyState.Complete)
            {
                completed = true;
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
        async Task<bool> WaitForComplete(int timeout = 20000)
        {
            int timeStep = 1;
            while (webBrowser1.ReadyState != WebBrowserReadyState.Complete && timeStep * 100 <= timeout)
            {
                await Task.Delay(100);
                timeStep++;
            }

            return (webBrowser1.ReadyState == WebBrowserReadyState.Complete);
        }
        async Task<bool> WaitForElement(string element, int timeout = 20000)
        {
            int timeStep = 1;
            while (webBrowser1.Document.GetElementById(element) == null && timeStep * 100 <= timeout)
            {
                await Task.Delay(100);
                timeStep++;
            }

            return (webBrowser1.Document.GetElementById(element) != null);
        }
        const int WM_GETTEXT = 0x00D;
        const int WM_SETTEXT = 0x000C;
        const int WM_CLOSE = 0x0010;

        async Task PopulateImportFile(HtmlElement file, string fleName)
        {
            this.xActivateAndBringToFront();

            //var thisHandle = FindWindow(null, "Importing data to the TRT Portal");
            //var currentForegroundWindow = GetForegroundWindow();
            //var thisWindowThreadId = GetWindowThreadProcessId(this.Handle, IntPtr.Zero);
            //var currentForegroundWindowThreadId = GetWindowThreadProcessId(currentForegroundWindow, IntPtr.Zero);
            //AttachThreadInput(currentForegroundWindowThreadId, thisWindowThreadId, true);

            //Application.DoEvents();
            //IntPtr pHandle = Process.GetCurrentProcess().MainWindowHandle;
            //ShowWindow(pHandle, 5);
            //SetForegroundWindow(pHandle);

            file.Focus();
            IntPtr h = IntPtr.Zero;
            var sendKeyTask = Task.Delay(500).ContinueWith((_) =>
            {
                //h = FindWindow("#32770", null);
                //while (h == IntPtr.Zero)
                //{
                //    Task.Delay(300);
                //    h = FindWindow("#32770", null);
                //}
                //var windowHandles = new ArrayList();
                //EnumWindowsProc callBackPtr = GetWindowHandle;
                //EnumChildWindows(h, callBackPtr, windowHandles);
                //MessageBox.Show(string.Format("{0}", h));
                //PostMessage(h, WM_SETTEXT, IntPtr.Zero, fleName);
                //PostMessage(h, WM_CLOSE, IntPtr.Zero, null);
                //PostMessage(Process.GetCurrentProcess().Handle, 0x000C, IntPtr.Zero, fleName);
                SendKeys.Send(fleName + "{ENTER}");
            }, TaskScheduler.FromCurrentSynchronizationContext());

            file.InvokeMember("Click");
            //await Task.Delay(300);
            //IntPtr h = FindWindow("#32770", null);
            //while (h==IntPtr.Zero)
            //{
            //    h = FindWindow("#32770", null);
            //    await Task.Delay(300);
            //}
            await sendKeyTask;
            //PostMessage(h, 0x000C, IntPtr.Zero, fleName);

            await Task.Delay(200);
            

            //AttachThreadInput(currentForegroundWindowThreadId, thisWindowThreadId, false);
            //this.WindowState = FormWindowState.Minimized;
        }
  

        #region Browser feature conntrol
        void SetBrowserFeatureControl()
        {
            // http://msdn.microsoft.com/en-us/library/ee330720(v=vs.85).aspx

            // FeatureControl settings are per-process
            var fleName = System.IO.Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);

            // make the control is not running inside Visual Studio Designer
            if (String.Compare(fleName, "devenv.exe", true) == 0 || String.Compare(fleName, "XDesProc.exe", true) == 0)
                return;

            SetBrowserFeatureControlKey("FEATURE_BROWSER_EMULATION", fleName, GetBrowserEmulationMode()); // Webpages containing standards-based !DOCTYPE directives are displayed in IE10 Standards mode.
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


        private async void Form1_Shown(object sender, EventArgs e)
        {
            if (Environment.GetCommandLineArgs().Count() == 0)
            {
                Close();
            }
            FileName = Environment.GetCommandLineArgs()[Environment.GetCommandLineArgs().Count()-1];
            
            textBox1.Text = FileName;
            //MessageBox.Show(FileName);
            //if (string.IsNullOrWhiteSpace(fileName))
                //this.Text = fileName;
            try
            {
                timer2.Interval = 1000;
                timer2.Enabled = true;
            }
            catch (Exception ex)
            {
                SaveResult(ex.Message);
                Close();
            }

        }
        void SaveResult(string result)
        {
            var conString = Settings.ConnectionStrings.ConnectionStrings["Default"].ConnectionString;
            var conn = new SqlConnection(conString);

            conn.Open();
            if (conn.State == ConnectionState.Open)
            {

                var impResult = (result == "Ok") ? 1 : 0;
                var errorMessage = impResult==1 ? "" : result;
                if (!string.IsNullOrWhiteSpace(errorMessage) && Attempt>0)
                {
                    errorMessage = string.Format("Attepmpt {0}: {1}", Attempt, errorMessage);
                }
                using (SqlCommand comm = new SqlCommand("", conn))
                {
                    comm.CommandText = string.Format("update [UploadLog] set Uploaded = {0}, UploadError = isnull([UploadError]+char(13),'')+nullif('{1}',''),[SendAttempts] = isnull([SendAttempts],0) + 1 where [FileName]='{2}'", impResult, errorMessage.Replace("'","''"), Path.GetFileName(FileName));
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

        private void timer1_Tick(object sender, EventArgs e)
        {
            SaveResult("The program is closed due to a lack of actions");
            this.Close();
        }

        private async void timer2_Tick(object sender, EventArgs e)
        {
            try
            {
                ((Timer)sender).Enabled = false;
                timer1.Interval = 60 * 1000;
                timer1.Enabled = true;
                if (FileName == "CheckNotSent")
                {
                    var files = GetFileList();
                    foreach (var item in files)
                    {
                        FileName = item.FileName;
                        SaveResult(await Import());
                    }
                }
                else
                {
                    SaveResult(await Import());
                }
            }
            catch (Exception ex)
            {
                SaveResult(ex.Message);
            }
            timer1.Enabled = false;

            this.Close();

        }

 
    }
    public class FileData
    {
        public string FileName { get; set; }
        public int Attempt { get; set; }
    }
    public static class Ext
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        public static void xActivateAndBringToFront(this Form form)
        {

            // activate window
            var currentForegroundWindow = GetForegroundWindow();
            var thisWindowThreadId = GetWindowThreadProcessId(form.Handle, IntPtr.Zero);
            var currentForegroundWindowThreadId = GetWindowThreadProcessId(currentForegroundWindow, IntPtr.Zero);
            AttachThreadInput(currentForegroundWindowThreadId, thisWindowThreadId, true);
            //form.Activate(); // or: 
            SetForegroundWindow(form.Handle); 
            AttachThreadInput(currentForegroundWindowThreadId, thisWindowThreadId, false);

            // set window to front
            form.TopMost = true;
            form.TopMost = false;
        }

    }
}
