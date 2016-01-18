using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using DirectShowLib;
using System.ComponentModel;

namespace babgvant.EVRPlay
{
    class EVRPlayListener : IDisposable
    {
        #region Enum

        private enum ClassStyle : uint
        {
            VREDRAW = 0x0001,
            HREDRAW = 0x0002,
            DBLCLKS = 0x0008,
            OWNDC = 0x0020,
            CLASSDC = 0x0040,
            PARENTDC = 0x0080,
            NOCLOSE = 0x0200,
            SAVEBITS = 0x0800,
            BYTEALIGNCLIENT = 0x1000,
            BYTEALIGNWINDOW = 0x2000,
            GLOBALCLASS = 0x4000
        }

        private enum WindowStyle : uint
        {
            OVERLAPPED = 0x00000000,
            POPUP = 0x80000000,
            CHILD = 0x40000000,
            MINIMIZE = 0x20000000,
            VISIBLE = 0x10000000,
            DISABLED = 0x08000000,
            CLIPSIBLINGS = 0x04000000,
            CLIPCHILDREN = 0x02000000,
            MAXIMIZE = 0x01000000,
            CAPTION = 0x00C00000,    /* WS_BORDER | WS_DLGFRAME  */
            BORDER = 0x00800000,
            DLGFRAME = 0x00400000,
            VSCROLL = 0x00200000,
            HSCROLL = 0x00100000,
            SYSMENU = 0x00080000,
            THICKFRAME = 0x00040000,
            GROUP = 0x00020000,
            TABSTOP = 0x00010000,
            MINIMIZEBOX = 0x00020000,
            MAXIMIZEBOX = 0x00010000,
            TILED = OVERLAPPED,
            ICONIC = MINIMIZE,
            SIZEBOX = THICKFRAME,
            TILEDWINDOW = OVERLAPPEDWINDOW,
            OVERLAPPEDWINDOW = OVERLAPPED | CAPTION |
                                         SYSMENU |
                                         THICKFRAME |
                                         MINIMIZEBOX |
                                         MAXIMIZEBOX,

            POPUPWINDOW = POPUP |
                                         BORDER |
                                         SYSMENU,
            CHILDWINDOW = CHILD,
            EX_DLGMODALFRAME = 0x00000001,
            EX_NOPARENTNOTIFY = 0x00000004,
            EX_TOPMOST = 0x00000008,
            EX_ACCEPTFILES = 0x00000010,
            EX_TRANSPARENT = 0x00000020,
            EX_MDICHILD = 0x00000040,
            EX_TOOLWINDOW = 0x00000080,
            EX_WINDOWEDGE = 0x00000100,
            EX_CLIENTEDGE = 0x00000200,
            EX_CONTEXTHELP = 0x00000400,
            EX_RIGHT = 0x00001000,
            EX_LEFT = 0x00000000,
            EX_RTLREADING = 0x00002000,
            EX_LTRREADING = 0x00000000,
            EX_LEFTSCROLLBAR = 0x00004000,
            EX_RIGHTSCROLLBAR = 0x00000000,
            EX_CONTROLPARENT = 0x00010000,
            EX_STATICEDGE = 0x00020000,
            EX_APPWINDOW = 0x00040000,
            EX_OVERLAPPEDWINDOW = (EX_WINDOWEDGE | EX_CLIENTEDGE),
            EX_PALETTEWINDOW = (EX_WINDOWEDGE | EX_TOOLWINDOW | EX_TOPMOST),
            EX_LAYERED = 0x00080000,
            EX_NOINHERITLAYOUT = 0x00100000, // Disable inheritence of mirroring by children
            EX_LAYOUTRTL = 0x00400000, // Right to left mirroring
            EX_COMPOSITED = 0x02000000,
            EX_NOACTIVATE = 0x08000000
        }

        #endregion

