using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ASTService
{
    class ShellAdd
    {
        private string watchPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public int RunApplication(string command, string arguments = "")
        {
            IntPtr dwSessionId = IntPtr.Zero; // Win32.WTSGetActiveConsoleSessionId();
            IntPtr winlogonPid = IntPtr.Zero;
            //bool isRDP = Environment.GetEnvironmentVariable("SESSIONNAME").StartsWith("RDP-");
            //File.AppendAllText(logFile, string.Format("  Session : {0} \r\n", Environment.GetEnvironmentVariable("SESSIONNAME")));

            List<Win32.WTS_SESSION_INFO> wtsSessionInfos = Win32.ListSessions();
            var activeSession = wtsSessionInfos.Where(x => x.State == Win32.ConnectionState.Active).ToList();
            if (activeSession.Count != 1)
            {
                //if (activeSession.Count == 0)
                //{
                //   // File.AppendAllText(logFile, string.Format("  {0} :  No active session detected\r\n", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss t")));
                //}
                //else
                //{
                //    //File.AppendAllText(logFile, string.Format("  {0} :  Can not continue, more than one active session detected\r\n", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss t")));
                //    foreach (var item in wtsSessionInfos.Where(x => x.State == Win32.ConnectionState.Active))
                //    {
                //        File.AppendAllText(logFile, string.Format("     Current active Session : {0} named {1} \r\n", item.SessionID, item.WinStationName));

                //    }
                //}
                return -1;
            }
            dwSessionId = (IntPtr)activeSession[0].SessionID;
            bool isRDP = activeSession[0].WinStationName.Contains("RDP-");
            Process[] processes = Process.GetProcessesByName("explorer");

            List<int> sessionList = new List<int> { };
            foreach (Process p in processes)
            {
                if (sessionList.Where(o => o == p.SessionId).ToList().Count == 0)
                {
                    sessionList.Add(p.SessionId);
                }
                if (!isRDP && (IntPtr)p.SessionId == dwSessionId || isRDP && (IntPtr)p.SessionId == dwSessionId)
                {
                    winlogonPid = (IntPtr)p.Id;
                }
            }

            if (winlogonPid == IntPtr.Zero)
            {
                //File.AppendAllText(logFile, string.Format("  {0} :  Win32API error: can not get Process ID\r\n",
                //    DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss t")));
                return -1;
            }


            IntPtr hProcess = Win32.OpenProcess(Win32.MAXIMUM_ALLOWED, false, winlogonPid);
            if (hProcess == IntPtr.Zero)
            {
                //File.AppendAllText(logFile, string.Format("  {0} :  Win32API error: can not open Process\r\n",
                //    DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss t")));

            }

            IntPtr hPToken = IntPtr.Zero;
            if (!Win32.OpenProcessToken(hProcess, Win32.TOKEN_DUPLICATE | Win32.TOKEN_QUERY | Win32.TOKEN_IMPERSONATE  // TOKEN_ADJUST_PRIVILEGES
                , ref hPToken))
            {
                Win32.CloseHandle(hProcess);
                //File.AppendAllText(logFile, string.Format("  {0} :  Win32API error: can not get a Token\r\n",
                //    DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss t")));
                return -1;
            }
            Win32.SECURITY_ATTRIBUTES sa = new Win32.SECURITY_ATTRIBUTES();
            //sa.bInheritHandle = true;
            sa.Length = Marshal.SizeOf(sa);
            IntPtr hUserTokenDup = IntPtr.Zero;
            if (!Win32.DuplicateTokenEx(hPToken, Win32.MAXIMUM_ALLOWED, ref sa,
                (int)Win32.SECURITY_IMPERSONATION_LEVEL.SecurityIdentification,
                (int)Win32.TOKEN_TYPE.TokenPrimary, ref hUserTokenDup))
            {
                Win32.CloseHandle(hProcess);
                Win32.CloseHandle(hPToken);
                //File.AppendAllText(logFile, string.Format("  {0} :  Win32API error: can not duplicate a Token\r\n",
                //    DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss t")));
                return -1;
            }
            Win32.STARTUPINFO si = new Win32.STARTUPINFO();
            //si.dwFlags = Win32.STARTF_USESHOWWINDOW;
            //si.wShowWindow = Win32.SW_NORMAL;
            //si.lpDesktop =  @"winsta0\default";
            si.cb = Marshal.SizeOf(si);

            Win32.PROCESS_INFORMATION procInfo = new Win32.PROCESS_INFORMATION();

            int dwCreationFlags = Win32.NORMAL_PRIORITY_CLASS | Win32.CREATE_NEW_CONSOLE | Win32.MAXIMUM_ALLOWED;
            IntPtr envBlock = IntPtr.Zero;
            if (Win32.CreateEnvironmentBlock(ref envBlock, hUserTokenDup, true))
            {
                dwCreationFlags |= Win32.CREATE_UNICODE_ENVIRONMENT;
            }
            else
            {
                envBlock = IntPtr.Zero;
                //File.AppendAllText(logFile, string.Format("  {1} :  CreateEnvironmentBlock error {0}\r\n", Win32.GetLastError(), DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss t")));
            }
            bool result = Win32.CreateProcessAsUser(hUserTokenDup,  // client's access token
                                command,          // file to execute
                                arguments,        // command line
                                ref sa,           // pointer to process SECURITY_ATTRIBUTES
                                ref sa,           // pointer to thread SECURITY_ATTRIBUTES
                                false,            // handles are not inheritable
                                dwCreationFlags,  // creation flags
                                envBlock,         // pointer to new environment block 
                                watchPath,        // name of current directory 
                                ref si,           // pointer to STARTUPINFO structure
                                ref procInfo      // receives information about new process
                                );
            int retID = -1; 
            if (result)
            {
                retID = procInfo.dwProcessID;
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                //File.AppendAllText(logFile, string.Format("  {1} :  CreateProcessAsUser Error: {0}\r\n", error, DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss t")));
            }

            if (hProcess != IntPtr.Zero)
                Win32.CloseHandle(hProcess);
            if (hPToken != IntPtr.Zero)
                Win32.CloseHandle(hPToken);

            if (procInfo.hProcess != IntPtr.Zero)
                Win32.CloseHandle(procInfo.hProcess);
            if (procInfo.hThread != IntPtr.Zero)
                Win32.CloseHandle(procInfo.hThread);

            if (hUserTokenDup != IntPtr.Zero)
                Win32.CloseHandle(hUserTokenDup);

            if (envBlock != IntPtr.Zero)
                Win32.DestroyEnvironmentBlock(envBlock);
            return retID;
        }

        private string GetApplication(string fileName)
        {
            var extension = Path.GetExtension(fileName);

            //if (extension.ToLower()==".exe" || extension.ToLower() == ".cmd" || extension.ToLower() == ".bat")
            //{
            //    return fileName;
            //}
            Win32.AssocStr association = Win32.AssocStr.Executable;
            IntPtr length = IntPtr.Zero;
            if (!Win32.AssocQueryString(Win32.AssocF.None, association, extension, null, null, ref length))
            {
                return "";
            }
            if (length == IntPtr.Zero)
            {
                return "";
            }
            var sb = new StringBuilder((int)length);
            if (Win32.AssocQueryString(Win32.AssocF.None, association, extension, null, sb, ref length))
            {
                return "";
            }

            return sb.ToString();
        }
    }
    public partial class ParamList
    {
        public string File { get; set; }
        public string Run { get; set; }
        public string Path { get; set; }
        public List<string> Parameters { get; set; }
        public int Index { get; set; }
    }
    public class Win32
    {
        public static int CREATE_NEW_CONSOLE = 0x00000010;
        public static int CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        public static int NORMAL_PRIORITY_CLASS = 0x00000020;
        public static int MAXIMUM_ALLOWED = (0x02000000);
        public static int ACCESS_SYSTEM_SECURITY = (0x01000000);

        public static int TOKEN_ASSIGN_PRIMARY = (0x0001);
        public static int TOKEN_DUPLICATE = (0x0002);
        public static int TOKEN_IMPERSONATE = (0x0004);
        public static int TOKEN_QUERY = (0x0008);
        public static int TOKEN_QUERY_SOURCE = (0x0010);
        public static int TOKEN_ADJUST_PRIVILEGES = (0x0020);
        public static int TOKEN_ADJUST_GROUPS = (0x0040);
        public static int TOKEN_ADJUST_DEFAULT = (0x0080);
        public static int TOKEN_ADJUST_SESSIONID = (0x0100);

        public static uint GENERIC_READ = (0x80000000);
        public static uint GENERIC_WRITE = (0x40000000);
        public static uint GENERIC_EXECUTE = (0x20000000);
        public static uint GENERIC_ALL = (0x10000000);

        // CreateProcesss flags
        public static int STARTF_USESHOWWINDOW = (0x00000001);
        public static int STARTF_USESIZE = (0x00000002);
        public static int STARTF_USEPOSITION = (0x00000004);
        public static int STARTF_USECOUNTCHARS = (0x00000008);
        public static int STARTF_USEFILLATTRIBUTE = (0x00000010);
        public static int STARTF_RUNFULLSCREEN = (0x00000020);
        public static int STARTF_FORCEONFEEDBACK = (0x00000040);
        public static int STARTF_FORCEOFFFEEDBACK = (0x00000080);
        public static int STARTF_USESTDHANDLES = (0x00000100);
        public static int STARTF_USEHOTKEY = (0x00000200);

        public static Int16 SW_HIDE             = 0;
        public static Int16 SW_SHOWNORMAL       = 1;
        public static Int16 SW_NORMAL           = 1;
        public static Int16 SW_SHOWMINIMIZED    = 2;
        public static Int16 SW_SHOWMAXIMIZED    = 3;
        public static Int16 SW_MAXIMIZE         = 3;
        public static Int16 SW_SHOWNOACTIVATE   = 4;
        public static Int16 SW_SHOW             = 5;
        public static Int16 SW_MINIMIZE         = 6;
        public static Int16 SW_SHOWMINNOACTIVE  = 7;
        public static Int16 SW_SHOWNA           = 8;
        public static Int16 SW_RESTORE          = 9;
        public static Int16 SW_SHOWDEFAULT      = 10;
        public static Int16 SW_FORCEMINIMIZE    = 11;
        public static Int16 SW_MAX              = 11;


        [DllImport("Kernel32.dll")]
        public static extern int GetLastError();

        [DllImport("Kernel32.dll")]
        public static extern IntPtr WTSGetActiveConsoleSessionId();
        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern int WTSQueryUserToken(UInt32 sessionId, out IntPtr Token);
        [DllImport("wtsapi32.dll")]
        public static extern void WTSFreeMemory(IntPtr memory);
        [DllImport("Kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwAccess, bool isInherit, IntPtr dwProcessID);

        [DllImport("Kernel32.dll")]
        public static extern int CloseHandle(IntPtr pHandle);

        [DllImport("Advapi32.dll")]
        public static extern bool OpenProcessToken(IntPtr pHandle, int DesiredAccess, ref IntPtr tHandle);

        [DllImport("advapi32.dll")]
        public static extern bool DuplicateTokenEx(IntPtr hExistingToken, Int32 dwDesiredAccess,
                        ref SECURITY_ATTRIBUTES lpThreadAttributes,
                        Int32 ImpersonationLevel, Int32 dwTokenType,
                        ref IntPtr phNewToken);

        [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUser", SetLastError = true,
               CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern bool CreateProcessAsUser(IntPtr hToken, string lpApplicationName, string lpCommandLine,
                                ref SECURITY_ATTRIBUTES lpProcessAttributes, ref SECURITY_ATTRIBUTES lpThreadAttributes,
                                bool bInheritHandle, Int32 dwCreationFlags, IntPtr lpEnvrionment,
                                string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo,
                                ref PROCESS_INFORMATION lpProcessInformation);

        [DllImport("Userenv.dll")]
        public static extern bool CreateEnvironmentBlock(ref IntPtr lpEnvrionment, IntPtr hToken, bool bInheritHandle);

        [DllImport("Userenv.dll")]
        public static extern bool DestroyEnvironmentBlock(IntPtr lpEnvrionment);

        [DllImport("Shlwapi.dll")]
        public static extern bool AssocQueryString(
            AssocF flags,
            AssocStr str,
            string pszAssoc,
            string pszExtra,
            [Out] StringBuilder pszOut,
            ref IntPtr pcchOut
        );
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern IntPtr OpenInputDesktop(IntPtr dwFlags, bool fInherit, uint dwDesiredAccess);

        private static IntPtr GetCurrentUserToken()
        {
            List<WTS_SESSION_INFO> wtsSessionInfos = ListSessions();
            int sessionId = wtsSessionInfos.Where(x => x.State == ConnectionState.Active).FirstOrDefault().SessionID;
            //int sessionId = GetCurrentSessionId();

            Debug.WriteLine(string.Format("sessionId: {0}", sessionId));
            if (sessionId == int.MaxValue)
            {
                return IntPtr.Zero;
            }
            else
            {
                IntPtr p = new IntPtr();
                int result = WTSQueryUserToken((UInt32)sessionId, out p);
                Debug.WriteLine(string.Format("WTSQueryUserToken result: {0}", result));
                Debug.WriteLine(string.Format("WTSQueryUserToken p: {0}", p));

                return p;
            }
        }

        public static List<WTS_SESSION_INFO> ListSessions()
        {
            IntPtr server = IntPtr.Zero;
            List<WTS_SESSION_INFO> ret = new List<WTS_SESSION_INFO>();

            try
            {
                IntPtr ppSessionInfo = IntPtr.Zero;

                Int32 count = 0;
                Int32 retval = WTSEnumerateSessions(IntPtr.Zero, 0, 1, ref ppSessionInfo, ref count);
                Int32 dataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));

                Int64 current = (int)ppSessionInfo;

                if (retval != 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        WTS_SESSION_INFO si = (WTS_SESSION_INFO)Marshal.PtrToStructure((System.IntPtr)current, typeof(WTS_SESSION_INFO));
                        current += dataSize;

                        ret.Add(si);
                    }

                    WTSFreeMemory(ppSessionInfo);
                }
            }
            catch (Exception exception)
            {
                //Debug.WriteLine(exception.ToString());
            }

            return ret;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct WTS_SESSION_INFO
        {
            public int SessionID;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string WinStationName;
            public ConnectionState State;
        }

        [DllImport("wtsapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Int32 WTSEnumerateSessions(IntPtr hServer, int reserved, int version,
                                                        ref IntPtr sessionInfo, ref int count);
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public Int32 dwProcessID;
            public Int32 dwThreadID;
        }

        public struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        public struct SECURITY_ATTRIBUTES
        {
            public Int32 Length;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        public enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }

        public enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation
        }

        [Flags]
        public enum AssocF
        {
            None = 0,
            Init_NoRemapCLSID = 0x1,
            Init_ByExeName = 0x2,
            Open_ByExeName = 0x2,
            Init_DefaultToStar = 0x4,
            Init_DefaultToFolder = 0x8,
            NoUserSettings = 0x10,
            NoTruncate = 0x20,
            Verify = 0x40,
            RemapRunDll = 0x80,
            NoFixUps = 0x100,
            IgnoreBaseClass = 0x200,
            Init_IgnoreUnknown = 0x400,
            Init_Fixed_ProgId = 0x800,
            Is_Protocol = 0x1000,
            Init_For_File = 0x2000
        }

        public enum AssocStr
        {
            Command = 1,
            Executable,
            FriendlyDocName,
            FriendlyAppName,
            NoOpen,
            ShellNewValue,
            DDECommand,
            DDEIfExec,
            DDEApplication,
            DDETopic,
            InfoTip,
            QuickTip,
            TileInfo,
            ContentType,
            DefaultIcon,
            ShellExtension,
            DropTarget,
            DelegateExecute,
            Supported_Uri_Protocols,
            ProgID,
            AppID,
            AppPublisher,
            AppIconReference,
            Max
        }
        /// <summary>
        /// Connection state of a session.
        /// </summary>
        public enum ConnectionState
        {
            /// <summary>
            /// A user is logged on to the session.
            /// </summary>
            Active,
            /// <summary>
            /// A client is connected to the session.
            /// </summary>
            Connected,
            /// <summary>
            /// The session is in the process of connecting to a client.
            /// </summary>
            ConnectQuery,
            /// <summary>
            /// This session is shadowing another session.
            /// </summary>
            Shadowing,
            /// <summary>
            /// The session is active, but the client has disconnected from it.
            /// </summary>
            Disconnected,
            /// <summary>
            /// The session is waiting for a client to connect.
            /// </summary>
            Idle,
            /// <summary>
            /// The session is listening for connections.
            /// </summary>
            Listening,
            /// <summary>
            /// The session is being reset.
            /// </summary>
            Reset,
            /// <summary>
            /// The session is down due to an error.
            /// </summary>
            Down,
            /// <summary>
            /// The session is initializing.
            /// </summary>
            Initializing
        }

    }
}
