#region license

/*
EvrPlay - A simple media player which plays using the Enhanced Video Renderer
Copyright (C) 2008 andy vt
http://babvant.com

This license governs use of the accompanying software. If you use the software, you accept this license. If you do not accept the license, do not use the software.

1. Definitions
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under U.S. copyright law.
A "contribution" is the original software, or any additions or changes to the software.
A "contributor" is any person that distributes its contribution under this license.
"Licensed patents" are a contributor's patent claims that read directly on its contribution.

2. Grant of Rights
(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.

3. Conditions and Limitations
(A) Reciprocal Grants- For any file you distribute that contains code from the software (in source code or binary format), you must provide recipients the source code to that file along with a copy of this license, which license will govern that file. Code that links to or derives from the software must be released under an OSI-certified open source license.
(B) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
(C) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.
(D) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
(E) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
(F) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.

*/

#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace babgvant.EVRPlay
{
    public enum AppCommand
    {
        BROWSER_BACKWARD = 1,
        BROWSER_FORWARD = 2,
        BROWSER_REFRESH = 3,
        BROWSER_STOP = 4,
        BROWSER_SEARCH = 5,
        BROWSER_FAVORITES = 6,
        BROWSER_HOME = 7,
        VOLUME_MUTE = 8,
        VOLUME_DOWN = 9,
        VOLUME_UP = 10,
        MEDIA_NEXTTRACK = 11,
        MEDIA_PREVIOUSTRACK = 12,
        MEDIA_STOP = 13,
        MEDIA_PLAY_PAUSE = 14,
        LAUNCH_MAIL = 15,
        LAUNCH_MEDIA_SELECT = 16,
        LAUNCH_APP1 = 17,
        LAUNCH_APP2 = 18,
        BASS_DOWN = 19,
        BASS_BOOST = 20,
        BASS_UP = 21,
        TREBLE_DOWN = 22,
        TREBLE_UP = 23,
        MICROPHONE_VOLUME_MUTE = 24,
        MICROPHONE_VOLUME_DOWN = 25,
        MICROPHONE_VOLUME_UP = 26,
        HELP = 27,
        FIND = 28,
        NEW = 29,
        OPEN = 30,
        CLOSE = 31,
        SAVE = 32,
        PRINT = 33,
        UNDO = 34,
        REDO = 35,
        COPY = 36,
        CUT = 37,
        PASTE = 38,
        REPLY_TO_MAIL = 39,
        FORWARD_MAIL = 40,
        SEND_MAIL = 41,
        SPELL_CHECK = 42,
        DICTATE_OR_COMMAND_CONTROL_TOGGLE = 43,
        MIC_ON_OFF_TOGGLE = 44,
        CORRECTION_LIST = 45,
        MEDIA_PLAY = 46,
        MEDIA_PAUSE = 47,
        MEDIA_RECORD = 48,
        MEDIA_FAST_FORWARD = 49,
        MEDIA_REWIND = 50,
        MEDIA_CHANNEL_UP = 51,
        MEDIA_CHANNEL_DOWN = 52,
        DELETE = 53,
        DWM_FLIP3D = 54
    }

    public enum RawInput{
        DETAILS = 0x209,
        GUIDE = 0x8D,
        TVJUMP = 0x25,
        STANDBY = 0x82,
        OEM1 = 0x80,
        OEM2 = 0x81,
        MYTV = 0x46,
        MYVIDEOS = 0x4A,
        MYPICTURES = 0x49,
        MYMUSIC = 0x47,
        RECORDEDTV = 0x48,
        DVDANGLE = 0x4B,
        DVDAUDIO = 0x4C,
        DVDMENU = 0x24,
        DVDSUBTITLE = 0x4D,
        INFO = 0x9,
        GREEN = 0xD
}

    public enum SageMessage
    {
        Left = 2,
        Right,
        Up,
        Down,
        Pause,
        Play,
        SkipFwdPageRight,
        SkipBkwdPageLeft,
        TimeScroll,
        ChannelUp,
        ChannelDown,
        VolumeUp,
        VolumeDown,
        TV,
        PlayFaster,
        PlaySlower,
        Guide,
        Power,
        Select,
        Watched,
        Favorite,
        DontLike,
        Info,
        Record,
        Mute,
        FullScreen,
        Home,
        Options,
        Num0,
        Num1,
        Num2,
        Num3,
        Num4,
        Num5,
        Num6,
        Num7,
        Num8,
        Num9,
        Search,
        Setup,
        Library,
        PowerOn,
        PowerOff,
        MuteOn,
        MuteOff,
        AspectRatioFill,
        AspectRatio4x3,
        AspectRatio16x9,
        AspectRatioSource,
        RightVolUp,
        LeftVolUp,
        UpVolUp,
        DownVolUp,
        PageUp,
        PageDown,
        PageRight,
        PageLeft,
        PlayPause,
        PreviousChannel,
        SkipFwd2,
        SkipBkwd2,
        LiveTV,
        DVDReversePlay,
        DVDNextChapter,
        DVDPrevChapter,
        DVDMenu,
        DVDTitleMenu,
        DVDReturn,
        DVDSubtitleChange,
        DVDSubtitleToggle,
        DVDAudioChange,
        DVDAngleChange,
        DVD,
        Back,
        Forward,
        Customize,
        Custom1,
        Custom2,
        Custom3,
        Custom4,
        Custom5,
        Delete,
        MusicJukebox,
        RecordingSchedule,
        SageTVRecordings,
        PictureLibrary,
        VideoLibrary,
        Stop,
        Eject,
        StopEject,
        Input,
        SmoothFF,
        SmoothRew,
        Dash,
        AspectRatioToggle,
        FullScreenOn,
        FullScreenOff,
        RightSkipFwd,
        LeftSkipBack,
        UpVolumnUp,
        DownVolumnDown,
        Online,
        VideoOutput
    }

    public class WindowsMessage
    {
        public static int GET_APPCOMMAND_LPARAM(IntPtr lParam)
        {
            return (lParam.ToInt32() >> 16) & 4095;
        }
    }
}
