using System;
using System.Runtime.InteropServices;

namespace FormsFeed.WinForms
{
    public class Utils
    {
        [StructLayout (LayoutKind.Sequential)]
        private struct _SHELLEXECUTEINFO
        {
            public uint cbSize;
            public uint fMask;
            public IntPtr hwnd;
            public string lpVerb;
            public string lpFile;
            public string lpParameters;
            public string lpDirectories;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIconOrMonitor;
            public IntPtr hProcess;
        }

        [Flags]
        private enum SEE_MASK
        {
            DEFAULT = 0x0,
            CLASSNAME = 0x1,
            CLASSKEY = 0x3,
            IDLIST = 0x4,
            INVOKEIDLIST = 0xc,
            ICON = 0x10,
            HOTKEY = 0x20,
            NOCLOSEPROCESS = 0x40,
            CONNECTNETDRV = 0x80,
            NOASYNC = 0x100,
            DDEWAIT = NOASYNC,
            DOENVSUBST = 0x200,
            NO_UI = 0x400,
            UNICODE = 0x4000,
            NO_CONSOLE = 0x8000,
            ASYNCOK = 0x100000,
            HMONITOR = 0x200000,
            NOZONECHECKS = 0x800000,
            NOQUERYCLASSSTORE = 0x1000000,
            WAITFORINPUTIDLE = 0x2000000,
            LOG_USAGE = 0x4000000
        }

        [DllImport("shell32.dll", SetLastError=true)]
        private static extern bool ShellExecuteEx([MarshalAs(UnmanagedType.Struct)] ref _SHELLEXECUTEINFO info);

        public static void OpenInDefaultBrowser(string uri)
        {
            _SHELLEXECUTEINFO info = new _SHELLEXECUTEINFO();
            info.cbSize = (uint)Marshal.SizeOf(typeof(_SHELLEXECUTEINFO));
            info.fMask = (uint)(SEE_MASK.CLASSNAME);
            info.lpClass = "http";
            info.lpFile = uri;
            info.nShow = 10;
            ShellExecuteEx(ref info);
        }
    }
}
