using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using DirectShowLib;
using DirectShowLib.Dvd;

namespace babgvant.EVRPlay
{
    public class MessageForm :NativeWindow
    {
        #region RawInput

        [DllImport("user32.dll")]
        public static extern bool RegisterRawInputDevices([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] RAWINPUTDEVICE[] pRawInputDevices, int uiNumDevices, int cbSize);

        [DllImport("User32.dll")]
        extern static uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

        [StructLayout(LayoutKind.Sequential)]
        internal struct RAWINPUTHEADER
        {
            [MarshalAs(UnmanagedType.U4)]
            public int dwType;
            [MarshalAs(UnmanagedType.U4)]
            public int dwSize;
            public IntPtr hDevice;
            [MarshalAs(UnmanagedType.U4)]
            public int wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RAWHID
        {
            [MarshalAs(UnmanagedType.U4)]
            public int dwSizHid;
            [MarshalAs(UnmanagedType.U4)]
            public int dwCount;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct RAWINPUT
        {
            [FieldOffset(0)]
            public RAWINPUTHEADER header;
            //   [FieldOffset (16)] public RAWMOUSE mouse;
            //   [FieldOffset (16)] public RAWKEYBOARD keyboard;
            [FieldOffset(16)]
            public RAWHID hid;
        }
        //[DllImport("User32.dll")]
        //extern static int GetRawInputData(IntPtr hRawInput, RawInputCommand uiCommand, out RAWINPUT pData, ref int pcbSize, int cbSizeHeader);

        //[DllImport("User32.dll")]
        //extern static uint GetRawInputData(IntPtr hRawInput, RawInputCommand uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

        [Flags()]
        public enum RawInputDeviceFlags
        {
            /// <summary>No flags.</summary>
            None = 0,
            /// <summary>If set, this removes the top level collection from the inclusion list. This tells the operating system to stop reading from a device which matches the top level collection.</summary>
            Remove = 0x00000001,
            /// <summary>If set, this specifies the top level collections to exclude when reading a complete usage page. This flag only affects a TLC whose usage page is already specified with PageOnly.</summary>
            Exclude = 0x00000010,
            /// <summary>If set, this specifies all devices whose top level collection is from the specified usUsagePage. Note that Usage must be zero. To exclude a particular top level collection, use Exclude.</summary>
            PageOnly = 0x00000020,
            /// <summary>If set, this prevents any devices specified by UsagePage or Usage from generating legacy messages. This is only for the mouse and keyboard.</summary>
            NoLegacy = 0x00000030,
            /// <summary>If set, this enables the caller to receive the input even when the caller is not in the foreground. Note that WindowHandle must be specified.</summary>
            InputSink = 0x00000100,
            /// <summary>If set, the mouse button click does not activate the other window.</summary>
            CaptureMouse = 0x00000200,
            /// <summary>If set, the application-defined keyboard device hotkeys are not handled. However, the system hotkeys; for example, ALT+TAB and CTRL+ALT+DEL, are still handled. By default, all keyboard hotkeys are handled. NoHotKeys can be specified even if NoLegacy is not specified and WindowHandle is NULL.</summary>
            NoHotKeys = 0x00000200,
            /// <summary>If set, application keys are handled.  NoLegacy must be specified.  Keyboard only.</summary>
            AppKeys = 0x00000400
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTDEVICE
        {
            /// <summary>Top level collection Usage page for the raw input device.</summary>
            public ushort UsagePage;
            /// <summary>Top level collection Usage for the raw input device. </summary>
            public ushort Usage;
            /// <summary>Mode flag that specifies how to interpret the information provided by UsagePage and Usage.</summary>
            public RawInputDeviceFlags Flags;
            /// <summary>Handle to the target device. If NULL, it follows the keyboard focus.</summary>
            public IntPtr WindowHandle;
        }

        private const int RIM_TYPEMOUSE = 0;
        private const int RIM_TYPEKEYBOARD = 1;
        private const int RIM_TYPEHID = 2;

        private const int RID_INPUT = 0x10000003;
        private const int RID_HEADER = 0x10000005;

        ///// <summary>
        ///// Enumeration containing the type device the raw input is coming from.
        ///// </summary>
        //public enum RawInputType
        //{
        //    /// <summary>
        //    /// Mouse input.
        //    /// </summary>
        //    Mouse = 0,
        //    /// <summary>
        //    /// Keyboard input.
        //    /// </summary>
        //    Keyboard = 1,
        //    /// <summary>
        //    /// Another device that is not the keyboard or the mouse.
        //    /// </summary>
        //    HID = 2
        //}

        ///// <summary>
        ///// Enumeration containing the flags for raw mouse data.
        ///// </summary>
        //[Flags()]
        //public enum RawMouseFlags
        //    : ushort
        //{
        //    /// <summary>Relative to the last position.</summary>
        //    MoveRelative = 0,
        //    /// <summary>Absolute positioning.</summary>
        //    MoveAbsolute = 1,
        //    /// <summary>Coordinate data is mapped to a virtual desktop.</summary>
        //    VirtualDesktop = 2,
        //    /// <summary>Attributes for the mouse have changed.</summary>
        //    AttributesChanged = 4
        //}

        ///// <summary>
        ///// Enumeration containing the button data for raw mouse input.
        ///// </summary>
        //[Flags()]
        //public enum RawMouseButtons
        //    : ushort
        //{
        //    /// <summary>No button.</summary>
        //    None = 0,
        //    /// <summary>Left (button 1) down.</summary>
        //    LeftDown = 0x0001,
        //    /// <summary>Left (button 1) up.</summary>
        //    LeftUp = 0x0002,
        //    /// <summary>Right (button 2) down.</summary>
        //    RightDown = 0x0004,
        //    /// <summary>Right (button 2) up.</summary>
        //    RightUp = 0x0008,
        //    /// <summary>Middle (button 3) down.</summary>
        //    MiddleDown = 0x0010,
        //    /// <summary>Middle (button 3) up.</summary>
        //    MiddleUp = 0x0020,
        //    /// <summary>Button 4 down.</summary>
        //    Button4Down = 0x0040,
        //    /// <summary>Button 4 up.</summary>
        //    Button4Up = 0x0080,
        //    /// <summary>Button 5 down.</summary>
        //    Button5Down = 0x0100,
        //    /// <summary>Button 5 up.</summary>
        //    Button5Up = 0x0200,
        //    /// <summary>Mouse wheel moved.</summary>
        //    MouseWheel = 0x0400
        //}

        ///// <summary>
        ///// Enumeration containing flags for raw keyboard input.
        ///// </summary>
        //[Flags()]
        //public enum RawKeyboardFlags
        //    : ushort
        //{
        //    /// <summary></summary>
        //    KeyMake = 0,
        //    /// <summary></summary>
        //    KeyBreak = 1,
        //    /// <summary></summary>
        //    KeyE0 = 2,
        //    /// <summary></summary>
        //    KeyE1 = 4,
        //    /// <summary></summary>
        //    TerminalServerSetLED = 8,
        //    /// <summary></summary>
        //    TerminalServerShadow = 0x10
        //}

        ///// <summary>
        ///// Enumeration contanining the command types to issue.
        ///// </summary>
        //public enum RawInputCommand
        //{
        //    /// <summary>
        //    /// Get input data.
        //    /// </summary>
        //    Input = 0x10000003,
        //    /// <summary>
        //    /// Get header data.
        //    /// </summary>
        //    Header = 0x10000005
        //}

        ///// <summary>
        ///// Value type for a raw input header.
        ///// </summary>
        //[StructLayout(LayoutKind.Sequential)]
        //public struct RAWINPUTHEADER
        //{
        //    /// <summary>Type of device the input is coming from.</summary>
        //    public RawInputType Type;
        //    /// <summary>Size of the packet of data.</summary>
        //    public int Size;
        //    /// <summary>Handle to the device sending the data.</summary>
        //    public IntPtr Device;
        //    /// <summary>wParam from the window message.</summary>
        //    public IntPtr wParam;
        //}

        ///// <summary>
        ///// Value type for raw input from a mouse.
        ///// </summary>
        //[StructLayout(LayoutKind.Sequential)]
        //public struct RAWINPUTMOUSE
        //{
        //    /// <summary>Flags for the event.</summary>
        //    public RawMouseFlags Flags;
        //    /// <summary>If the mouse wheel is moved, this will contain the delta amount.</summary>
        //    public ushort ButtonData;
        //    /// <summary>Flags for the event.</summary>
        //    public RawMouseButtons ButtonFlags;
        //    /// <summary>Raw button data.</summary>
        //    public uint RawButtons;
        //    /// <summary>Relative direction of motion, depending on flags.</summary>
        //    public int LastX;
        //    /// <summary>Relative direction of motion, depending on flags.</summary>
        //    public int LastY;
        //    /// <summary>Extra information.</summary>
        //    public uint ExtraInformation;
        //}

        ///// <summary>
        ///// Value type for raw input from a keyboard.
        ///// </summary>    
        //[StructLayout(LayoutKind.Sequential)]
        //public struct RAWINPUTKEYBOARD
        //{
        //    /// <summary>Scan code for key depression.</summary>
        //    public short MakeCode;
        //    /// <summary>Scan code information.</summary>
        //    public RawKeyboardFlags Flags;
        //    /// <summary>Reserved.</summary>
        //    public short Reserved;
        //    /// <summary>Virtual key code.</summary>
        //    public ushort VirtualKey;
        //    /// <summary>Corresponding window message.</summary>
        //    public WM Message;
        //    /// <summary>Extra information.</summary>
        //    public int ExtraInformation;
        //}

        ///// <summary>
        ///// Value type for raw input from a HID.
        ///// </summary>    
        //[StructLayout(LayoutKind.Sequential)]
        //public struct RAWINPUTHID
        //{
        //    /// <summary>Size of the HID data in bytes.</summary>
        //    public int Size;
        //    /// <summary>Number of HID in Data.</summary>
        //    public int Count;
        //    /// <summary>Data for the HID.</summary>
        //    public IntPtr Data;
        //}

        ///// <summary>
        ///// Value type for raw input.
        ///// </summary>
        //[StructLayout(LayoutKind.Explicit)]
        //public struct RAWINPUT
        //{
        //    /// <summary>Header for the data.</summary>
        //    [FieldOffset(0)]
        //    public RAWINPUTHEADER Header;
        //    /// <summary>Mouse raw input data.</summary>
        //    [FieldOffset(16)]
        //    public RAWINPUTMOUSE Mouse;
        //    /// <summary>Keyboard raw input data.</summary>
        //    [FieldOffset(16)]
        //    public RAWINPUTKEYBOARD Keyboard;
        //    /// <summary>HID raw input data.</summary>
        //    [FieldOffset(16)]
        //    public RAWINPUTHID HID;
        //}        

        //private const int FAPPCOMMAND_MASK = 0xF000;
        //private const int FAPPCOMMAND_MOUSE = 0x8000;
        //private const int FAPPCOMMAND_KEY = 0;
        //private const int FAPPCOMMAND_OEM = 0x1000;

        #endregion

        private MainForm mf;

        public MessageForm(MainForm mf)
        {
            this.mf = mf;

            //RAWINPUTDEVICE[] Rid = new RAWINPUTDEVICE[2];

            //Rid[0].UsagePage = 0xFFBC;
            //Rid[0].Usage = 0x88;
            //Rid[0].Flags = RawInputDeviceFlags.None;
            //Rid[0].WindowHandle = this.Handle;

            //Rid[1].UsagePage = 0x0C;
            //Rid[1].Usage = 0x01;
            //Rid[1].Flags = RawInputDeviceFlags.None;
            //Rid[1].WindowHandle = this.Handle;

            RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[3];

            rid[0].UsagePage = 0xFFBC;
            rid[0].Usage = 0x88;
            rid[0].Flags = RawInputDeviceFlags.None;

            rid[1].UsagePage = 0x0C;
            rid[1].Usage = 0x01;
            rid[1].Flags = RawInputDeviceFlags.None;

            rid[2].UsagePage = 0x0C;
            rid[2].Usage = 0x80;
            rid[2].Flags = RawInputDeviceFlags.None;

            if (!RegisterRawInputDevices(rid, rid.Length, Marshal.SizeOf(rid[0])))
            {
                FileLogger.Log("Couldn't register eHome remote");
            }
            else
            {
                FileLogger.Log("Registered eHome remote");
            }
        }

        protected override void WndProc(ref Message m)
        {
            try{
            switch (m.Msg)
                {
                    //case (int)WM.GRAPH_NOTIFY:
                    //    mf.HandleGraphEvent();
                    //    break;
                    //case (int)WM.DVD_EVENT:
                    //    mf.OnDvdEvent();
                    //    break;
                    //case (int)WM.APPCOMMAND:
                    //    {
                    //        int iChar = WindowsMessage.GET_APPCOMMAND_LPARAM(m.LParam);

                    //        //FileLogger.Log("WM_APPCOMMAND: {0}", System.Enum.GetName(typeof(AppCommand), iChar));

                    //        switch ((AppCommand)iChar)
                    //        {
                    //            case AppCommand.MEDIA_PLAY:
                    //            case AppCommand.MEDIA_PAUSE:
                    //                ThreadPool.QueueUserWorkItem(new WaitCallback(PauseClip), null);
                    //                break;
                    //            case AppCommand.MEDIA_FAST_FORWARD:
                    //                ThreadPool.QueueUserWorkItem(new WaitCallback(SkipForward), ps.SkipForward2);
                    //                break;
                    //            case AppCommand.MEDIA_REWIND:
                    //                ThreadPool.QueueUserWorkItem(new WaitCallback(SkipBack), ps.SkipBack2);
                    //                break;
                    //            case AppCommand.MEDIA_RECORD:
                    //                ThreadPool.QueueUserWorkItem(new WaitCallback(CloseApplication));
                    //                break;
                    //        }
                    //        break;
                    //    }
                    case (int)WM.INPUT:
                        //RAWINPUT input = new RAWINPUT();
                        //int outSize = 0;
                        //int size = Marshal.SizeOf(typeof(RAWINPUT));

                        //outSize = GetRawInputData(m.LParam, RawInputCommand.Input, out input, ref size, Marshal.SizeOf(typeof(RAWINPUTHEADER)));
                        //if (outSize != -1)
                        //{
                        //    if (input.Header.Type == RawInputType.HID)
                        //    {
                        //        byte[] bRawData = new byte[input.HID.Size];
                        //        //Marshal.Copy(input.HID.Data, bRawData, 0, input.HID.Size - 1);
                        //        Marshal.Copy(
                        //    }
                        //}

                        uint dwSize = 0;

                        GetRawInputData(m.LParam,
                            RID_INPUT,
                            IntPtr.Zero,
                            ref dwSize,
                            (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)
                            ));

                        IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
                        try
                        {
                            if (buffer == IntPtr.Zero)
                                return;

                            if (GetRawInputData(m.LParam,
                                RID_INPUT,
                                buffer,
                                ref dwSize,
                                (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) != dwSize
                                )
                            {
                                return;
                            }

                            RAWINPUT raw = (RAWINPUT)Marshal.PtrToStructure(buffer, typeof(RAWINPUT));
                            if (raw.header.dwType == RIM_TYPEHID)
                            {
                                byte[] bRawData = new byte[raw.hid.dwSizHid];
                                int pRawData = buffer.ToInt32() + Marshal.SizeOf(typeof(RAWINPUT)) + 1;

                                Marshal.Copy(new IntPtr(pRawData), bRawData, 0, raw.hid.dwSizHid - 1);
                                int rawData = bRawData[0] | bRawData[1] << 8;

                                switch ((RawInput)rawData)
                                {
                                    case RawInput.DETAILS:

                                        break;
                                    case RawInput.GUIDE:

                                        break;
                                    case RawInput.TVJUMP:

                                        break;
                                    case RawInput.STANDBY:

                                        break;
                                    case RawInput.OEM1:

                                        break;
                                    case RawInput.OEM2:

                                        break;
                                    case RawInput.MYTV:
                                    case RawInput.RECORDEDTV:
                                        //browse to recordings
                                        break;
                                    case RawInput.MYVIDEOS:
                                        //browse to videos
                                        break;
                                    case RawInput.MYPICTURES:
                                        //browse to pictures
                                        break;
                                    case RawInput.MYMUSIC:
                                        //browse to music
                                        break;
                                    case RawInput.DVDANGLE:
                                        //ignore
                                        break;
                                    case RawInput.DVDAUDIO:
                                        //ignore
                                        break;
                                    case RawInput.DVDMENU:
                                        //navigate menu
                                        mf.ShowDvdMenu(DvdMenuId.Root);
                                        break;
                                    case RawInput.DVDSUBTITLE:
                                        //toggle subtitles
                                        mf.ToogleSubtitles();
                                        break;
                                }

                            }
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(buffer);
                        }
                        break;
                    //case (int)WM.KEYDOWN:
                    //    break;
                }
            }
            catch (Exception ex)
            {
                FileLogger.Log("Error in WndProc: {0}", ex.ToString());
            }
            base.WndProc(ref m);
        }
    }
}