        #region Struct

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct WNDCLASS
        {
            public ClassStyle style;
            public WndProc lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszMenuName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszClassName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct WNDCLASSEX
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cbSize;
            [MarshalAs(UnmanagedType.U4)]
            public ClassStyle style;
            public WndProc lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        #endregion

        #region pinvoke

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern ushort RegisterClass([In] ref WNDCLASS lpWndClass);

        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        extern static ushort RegisterClassEx([MarshalAs(UnmanagedType.Struct)]ref WNDCLASSEX wndClassEx);

        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        extern static bool UnregisterClass(string lpClassName, IntPtr hInstance);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool GetClassInfoEx(IntPtr hinst, string lpszClass, out WNDCLASSEX lpwcx);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr CreateWindowEx(
           WindowStyle dwExStyle,
           ushort sClassAtom,
           string lpWindowName,
           UInt32 dwStyle,
           Int32 x,
           Int32 y,
           Int32 nWidth,
           Int32 nHeight,
           IntPtr hWndParent,
           IntPtr hMenu,
           IntPtr hInstance,
           IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr CreateWindowEx(
            WindowStyle dwExStyle,
            string lpClassName,
            string lpWindowName,
            UInt32 dwStyle,
            Int32 x,
            Int32 y,
            Int32 nWidth,
            Int32 nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern System.IntPtr DefWindowProc(IntPtr hWnd, WM msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool DestroyWindow(IntPtr hWnd);

        #endregion

        delegate IntPtr WndProc(IntPtr hWnd, WM msg, IntPtr wParam, IntPtr lParam);

        private bool m_disposed = false;
        private IntPtr m_hwnd;
        private MainForm m_parent;
        private WndProc m_KeepMe;

        private const string EVR_CLASS = "EVRPlayApp";
        private const string EVR_WIN = "EVRPlayWin";

        public EVRPlayListener(MainForm parent)
        {
            m_parent = parent;
            m_KeepMe = new WndProc(CustomWndProc);
            IntPtr hInstance = Marshal.GetHINSTANCE(this.GetType().Module);

            WNDCLASSEX wndClassEx = new WNDCLASSEX();
            wndClassEx.cbSize = Marshal.SizeOf(typeof(WNDCLASSEX));
            wndClassEx.style = ClassStyle.GLOBALCLASS;
            wndClassEx.cbClsExtra = 0;
            wndClassEx.cbWndExtra = 0;
            wndClassEx.hbrBackground = IntPtr.Zero;
            wndClassEx.hCursor = IntPtr.Zero;
            wndClassEx.hIcon = IntPtr.Zero;
            wndClassEx.hIconSm = IntPtr.Zero;
            wndClassEx.lpszClassName = EVR_CLASS;
            wndClassEx.lpszMenuName = null;
            wndClassEx.hInstance = hInstance;
            wndClassEx.lpfnWndProc = m_KeepMe;

            ushort atom = RegisterClassEx(ref wndClassEx);

            //WNDCLASSEX lpwcx;
            //bool isReg = GetClassInfoEx(hInstance, EVR_CLASS, out lpwcx);

            if (atom == 0)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error);
            }            

            // Create window
            //m_hwnd = CreateWindowEx(
            //    WindowStyle.EX_NOACTIVATE,
            //    atom,
            //    EVR_WIN,
            //    0,
            //    0,
            //    0,
            //    0,
            //    0,
            //    IntPtr.Zero,
            //    IntPtr.Zero,
            //    Marshal.GetHINSTANCE(parent.GetType().Module),
            //    IntPtr.Zero
            //);

            m_hwnd = CreateWindowEx(
                WindowStyle.EX_NOACTIVATE,
                EVR_CLASS,
                EVR_WIN,
                0,
                0,
                0,
                0,
                0,
                IntPtr.Zero,
                IntPtr.Zero,
                hInstance,
                IntPtr.Zero
            );            
                
            if (m_hwnd == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error);
            }
        }

        private IntPtr CustomWndProc(IntPtr hWnd, WM msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM.SAGE:
                    switch ((SageMessage)lParam)
                    {
                        case SageMessage.SageTVRecordings:
                            m_parent.ShowMediaBrowser(BrowseMode.Recordings);
                            break;
                        case SageMessage.VideoLibrary:
                            m_parent.ShowMediaBrowser(BrowseMode.Media);
                            break;
                    }
                    break;
            }
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                }

                // Dispose unmanaged resources
                if (m_hwnd != IntPtr.Zero)
                {
                    DestroyWindow(m_hwnd);
                    m_hwnd = IntPtr.Zero;
                }

            }
        }

        #endregion
    }

}
