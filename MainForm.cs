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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

using DirectShowLib;
using DirectShowLib.Dvd;
using DirectShowLib.Utils;

using MediaFoundation;
using MediaFoundation.EVR;

using Microsoft.Win32;

using Wrapper.Windows.RawInput;
using MediaFoundation.Misc;
using CoreAudioApi.Interfaces;
using CoreAudioApi;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace babgvant.EVRPlay
{
    public class MainForm : System.Windows.Forms.Form
    {
        internal static bool DebugMode = false;

        private const int VolumeFull = 0;
        private const int VolumeSilence = -10000;
        
        private IDvdCmd cmdOption = null;
        private bool pendingCmd;
        private IDvdControl2 dvdCtrl = null;
        private IDvdInfo2 dvdInfo = null;
        private IAMLine21Decoder dvdSubtitle = null;

        private IDvdGraphBuilder dvdGraph = null;
        private IGraphBuilder graphBuilder = null;
        private IMediaControl mediaControl = null;
        private IMediaEventEx mediaEventEx = null;
        //private IVideoWindow videoWindow = null;
        private IBasicAudio basicAudio = null;
        private IBasicVideo basicVideo = null;
        private IMediaSeeking mediaSeeking = null;
        private IMediaPosition mediaPosition = null;
        private IVideoFrameStep frameStep = null;
        private IBaseFilter evrRenderer = null;
        private IMFVideoDisplayControl evrDisplay = null;
        IMFVideoMixerBitmap mixBmp = null;
        //IMFRateSupport rateSupport = null;
        //IMFClockStateSink stateSink = null;
        IEVRCPConfig cpsett = null;
                    
        private List<string> filename = new List<string>();
        private int currentIndex = 0;
        private bool isAudioOnly = false;
        public bool isFullScreen = false;
        private int currentVolume = VolumeFull;
        private PlayState currentState = PlayState.Init;
        private double currentPlaybackRate = 1.0;
        internal PlaySettings ps = new PlaySettings();
        //private System.Threading.Timer backupTimer = null;
        private int preFSHeight = 0;
        private int preFSWidth = 0;
        private int preFSLeft = 0;
        private int preFSTop = 0;
        private IntPtr hDrain = IntPtr.Zero;
        private FileSystemWatcher commWatcher = null;
        private long lastJump = 0;
        private List<double[]> commList = null;
        private const long TICK_MULT = 10000000;
        private DateTime lastMove = DateTime.MinValue;
        //private bool showingControls = true;
        private long currentPosition = 0;
        private long fileDuration = 0;
        //#if DEBUG
        private DsROTEntry rot = null;
        //#endif
        private delegate void BlankInvoke();
        private delegate void StringInvoke(string strArg);
        
        private FileHistory fHist = new FileHistory();
        private EpServer eps = null;

        private DvdHMSFTimeCode currnTime = new DvdHMSFTimeCode();		// copy of current playback states, see OnDvdEvent()
        private int currnTitle;
        private int currnChapter;
        private DvdDomain currnDomain;
        private MenuMode menuMode;
        private double dvdPlayRate = 1.0;
        private float zoomAmt = 1.0f;
        private bool disabledScreenSaver = false;
        private ProgressForm pf = null;
        private BrowserForm bf = null;
        private EVRPlayListener messageWindow = null;
        //private SageMessageListener messageWindow = null;
        private MediaBrowser mb = null;
        Guid m_audioRendererClsid = new Guid("{79376820-07D0-11CF-A24D-0020AFD79767}");//DS audio renderer

        //private RawDevice rd = null;

        private System.Windows.Forms.MainMenu mainMenu1;
        private System.Windows.Forms.MenuItem menuItem1;
        private System.Windows.Forms.MenuItem menuItem4;
        private System.Windows.Forms.MenuItem menuItem6;
        private System.Windows.Forms.MenuItem menuItem10;
        private System.Windows.Forms.MenuItem menuItem12;
        private System.Windows.Forms.MenuItem menuItem17;
        private System.Windows.Forms.MenuItem menuItem19;
        private System.Windows.Forms.MenuItem menuItem22;
        private System.Windows.Forms.MenuItem menuItem26;
        private System.Windows.Forms.MenuItem menuFileOpenClip;
        private System.Windows.Forms.MenuItem menuFileClose;
        private System.Windows.Forms.MenuItem menuFileExit;
        private System.Windows.Forms.MenuItem menuFilePause;
        private System.Windows.Forms.MenuItem menuFileStop;
        private System.Windows.Forms.MenuItem menuFileMute;
        private System.Windows.Forms.MenuItem menuSingleStep;
        private System.Windows.Forms.MenuItem menuFileSizeHalf;
        private System.Windows.Forms.MenuItem menuFileSizeThreeQuarter;
        private System.Windows.Forms.MenuItem menuFileSizeNormal;
        private System.Windows.Forms.MenuItem menuFileSizeDouble;
        private System.Windows.Forms.MenuItem menuFileFullScreen;
        private System.Windows.Forms.MenuItem menuRateIncrease;
        private System.Windows.Forms.MenuItem menuRateDecrease;
        private System.Windows.Forms.MenuItem menuRateNormal;
        private System.Windows.Forms.MenuItem menuRateHalf;
        private System.Windows.Forms.MenuItem menuRateDouble;
        private System.Windows.Forms.MenuItem menuHelpAbout;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private MenuItem miSkip1;
        private MenuItem miSkipBack1;
        private MenuItem miSkipForward2;
        private MenuItem miSkipBack2;
        private System.Windows.Forms.Timer tmStatus;
        private System.Windows.Forms.Timer tmPosition;
        private System.Windows.Forms.Timer tmMouseMove;
        private IContainer components;
        private MenuItem menuItemSettings;
        private MenuItem menuItemOpenVideoTS;
        private FolderBrowserDialog folderBrowserDialog1;
        private MenuItem menuItem2;
        private MenuItem menuItemRootMenu;
        private MenuItem menuItemTitleMenu;
        private MenuItem menuItemSubtitles;
        private MenuItem menuItem5;
        private MenuItem menuItemHistory;
        private MenuItem menuItemWebserver;
        private MenuItem menuItem3;
        private MenuItem menuItemZoomIn;
        private MenuItem menuItemZoomOut;
        private MenuItem menuItemMediaBrowser;
        private MenuItem miOpenUrl;
        private MenuItem menuItem7;
        private DateTime enteredFullScreen = DateTime.MinValue;
        private MenuItem menuItem8;
        private MenuItem menuItem9;
        private MenuItem menuItem11;
        private MenuItem menuItem13;
        private MenuItem menuItem14;
        private MenuItem menuItem15;
        private MenuItem menuItem16;
        private MenuItem menuFileSizeFI;
        private MenuItem menuFileSizeFO;
        private MenuItem menuFileSizeZ1;
        private MenuItem menuFileSizeZ2;
        private MenuItem menuItem18;
        private MenuItem menuItem20;
        private MenuItem menuItem21;
        private MenuItem menuItem23;

        private Guid DSOUND_RENDERER = new Guid("{79376820-07D0-11CF-A24D-0020AFD79767}");

        public MainForm()
        {            
            //
            // Requis pour la prise en charge du Concepteur Windows Forms
            //
            InitializeComponent();
        }

        /// <summary>
        /// Nettoyage des ressources utilisées.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (eps != null)
                        eps.Dispose();
                }
                catch { }

                try
                {
                    if (messageWindow != null)
                        messageWindow.Dispose();
                }
                catch { }

                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }


        #region Code généré par le Concepteur Windows Form
        /// <summary>
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.mainMenu1 = new System.Windows.Forms.MainMenu(this.components);
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuFileOpenClip = new System.Windows.Forms.MenuItem();
            this.menuItemOpenVideoTS = new System.Windows.Forms.MenuItem();
            this.miOpenUrl = new System.Windows.Forms.MenuItem();
            this.menuFileClose = new System.Windows.Forms.MenuItem();
            this.menuItem7 = new System.Windows.Forms.MenuItem();
            this.menuItemWebserver = new System.Windows.Forms.MenuItem();
            this.menuItemMediaBrowser = new System.Windows.Forms.MenuItem();
            this.menuItem4 = new System.Windows.Forms.MenuItem();
            this.menuFileExit = new System.Windows.Forms.MenuItem();
            this.menuItem5 = new System.Windows.Forms.MenuItem();
            this.menuItemHistory = new System.Windows.Forms.MenuItem();
            this.menuItem6 = new System.Windows.Forms.MenuItem();
            this.menuFilePause = new System.Windows.Forms.MenuItem();
            this.menuFileStop = new System.Windows.Forms.MenuItem();
            this.menuFileMute = new System.Windows.Forms.MenuItem();
            this.menuItem10 = new System.Windows.Forms.MenuItem();
            this.menuSingleStep = new System.Windows.Forms.MenuItem();
            this.miSkip1 = new System.Windows.Forms.MenuItem();
            this.miSkipForward2 = new System.Windows.Forms.MenuItem();
            this.miSkipBack1 = new System.Windows.Forms.MenuItem();
            this.miSkipBack2 = new System.Windows.Forms.MenuItem();
            this.menuItem12 = new System.Windows.Forms.MenuItem();
            this.menuFileSizeHalf = new System.Windows.Forms.MenuItem();
            this.menuFileSizeNormal = new System.Windows.Forms.MenuItem();
            this.menuFileSizeDouble = new System.Windows.Forms.MenuItem();
            this.menuFileSizeThreeQuarter = new System.Windows.Forms.MenuItem();
            this.menuFileSizeFI = new System.Windows.Forms.MenuItem();
            this.menuFileSizeFO = new System.Windows.Forms.MenuItem();
            this.menuFileSizeZ1 = new System.Windows.Forms.MenuItem();
            this.menuFileSizeZ2 = new System.Windows.Forms.MenuItem();
            this.menuItem17 = new System.Windows.Forms.MenuItem();
            this.menuFileFullScreen = new System.Windows.Forms.MenuItem();
            this.menuItemZoomIn = new System.Windows.Forms.MenuItem();
            this.menuItemZoomOut = new System.Windows.Forms.MenuItem();
            this.menuItem19 = new System.Windows.Forms.MenuItem();
            this.menuRateIncrease = new System.Windows.Forms.MenuItem();
            this.menuRateDecrease = new System.Windows.Forms.MenuItem();
            this.menuItem22 = new System.Windows.Forms.MenuItem();
            this.menuRateNormal = new System.Windows.Forms.MenuItem();
            this.menuRateHalf = new System.Windows.Forms.MenuItem();
            this.menuRateDouble = new System.Windows.Forms.MenuItem();
            this.menuItem9 = new System.Windows.Forms.MenuItem();
            this.menuItem13 = new System.Windows.Forms.MenuItem();
            this.menuItem11 = new System.Windows.Forms.MenuItem();
            this.menuItem2 = new System.Windows.Forms.MenuItem();
            this.menuItemRootMenu = new System.Windows.Forms.MenuItem();
            this.menuItemTitleMenu = new System.Windows.Forms.MenuItem();
            this.menuItemSubtitles = new System.Windows.Forms.MenuItem();
            this.menuItem26 = new System.Windows.Forms.MenuItem();
            this.menuHelpAbout = new System.Windows.Forms.MenuItem();
            this.menuItemSettings = new System.Windows.Forms.MenuItem();
            this.menuItem3 = new System.Windows.Forms.MenuItem();
            this.menuItem8 = new System.Windows.Forms.MenuItem();
            this.menuItem14 = new System.Windows.Forms.MenuItem();
            this.menuItem15 = new System.Windows.Forms.MenuItem();
            this.menuItem16 = new System.Windows.Forms.MenuItem();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.tmStatus = new System.Windows.Forms.Timer(this.components);
            this.tmPosition = new System.Windows.Forms.Timer(this.components);
            this.tmMouseMove = new System.Windows.Forms.Timer(this.components);
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.menuItem18 = new System.Windows.Forms.MenuItem();
            this.menuItem20 = new System.Windows.Forms.MenuItem();
            this.menuItem21 = new System.Windows.Forms.MenuItem();
            this.menuItem23 = new System.Windows.Forms.MenuItem();
            this.SuspendLayout();
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem1,
            this.menuItem6,
            this.menuItem19,
            this.menuItem2,
            this.menuItem26});
            // 
            // menuItem1
            // 
            this.menuItem1.Index = 0;
            this.menuItem1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuFileOpenClip,
            this.menuItemOpenVideoTS,
            this.miOpenUrl,
            this.menuFileClose,
            this.menuItem7,
            this.menuItemWebserver,
            this.menuItemMediaBrowser,
            this.menuItem4,
            this.menuFileExit,
            this.menuItem5,
            this.menuItemHistory});
            this.menuItem1.Text = "File";
            // 
            // menuFileOpenClip
            // 
            this.menuFileOpenClip.Index = 0;
            this.menuFileOpenClip.Text = "Open clip...";
            this.menuFileOpenClip.Click += new System.EventHandler(this.menuFileOpenClip_Click);
            // 
            // menuItemOpenVideoTS
            // 
            this.menuItemOpenVideoTS.Index = 1;
            this.menuItemOpenVideoTS.Text = "Open VIDEO_TS";
            this.menuItemOpenVideoTS.Click += new System.EventHandler(this.menuItemOpenVideoTS_Click);
            // 
            // miOpenUrl
            // 
            this.miOpenUrl.Index = 2;
            this.miOpenUrl.Text = "Open URL";
            this.miOpenUrl.Click += new System.EventHandler(this.miOpenUrl_Click);
            // 
            // menuFileClose
            // 
            this.menuFileClose.Index = 3;
            this.menuFileClose.Text = "Close clip";
            this.menuFileClose.Click += new System.EventHandler(this.menuFileClose_Click);
            // 
            // menuItem7
            // 
            this.menuItem7.Index = 4;
            this.menuItem7.Text = "-";
            // 
            // menuItemWebserver
            // 
            this.menuItemWebserver.Index = 5;
            this.menuItemWebserver.Text = "Webserver";
            this.menuItemWebserver.Click += new System.EventHandler(this.menuItemWebserver_Click);
            // 
            // menuItemMediaBrowser
            // 
            this.menuItemMediaBrowser.Index = 6;
            this.menuItemMediaBrowser.Text = "Media Browser";
            this.menuItemMediaBrowser.Click += new System.EventHandler(this.menuItemMediaBrowser_Click);
            // 
            // menuItem4
            // 
            this.menuItem4.Index = 7;
            this.menuItem4.Text = "-";
            // 
            // menuFileExit
            // 
            this.menuFileExit.Index = 8;
            this.menuFileExit.Text = "Exit";
            this.menuFileExit.Click += new System.EventHandler(this.menuFileExit_Click);
            // 
            // menuItem5
            // 
            this.menuItem5.Index = 9;
            this.menuItem5.Text = "-";
            // 
            // menuItemHistory
            // 
            this.menuItemHistory.Index = 10;
            this.menuItemHistory.Text = "History";
            // 
            // menuItem6
            // 
            this.menuItem6.Index = 1;
            this.menuItem6.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuFilePause,
            this.menuFileStop,
            this.menuFileMute,
            this.menuItem10,
            this.menuSingleStep,
            this.miSkip1,
            this.miSkipForward2,
            this.miSkipBack1,
            this.miSkipBack2,
            this.menuItem12,
            this.menuFileSizeHalf,
            this.menuFileSizeNormal,
            this.menuFileSizeDouble,
            this.menuFileSizeThreeQuarter,
            this.menuFileSizeFI,
            this.menuFileSizeFO,
            this.menuFileSizeZ1,
            this.menuFileSizeZ2,
            this.menuItem17,
            this.menuFileFullScreen,
            this.menuItemZoomIn,
            this.menuItemZoomOut});
            this.menuItem6.Text = "Control";
            // 
            // menuFilePause
            // 
            this.menuFilePause.Index = 0;
            this.menuFilePause.Text = "Play/Pause";
            this.menuFilePause.Click += new System.EventHandler(this.menuFilePause_Click);
            // 
            // menuFileStop
            // 
            this.menuFileStop.Index = 1;
            this.menuFileStop.Text = "Stop";
            this.menuFileStop.Click += new System.EventHandler(this.menuFileStop_Click);
            // 
            // menuFileMute
            // 
            this.menuFileMute.Index = 2;
            this.menuFileMute.Text = "Mute/Unmute";
            this.menuFileMute.Click += new System.EventHandler(this.menuFileMute_Click);
            // 
            // menuItem10
            // 
            this.menuItem10.Index = 3;
            this.menuItem10.Text = "-";
            // 
            // menuSingleStep
            // 
            this.menuSingleStep.Index = 4;
            this.menuSingleStep.Text = "Single Frame Step";
            this.menuSingleStep.Click += new System.EventHandler(this.menuSingleStep_Click);
            // 
            // miSkip1
            // 
            this.miSkip1.Index = 5;
            this.miSkip1.Text = "Forward 1";
            this.miSkip1.Click += new System.EventHandler(this.miSkip1_Click);
            // 
            // miSkipForward2
            // 
            this.miSkipForward2.Index = 6;
            this.miSkipForward2.Text = "Forward 2";
            this.miSkipForward2.Click += new System.EventHandler(this.miSkipForward2_Click);
            // 
            // miSkipBack1
            // 
            this.miSkipBack1.Index = 7;
            this.miSkipBack1.Text = "Back 1";
            this.miSkipBack1.Click += new System.EventHandler(this.miSkipBack1_Click);
            // 
            // miSkipBack2
            // 
            this.miSkipBack2.Index = 8;
            this.miSkipBack2.Text = "Back 2";
            this.miSkipBack2.Click += new System.EventHandler(this.miSkipBack2_Click);
            // 
            // menuItem12
            // 
            this.menuItem12.Index = 9;
            this.menuItem12.Text = "-";
            // 
            // menuFileSizeHalf
            // 
            this.menuFileSizeHalf.Index = 10;
            this.menuFileSizeHalf.Tag = "HALF";
            this.menuFileSizeHalf.Text = "Half size (50%)";
            this.menuFileSizeHalf.Click += new System.EventHandler(this.menuFileSize_Click);
            // 
            // menuFileSizeNormal
            // 
            this.menuFileSizeNormal.Index = 11;
            this.menuFileSizeNormal.Tag = "NORMAL";
            this.menuFileSizeNormal.Text = "Normal size (100%)";
            this.menuFileSizeNormal.Click += new System.EventHandler(this.menuFileSize_Click);
            // 
            // menuFileSizeDouble
            // 
            this.menuFileSizeDouble.Index = 12;
            this.menuFileSizeDouble.Tag = "DOUBLE";
            this.menuFileSizeDouble.Text = "Double size (200%)";
            this.menuFileSizeDouble.Click += new System.EventHandler(this.menuFileSize_Click);
            // 
            // menuFileSizeThreeQuarter
            // 
            this.menuFileSizeThreeQuarter.Index = 13;
            this.menuFileSizeThreeQuarter.Tag = "STRETCH";
            this.menuFileSizeThreeQuarter.Text = "Strech";
            this.menuFileSizeThreeQuarter.Click += new System.EventHandler(this.menuFileSize_Click);
            // 
            // menuFileSizeFI
            // 
            this.menuFileSizeFI.Index = 14;
            this.menuFileSizeFI.Tag = "FROMINSIDE";
            this.menuFileSizeFI.Text = "From Inside";
            this.menuFileSizeFI.Click += new System.EventHandler(this.menuFileSize_Click);
            // 
            // menuFileSizeFO
            // 
            this.menuFileSizeFO.Index = 15;
            this.menuFileSizeFO.Tag = "FROMOUTSIDE";
            this.menuFileSizeFO.Text = "From Outside";
            this.menuFileSizeFO.Click += new System.EventHandler(this.menuFileSize_Click);
            // 
            // menuFileSizeZ1
            // 
            this.menuFileSizeZ1.Index = 16;
            this.menuFileSizeZ1.Tag = "ZOOM1";
            this.menuFileSizeZ1.Text = "Zoom 1";
            this.menuFileSizeZ1.Click += new System.EventHandler(this.menuFileSize_Click);
            // 
            // menuFileSizeZ2
            // 
            this.menuFileSizeZ2.Index = 17;
            this.menuFileSizeZ2.Tag = "ZOOM2";
            this.menuFileSizeZ2.Text = "Zoom 2";
            this.menuFileSizeZ2.Click += new System.EventHandler(this.menuFileSize_Click);
            // 
            // menuItem17
            // 
            this.menuItem17.Index = 18;
            this.menuItem17.Text = "-";
            // 
            // menuFileFullScreen
            // 
            this.menuFileFullScreen.Index = 19;
            this.menuFileFullScreen.Text = "Full Screen";
            this.menuFileFullScreen.Click += new System.EventHandler(this.menuFileFullScreen_Click);
            // 
            // menuItemZoomIn
            // 
            this.menuItemZoomIn.Index = 20;
            this.menuItemZoomIn.Text = "Zoom In";
            this.menuItemZoomIn.Click += new System.EventHandler(this.menuItemZoomIn_Click);
            // 
            // menuItemZoomOut
            // 
            this.menuItemZoomOut.Index = 21;
            this.menuItemZoomOut.Text = "Zoom Out";
            this.menuItemZoomOut.Click += new System.EventHandler(this.menuItemZoomOut_Click);
            // 
            // menuItem19
            // 
            this.menuItem19.Index = 2;
            this.menuItem19.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuRateIncrease,
            this.menuRateDecrease,
            this.menuItem22,
            this.menuRateNormal,
            this.menuRateHalf,
            this.menuRateDouble,
            this.menuItem9,
            this.menuItem13,
            this.menuItem11,
            this.menuItem18,
            this.menuItem20,
            this.menuItem21,
            this.menuItem23});
            this.menuItem19.Text = "Rate";
            // 
            // menuRateIncrease
            // 
            this.menuRateIncrease.Index = 0;
            this.menuRateIncrease.Text = "Increase Playback Rate";
            this.menuRateIncrease.Click += new System.EventHandler(this.menuRate_Click);
            // 
            // menuRateDecrease
            // 
            this.menuRateDecrease.Index = 1;
            this.menuRateDecrease.Text = "Decrease Playback Rate";
            this.menuRateDecrease.Click += new System.EventHandler(this.menuRate_Click);
            // 
            // menuItem22
            // 
            this.menuItem22.Index = 2;
            this.menuItem22.Text = "-";
            // 
            // menuRateNormal
            // 
            this.menuRateNormal.Index = 3;
            this.menuRateNormal.Text = "Normal Playback Rate";
            this.menuRateNormal.Click += new System.EventHandler(this.menuRate_Click);
            // 
            // menuRateHalf
            // 
            this.menuRateHalf.Index = 4;
            this.menuRateHalf.Text = "Half Playback Rate";
            this.menuRateHalf.Click += new System.EventHandler(this.menuRate_Click);
            // 
            // menuRateDouble
            // 
            this.menuRateDouble.Index = 5;
            this.menuRateDouble.Text = "Double Playback Rate";
            this.menuRateDouble.Click += new System.EventHandler(this.menuRate_Click);
            // 
            // menuItem9
            // 
            this.menuItem9.Index = 6;
            this.menuItem9.Tag = "4";
            this.menuItem9.Text = "4x";
            this.menuItem9.Click += new System.EventHandler(this.menuRate_Click);
            // 
            // menuItem13
            // 
            this.menuItem13.Index = 7;
            this.menuItem13.Tag = "6";
            this.menuItem13.Text = "6x";
            this.menuItem13.Click += new System.EventHandler(this.menuRate_Click);
            // 
            // menuItem11
            // 
            this.menuItem11.Index = 8;
            this.menuItem11.Tag = "8";
            this.menuItem11.Text = "8x";
            this.menuItem11.Click += new System.EventHandler(this.menuRate_Click);
            // 
            // menuItem2
            // 
            this.menuItem2.Index = 3;
            this.menuItem2.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItemRootMenu,
            this.menuItemTitleMenu,
            this.menuItemSubtitles});
            this.menuItem2.Text = "DVD";
            // 
            // menuItemRootMenu
            // 
            this.menuItemRootMenu.Index = 0;
            this.menuItemRootMenu.Text = "Root Menu";
            this.menuItemRootMenu.Click += new System.EventHandler(this.menuItemRootMenu_Click);
            // 
            // menuItemTitleMenu
            // 
            this.menuItemTitleMenu.Index = 1;
            this.menuItemTitleMenu.Text = "Title Menu";
            this.menuItemTitleMenu.Click += new System.EventHandler(this.menuItem3_Click);
            // 
            // menuItemSubtitles
            // 
            this.menuItemSubtitles.Index = 2;
            this.menuItemSubtitles.Text = "Subtitles";
            this.menuItemSubtitles.Click += new System.EventHandler(this.menuItemSubtitles_Click);
            // 
            // menuItem26
            // 
            this.menuItem26.Index = 4;
            this.menuItem26.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuHelpAbout,
            this.menuItemSettings,
            this.menuItem3,
            this.menuItem8,
            this.menuItem14,
            this.menuItem15,
            this.menuItem16});
            this.menuItem26.Text = "More";
            // 
            // menuHelpAbout
            // 
            this.menuHelpAbout.Index = 0;
            this.menuHelpAbout.Text = "About";
            this.menuHelpAbout.Click += new System.EventHandler(this.menuHelpAbout_Click);
            // 
            // menuItemSettings
            // 
            this.menuItemSettings.Index = 1;
            this.menuItemSettings.Text = "Settings";
            this.menuItemSettings.Click += new System.EventHandler(this.menuItemSettings_Click);
            // 
            // menuItem3
            // 
            this.menuItem3.Index = 2;
            this.menuItem3.Text = "Test";
            this.menuItem3.Click += new System.EventHandler(this.menuItem3_Click_2);
            // 
            // menuItem8
            // 
            this.menuItem8.Index = 3;
            this.menuItem8.Text = "Screen Shot";
            this.menuItem8.Click += new System.EventHandler(this.menuItem8_Click);
            // 
            // menuItem14
            // 
            this.menuItem14.Index = 4;
            this.menuItem14.Text = "Set Bitmap";
            this.menuItem14.Click += new System.EventHandler(this.menuItem14_Click);
            // 
            // menuItem15
            // 
            this.menuItem15.Index = 5;
            this.menuItem15.Text = "Clear Bitmap";
            this.menuItem15.Click += new System.EventHandler(this.menuItem15_Click);
            // 
            // menuItem16
            // 
            this.menuItem16.Index = 6;
            this.menuItem16.Text = "Toggle Range";
            this.menuItem16.Click += new System.EventHandler(this.menuItem16_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.DereferenceLinks = false;
            this.openFileDialog1.Filter = "Video Files |*.ts; *.avi; *.qt; *.mov; *.mpg; *.mpeg; *.m1v; *.mkv; *.wmv; *.dvr-" +
                "ms; *.wtv;*.ifo;*.m2ts|PLS File(*.pls)|*.pls|All Files (*.*)|*.*";
            // 
            // tmStatus
            // 
            this.tmStatus.Interval = 1000;
            this.tmStatus.Tick += new System.EventHandler(this.UpdateStatus);
            // 
            // tmPosition
            // 
            this.tmPosition.Tick += new System.EventHandler(this.tmPosition_Tick);
            // 
            // tmMouseMove
            // 
            this.tmMouseMove.Interval = 1000;
            this.tmMouseMove.Tick += new System.EventHandler(this.tmMouseMove_Tick);
            // 
            // menuItem18
            // 
            this.menuItem18.Index = 9;
            this.menuItem18.Tag = "-4";
            this.menuItem18.Text = "<< 4x";
            this.menuItem18.Click += new System.EventHandler(this.menuRate_Click);
            // 
            // menuItem20
            // 
            this.menuItem20.Index = 10;
            this.menuItem20.Tag = "-2";
            this.menuItem20.Text = "<< 2x";
            this.menuItem20.Click += new System.EventHandler(this.menuRate_Click);
            // 
            // menuItem21
            // 
            this.menuItem21.Index = 11;
            this.menuItem21.Tag = "-1.5";
            this.menuItem21.Text = "<< 1.5x";
            this.menuItem21.Click += new System.EventHandler(this.menuRate_Click);
            // 
            // menuItem23
            // 
            this.menuItem23.Index = 12;
            this.menuItem23.Tag = "-6";
            this.menuItem23.Text = "<< 6x";
            this.menuItem23.Click += new System.EventHandler(this.menuRate_Click);
            // 
            // MainForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(661, 346);
            this.Menu = this.mainMenu1;
            this.Name = "MainForm";
            this.Text = "EVR Media Player";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.MainForm_MouseDoubleClick);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.Repainted);
            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.MainForm_MouseClick);
            this.DoubleClick += new System.EventHandler(this.MainForm_DoubleClick);
            this.Activated += new System.EventHandler(this.MainForm_Activated);
            this.Click += new System.EventHandler(this.MainForm_Click);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.MainForm_MouseDown);
            this.Closing += new System.ComponentModel.CancelEventHandler(this.MainForm_Closing);
            this.Move += new System.EventHandler(this.MainForm_Move);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.MainForm_MouseMove);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
            this.ResumeLayout(false);

        }
        #endregion

        public static string BaseBookmarkPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EVRPlay");                
            }
        }

        private string BookmarkFile
        {
            get
            {
                string bookmarkDir = BaseBookmarkPath;
                if (!string.IsNullOrEmpty(ps.BookmarkDir))
                    bookmarkDir = ps.BookmarkDir;

                if (!Directory.Exists(bookmarkDir))
                    Directory.CreateDirectory(bookmarkDir);
                if (dvdGraph == null)
                    return Path.Combine(bookmarkDir, string.Format("{0}{1}", Path.GetFileName(this.filename[currentIndex]), ".vpos"));
                else
                {
                    string pDir = Path.GetDirectoryName(this.filename[currentIndex]);
                    if (pDir.ToLower().IndexOf("video_ts") > 0)
                        pDir = Path.GetDirectoryName(pDir);

                    return Path.Combine(bookmarkDir, string.Format("{0}{1}", Path.GetFileName(pDir), ".vpos"));
                }
            }
        }


        /*
     * Graph creation and destruction methods
     */
        internal void OpenFile(object objShowFileDialog)
        {
            OpenClip(objShowFileDialog);
        }

        delegate void GetClipDelgate(OpenClipMode mode);

        internal bool OpenClip(object objShowFileDialog)
        {
            if (this.InvokeRequired)
            {
                return (bool)this.Invoke(new BoolInvoke(OpenClip), new object[] { objShowFileDialog });
            }
            else
            {
                OpenClipMode showFileDialog = (OpenClipMode)objShowFileDialog;
                //CommonOpenFileDialog cfd = new CommonOpenFileDialog();
                //cfd.EnsureReadOnly = true;

                //if (showFileDialog == OpenClipMode.Folder)
                //{
                //    cfd.IsFolderPicker = true;
                //}
                //else
                //{
                //    cfd.IsFolderPicker = false;
                //}
                try
                {
                    //// If no filename specified by command line, show file open dialog
                    if (this.filename.Count == 0)
                    {
                        UpdateMainTitle();
                        this.filename.Clear();
                        
                        switch (showFileDialog)
                        {
                            case OpenClipMode.File:
                            case OpenClipMode.Url:
                                Durrant.Common.ApartmentStateSwitcher.Execute(new GetClipDelgate(GetClipFileName), new object[] {showFileDialog}, ApartmentState.STA);
                                break;
                            case OpenClipMode.Folder:
                                GetDvdPath();
                                break;
                        }
                    
                        if (this.filename.Count == 0)
                            return false;
                    }

                    // Reset status variables
                    this.currentState = PlayState.Stopped;
                    this.currentVolume = VolumeFull;

                    BuildHistoryMenu(this.filename[currentIndex]);

                    // Start playing the media file
                    if (Regex.IsMatch(this.filename[currentIndex], @"VIDEO_TS\\?$") || Path.GetExtension(this.filename[currentIndex]).ToLower() == ".ifo")
                        PlayDVDInWindow(this.filename[currentIndex]);
                    else
                        PlayMovieInWindow(this.filename[currentIndex]);

                    disabledScreenSaver = NativeMethods2.SystemParametersInfo(SPI.SPI_SETSCREENSAVEACTIVE,
                              Convert.ToUInt32(false),      // TRUE to enable
                              IntPtr.Zero,
                              SPIF.SPIF_SENDWININICHANGE
                             );

                    tmStatus.Enabled = true;
                    //if (isFullScreen)
                    ToogleControls(!isFullScreen);
                    this.Activate();
                    return true;
                }
                catch (Exception ex)
                {
                    FileLogger.Log("Error opening file: {0}", ex.ToString());
                    CloseClip();
                    //MessageBox.Show(string.Format("Error: {0}", ex.Message));
                    using (EPDialog ed = new EPDialog())
                        ed.ShowDialog("Error", string.Format("Error Opening: {0}", ex.Message), 20, this);
                }
                return false;
            }
        }

        private void BuildHistoryMenu(string filePath)
        {
            if (fHist.FileList == null)
                fHist.FileList = new System.Collections.Specialized.StringCollection();

            if (!string.IsNullOrEmpty(filePath))
            {
                if (fHist.FileList.Contains(filePath))
                    fHist.FileList.Remove(filePath);

                fHist.FileList.Insert(0, filePath);
            }

            if (fHist.FileList.Count > ps.FileHistory)
                fHist.FileList.RemoveAt(fHist.FileList.Count - 1);

            menuItemHistory.MenuItems.Clear();

            if (fHist.FileList.Count > 0)
            {
                foreach (string f in fHist.FileList)
                {
                    try
                    {
                        string pDir = string.Empty;// Path.GetDirectoryName(f);
                        if (f.ToLower().IndexOf("video_ts") > 0)
                            pDir = Path.GetDirectoryName(f);
                        else
                            pDir = f;

                        string fName = string.Empty;
                        Uri fPath = new Uri(pDir);

                        if (fPath.IsFile)
                            fName = Path.GetFileName(pDir);
                        else
                            fName = f;

                        MenuItem mi = new MenuItem(fName, new EventHandler(miFileHistoryClick));
                        mi.Tag = f;
                        menuItemHistory.MenuItems.Add(mi);
                    }
                    catch { }
                }
                fHist.Save();
            }
        }

        private void GetDvdPath()
        {
            //if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            //{
            //    if (folderBrowserDialog1.SelectedPath.ToLower().IndexOf("video_ts") < 0)
            //    {
            //        //MessageBox.Show("Please select a valid VIDEO_TS folder");
            //        using (EPDialog ed = new EPDialog())
            //            ed.ShowDialog("Error", "Please select a valid VIDEO_TS folder", 10, this);
            //    }
            //    else
            //        filename.Add(folderBrowserDialog1.SelectedPath);
            //}

            CommonOpenFileDialog cfd = new CommonOpenFileDialog();
            cfd.EnsureReadOnly = true;

            cfd.IsFolderPicker = true;
            if (cfd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                if (cfd.FileName.ToLower().IndexOf("video_ts") < 0)
                {
                    //MessageBox.Show("Please select a valid VIDEO_TS folder");
                    using (EPDialog ed = new EPDialog())
                        ed.ShowDialog("Error", "Please select a valid VIDEO_TS folder", 10, this);
                }
                else
                    filename.Add(cfd.FileName);
            }
              
        }

        private void PlayDVDInWindow(string filepath)
        {
            FileLogger.Log("PlayDVDInWindow: {0}", filepath);
            int hr = 0;
            dvdGraph = new DvdGraphBuilder() as IDvdGraphBuilder;

            if (dvdGraph != null)
            {
                hr = dvdGraph.GetFiltergraph(out this.graphBuilder);
                DsError.ThrowExceptionForHR(hr);

                if (ps.PublishGraph)
                    rot = new DsROTEntry(this.graphBuilder);

                AMDvdGraphFlags buildFlags = AMDvdGraphFlags.EvrOnly;

                if (ps.PreferredDecoders != null)
                {
                    foreach (string pa in ps.PreferredDecoders)
                    {
                        string[] pvA = pa.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                        if (pvA[0].ToLower() == ".ifo")
                        {
                            for (int i = 1; i < pvA.Length; i++)
                            {
                                string strFilter = pvA[i].Trim();
                                IBaseFilter filter = null;
                                try
                                {
                                    if (Regex.IsMatch(strFilter, @"{?\w{8}-\w{4}-\w{4}-\w{4}-\w{12}}?"))
                                        filter = FilterGraphTools.AddFilterFromClsid(graphBuilder, new Guid(strFilter), strFilter);
                                    else
                                        filter = FilterGraphTools.AddFilterByName(graphBuilder, FilterCategory.LegacyAmFilterCategory, strFilter);

                                    if (filter != null)
                                    {
                                        FileLogger.Log("Added {0} to the graph", strFilter);
                                        buildFlags = AMDvdGraphFlags.EvrOnly | AMDvdGraphFlags.DoNotClear;
                                    }
                                    else
                                        FileLogger.Log("{0} not added to the graph", strFilter);
                                }
                                finally
                                {
                                    if (filter != null)
                                        Marshal.ReleaseComObject(filter);
                                    filter = null;
                                }
                            }
                        }
                    }
                }

                object objEvr;
                hr = dvdGraph.GetDvdInterface(typeof(IMFVideoRenderer).GUID, out objEvr);
                DsError.ThrowExceptionForHR(hr);

                evrRenderer = objEvr as IBaseFilter;
                SetupEvrDisplay();

                AMDvdRenderStatus buildStatus;
                hr = dvdGraph.RenderDvdVideoVolume(filepath, buildFlags, out buildStatus);
                DsError.ThrowExceptionForHR(hr);

                if (buildStatus.iNumStreamsFailed > 1)
                {
                 //   throw new ApplicationException("Could not render video_ts, try forcing a dvd decoder");
                    IBaseFilter lavVideo = FilterGraphTools.FindFilterByClsid(graphBuilder, new Guid("{EE30215D-164F-4A92-A4EB-9D4C13390F9F}"));
                    IPin lavOut = null;
                    IPin evrIn = null;

                    try
                    {
                        lavOut = DsFindPin.ByDirection(lavVideo, PinDirection.Output, 0);
                        if (lavOut != null)
                        {
                            hr = lavOut.ConnectedTo(out evrIn);
                            //DsError.ThrowExceptionForHR(hr);

                            if (evrIn == null)
                            {
                                evrIn = DsFindPin.ByDirection(evrRenderer, PinDirection.Input, 0);
                                hr = graphBuilder.ConnectDirect(lavOut, evrIn, null);
                                DsError.ThrowExceptionForHR(hr);
                            }
                        }
                    }
                    finally
                    {
                        if (lavOut != null)
                            Marshal.ReleaseComObject(lavOut);
                        if (evrIn != null)
                            Marshal.ReleaseComObject(evrIn);
                        if (lavVideo != null)
                            Marshal.ReleaseComObject(lavVideo);
                    }
                }
                SetEvrVideoMode();

                object comobj = null;

                hr = dvdGraph.GetDvdInterface(typeof(IDvdInfo2).GUID, out comobj);
                //DsError.ThrowExceptionForHR(hr);
                if (comobj != null)
                {
                    dvdInfo = (IDvdInfo2)comobj;
                    comobj = null;
                }

                hr = dvdGraph.GetDvdInterface(typeof(IDvdControl2).GUID, out comobj);
                //DsError.ThrowExceptionForHR(hr);
                if (comobj != null)
                {
                    dvdCtrl = (IDvdControl2)comobj;
                    comobj = null;
                }

                hr = dvdGraph.GetDvdInterface(typeof(IAMLine21Decoder).GUID, out comobj);
                //DsError.ThrowExceptionForHR(hr);
                if (comobj != null)
                {
                    dvdSubtitle = (IAMLine21Decoder)comobj;
                    comobj = null;
                }

                //IBaseFilter yo = dvdSubtitle as IBaseFilter;
                //IPin mama = dvdSubtitle as IPin;

                menuItemSubtitles.Checked = ToogleSubtitles();

                hr = dvdCtrl.SetOption(DvdOptionFlag.HMSFTimeCodeEvents, true);	// use new HMSF timecode format
                DsError.ThrowExceptionForHR(hr);

                hr = dvdCtrl.SetOption(DvdOptionFlag.ResetOnStop, false);
                DsError.ThrowExceptionForHR(hr);
                


                // QueryInterface for DirectShow interfaces
                this.mediaControl = (IMediaControl)this.graphBuilder;
                this.mediaEventEx = (IMediaEventEx)this.graphBuilder;
                this.mediaSeeking = (IMediaSeeking)this.graphBuilder;
                this.mediaPosition = (IMediaPosition)this.graphBuilder;

                // Query for audio interfaces, which may not be relevant for video-only files
                this.basicAudio = this.graphBuilder as IBasicAudio;

                // Is this an audio-only file (no video component)?
                CheckVisibility();

                // Have the graph signal event via window callbacks for performance
                //hr = this.mediaEventEx.SetNotifyWindow(this.Handle, WMGraphNotify, IntPtr.Zero);
                //DsError.ThrowExceptionForHR(hr);
                hr = mediaEventEx.SetNotifyWindow(this.Handle, WM.DVD_EVENT, IntPtr.Zero);
                DsError.ThrowExceptionForHR(hr);

                if (!this.isAudioOnly)
                {
                    // Setup the video window
                    //hr = this.videoWindow.put_Owner(this.Handle);
                    //DsError.ThrowExceptionForHR(hr);
                    //this.evrDisplay.SetVideoWindow(this.Handle);

                    //hr = this.videoWindow.put_WindowStyle(WindowStyle.Child | WindowStyle.ClipSiblings | WindowStyle.ClipChildren);
                    //DsError.ThrowExceptionForHR(hr);

                    hr = InitVideoWindow();//1, 1);
                    DsError.ThrowExceptionForHR(hr);

                    GetFrameStepInterface();
                }
                else
                {
                    // Initialize the default player size and enable playback menu items
                    hr = InitPlayerWindow();
                    DsError.ThrowExceptionForHR(hr);

                    EnablePlaybackMenu(true, MediaType.Audio);
                }

                // Complete window initialization
                //CheckSizeMenu(menuFileSizeNormal);
                //this.isFullScreen = false;
                this.currentPlaybackRate = 1.0;
                UpdateMainTitle();

                this.Activate();

                //pre-roll the graph
                hr = this.mediaControl.Pause();
                DsError.ThrowExceptionForHR(hr);

                // Run the graph to play the media file
                hr = this.mediaControl.Run();
                DsError.ThrowExceptionForHR(hr);
                
                MoveToBookmark();

                this.currentState = PlayState.Running;
                if (isFullScreen)
                    tmMouseMove.Enabled = true;
            }
        }

        public bool ToogleSubtitles()
        {
            FileLogger.Log("ToogleSubtitles");
            if (dvdCtrl != null)
            {
                AMLine21CCState cState = AMLine21CCState.On;

                if (dvdSubtitle != null)
                {
                    int hr = 0;


                    hr = dvdSubtitle.GetServiceState(out cState);
                    DsError.ThrowExceptionForHR(hr);

                    FileLogger.Log("ToogleSubtitles - CurrentState: {0}", cState);

                    if (cState == AMLine21CCState.Off)
                        hr = dvdSubtitle.SetServiceState(AMLine21CCState.On);
                    else
                        hr = dvdSubtitle.SetServiceState(AMLine21CCState.Off);
                    DsError.ThrowExceptionForHR(hr);

                }
                return cState != AMLine21CCState.On;
            }
            else
                return false;
        }

        private void PlayMovieInWindow(string filename)
        {
            WindowsMediaLib.IWMReaderAdvanced2 wmReader = null;
            IBaseFilter sourceFilter = null;

            try
            {

                FileLogger.Log("PlayMovieInWindow: {0}", filename);
                lastJump = 0;

                int hr = 0;

                if (filename == string.Empty)
                    return;

                this.graphBuilder = (IGraphBuilder)new FilterGraph();
                FileLogger.Log("PlayMovieInWindow: Create Graph");

                this.evrRenderer = FilterGraphTools.AddFilterFromClsid(this.graphBuilder, new Guid("{FA10746C-9B63-4B6C-BC49-FC300EA5F256}"), "EVR");

                if (evrRenderer != null)
                {
                    FileLogger.Log("PlayMovieInWindow: Add EVR");

                    SetupEvrDisplay();

                    //#if DEBUG
                    if (ps.PublishGraph)
                        rot = new DsROTEntry(this.graphBuilder);
                    //#endif

                    IObjectWithSite grfSite = graphBuilder as IObjectWithSite;
                    if (grfSite != null)
                        grfSite.SetSite(new FilterBlocker(filename));

                    string fileExt = Path.GetExtension(filename).ToLower();

                    if (ps.PreferredDecoders != null)
                    {
                        foreach (string pa in ps.PreferredDecoders)
                        {
                            string[] pvA = pa.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                            if (pvA[0].ToLower() == fileExt)
                            {
                                for (int i = 1; i < pvA.Length; i++)
                                {
                                    string strFilter = pvA[i].Trim();
                                    IBaseFilter filter = null;
                                    try
                                    {
                                        if (Regex.IsMatch(strFilter, @"{?\w{8}-\w{4}-\w{4}-\w{4}-\w{12}}?"))
                                            filter = FilterGraphTools.AddFilterFromClsid(graphBuilder, new Guid(strFilter), strFilter);
                                        else
                                            filter = FilterGraphTools.AddFilterByName(graphBuilder, FilterCategory.LegacyAmFilterCategory, strFilter);

                                        if (filter != null)
                                        {
                                            FileLogger.Log("Added {0} to the graph", strFilter);
                                        }
                                        else
                                            FileLogger.Log("{0} not added to the graph", strFilter);
                                    }
                                    finally
                                    {
                                        if (filter != null)
                                            Marshal.ReleaseComObject(filter);
                                        filter = null;
                                    }
                                }
                            }
                        }
                    }
                    // Have the graph builder construct its the appropriate graph automatically
                    //hr = this.graphBuilder.RenderFile(filename, null);
                    if (ps.UseCustomAudioRenderer)
                    {
                        m_audioRendererClsid = new Guid(ps.CustomAudioRender);
                    }

                    audioRenderer = FilterGraphTools.AddFilterFromClsid(graphBuilder, m_audioRendererClsid, "Audio Renderer");
                    //IAVSyncClock wtf = audioRenderer as IAVSyncClock;
                    //double cap;
                    //hr = wtf.GetBias(out cap);
                    //IMPAudioSettings arSett = audioRenderer as IMPAudioSettings;
                    //if (arSett != null)
                    //{
                    //    AC3Encoding ac3Mode;
                    //    hr = arSett.GetAC3EncodingMode(out ac3Mode);
                    //    SpeakerConfig sc;
                    //    hr = arSett.GetSpeakerConfig(out sc);
                    //    AUDCLNT_SHAREMODE sm;
                    //    hr = arSett.GetWASAPIMode(out sm);
                    //    bool em;
                    //    hr = arSett.GetUseWASAPIEventMode(out em);
                    //    /*DeviceDefinition[] */IntPtr dc;
                    //    //int count;
                    //    //hr = arSett.GetAvailableAudioDevices(out dc, out count);
                    //    //DsError.ThrowExceptionForHR(hr);

                    //    ////DeviceDefinition[] dd = new DeviceDefinition[count];
                    //    //AudioDeviceDefinition dd = (AudioDeviceDefinition)Marshal.PtrToStructure(dc, typeof(AudioDeviceDefinition));
                    //    //if (dc != null)
                    //    //    Marshal.ReleaseComObject(dc);
                    //    hr = arSett.SetAudioDeviceById(null);
                    //    //arSett.SetSpeakerMatchOutput(true);

                    //    arSett.SetUseWASAPIEventMode(true);
                    //    arSett.SetUseFilters((int)MPARUseFilters.ALL);
                    //    arSett.SetAllowBitStreaming(true);
                    //    arSett.SetAC3EncodingMode(AC3Encoding.DISABLED);
                    //    arSett.SetUseTimeStretching(false);
                    //}

                    IMPAudioRendererConfig arSett = audioRenderer as IMPAudioRendererConfig;
                    if (arSett != null)
                    {
                        int ac3Mode;
                        hr = arSett.GetInt(MPARSetting.AC3_ENCODING, out ac3Mode);
                        int sc;
                        hr = arSett.GetInt(MPARSetting.SPEAKER_CONFIG, out sc);
                        int sm;
                        hr = arSett.GetInt(MPARSetting.WASAPI_MODE, out sm);
                        bool em;
                        hr = arSett.GetBool(MPARSetting.WASAPI_EVENT_DRIVEN, out em);
                        /*DeviceDefinition[] */
                        IntPtr dc;
                        //int count;
                        //hr = arSett.GetAvailableAudioDevices(out dc, out count);
                        //DsError.ThrowExceptionForHR(hr);

                        ////DeviceDefinition[] dd = new DeviceDefinition[count];
                        //AudioDeviceDefinition dd = (AudioDeviceDefinition)Marshal.PtrToStructure(dc, typeof(AudioDeviceDefinition));
                        //if (dc != null)
                        //    Marshal.ReleaseComObject(dc);
                        hr = arSett.SetString(MPARSetting.SETTING_AUDIO_DEVICE, ps.AudioPlaybackDevice);
                        //arSett.SetSpeakerMatchOutput(true);

                        arSett.SetBool(MPARSetting.WASAPI_EVENT_DRIVEN, true);
                        arSett.SetInt(MPARSetting.USE_FILTERS, (int)MPARUseFilters.ALL);
                        arSett.SetBool(MPARSetting.ALLOW_BITSTREAMING, true);
                        arSett.SetInt(MPARSetting.AC3_ENCODING, (int)AC3Encoding.DISABLED);
                        arSett.SetBool(MPARSetting.ENABLE_TIME_STRETCHING, false);
                    }
                    
                    //try
                    //{
                        hr = graphBuilder.AddSourceFilter(filename, "Source", out sourceFilter);
                        if (hr < 0)
                        {
                            //if it doesn't work before failing try to load it with the WMV reader
                            sourceFilter = (IBaseFilter)new WMAsfReader();
                            hr = graphBuilder.AddFilter(sourceFilter, "WM/ASF Reader");
                            DsError.ThrowExceptionForHR(hr);

                            hr = ((IFileSourceFilter)sourceFilter).Load(filename, null);
                            DsError.ThrowExceptionForHR(hr);

                            wmReader = sourceFilter as WindowsMediaLib.IWMReaderAdvanced2;
                        }

                        IPin outPin = DsFindPin.ByConnectionStatus(sourceFilter, PinConnectedStatus.Unconnected, 0);
                        while (outPin != null)
                        {
                            try
                            {
                                hr = graphBuilder.Render(outPin);
                                DsError.ThrowExceptionForHR(hr);
                            }
                            finally
                            {
                                if (outPin != null)
                                    Marshal.ReleaseComObject(outPin);
                                outPin = null;
                            }
                            outPin = DsFindPin.ByConnectionStatus(sourceFilter, PinConnectedStatus.Unconnected, 0);
                        }

                        if (ps.MultiChannelWMA)
                        {
                            FileLogger.Log("Set multichannel mode for WMA");

                            IBaseFilter wmaDec = FilterGraphTools.FindFilterByName(graphBuilder, "WMAudio Decoder DMO");
                            if (wmaDec != null)
                            {
                                try
                                {
                                    //http://msdn.microsoft.com/en-us/library/aa390550(VS.85).aspx
                                    IPropertyBag bag = wmaDec as IPropertyBag;
                                    if (bag != null)
                                    {
                                        object pVar;
                                        hr = bag.Read("_HIRESOUTPUT", out pVar, null);
                                        DsError.ThrowExceptionForHR(hr);
                                        bool bVar = (bool)pVar;
                                        FileLogger.Log("_HIRESOUTPUT = {0}", bVar);
                                        if (!bVar)
                                        {
                                            IPin wmaOut = DsFindPin.ByDirection(wmaDec, PinDirection.Output, 0);
                                            IPin cPin = null;

                                            try
                                            {
                                                hr = wmaOut.ConnectedTo(out cPin);
                                                DsError.ThrowExceptionForHR(hr);

                                                if (cPin != null) //cpin should never be null at this point, but lets be safe
                                                {
                                                    hr = wmaOut.Disconnect();
                                                    DsError.ThrowExceptionForHR(hr);
                                                
                                                    List<Guid> oldFilters = new List<Guid>();
                                                    IBaseFilter oFilt = FilterGraphTools.GetFilterFromPin(cPin);
                                                    try
                                                    {
                                                        while (oFilt != null)
                                                        {
                                                            IBaseFilter cFilter = null;

                                                            try
                                                            {
                                                                Guid clsid;
                                                                hr = oFilt.GetClassID(out clsid);
                                                                DsError.ThrowExceptionForHR(hr);

                                                                if (clsid != DSOUND_RENDERER)
                                                                {
                                                                    oldFilters.Add(clsid);
                                                                    cFilter = FilterGraphTools.GetConnectedFilter(oFilt, PinDirection.Output, 0);
                                                                }
                                                                hr = graphBuilder.RemoveFilter(oFilt);
                                                                DsError.ThrowExceptionForHR(hr);
                                                            }
                                                            finally
                                                            {
                                                                if (oFilt != null)
                                                                    Marshal.ReleaseComObject(oFilt);
                                                                oFilt = null;
                                                            }
                                                            oFilt = cFilter;
                                                        }
                                                    }
                                                    finally
                                                    {
                                                        if (oFilt != null)
                                                            Marshal.ReleaseComObject(oFilt);
                                                        oFilt = null;
                                                    }

                                                    foreach (Guid addFilt in oldFilters)
                                                    {
                                                        IBaseFilter addMe = FilterGraphTools.AddFilterFromClsid(graphBuilder, addFilt, addFilt.ToString());
                                                        if (addMe != null)
                                                            Marshal.ReleaseComObject(addMe);
                                                    }
                                                }

                                                pVar = true;
                                                hr = bag.Write("_HIRESOUTPUT", ref pVar);
                                                DsError.ThrowExceptionForHR(hr);

                                                hr = graphBuilder.Render(wmaOut);
                                                DsError.ThrowExceptionForHR(hr);

                                            }
                                            finally
                                            {
                                                if (wmaOut != null)
                                                    Marshal.ReleaseComObject(wmaOut);
                                                if (cPin != null)
                                                    Marshal.ReleaseComObject(cPin);
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    FileLogger.Log("Error setting multichannel mode for WMA: {0}", ex.Message);
                                }
                                finally
                                {
                                    while(Marshal.ReleaseComObject(wmaDec) > 0);
                                }
                            }
                        }
                    //}
                    //finally
                    //{
                    //    if (sourceFilter != null)
                    //        Marshal.ReleaseComObject(sourceFilter);
                    //}
                        if (ps.DXVAWMV)
                        {
                            FileLogger.Log("Set DXVA for WMV");
                            IBaseFilter wmvDec = FilterGraphTools.FindFilterByName(graphBuilder, "WMVideo Decoder DMO");
                            if (wmvDec != null)
                            {
                                try
                                {
                                    MediaFoundation.Misc.IPropertyStore config = wmvDec as MediaFoundation.Misc.IPropertyStore;

                                    if (config != null)
                                    {
                                        MediaFoundation.Misc.PropVariant pv = new MediaFoundation.Misc.PropVariant();
                                        //config.GetValue(MediaFoundation.Misc.WMVConst.MFPKEY_DXVA_ENABLED, pv);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    FileLogger.Log("Error setting DXVA mode for WMV: {0}", ex.Message);
                                }
                                finally
                                {
                                    while (Marshal.ReleaseComObject(wmvDec) > 0) ;
                                }
                            }
                        }

                    SetEvrVideoMode();

                    // QueryInterface for DirectShow interfaces
                    this.mediaControl = (IMediaControl)this.graphBuilder;
                    this.mediaEventEx = (IMediaEventEx)this.graphBuilder;
                    this.mediaSeeking = (IMediaSeeking)this.graphBuilder;
                    this.mediaPosition = (IMediaPosition)this.graphBuilder;

                    // Query for video interfaces, which may not be relevant for audio files
                    //this.videoWindow = this.graphBuilder as IVideoWindow;
                    //this.basicVideo = this.graphBuilder as IBasicVideo;

                    // Query for audio interfaces, which may not be relevant for video-only files
                    this.basicAudio = this.graphBuilder as IBasicAudio;

                    // Is this an audio-only file (no video component)?
                    CheckVisibility();

                    // Have the graph signal event via window callbacks for performance
                    hr = this.mediaEventEx.SetNotifyWindow(this.Handle, WM.GRAPH_NOTIFY, IntPtr.Zero);
                    DsError.ThrowExceptionForHR(hr);

                    if (!this.isAudioOnly)
                    {
                        // Setup the video window
                        //hr = this.videoWindow.put_Owner(this.Handle);
                        //DsError.ThrowExceptionForHR(hr);
                        //this.evrDisplay.SetVideoWindow(this.Handle);

                        //hr = this.videoWindow.put_WindowStyle(WindowStyle.Child | WindowStyle.ClipSiblings | WindowStyle.ClipChildren);
                        //DsError.ThrowExceptionForHR(hr);

                        hr = InitVideoWindow();//1, 1);
                        DsError.ThrowExceptionForHR(hr);

                        GetFrameStepInterface();
                    }
                    else
                    {
                        // Initialize the default player size and enable playback menu items
                        hr = InitPlayerWindow();
                        DsError.ThrowExceptionForHR(hr);

                        EnablePlaybackMenu(true, MediaType.Audio);
                    }

                    // Complete window initialization
                    //CheckSizeMenu(menuFileSizeNormal);
                    //this.isFullScreen = false;
                    this.currentPlaybackRate = 1.0;
                    UpdateMainTitle();

                    this.Activate();

                    //pre-roll the graph
                    hr = this.mediaControl.Pause();
                    DsError.ThrowExceptionForHR(hr);

                    if (wmReader != null)
                    {
                        WindowsMediaLib.PlayMode pMode;

                        hr = wmReader.GetPlayMode(out pMode);
                        DsError.ThrowExceptionForHR(hr);
                        
                        if (pMode == WindowsMediaLib.PlayMode.Streaming)
                        {
                            int pdwPercent = 0;
                            long pcnsBuffering;

                            while (pdwPercent < 100)
                            {
                                hr = wmReader.GetBufferProgress(out pdwPercent, out pcnsBuffering);
                                DsError.ThrowExceptionForHR(hr);

                                if (pdwPercent >= 100)
                                    break;

                                int sleepFor = Convert.ToInt32(pcnsBuffering / 1000);
                                Thread.Sleep(100);
                            }
                        }
                    }

                    // Run the graph to play the media file
                    hr = this.mediaControl.Run();
                    DsError.ThrowExceptionForHR(hr);

                    if (commWatcher != null)
                        commWatcher.Dispose();

                    string commPath = string.Empty;

                    if (ps.UseDtbXml)
                    {
                        commWatcher = new FileSystemWatcher(Commercials.XmlDirectory, Commercials.GetXmlFilename(filename));
                        commPath = Path.Combine(Commercials.XmlDirectory, Commercials.GetXmlFilename(filename));
                    }
                    else
                    {
                        commWatcher = new FileSystemWatcher(Path.GetDirectoryName(filename), Commercials.GetEdlFilename(filename));
                        commPath = Path.Combine(Path.GetDirectoryName(filename), Commercials.GetEdlFilename(filename));
                    }

                    ReadComm(commPath);

                    commWatcher.Changed += new FileSystemEventHandler(commWatcher_Changed);
                    commWatcher.Created += new FileSystemEventHandler(commWatcher_Changed);
                    //commWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
                    commWatcher.EnableRaisingEvents = true;

                    MoveToBookmark();

                    this.currentState = PlayState.Running;
                    if (isFullScreen)
                        tmMouseMove.Enabled = true;
                }
                else
                {
                    //MessageBox.Show("EVR cannot be loaded on this PC");
                    using (EPDialog ed = new EPDialog())
                        ed.ShowDialog("Error", "The Enhanced Video Renderer cannot be loaded", 20, this);
                }
            }
            finally
            {
                //if (wmReader != null)
                //    Marshal.ReleaseComObject(wmReader);
                if (sourceFilter != null)
                    while(Marshal.ReleaseComObject(sourceFilter)>0);
            }
        }

        private void SetupEvrDisplay()
        {
            int hr=0;
            //Guid presenterCLSID = new Guid(0xeb449d36, 0x4, 0x4ea8, 0x90, 0x74, 0x40, 0xc5, 0xf4, 0x94, 0xb5, 0xe4);
            //Guid presenterCLSID = new Guid(0x9707fc9c, 0x807b, 0x41e3, 0x98, 0xa8, 0x75, 0x17, 0x6f, 0x95, 0xa0, 0x62);
            //Guid presenterCLSID = new Guid("29FAB022-F7CC-4819-B2B8-D9B6BCFB6698");
            IMFGetService mfgs = evrRenderer as IMFGetService;
            if (mfgs != null)
            {
                IMFVideoPresenter pPresenter = null;

                try
                {
                    if (ps.CustomPresenterEnabled)
                    {
                        Guid presenterCLSID = new Guid(ps.CustomPresenter);
     
                        IMFVideoRenderer pRenderer = evrRenderer as IMFVideoRenderer;
                        Type type = Type.GetTypeFromCLSID(presenterCLSID);

                        pPresenter = (IMFVideoPresenter)Activator.CreateInstance(type);

                        if (pPresenter != null)
                        {
                            try
                            {
                                pRenderer.InitializeRenderer(null, pPresenter);
                                cpsett = pPresenter as IEVRCPConfig;
                                if (cpsett != null)
                                {
                                    int range;
                                    float alpha;
                                    bool mftime;
                                    hr = cpsett.GetInt(EVRCPSetting.NOMINAL_RANGE, out range);
                                    hr = cpsett.SetInt(EVRCPSetting.NOMINAL_RANGE, range);
                                    hr = cpsett.GetFloat(EVRCPSetting.SUBTITLE_ALPHA, out alpha);
                                    hr = cpsett.SetFloat(EVRCPSetting.SUBTITLE_ALPHA, alpha);
                                    hr = cpsett.GetBool(EVRCPSetting.USE_MF_TIME_CALC, out mftime);
                                    hr = cpsett.SetBool(EVRCPSetting.USE_MF_TIME_CALC, mftime);
                                    hr = cpsett.GetInt(EVRCPSetting.FRAME_DROP_THRESHOLD, out range);
                                    hr = cpsett.SetInt(EVRCPSetting.FRAME_DROP_THRESHOLD, range+1);
                                }
                                
                            }
                            finally
                            {
                                if (pPresenter != null && cpsett == null)
                                    Marshal.ReleaseComObject(pPresenter);
                            }
                        }
                    }

                    //object objStateSink = null;
                    //mfgs.GetService(MFServices.MR_VIDEO_RENDER_SERVICE,
                    //    typeof(IMFClockStateSink).GUID,
                    //    out objStateSink);
                    //stateSink = objStateSink as IMFClockStateSink;

                    //object objRateSupp= null;
                    //mfgs.GetService(MFServices.MR_VIDEO_RENDER_SERVICE,
                    //    typeof(IMFRateSupport).GUID,
                    //    out objRateSupp);
                    //rateSupport = objRateSupp as IMFRateSupport;

                    object objMixBmp = null;
                    mfgs.GetService(MFServices.MR_VIDEO_MIXER_SERVICE,
                        typeof(IMFVideoMixerBitmap).GUID,
                        out objMixBmp);
                    mixBmp = objMixBmp as IMFVideoMixerBitmap;

                    object objDisplay = null;
                    mfgs.GetService(MFServices.MR_VIDEO_RENDER_SERVICE,
                        typeof(IMFVideoDisplayControl).GUID,
                        out objDisplay
                        );
                    FileLogger.Log("PlayMovieInWindow: MR_VIDEO_RENDER_SERVICE");
                    evrDisplay = objDisplay as IMFVideoDisplayControl;
                    this.evrDisplay.SetVideoWindow(this.Handle);

                    MediaFoundation.Misc.MFSize videoSize = new MediaFoundation.Misc.MFSize();
                    MediaFoundation.Misc.MFSize ar = new MediaFoundation.Misc.MFSize();
                    //evrDisplay.GetNativeVideoSize(videoSize, ar);

                    if ((videoSize.cx == 0 && videoSize.cy == 0) || videoSize.cx <= 0)
                    {
                        IEVRFilterConfig evrConfig = evrRenderer as IEVRFilterConfig;
                        int pdwMaxStreams;

                        evrConfig.GetNumberOfStreams(out pdwMaxStreams);
                        FileLogger.Log("NumberOfStreams: {0}", pdwMaxStreams);

                        if (pdwMaxStreams < 1)
                        {
                            evrConfig.SetNumberOfStreams(1);
                            FileLogger.Log("Set NumberOfStreams: {0}", 1);
                        }
                    }

                    //object objMemConfig = null;
                    //mfgs.GetService(MFServices.MR_VIDEO_ACCELERATION_SERVICE, typeof(IDirectXVideoMemoryConfiguration).GUID, out objMemConfig);
                }
                catch (InvalidCastException)
                {
                    //do nothing
                }
            }
        }

        private void SetEvrVideoMode()
        {
            object objVideoProc = null;
            IMFGetService mfgs = evrRenderer as IMFGetService;
            if (mfgs != null)
            {
                try
                {
                    mfgs.GetService(MFServices.MR_VIDEO_MIXER_SERVICE,
                        typeof(IMFVideoProcessor).GUID,
                        out objVideoProc
                        );
                    IMFVideoProcessor evrProc = objVideoProc as IMFVideoProcessor;
                    int dModes;
                    IntPtr ppModes = IntPtr.Zero;
                    Guid lpMode = Guid.Empty;
                    Guid bestMode = Guid.Empty;
                    evrProc.GetVideoProcessorMode(out lpMode);
                    List<Guid> vpModes = new List<Guid>();

                    if (!string.IsNullOrEmpty(ps.VideoProcessorMode))
                    {
                        bestMode = new Guid(ps.VideoProcessorMode);
                        FileLogger.Log("Set ProcessorMode: {0} VideoProcessorMode: {1}", lpMode, bestMode);
                        if (bestMode.CompareTo(lpMode) != 0)
                            evrProc.SetVideoProcessorMode(ref bestMode);

                        evrProc.GetVideoProcessorMode(out lpMode);
                        FileLogger.Log("Current ProcessorMode: {0} VideoProcessorMode: {1}", lpMode, bestMode);

                    }
                    else
                    {

                        try
                        {
                            evrProc.GetAvailableVideoProcessorModes(out dModes, out ppModes);
                            if (dModes > 0)
                            {
                                for (int i = 0; i < dModes; i++)
                                {
                                    int offSet = Marshal.SizeOf(Guid.Empty) * i;
                                    Guid vpMode = (Guid)Marshal.PtrToStructure(((IntPtr)((int)ppModes + offSet)), typeof(Guid));
                                    vpModes.Add(vpMode);
                                    FileLogger.Log("VideoMode Found: {0}", vpMode);
                                }
                            }
                        }
                        finally
                        {
                            if (ppModes != IntPtr.Zero)
                                Marshal.FreeCoTaskMem(ppModes);
                        }
                        bestMode = vpModes[0];
                        FileLogger.Log("Set ProcessorMode: {0} BestMode: {1}", lpMode, bestMode);
                        if (vpModes.Count > 0 && lpMode.CompareTo(bestMode) != 0)
                            evrProc.SetVideoProcessorMode(ref bestMode);

                        evrProc.GetVideoProcessorMode(out lpMode);
                        FileLogger.Log("Current ProcessorMode: {0} BestMode: {1}", lpMode, bestMode);
                    }
                }
                finally
                {
                    if (objVideoProc != null)
                        Marshal.ReleaseComObject(objVideoProc);
                }
            }
        }

        void MoveToBookmark()
        {
            try
            {
                if (File.Exists(BookmarkFile) && ps.UseBookmarks)
                {
                    if (dvdGraph == null)
                    {
                        using (StreamReader sr = File.OpenText(BookmarkFile))
                        {
                            string strPos = sr.ReadLine();
                            try
                            {
                                FileLogger.Log("MoveToBookmark: {0} - {1}", BookmarkFile, strPos);

                                currentPosition = Convert.ToInt64(strPos);
                                ChangePosition(currentPosition, AMSeekingSeekingFlags.AbsolutePositioning);
                            }
                            catch (Exception ex)
                            {
                                FileLogger.Log("MoveToBookmark error: {0}", ex.Message);
                            }
                        }
                    }
                    else
                    {
                        if (dvdCtrl != null)
                        {
                            IStorage storage = null;
                            IStream stream = null;
                            int hr = 0;
                            IDvdState pStateData = null;
                            IDvdCmd dCmd = null;

                            try
                            {
                                if (NativeMethods.StgIsStorageFile(BookmarkFile) != 0)
                                    throw new ArgumentException();

                                hr = NativeMethods.StgOpenStorage(
                                    BookmarkFile,
                                    null,
                                    STGM.Read | STGM.ShareExclusive,
                                    IntPtr.Zero,
                                    0,
                                    out storage
                                    );

                                Marshal.ThrowExceptionForHR(hr);

                                storage.OpenStream(
                                    @"EVRPlayDvdBookmark",
                                    IntPtr.Zero,
                                    (STGM.Read | STGM.ShareExclusive),
                                    0,
                                out stream
                                    );

                                Marshal.ThrowExceptionForHR(hr);
                                Guid stateGuid = new Guid("86303d6d-1c4a-4087-ab42-f711167048ef");
                                object state;

                                hr = NativeMethods2.OleLoadFromStream(stream, ref stateGuid, out state);
                                Marshal.ThrowExceptionForHR(hr);

                                pStateData = (IDvdState)state;

                                hr = dvdCtrl.SetState(pStateData, DvdCmdFlags.Block, out dCmd);
                                DsError.ThrowExceptionForHR(hr);
                            }
                            finally
                            {
                                if (stream != null)
                                    Marshal.ReleaseComObject(stream);
                                if (storage != null)
                                    Marshal.ReleaseComObject(storage);
                                if (pStateData != null)
                                    Marshal.ReleaseComObject(pStateData);
                                if (dCmd != null)
                                    Marshal.ReleaseComObject(dCmd);
                            }
                        }
                        //hr = (pStateData as IPersistStream).Load(stream);
                        //Marshal.ThrowExceptionForHR(hr);
                    }
                }
            }
            catch (Exception ex)
            {
                using (EPDialog ed = new EPDialog())
                    ed.ShowDialog("Bookmark Error", ex.Message, 10, this);
            }
        }

        void SaveBookmark()
        {
            if (dvdGraph == null)
            {
                FileLogger.Log("SaveBookmark: {0} position: {1} duration: {2}", BookmarkFile, currentPosition, fileDuration);

                if (fileDuration - currentPosition < 1 * TICK_MULT)
                {
                    if (File.Exists(BookmarkFile))
                        File.Delete(BookmarkFile);
                }
                else
                {
                    using (StreamWriter sw = new StreamWriter(BookmarkFile, false))
                        sw.WriteLine(currentPosition);
                }
            }
            else
            {
                if (dvdInfo != null)
                {
                    IDvdState pStateData = null;

                    try
                    {
                        int hr = dvdInfo.GetState(out pStateData);
                        DsError.ThrowExceptionForHR(hr);

                        IStorage storage = null;
                        IStream stream = null;

                        if (File.Exists(BookmarkFile))
                            File.Delete(BookmarkFile);

                        try
                        {
                            hr = NativeMethods.StgCreateDocfile(
                        BookmarkFile,
                        STGM.Create | STGM.Write | STGM.ShareExclusive,
                        0,
                        out storage
                        );

                            Marshal.ThrowExceptionForHR(hr);

                            storage.CreateStream(
                                @"EVRPlayDvdBookmark",
                                (STGM.Write | STGM.Create | STGM.ShareExclusive),
                                0,
                                0,
                        out stream
                                );

                            Marshal.ThrowExceptionForHR(hr);

                            hr = NativeMethods2.OleSaveToStream((IPersistStream)pStateData, stream);
                            Marshal.ThrowExceptionForHR(hr);
                        }
                        finally
                        {
                            if (stream != null)
                                Marshal.ReleaseComObject(stream);
                            if (storage != null)
                                Marshal.ReleaseComObject(storage);
                        }
                    }
                    finally
                    {
                        if (pStateData != null)
                            Marshal.ReleaseComObject(pStateData);
                    }
                }
            }
        }

        void commWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            //throw new Exception("The method or operation is not implemented.");
            ThreadPool.QueueUserWorkItem(new WaitCallback(ReadComm), e.FullPath);
        }

        private void ReadComm(object objEdlPath)
        {
            if (this.InvokeRequired)
                this.Invoke(new TFS(ReadComm), new object[] { objEdlPath });
            else
            {
                string edlPath = (string)objEdlPath;
                try
                {
                    if (File.Exists(edlPath))
                    {
                        if (Path.GetExtension(edlPath).ToLower() == ".edl")
                        {
                            FileLogger.Log("Edl file exists: {0}", edlPath);
                            commList = Commercials.ReadEdlCommercials(edlPath);
                        } 
                        else if (Path.GetExtension(edlPath).ToLower() == ".xml")
                        {
                            FileLogger.Log("xml file exists: {0}", edlPath);
                            commList = Commercials.ReadCommercials(edlPath);
                        }
                        if (!tmPosition.Enabled)
                            tmPosition.Enabled = true;
                    }
                }
                catch (Exception ex)
                {
                    FileLogger.Log("Error reading commmerical segments: {0}", ex.ToString());
                }
            }
        }

        private void CloseClip()
        {
            int hr = 0;

            // Stop media playback
            if (this.mediaControl != null)
                hr = this.mediaControl.Stop();

            //StopClip(null);

            // Clear global flags
            this.currentState = PlayState.Stopped;
            this.isAudioOnly = true;
            //this.isFullScreen = false;
            this.currentPosition = 0;
            fileDuration = 0;
            pf.Value = 0;

            if (bf != null)
                bf.Show();
            else if (mb != null)
                mb.Show();
            //wbSageServer.Visible = true;

            // Free DirectShow interfaces
            CloseInterfaces();

            // Clear file name to allow selection of new file with open dialog
            //this.filename = string.Empty;
            filename.Clear();

            // No current media state
            this.currentState = PlayState.Init;

            tmStatus.Enabled = false;
            tmMouseMove.Enabled = false;
            tmPosition.Enabled = false;

            UpdateMainTitle();
            InitPlayerWindow();

            this.BackColor = System.Drawing.Color.Black; 

            ToogleControls(true);

            if (disabledScreenSaver)
                NativeMethods2.SystemParametersInfo(SPI.SPI_SETSCREENSAVEACTIVE,
                      Convert.ToUInt32(true),      // TRUE to enable
                      IntPtr.Zero,
                      SPIF.SPIF_SENDWININICHANGE
                     ); 
        }

        private int InitVideoWindow()//int nMultiplier, int nDivider)
        {
            int hr = 0;
            //int lHeight = ClientRectangle.Height, lWidth = ClientRectangle.Width;

            //if (this.basicVideo == null)
            //    return 0;

            //// Read the default video size
            //hr = this.basicVideo.GetVideoSize(out lWidth, out lHeight);
            //if (hr == DsResults.E_NoInterface)
            //    return 0;

            MediaFoundation.Misc.MFSize videoSize = new MediaFoundation.Misc.MFSize();
            MediaFoundation.Misc.MFSize ar = new MediaFoundation.Misc.MFSize();
            evrDisplay.GetNativeVideoSize(videoSize, ar);

            //if (videoSize.cx > 0 && videoSize.cy > 0)
            //{
            //    return;
            //}
            //lWidth = videoSize.cx;
            //lHeight = videoSize.cy;

            EnablePlaybackMenu(true, MediaType.Video);

            //if (!this.isFullScreen && ps.MaxInitialHeight > 0 && ps.MaxInitialWidth > 0)
            //{
            //    if ((lWidth > ps.MaxInitialWidth || lHeight > ps.MaxInitialHeight) && (lWidth > ClientRectangle.Width || lHeight > ClientRectangle.Height))
            //    {
            //        lWidth = ps.MaxInitialWidth;
            //        lHeight = ps.MaxInitialHeight;
            //    }
            //    //if (lHeight > ps.MaxInitialHeight && lHeight > ClientRectangle.Height)
            //    //    lHeight = ps.MaxInitialHeight;
            //}
            //else
            //{
            //    lWidth = ClientRectangle.Width;
            //    lHeight = ClientRectangle.Height;
            //}

            //// Account for requests of normal, half, or double size
            //lWidth = lWidth * nMultiplier / nDivider;
            //lHeight = lHeight * nMultiplier / nDivider;

            //this.ClientSize = new Size(lWidth, lHeight);
            Application.DoEvents();

            //hr = this.videoWindow.SetWindowPosition(0, 0, lWidth, lHeight);
            //MFVideoNormalizedRect sRect = new MFVideoNormalizedRect();
            //sRect.top = 0;
            //sRect.left = 0;
            //sRect.right = 1;
            //sRect.bottom = 1;
            //MediaFoundation.Misc.MFRect dRect = new MediaFoundation.Misc.MFRect();
            //dRect.top = 0-(ps.OverscanHeight/2);
            //dRect.left = 0 - (ps.OverscanWidth / 2);
            //dRect.right = lWidth + (ps.OverscanWidth / 2);//this.Width;
            //dRect.bottom = lHeight + (ps.OverscanHeight / 2);//this.Height;
            //this.evrDisplay.SetVideoPosition(sRect, dRect);
            MoveVideoWindow();

            if (bf != null)
                bf.Hide();

            if (mb != null)
                mb.Hide();

            //wbSageServer.Visible = false;

            MFVideoAspectRatioMode varm;

            this.evrDisplay.GetAspectRatioMode(out varm);

            if (varm != (MFVideoAspectRatioMode)ps.VideoAspectRatioMode)
                evrDisplay.SetAspectRatioMode((MFVideoAspectRatioMode)ps.VideoAspectRatioMode);

            MFVideoRenderPrefs renderingFlags;

            evrDisplay.GetRenderingPrefs(out renderingFlags);

            if (renderingFlags != (MFVideoRenderPrefs)ps.VideoRenderingPrefs)
                evrDisplay.SetRenderingPrefs((MFVideoRenderPrefs)ps.VideoRenderingPrefs);

            int pClr;

            evrDisplay.GetBorderColor(out pClr);

            if (pClr != ps.BorderColor)
                evrDisplay.SetBorderColor(ps.BorderColor);

            return hr;
        }

        enum dvstype
        {
            HALF,
            NORMAL,
            DOUBLE,
            STRETCH,
            FROMINSIDE,
            FROMOUTSIDE,
            ZOOM1,
            ZOOM2
        };

        dvstype iVideoScaling = dvstype.FROMINSIDE;

        private void MoveVideoWindow()
        {
            int hr = 0;
            try
            {
                // Track the movement of the container window and resize as needed
                if (this.evrDisplay != null)
                {
                    MFVideoNormalizedRect sRect = new MFVideoNormalizedRect();
                    sRect.top = 0;
                    sRect.left = 0;
                    sRect.right = 1;
                    sRect.bottom = 1;
                    MediaFoundation.Misc.MFRect dRect = new MediaFoundation.Misc.MFRect();
                    MFSize vSize = new MFSize(), vAR = new MFSize();
                    double m_ZoomX = 1, m_ZoomY = 1, m_PosX = 0.5, m_PosY = 0.5;
                    //dRect.top = 0;
                    //dRect.left = 0;
                    //dRect.right = ClientRectangle.Width;//this.Width;
                    //dRect.bottom = ClientRectangle.Height;//this.Height;
                    //dRect.top = 0 - (ps.OverscanHeight / 2);
                    //dRect.left = 0 - (ps.OverscanWidth / 2);
                    //dRect.right = ClientRectangle.Width + (ps.OverscanWidth / 2);//this.Width;
                    //dRect.bottom = ClientRectangle.Height + (ps.OverscanHeight / 2);//this.Height;

                    hr = evrDisplay.GetNativeVideoSize(vSize, vAR);
                    DsError.ThrowExceptionForHR(hr);

                    double dVideoAR = (double)vSize.Width / vSize.Height;

                    double dWRWidth = ClientRectangle.Width;
                    double dWRHeight = ClientRectangle.Height;

                    double dVRWidth = dWRHeight * dVideoAR;
                    double dVRHeight;

                    switch (iVideoScaling)
                    {
                        case dvstype.HALF:
                            dVRWidth = vSize.Width * 0.5;
                            dVRHeight = vSize.Height * 0.5;
                            break;
                        case dvstype.NORMAL:
                            dVRWidth = vSize.Width;
                            dVRHeight = vSize.Height;
                            break;
                        case dvstype.DOUBLE:
                            dVRWidth = vSize.Width * 2.0;
                            dVRHeight = vSize.Height * 2.0;
                            break;
                        case dvstype.STRETCH:
                            dVRWidth = dWRWidth;
                            dVRHeight = dWRHeight;
                            break;
                        default:
                        //ASSERT(FALSE);
                        // Fallback to "Touch Window From Inside" if settings were corrupted.
                        case dvstype.FROMINSIDE:
                        case dvstype.FROMOUTSIDE:
                            if ((ClientRectangle.Width < dVRWidth) != (iVideoScaling == dvstype.FROMOUTSIDE))
                            {
                                dVRWidth = dWRWidth;
                                dVRHeight = dVRWidth / dVideoAR;
                            }
                            else
                            {
                                dVRHeight = dWRHeight;
                            }
                            break;
                        case dvstype.ZOOM1:
                        case dvstype.ZOOM2:
                            {
                                double minw = dWRWidth < dVRWidth ? dWRWidth : dVRWidth;
                                double maxw = dWRWidth > dVRWidth ? dWRWidth : dVRWidth;

                                double scale = iVideoScaling == dvstype.ZOOM1 ? 1.0 / 3.0 : 2.0 / 3.0;
                                dVRWidth = minw + (maxw - minw) * scale;
                                dVRHeight = dVRWidth / dVideoAR;
                                break;
                            }
                    }

                    // Scale video frame
                    double dScaledVRWidth = m_ZoomX * dVRWidth;
                    double dScaledVRHeight = m_ZoomY * dVRHeight;

                    // Position video frame
                    // left and top parts are allowed to be negative
                    dRect.left = (int)Math.Round(m_PosX * (dWRWidth * 3.0 - dScaledVRWidth) - dWRWidth);
                    dRect.top = (int)Math.Round(m_PosY * (dWRHeight * 3.0 - dScaledVRHeight) - dWRHeight);
                    // right and bottom parts are always at picture center or beyond, so never negative
                    dRect.right = (int)Math.Round(dRect.left + dScaledVRWidth);
                    dRect.bottom = (int)Math.Round(dRect.top + dScaledVRHeight);

                    //apply overscan
                    dRect.top = dRect.top - (ps.OverscanHeight / 2);
                    dRect.left = dRect.left - (ps.OverscanWidth / 2);
                    dRect.right = dRect.right + (ps.OverscanWidth / 2);//this.Width;
                    dRect.bottom = dRect.bottom + (ps.OverscanHeight / 2);//this.Height;

                    this.evrDisplay.SetVideoPosition(sRect, dRect);
                    Debug.Print("t: {0} l: {1} r:{2} b:{3}", dRect.top, dRect.left, dRect.right, dRect.bottom);
                }
            }
            catch (Exception ex)
            {
                FileLogger.Log("MoveVideoWindow Error: {0}", ex.Message);
            }
        }

        private void CheckVisibility()
        {
            //int hr = 0;
            //OABool lVisible;

            if (this.evrRenderer == null)//(this.videoWindow == null) || (this.basicVideo == null))
            {
                // Audio-only files have no video interfaces.  This might also
                // be a file whose video component uses an unknown video codec.
                this.isAudioOnly = true;
                return;
            }
            else
            {
                // Clear the global flag
                this.isAudioOnly = false;
            }

            MediaFoundation.Misc.MFSize videoSize = new MediaFoundation.Misc.MFSize();
            MediaFoundation.Misc.MFSize ar = new MediaFoundation.Misc.MFSize();
            evrDisplay.GetNativeVideoSize(videoSize, ar);

            if (videoSize.cx > 0 && videoSize.cy > 0)
            {
                return;
            }
            else
            {
                this.isAudioOnly = true;
            }

            //hr = this.videoWindow.get_Visible(out lVisible);
            //if (hr < 0)
            //{
            //    // If this is an audio-only clip, get_Visible() won't work.
            //    //
            //    // Also, if this video is encoded with an unsupported codec,
            //    // we won't see any video, although the audio will work if it is
            //    // of a supported format.
            //    if (hr == unchecked((int)0x80004002)) //E_NOINTERFACE
            //    {
            //        this.isAudioOnly = true;
            //    }
            //    else
            //        DsError.ThrowExceptionForHR(hr);
            //}
        }

        //
        // Some video renderers support stepping media frame by frame with the
        // IVideoFrameStep interface.  See the interface documentation for more
        // details on frame stepping.
        //
        private bool GetFrameStepInterface()
        {
            int hr = 0;

            IVideoFrameStep frameStepTest = null;

            // Get the frame step interface, if supported
            frameStepTest = (IVideoFrameStep)this.graphBuilder;

            // Check if this decoder can step
            hr = frameStepTest.CanStep(0, null);
            if (hr == 0)
            {
                this.frameStep = frameStepTest;
                return true;
            }
            else
            {
                // BUG 1560263 found by husakm (thanks)...
                // Marshal.ReleaseComObject(frameStepTest);
                this.frameStep = null;
                return false;
            }
        }

        private void CloseInterfaces()
        {
            int hr = 0;

            try
            {
                lock (this)
                {
                    // Relinquish ownership (IMPORTANT!) after hiding video window
                    //if (!this.isAudioOnly)
                    //{
                    //    hr = this.videoWindow.put_Visible(OABool.False);
                    //    DsError.ThrowExceptionForHR(hr);
                    //    hr = this.videoWindow.put_Owner(IntPtr.Zero);
                    //    DsError.ThrowExceptionForHR(hr);
                    //}

                    //#if DEBUG
                    try
                    {
                        if (rot != null)
                        {
                            rot.Dispose();
                            rot = null;
                        }
                    } catch {}
                    //#endif

                    if (evrDisplay != null)
                    {
                        //evrDisplay.SetVideoWindow(IntPtr.Zero);
                        Marshal.ReleaseComObject(evrDisplay);
                    }
                    evrDisplay = null;

                    //if (rateSupport != null)
                    //{
                    //    //evrDisplay.SetVideoWindow(IntPtr.Zero);
                    //    Marshal.ReleaseComObject(rateSupport);
                    //}
                    //rateSupport = null;

                    //if (stateSink != null)
                    //{
                    //    //evrDisplay.SetVideoWindow(IntPtr.Zero);
                    //    Marshal.ReleaseComObject(stateSink);
                    //}
                    //stateSink = null;

                    if (mixBmp != null)
                        Marshal.ReleaseComObject(mixBmp);
                    mixBmp = null;

                    if (this.evrRenderer != null)
                        Marshal.ReleaseComObject(evrRenderer);
                    evrRenderer = null;

                    if (cpsett != null)
                        Marshal.ReleaseComObject(cpsett);
                    cpsett = null;

                    if (dvdSubtitle != null)
                        Marshal.ReleaseComObject(dvdSubtitle);
                    dvdSubtitle = null;

                    if (dvdCtrl != null)
                    {
                        hr = dvdCtrl.SetOption(DvdOptionFlag.ResetOnStop, true);
                    }

                    if (cmdOption != null)
                    {
                        Marshal.ReleaseComObject(cmdOption);
                        cmdOption = null;
                    }

                    pendingCmd = false;

                    dvdCtrl = null;
                    if (dvdInfo != null)
                    {
                        Marshal.ReleaseComObject(dvdInfo);
                        dvdInfo = null;
                    }

                    if (this.mediaEventEx != null)
                    {
                        if(dvdGraph != null)
                            hr = this.mediaEventEx.SetNotifyWindow(IntPtr.Zero, WM.DVD_EVENT, IntPtr.Zero);
                        else
                            hr = this.mediaEventEx.SetNotifyWindow(IntPtr.Zero, WM.NULL, IntPtr.Zero);
                        //DsError.ThrowExceptionForHR(hr);
                    }

                    

                    if (audioRenderer != null)
                        Marshal.ReleaseComObject(audioRenderer);
                    audioRenderer = null;

                    // Release and zero DirectShow interfaces
                    if (this.mediaEventEx != null)
                        this.mediaEventEx = null;
                    if (this.mediaSeeking != null)
                        this.mediaSeeking = null;
                    if (this.mediaPosition != null)
                        this.mediaPosition = null;
                    if (this.mediaControl != null)
                        this.mediaControl = null;
                    if (this.basicAudio != null)
                        this.basicAudio = null;
                    if (this.basicVideo != null)
                        this.basicVideo = null;
                    //if (this.videoWindow != null)
                    //    this.videoWindow = null;
                    if (this.frameStep != null)
                        this.frameStep = null;
                    if (this.graphBuilder != null)
                        Marshal.ReleaseComObject(this.graphBuilder); this.graphBuilder = null;
                    if (this.dvdGraph != null)
                        Marshal.ReleaseComObject(dvdGraph); dvdGraph = null;

                    GC.Collect();
                }
            }
            catch
            {
            }
        }

        /*
         * Media Related methods
         */

        private void PauseClip(object notUsed)
        {
            if (this.InvokeRequired)
                this.Invoke(new TFS(PauseClip), new object[] { null });
            else
            {
                if (this.mediaControl == null)
                    return;
                                
                // Toggle play/pause behavior
                if (dvdPlayRate > 1)
                {
                    SetRate(1);
                }
                else if ((this.currentState == PlayState.Paused) || (this.currentState == PlayState.Stopped))
                {
                    if (this.mediaControl.Run() >= 0)
                        this.currentState = PlayState.Running;
                }
                else
                {
                    if (this.mediaControl.Pause() >= 0)
                        this.currentState = PlayState.Paused;
                }

                if (this.isFullScreen)
                {
                    ToogleProgress(true);
                    lastMove = DateTime.Now;
                    tmMouseMove.Enabled = true;
                }

                UpdateMainTitle();
            }
        }

        private void StopClip(object notUsed)
        {
            if (this.InvokeRequired)
                this.Invoke(new TFS(StopClip), new object[] { null });
            else
            {
                int hr = 0;
                DsLong pos = new DsLong(0);
                dvdPlayRate = 0.0;
                tmPosition.Enabled = false;
                commList = new List<double[]>();

                if ((this.mediaControl == null) || (this.mediaSeeking == null))
                    return;
                // Stop and reset postion to beginning
                if ((this.currentState == PlayState.Paused) || (this.currentState == PlayState.Running))
                {
                    hr = this.mediaControl.Pause();

                    SaveBookmark();

                    hr = this.mediaControl.Stop();
                    this.currentState = PlayState.Stopped;

                    // Seek to the beginning
                    //hr = this.mediaSeeking.SetPositions(pos, AMSeekingSeekingFlags.AbsolutePositioning, null, AMSeekingSeekingFlags.NoPositioning);
                    //hr = this.mediaControl.Run();
                    //Thread.Sleep(100);
                    hr = this.mediaControl.Pause();
                }
                //else if (this.currentState == PlayState.Stopped && wbSageServer.Url.ToString() != ABOUT_BLANK)
                //    wbSageServer.Visible = true;

                UpdateMainTitle();

                StopDialog sd = new StopDialog();
                switch (sd.ShowDialog())
                {
                    case DialogResult.OK:
                        PauseClip(null);
                        break;
                    case DialogResult.Cancel:
                        CloseClip();
                        //ToogleControls(true);
                        Application.DoEvents();
                        
                        //if (wbSageServer.Url.ToString() != ABOUT_BLANK)
                        //    wbSageServer.Visible = true;
                        //else
                        //{
                        //    wbSageServer.Navigate(ABOUT_BLANK);
                        //    wbSageServer.Hide();
                        //}
                        break;
                    case DialogResult.Retry:
                        hr = this.mediaSeeking.SetPositions(pos, AMSeekingSeekingFlags.AbsolutePositioning, null, AMSeekingSeekingFlags.NoPositioning);
                        PauseClip(null);
                        break;
                    case DialogResult.Abort:
                        CloseClip();
                        //ToogleControls(true);
                        this.Close();
                        break;
                }
            }
        }

        private void ToggleMute(object notUsed)
        {
            if (this.InvokeRequired)
                this.Invoke(new TFS(ToggleMute), new object[] { null });
            else
            {
                int hr = 0;

                if ((this.graphBuilder == null) || (this.basicAudio == null))
                    return;
                // Read current volume
                hr = this.basicAudio.get_Volume(out this.currentVolume);
                if (hr == -1) //E_NOTIMPL
                {
                    // Fail quietly if this is a video-only media file
                    return;
                }
                else if (hr < 0)
                {
                    return;
                }

                // Switch volume levels
                if (this.currentVolume == VolumeFull)
                    this.currentVolume = VolumeSilence;
                else
                    this.currentVolume = VolumeFull;

                // Set new volume
                hr = this.basicAudio.put_Volume(this.currentVolume);

                UpdateMainTitle();
                //return hr;
            }
        }
        private delegate void TFS(object fromWhere);
        private delegate bool BoolInvoke(object state);

        internal void ToggleFullScreen()
        {
            ToggleFullScreen("ToggleFullScreen");
        }

        private void ToggleFullScreen(object fromWhere)
        {
            FileLogger.Log("Toggle Fullscreen: {0} IsFullScreen: {1}", fromWhere, isFullScreen);

            if (this.InvokeRequired)
            {
                FileLogger.Log("Invoke Required");
                this.Invoke(new TFS(ToggleFullScreen), new object[] { "Invoke" });
            }
            else
            {
                try
                {
                    //// Don't bother with full-screen for audio-only files
                    //if ((this.isAudioOnly) || (this.evrDisplay == null))
                    //    return;

                    if (this.currentState == PlayState.Running)
                    {
                        FileLogger.Log("Pause");
                        PauseClip(null);
                        Thread.Sleep(500);
                    }

                    if (!this.isFullScreen)
                    {
                        preFSHeight = this.Height;
                        preFSWidth = this.Width;
                        preFSLeft = this.Left;
                        preFSTop = this.Top;

                        this.Width = Screen.PrimaryScreen.Bounds.Width;
                        this.Height = Screen.PrimaryScreen.Bounds.Height;
                        this.Left = 0;
                        this.Top = 0;

                        enteredFullScreen = DateTime.Now;

                        if (this.currentState != PlayState.Init && this.currentState != PlayState.Stopped)
                            ToogleControls(false);

                        this.FormBorderStyle = FormBorderStyle.None;
                        this.Bounds = Screen.FromHandle(this.Handle).Bounds;

                        this.isFullScreen = true;
                        //showingControls = false;
                        Application.DoEvents();
                        if (currentState == PlayState.Paused)
                            Cursor.Hide();
                        //pf.ChangeLocation();

                    }
                    else
                    {
                        this.FormBorderStyle = FormBorderStyle.Sizable;

                        this.Width = preFSWidth;
                        this.Height = preFSHeight;
                        this.Left = preFSLeft;
                        this.Top = preFSTop;

                        this.isFullScreen = false;
                        //showingControls = true;

                        ToogleControls(true);
                    }

                    this.Activate();
                    //this.TopMost = this.isFullScreen;
                    if (bf != null)
                        bf.ChangeLocation();
                    if (pf != null)
                        pf.ChangeLocation();

                    if (this.currentState == PlayState.Paused)
                    {
                        Thread.Sleep(500);
                        FileLogger.Log("Play");
                        PauseClip(null);
                    }
                    //return hr;
                }
                catch (Exception ex)
                {
                    FileLogger.Log(ex.ToString());
                    throw ex;
                }
            }
        }

        private void StepOneFrame(object notUsed)
        {
            int hr = 0;

            // If the Frame Stepping interface exists, use it to step one frame
            if (this.frameStep != null)
            {
                // The graph must be paused for frame stepping to work
                if (this.currentState != PlayState.Paused)
                    PauseClip(null);

                // Step the requested number of frames, if supported
                hr = this.frameStep.Step(1, null);
            }

            // return hr;
        }

        private int StepFrames(int nFramesToStep)
        {
            int hr = 0;

            // If the Frame Stepping interface exists, use it to step frames
            if (this.frameStep != null)
            {
                // The renderer may not support frame stepping for more than one
                // frame at a time, so check for support.  S_OK indicates that the
                // renderer can step nFramesToStep successfully.
                hr = this.frameStep.CanStep(nFramesToStep, null);
                if (hr == 0)
                {
                    // The graph must be paused for frame stepping to work
                    if (this.currentState != PlayState.Paused)
                        PauseClip(null);

                    // Step the requested number of frames, if supported
                    hr = this.frameStep.Step(nFramesToStep, null);
                }
            }

            return hr;
        }

        private int ModifyRate(double dRateAdjust)
        {
            int hr = 0;
            double dRate;
            double dNewRate = 0;

            if (dvdCtrl != null)
            {
                dNewRate += dRateAdjust;

                if (dNewRate == 0) dNewRate += dRateAdjust;
            }
            else  if ((this.mediaSeeking != null) && (dRateAdjust != 0.0))
            {
                hr = this.mediaSeeking.GetRate(out dRate);
                if (hr == 0)
                {
                    // Add current rate to adjustment value
                    dNewRate = dRate + dRateAdjust;
                }
            }

            return SetRate(dNewRate);
        }

        IPin m_adecOut = null;
        IBaseFilter audioRenderer = null;
        MMDevice device = null;
        float fVol = float.MinValue;

        private void SetupGraphForRateChange(double rate)
        {
            int hr = 0;

            if (ps.UseCustomAudioRenderer)
            {
                if (device == null)
                {
                    MMDeviceEnumerator DevEnum = new MMDeviceEnumerator();
                    if (!string.IsNullOrEmpty(ps.AudioPlaybackDevice))
                        device = DevEnum.GetDevice(ps.AudioPlaybackDevice);

                    if (device == null)
                        device = DevEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);
                }

                //if (rate > Math.Abs(1.5))
                //{
                //    //if(fVol == float.MinValue)
                //    //    fVol = device.AudioEndpointVolume.MasterVolumeLevelScalar;
                //    //device.AudioEndpointVolume.MasterVolumeLevelScalar = 0;
                //    device.AudioEndpointVolume.Mute = true;
                //}
                //else if (rate <= Math.Abs(1.5))
                //{
                //    //if (fVol > float.MinValue)
                //    //{
                //    //    device.AudioEndpointVolume.MasterVolumeLevelScalar = fVol;
                //    //    fVol = float.MinValue;
                //    //}
                //    device.AudioEndpointVolume.Mute = false;
                //}

                IBasicAudio ba = graphBuilder as IBasicAudio;
                if (ba != null)
                {
                    int orgVol = 0;
                    hr = ba.get_Volume(out orgVol);
                    DsError.ThrowExceptionForHR(hr);

                    if (Math.Abs(rate) > 1.5)
                    {
                        hr = ba.put_Volume(-10000); //turn off the volume so we can ffwd
                        DsError.ThrowExceptionForHR(hr);
                    }
                    else if (Math.Abs(rate) <= 1.5)
                    {
                        hr = ba.put_Volume(0); //set the volume back to full
                        DsError.ThrowExceptionForHR(hr);
                    }
                }
            }
            else
            {
                IPin arIn = null;

                try
                {
                    if (Math.Abs(rate) > 4 && m_adecOut == null)
                    {
                        //find the audio renderer
                        if (audioRenderer == null)
                            audioRenderer = FilterGraphTools.FindFilterByClsid(graphBuilder, m_audioRendererClsid);

                        if (audioRenderer != null)
                        {
                            hr = audioRenderer.GetClassID(out m_audioRendererClsid);
                            DsError.ThrowExceptionForHR(hr);

                            //grab the audio decoder's output pin
                            arIn = DsFindPin.ByDirection(audioRenderer, PinDirection.Input, 0);
                            hr = arIn.ConnectedTo(out m_adecOut);
                            DsError.ThrowExceptionForHR(hr);

                            //stop the graph
                            hr = mediaControl.Stop();
                            DsError.ThrowExceptionForHR(hr);

                            //remove it
                            hr = graphBuilder.RemoveFilter(audioRenderer);
                            DsError.ThrowExceptionForHR(hr);

                            //start the graph again
                            hr = mediaControl.Run();
                            DsError.ThrowExceptionForHR(hr);
                        }
                    }
                    else if (Math.Abs(rate) <= 4 && m_adecOut != null)
                    {
                        //audioRenderer = FilterGraphTools.AddFilterFromClsid(graphBuilder, m_audioRendererClsid, "Audio Renderer");
                        if (audioRenderer != null)
                        {
                            //stop the graph
                            hr = mediaControl.Stop();
                            DsError.ThrowExceptionForHR(hr);

                            hr = graphBuilder.AddFilter(audioRenderer, "Audio Renderer");
                            DsError.ThrowExceptionForHR(hr);

                            //connect it to the decoder pin
                            arIn = DsFindPin.ByDirection(audioRenderer, PinDirection.Input, 0);
                            hr = graphBuilder.ConnectDirect(m_adecOut, arIn, null);
                            DsError.ThrowExceptionForHR(hr);

                            Marshal.ReleaseComObject(m_adecOut);
                            m_adecOut = null;

                            //start the graph again
                            hr = mediaControl.Run();
                            DsError.ThrowExceptionForHR(hr);
                        }
                    }

                    if (Math.Abs(rate) <= 4)
                    {
                        //{601D2A2B-9CDE-40BD-8650-0485E3522727}
                        //{EC9ED6FC-7B03-4CB6-8C01-4EABE109F26B}
                        IBasicAudio ba = graphBuilder as IBasicAudio;
                        if (ba != null)
                        {
                            int orgVol = 0;
                            hr = ba.get_Volume(out orgVol);
                            DsError.ThrowExceptionForHR(hr);

                            if (Math.Abs(rate) > 1.5)
                            {

                                hr = ba.put_Volume(-10000); //turn off the volume so we can ffwd
                                DsError.ThrowExceptionForHR(hr);
                            }
                            else if (Math.Abs(rate) <= 1.5)
                            {
                                hr = ba.put_Volume(0); //set the volume back to full
                                DsError.ThrowExceptionForHR(hr);
                            }
                        }
                    }
                }
                finally
                {
                    //if (audioRenderer != null)
                    //    Marshal.ReleaseComObject(audioRenderer);
                    if (arIn != null)
                        Marshal.ReleaseComObject(arIn);
                }
            }
        }

        private int SetRate(double rate)
        {
            int hr = 0;

            SetupGraphForRateChange(rate);

            if (dvdCtrl != null)
            {
                if (rate < 0)
                    hr = dvdCtrl.PlayBackwards(Math.Abs(rate), DvdCmdFlags.SendEvents, out cmdOption);
                else
                    hr = dvdCtrl.PlayForwards(rate, DvdCmdFlags.SendEvents, out cmdOption);
                //DsError.ThrowExceptionForHR(hr);
                if (hr >= 0)
                {
                    dvdPlayRate = rate;
                    if (cmdOption != null)
                    {
                        pendingCmd = true;
                    }
                }
                //dvdPlayRate = rate;
                //hr = dvdCtrl.PlayForwards(rate, DvdCmdFlags.SendEvents, out cmdOption);
                //DsError.ThrowExceptionForHR(hr);

                //if (cmdOption != null)
                //{
                //    pendingCmd = true;
                //}
            }
            else if (this.mediaSeeking != null)
            {
                //double dRate;
                //hr = this.mediaSeeking.GetRate(out dRate);
                //DsError.ThrowExceptionForHR(hr);
                //dRate *= 2;
                 
                //while (dRate < rate)
                //{
                //    hr = this.mediaSeeking.SetRate(dRate);
                //    DsError.ThrowExceptionForHR(hr);
                //    hr = this.mediaSeeking.GetRate(out dRate);
                //    DsError.ThrowExceptionForHR(hr);
                //    dRate *= 2;
                //}

                //if (rateSupport != null)
                //{
                //    MfFloat nRate = new MfFloat((float)rate);
                //    hr = rateSupport.IsRateSupported(true, (float)rate, nRate);
                //}

                hr = this.mediaSeeking.SetRate(rate);
                if (hr >= 0)
                {
                    this.currentPlaybackRate = rate;
                    UpdateMainTitle();
                    
                    //if (stateSink != null)
                    //{
                    //    hr = stateSink.OnClockSetRate(0, (float)rate);
                    //}
                }
                else
                {
                    hr = mediaSeeking.GetRate(out currentPlaybackRate);
                    if (hr >= 0)
                    {
                        SetupGraphForRateChange(currentPlaybackRate);
                    }
                }
                //try
                //{
                //    DsError.ThrowExceptionForHR(hr);
                //}
                //catch (Exception ex)
                //{
                //    Debug.WriteLine(ex.Message);
                //}
            }

            return hr;
        }

        private void HandleGraphEvent()
        {
            int hr = 0;
            EventCode evCode;
            IntPtr evParam1, evParam2;

            // Make sure that we don't access the media event interface
            // after it has already been released.
            if (this.mediaEventEx == null)
                return;

            // Process all queued events
            while (this.mediaEventEx.GetEvent(out evCode, out evParam1, out evParam2, 0) == 0)
            {
                if (evCode == EventCode.OleEvent)
                {
                    string one, two;
                    one = Marshal.PtrToStringAuto(evParam1);
                    two = Marshal.PtrToStringAuto(evParam2);
                }
                // Free memory associated with callback, since we're not using it
                hr = this.mediaEventEx.FreeEventParams(evCode, evParam1, evParam2);

                // If this is the end of the clip, reset to beginning
                if (evCode == EventCode.Complete)
                {
                    //DsLong pos = new DsLong(0);
                    // Reset to first frame of movie
                    StopClip(null);
                    if (currentIndex < filename.Count - 1)
                    {
                        currentIndex++;
                        
                        this.currentPosition = 0;
                        fileDuration = 0;

                        // Free DirectShow interfaces
                        CloseInterfaces();

                        ThreadPool.QueueUserWorkItem(new WaitCallback(QueuedPlayback));
                        return;
                    }
                }
            }
        }

        private void QueuedPlayback(object notUsed)
        {
            if (this.InvokeRequired)
                this.Invoke(new TFS(QueuedPlayback), new object[] { null });
            else
            {
                if (currentIndex < filename.Count)
                    PlayMovieInWindow(filename[currentIndex]);
            }
        }

        /*
         * WinForm Related methods
         */

        internal void OnDvdEvent()
        {
            IntPtr p1, p2;
            int hr = 0;
            EventCode code;
            do
            {
                hr = mediaEventEx.GetEvent(out code, out p1, out p2, 0);
                if (hr < 0)
                {
                    break;
                }

                switch (code)
                {
                    case EventCode.DvdCurrentHmsfTime:
                        {
                            byte[] ati = BitConverter.GetBytes(p1.ToInt32());
                            currnTime.bHours = ati[0];
                            currnTime.bMinutes = ati[1];
                            currnTime.bSeconds = ati[2];
                            currnTime.bFrames = ati[3];
                            UpdateMainTitle();
                            break;
                        }
                    case EventCode.DvdChapterStart:
                        {
                            currnChapter = p1.ToInt32();
                            UpdateMainTitle();
                            break;
                        }
                    case EventCode.DvdTitleChange:
                        {
                            currnTitle = p1.ToInt32();
                            UpdateMainTitle();
                            break;
                        }
                    case EventCode.DvdDomainChange:
                        {
                            currnDomain = (DvdDomain)p1;
                            UpdateMainTitle();
                            break;
                        }

                    case EventCode.DvdCmdStart:
                        {
                            break;
                        }
                    case EventCode.DvdCmdEnd:
                        {
                            OnCmdComplete(p1, p2);
                            break;
                        }

                    case EventCode.DvdStillOn:
                        {
                            if (p1 == IntPtr.Zero)
                            {
                                menuMode = MenuMode.Buttons;
                            }
                            else
                            {
                                menuMode = MenuMode.Still;
                            }
                            break;
                        }
                    case EventCode.DvdStillOff:
                        {
                            if (menuMode == MenuMode.Still)
                            {
                                menuMode = MenuMode.No;
                            }
                            break;
                        }
                    case EventCode.DvdButtonChange:
                        {
                            if (p1.ToInt32() <= 0)
                            {
                                menuMode = MenuMode.No;
                            }
                            else
                            {
                                menuMode = MenuMode.Buttons;
                            }
                            break;
                        }

                    case EventCode.DvdNoFpPgc:
                        {
                            IDvdCmd icmd;

                            if (dvdCtrl != null)
                            {
                                hr = dvdCtrl.PlayTitle(1, DvdCmdFlags.None, out icmd);
                            }
                            break;
                        }
                }

                hr = mediaEventEx.FreeEventParams(code, p1, p2);
            }
            while (hr == 0);
        }


        protected override void WndProc(ref Message m)
        {
            try
            {
                //FileLogger.Log("WndProc: {0}", m.Msg);
                //HandleGraphEvent();
                        
                switch ((WM)m.Msg)
                {
                    case WM.GRAPH_NOTIFY:
                        HandleGraphEvent();
                        break;
                    case WM.DVD_EVENT:
                        OnDvdEvent();
                        break;
                    case WM.APPCOMMAND:
                        int iChar = WindowsMessage.GET_APPCOMMAND_LPARAM(m.LParam);

                        switch ((AppCommand)iChar)
                        {
                            case AppCommand.MEDIA_PLAY:
                            case AppCommand.MEDIA_PAUSE:
                                ThreadPool.QueueUserWorkItem(new WaitCallback(PauseClip), null);
                                break;
                            case AppCommand.MEDIA_FAST_FORWARD:
                                ThreadPool.QueueUserWorkItem(new WaitCallback(SkipForward), ps.SkipForward2);
                                break;
                            case AppCommand.MEDIA_REWIND:
                                ThreadPool.QueueUserWorkItem(new WaitCallback(SkipBack), ps.SkipBack2);
                                break;
                            case AppCommand.MEDIA_RECORD:
                                ThreadPool.QueueUserWorkItem(new WaitCallback(CloseApplication));
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                FileLogger.Log("Error in WndProc: {0}", ex.ToString());
            }

            base.WndProc(ref m);
        }

        private void CloseApplication(object notUsed)
        {
            if (this.InvokeRequired)
                this.Invoke(new TFS(CloseApplication), new object[] { null });
            else
                this.Close();
        }

        private void GetClipFileName(OpenClipMode mode)
        {
            string mediaPath = string.Empty;

            if (mode == OpenClipMode.File)
            {
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    mediaPath = openFileDialog1.FileName;
                }
            }
            else if (mode == OpenClipMode.Url)
            {
                using (OpenUrlForm ouf = new OpenUrlForm())
                {
                    if (ouf.ShowDialog() == DialogResult.OK)
                    {
                        mediaPath = ouf.Value;
                    }
                }
            }

            if (!string.IsNullOrEmpty(mediaPath))
            {
                if (Path.GetExtension(mediaPath).ToLower() == ".pls")
                    ParsePls(this, mediaPath);
                else
                    filename.Add(mediaPath);
            }
        }

        private int InitPlayerWindow()
        {
            // Reset to a default size for audio and after closing a clip
            //this.ClientSize = new Size(240, 120);
           
            // Check the 'full size' menu item
            //CheckSizeMenu(menuFileSizeNormal);
            EnablePlaybackMenu(false, MediaType.Audio);

            return 0;
        }

        private void UpdateMainTitle()
        {
            if (this.InvokeRequired)
                this.Invoke(new BlankInvoke(UpdateMainTitle));
            else
            {
                // If no file is loaded, just show the application title
                if (this.filename.Count == 0)
                    this.Text = "EVR Media Player";
                else
                {
                    //string media = (isAudioOnly) ? "Audio" : "Video";
                    string muted = (currentVolume == VolumeSilence) ? " Mute" : "";
                    string paused = (currentState == PlayState.Paused) ? " Paused" : "";
                    if (dvdGraph == null)
                    {
                        string timeStatus = string.Empty;
                        if (GetPositionAndDuration(out currentPosition, out fileDuration) >= 0)
                        {
                            pf.Value = (int)TimeSpan.FromTicks(currentPosition).TotalSeconds;
                            pf.Maximum = (int)TimeSpan.FromTicks(fileDuration).TotalSeconds;
                            DateTime cDate = new DateTime(currentPosition);
                            DateTime fDuration = new DateTime(fileDuration);
                            timeStatus = string.Format(" {0:HH:mm:ss}-{1:HH:mm:ss}", cDate, fDuration);

                        }
                        this.Text = String.Format("{0}{1}{2}{3}", System.IO.Path.GetFileName(this.filename[currentIndex]), muted, paused, timeStatus);
                    }
                    else
                    {
                        DvdHMSFTimeCode tc = new DvdHMSFTimeCode();
                        DvdTimeCodeFlags tf;

                        int hr = dvdInfo.GetTotalTitleTime(tc, out tf);

                        string ti = String.Format("{0:00}:{1:00}:{2:00}", currnTime.bHours, currnTime.bMinutes, currnTime.bSeconds);
                        string tt = String.Format("{0:00}:{1:00}:{2:00}", tc.bHours, tc.bMinutes, tc.bSeconds);
                        string pr = string.Format(" {0}x", dvdPlayRate);
                        if (dvdPlayRate == 1)
                            pr = string.Empty;

                        this.Text = String.Format("Chapter:{0} Title:{1} {2}-{3}{4}", currnChapter, currnTitle, ti, tt, pr);

                        TimeSpan tsDuration = new TimeSpan(tc.bHours, tc.bMinutes, tc.bSeconds);
                        TimeSpan tsPos = new TimeSpan(currnTime.bHours, currnTime.bMinutes, currnTime.bSeconds);

                        pf.Value = (int)tsPos.TotalSeconds;
                        pf.Maximum = (int)tsDuration.TotalSeconds;
                    }
                }
            }
        }

        private void CheckSizeMenu(MenuItem item)
        {
            menuFileSizeHalf.Checked = false;
            menuFileSizeThreeQuarter.Checked = false;
            menuFileSizeNormal.Checked = false;
            menuFileSizeDouble.Checked = false;
            menuFileSizeFI.Checked = false;
            menuFileSizeFO.Checked = false;
            menuFileSizeZ1.Checked = false;
            menuFileSizeZ2.Checked = false;

            item.Checked = true;
        }

        private void EnablePlaybackMenu(bool bEnable, MediaType nMediaType)
        {
            // Enable/disable menu items related to playback (pause, stop, mute)
            menuFilePause.Enabled = bEnable;
            menuFileStop.Enabled = bEnable;
            menuFileMute.Enabled = bEnable;
            menuRateIncrease.Enabled = bEnable;
            menuRateDecrease.Enabled = bEnable;
            menuRateNormal.Enabled = bEnable;
            menuRateHalf.Enabled = bEnable;
            menuRateDouble.Enabled = bEnable;

            // Enable/disable menu items related to video size
            bool isVideo = (nMediaType == MediaType.Video) ? true : false;

            menuSingleStep.Enabled = isVideo;
            menuFileSizeHalf.Enabled = isVideo;
            menuFileSizeDouble.Enabled = isVideo;
            menuFileSizeNormal.Enabled = isVideo;
            menuFileSizeThreeQuarter.Enabled = isVideo;
            menuFileFullScreen.Enabled = isVideo;

            bool isDvd = dvdGraph != null;

            menuItemRootMenu.Enabled = isDvd;
            menuItemTitleMenu.Enabled = isDvd;
        }

        internal void MainForm_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            FileLogger.Log("KeyDown: {0} {1} {2} {3} {4}", System.Enum.GetName(typeof(Keys), e.KeyCode), e.KeyData, e.KeyValue, e.Modifiers, e.SuppressKeyPress);
            if (currentState != PlayState.Init)
            {
                switch (e.KeyCode)
                {
                    case Keys.Space:
                        StepOneFrame(null);
                        e.SuppressKeyPress = true;
                        break;
                    case Keys.MediaNextTrack:
                    case Keys.F:
                        SkipForward(ps.SkipForward1);
                        e.SuppressKeyPress = true;
                        break;
                    case Keys.MediaPreviousTrack:
                    case Keys.R:
                        SkipBack(ps.SkipBack1);
                        e.SuppressKeyPress = true;
                        break;
                    case Keys.G:
                        SkipForward(ps.SkipForward2);
                        e.SuppressKeyPress = true;
                        break;
                    case Keys.T:
                        SkipBack(ps.SkipBack2);
                        e.SuppressKeyPress = true;
                        break;
                    case Keys.P:
                    case Keys.MediaPlayPause:
                        PauseClip(null);
                        e.SuppressKeyPress = true;
                        break;
                    case Keys.S:
                    case Keys.MediaStop:
                        StopClip(null);
                        e.SuppressKeyPress = true;
                        break;
                    case Keys.M:
                        ToggleMute(null);
                        e.SuppressKeyPress = true;
                        break;
                    case Keys.Return:
                        if ((menuMode == MenuMode.Buttons) && (dvdCtrl != null))
                        {
                            dvdCtrl.ActivateButton();
                            e.SuppressKeyPress = true;
                        }
                        else if ((menuMode == MenuMode.Still) && (dvdCtrl != null))
                        {
                            dvdCtrl.StillOff();
                            e.SuppressKeyPress = true;
                        }
                        //else
                        //{
                        //    ToggleFullScreen("Return");
                        //}

                        break;
                    case Keys.D0:
                        ToggleFullScreen("0");
                        e.SuppressKeyPress = true;
                        break;
                    //case Keys.H:
                    //    InitVideoWindow(1, 2);
                    //    CheckSizeMenu(menuFileSizeHalf);
                    //    e.SuppressKeyPress = true;
                    //    break;
                    //case Keys.N:
                    //    InitVideoWindow(1, 1);
                    //    CheckSizeMenu(menuFileSizeNormal);
                    //    e.SuppressKeyPress = true;
                    //    break;
                    //case Keys.D:
                    //    InitVideoWindow(2, 1);
                    //    CheckSizeMenu(menuFileSizeDouble);
                    //    e.SuppressKeyPress = true;
                    //    break;
                    //case Keys.T:
                    //    InitVideoWindow(3, 4);
                    //    CheckSizeMenu(menuFileSizeThreeQuarter);
                    //    e.SuppressKeyPress = true;
                    //    break;
                    case Keys.Escape:
                        if (this.isFullScreen)
                            ToggleFullScreen("ESC");
                        else
                            CloseClip();

                        //e.SuppressKeyPress = true;
                        break;
                    case Keys.F12 | Keys.X:
                        CloseClip();
                        e.SuppressKeyPress = true;
                        break;
                    case Keys.Q:
                        this.Close();
                        e.SuppressKeyPress = true;
                        break;
                    case Keys.C:
                    case Keys.D2:
                        ToogleCommSkip(null);
                        e.SuppressKeyPress = true;
                        break;
                    case Keys.Left:
                        if (dvdCtrl != null)
                        {
                            if (menuMode == MenuMode.Buttons)
                                dvdCtrl.SelectRelativeButton(DvdRelativeButton.Left);
                        }
                        else
                        {
                            SkipBackComm(null);
                        }
                        e.SuppressKeyPress = true;
                        break;
                    case Keys.Right:
                        if (dvdCtrl != null)
                        {
                            if (menuMode == MenuMode.Buttons)
                                dvdCtrl.SelectRelativeButton(DvdRelativeButton.Right);
                        }
                        else
                        {
                            SkipForwardComm(null);
                        }
                        e.SuppressKeyPress = true;
                        break;
                    case Keys.Up:
                        {
                            if ((menuMode == MenuMode.Buttons) && (dvdCtrl != null))
                            {
                                dvdCtrl.SelectRelativeButton(DvdRelativeButton.Upper);
                            }
                            break;
                        }
                    case Keys.Down:
                        {
                            if ((menuMode == MenuMode.Buttons) && (dvdCtrl != null))
                            {
                                dvdCtrl.SelectRelativeButton(DvdRelativeButton.Lower);
                            }
                            break;
                        }
                    case Keys.A:
                    //case Keys.Zoom:
                    case Keys.D9:
                        ToogleAspectRatio(null);
                        e.SuppressKeyPress = true;
                        break;
                    case Keys.Menu:
                        if (dvdCtrl != null)
                        {
                            menuItemRootMenu_Click(null, null);
                            e.SuppressKeyPress = true;
                        }
                        break;
                    case Keys.U:
                    case Keys.D7:
                        menuItemSubtitles.Checked = ToogleSubtitles();
                        e.SuppressKeyPress = true;
                        break;
                    case Keys.Add:
                    case Keys.Oemplus:
                        ZoomIn();
                        e.SuppressKeyPress = true;
                        break;
                    case Keys.Subtract:
                    case Keys.OemMinus:
                        ZoomOut();
                        e.SuppressKeyPress = true;
                        break;
                    case Keys.Zoom:
                    case Keys.Z:
                        CycleZoom();
                        e.SuppressKeyPress = true;
                        break;
                }
            }
        }

        private void CycleZoom()
        {
            if (zoomAmt != 1.0f)
            {
                zoomAmt = 1;
                ChangeZoom(0f);
            }
            else
                ChangeZoom(ps.Zoom);
        }

        private void ToogleAspectRatio(object notUsed)
        {
            if (this.InvokeRequired)
                this.Invoke(new TFS(ToogleAspectRatio), new object[] { null });
            else
            {
                MFVideoAspectRatioMode varm;
                MFVideoAspectRatioMode varmNew = MFVideoAspectRatioMode.None;

                this.evrDisplay.GetAspectRatioMode(out varm);

                if (varm < MFVideoAspectRatioMode.Mask)
                    varmNew = (varm + 1);
                
                evrDisplay.SetAspectRatioMode(varmNew);

                FileLogger.Log("ToggleZoom: {0} -> {1}", varm, varmNew);
            }
        }

        private void SkipForwardComm(object notUsed)
        {
            if (this.InvokeRequired)
                this.Invoke(new TFS(SkipForwardComm), new object[] { null });
            else
            {
                FileLogger.Log("SkipForwardComm");
                long currentPos = 0;
                long duration = 0;

                if (mediaPosition != null)
                {

                    if (GetPositionAndDuration(out currentPos, out duration) >= 0)
                    {
                        FileLogger.Log("SkipForwardComm: {0} {1}", currentPos, duration);

                        foreach (double[] comm in commList)
                        {
                            long cStart = (long)(comm[0] * TICK_MULT);
                            long cEnd = (long)(comm[1] * TICK_MULT);

                            if (currentPos < cEnd)
                            {
                                FileLogger.Log("SkipForwardComm: {0} {1}", currentPos, cEnd);

                                ChangePosition(cEnd, AMSeekingSeekingFlags.AbsolutePositioning);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void SkipBackComm(object notUsed)
        {
            if (this.InvokeRequired)
                this.Invoke(new TFS(SkipBackComm), new object[] { null });
            else
            {
                FileLogger.Log("SkipBackComm");

                long currentPos = 0;
                long duration = 0;

                if (mediaPosition != null)
                {
                    if (GetPositionAndDuration(out currentPos, out duration) >= 0)
                    {
                        FileLogger.Log("SkipBackComm: {0} {1}", currentPos, duration);

                        for (int i = 0; i < commList.Count; i++)
                        {
                            double[] comm = commList[i];
                            double[] commNext = commList[i + 1];

                            long cStartNext = (long)(commNext[0] * TICK_MULT);
                            long cStart = (long)(comm[0] * TICK_MULT);
                            long cEnd = (long)(comm[1] * TICK_MULT);

                            if (currentPos >= cEnd && currentPos < cStartNext)
                            {
                                ChangePosition(cStart, AMSeekingSeekingFlags.AbsolutePositioning);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private bool ToogleCommSkip(object notUsed)
        {
            FileLogger.Log("Toggle CommSkip: {0} -> {1}", tmPosition.Enabled, !tmPosition.Enabled);
            tmPosition.Enabled = !tmPosition.Enabled;
            return tmPosition.Enabled;
        }

        private void menuFileOpenClip_Click(object sender, System.EventArgs e)
        {
            // If we have ANY file open, close it and shut down DirectShow
            if (this.currentState != PlayState.Init)
                CloseClip();

            // Open the new clip
            OpenClip(OpenClipMode.File);
        }

        private void menuFileClose_Click(object sender, System.EventArgs e)
        {
            CloseClip();
        }

        private void menuFileExit_Click(object sender, System.EventArgs e)
        {
            CloseClip();
            this.Close();
        }

        private void menuFilePause_Click(object sender, System.EventArgs e)
        {
            PauseClip(null);
        }

        private void menuFileStop_Click(object sender, System.EventArgs e)
        {
            StopClip(null);
        }

        private void menuFileMute_Click(object sender, System.EventArgs e)
        {
            ToggleMute(null);
        }

        private void menuFileFullScreen_Click(object sender, System.EventArgs e)
        {
            ToggleFullScreen("MENU");
        }

        private void menuFileSize_Click(object sender, System.EventArgs e)
        {
            MenuItem c = sender as MenuItem;
            if (c != null)
            {
                iVideoScaling = (dvstype)Enum.Parse(typeof(dvstype), c.Tag.ToString());
            }
            //if (sender == menuFileSizeHalf) iVideoScaling = dvstype.HALF;//InitVideoWindow(1, 2);
            //if (sender == menuFileSizeNormal) iVideoScaling = dvstype.NORMAL;//InitVideoWindow(1, 1);
            //if (sender == menuFileSizeDouble) iVideoScaling = dvstype.DOUBLE;//InitVideoWindow(2, 1);
            //if (sender == menuFileSizeThreeQuarter) iVideoScaling = dvstype.HALF;//InitVideoWindow(3, 4);

            InitVideoWindow();

            CheckSizeMenu((MenuItem)sender);
        }

        private void menuSingleStep_Click(object sender, System.EventArgs e)
        {
            StepOneFrame(null);
        }

        private void menuRate_Click(object sender, System.EventArgs e)
        {
            if (sender == menuRateDecrease) 
                ModifyRate(-0.25);
            else if (sender == menuRateIncrease) 
                ModifyRate(+0.25);
            else if (sender == menuRateNormal) 
                SetRate(1.0);
            else if (sender == menuRateHalf) 
                SetRate(0.5);
            else if (sender == menuRateDouble)
                SetRate(2.0);
            else if((sender as MenuItem).Tag != null)
            {
                double nRate = 0;
                if (double.TryParse((sender as MenuItem).Tag.ToString(), out nRate))
                    SetRate(nRate);
            }
        }

        private void MainForm_Move(object sender, System.EventArgs e)
        {
            if (!this.isAudioOnly)
                MoveVideoWindow();
        }

        private void MainForm_Resize(object sender, System.EventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized)
            {
                this.WindowState = FormWindowState.Normal;
                ToggleFullScreen("Resize");
            }

            if (!this.isAudioOnly)
                MoveVideoWindow();

            if (pf != null)
                pf.ChangeLocation();
            if (bf != null)
                bf.ChangeLocation();
        }

        private void MainForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //StopClip(null);
            //CloseInterfaces();
            CloseClip();
            if (this.isFullScreen)
            {
                fHist.LastHeight = 0;
                fHist.LastWidth = 0;
            }
            else
            {
                fHist.LastHeight = this.Height;
                fHist.LastWidth = this.Width;
            }
            fHist.Save();
        }

        private void menuHelpAbout_Click(object sender, System.EventArgs e)
        {
            string title = "About EVR Media Player";
            string text = "EVR Media Player is a modifcation of the DirectShow.Net PlayWindow Sample by andy vt.";

            AboutBox.Show(title, text);
        }

        public static MainForm theForm = null;

        /// <summary>
        /// Main entry point
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            string mutexName = "EVRPlay";
            bool grantedOwnership = false;
            Mutex singleInstanceMutex = null;

            // Enable XP style controls
            Application.EnableVisualStyles();
            Application.DoEvents();            
                        
            try
            {
                theForm = new MainForm();
                //using (MainForm form = new MainForm())
                //{
                    if (theForm.ps.SingleInstance)
                    {
                        FileLogger.Log("Single Instance Mode");
                        
                        try
                        {
                            singleInstanceMutex = new Mutex(true, mutexName, out grantedOwnership);
                            FileLogger.Log("grantedOwnership: {0}", grantedOwnership);
                        }
                        catch (Exception ex)
                        {
                            FileLogger.Log("Error getting mutex: {0}", ex.ToString());
                        }
                        
                        if (!(grantedOwnership))
                        {
                            using (EpClient epc = new EpClient())
                            {
                                if (args.Length > 0)
                                    epc.PlayFile(args[0]);
                                else
                                    epc.Focus();
                            }
                            return;
                        }
                        else
                        {
                            theForm.eps = new EpServer();
                        }
                    }

                    if (theForm.ps.PriorityClass > 0)
                    {
                        using (Process cp = Process.GetCurrentProcess())
                            cp.PriorityClass = (ProcessPriorityClass)theForm.ps.PriorityClass;
                    }

                    if (args.Length > 0)
                    {
                        List<string> lArgs = new List<string>();
                        lArgs.AddRange(args);
                        bool hasMedia = true;

                        if (lArgs.Contains("-d"))
                        {
                            FileLogger.Log("In Debug Mode");
                            DebugMode = true;
                            if (args.Length == 1)
                                hasMedia = false;
                        }

                        if (hasMedia)
                        {
                            FileLogger.Log("arg[0] = {0}", args[0]);
                            string mediaPath = args[0];

                            if (Path.GetExtension(mediaPath).ToLower() == ".pls")
                                ParsePls(theForm, mediaPath);
                            else
                                theForm.filename.Add(mediaPath);
                        }
                    }

                    theForm.Show();
                    ThreadPool.QueueUserWorkItem(new WaitCallback(theForm.OpenFile), OpenClipMode.None);

                    Application.Run(theForm);
                //}
            }
            catch (Exception ex)
            {
                FileLogger.Log("Main Error: {0}", ex.ToString());
            }
            finally
            {
                if (singleInstanceMutex != null)
                {
                    FileLogger.Log("Release mutex");
                    singleInstanceMutex.Close();
                }
                if (theForm != null)
                    theForm.Dispose();
            }
        }

        public void FocusForm()
        {
            if (this.InvokeRequired)
                this.Invoke(new BlankInvoke(FocusForm));
            else
            {
                //this.TopMost = true;
                //this.Focus();
                //this.TopMost = false;
                this.Activate();
            }
        }

        public void PlayFile(string filePath)
        {
            if (this.InvokeRequired)
                this.Invoke(new StringInvoke(PlayFile), new object[] { filePath });
            else
            {
                // If we have ANY file open, close it and shut down DirectShow
                if (this.currentState != PlayState.Init)
                    CloseClip();

                filename.Clear();

                if (Path.GetExtension(filePath).ToLower() == ".pls")
                    ParsePls(theForm, filePath);
                else
                    filename.Add(filePath);

                // Open the new clip
                OpenClip(OpenClipMode.None);
            }
        }

        private static void ParsePls(MainForm form, string plsPath)
        {
            if (File.Exists(plsPath))
            {
                using (StreamReader sr = File.OpenText(plsPath))
                {
                    string plsText = sr.ReadToEnd();
                    ParsePlsText(form, plsText);
                }
            }
        }

        internal static void ParsePlsText(MainForm form, string plsText)
        {
            form.filename.Clear();

            MatchCollection mc = Regex.Matches(plsText, @"File\d=(.+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            foreach (Match m in mc)
            {
                if (m.Success)
                {
                    string mPath = m.Groups[1].Value.Trim();
                    if (File.Exists(mPath) || (mPath.ToLower().IndexOf(@"video_ts") > 0 && Directory.Exists(mPath)))
                        form.filename.Add(mPath);
                    else
                    {
                        if (form.ps.ReplacePaths != null && form.ps.ReplacePaths.Count > 0)
                        {
                            foreach (string rPath in form.ps.ReplacePaths)
                            {
                                string[] aRepPath = rPath.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                                if (aRepPath.Length > 1)
                                {
                                    string nmPath = mPath.ToLower().Replace(aRepPath[0].ToLower(), aRepPath[1]);
                                    if (File.Exists(nmPath))
                                    {
                                        form.filename.Add(nmPath);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void Repainted(object sender, PaintEventArgs e)
        {
            if (evrDisplay != null)
            {
                try
                {
                    evrDisplay.RepaintVideo();
                }
                catch
                {
                    // evr is not connected
                }
            }
        }

        private void miSkip1_Click(object sender, EventArgs e)
        {
            //ChangePosition(ps.SkipForward1, AMSeekingSeekingFlags.RelativePositioning);
            SkipForward(ps.SkipForward1);
        }

        private void SkipForward(object objChangeBy)
        {
            if (this.InvokeRequired)
                this.Invoke(new TFS(SkipForward), new object[] { objChangeBy });
            else
            {
                int changeBy = (int)objChangeBy;

                if (dvdCtrl != null)
                {
                    if (changeBy == ps.SkipForward1)
                    {
                        int hr = dvdCtrl.PlayNextChapter(DvdCmdFlags.SendEvents, out cmdOption);
                        DsError.ThrowExceptionForHR(hr);

                        if (cmdOption != null)
                        {
                            pendingCmd = true;
                        }
                    }
                    else if (changeBy == ps.SkipForward2)
                    {
                        ModifyRate(1);
                    }
                }
                else
                {                    
                    ChangePosition(changeBy, AMSeekingSeekingFlags.RelativePositioning);
                }
            }
        }

        private void SkipBack(object objChangeBy)
        {
            if (this.InvokeRequired)
                this.Invoke(new TFS(SkipBack), new object[] { objChangeBy });
            else
            {
                int changeBy = (int)objChangeBy;

                if (dvdCtrl != null)
                {
                    if (changeBy == ps.SkipBack1)
                    {
                        int hr = dvdCtrl.PlayPrevChapter(DvdCmdFlags.SendEvents, out cmdOption);
                        DsError.ThrowExceptionForHR(hr);

                        if (cmdOption != null)
                        {
                            pendingCmd = true;
                        }
                    }
                    else if (changeBy == ps.SkipBack2)
                    {
                        ModifyRate(-1);
                    }
                }
                else
                {
                    if (changeBy < 0)
                        ChangePosition(changeBy, AMSeekingSeekingFlags.RelativePositioning);
                    else
                        ChangePosition(changeBy * -1, AMSeekingSeekingFlags.RelativePositioning);
                }
            }
        }

        private void ChangePosition(long changeBy, AMSeekingSeekingFlags seekType)
        {
            long cPos, cDuration;
            int hr;

            try
            {
                hr = this.mediaSeeking.GetCurrentPosition(out cPos);
                DsError.ThrowExceptionForHR(hr);

                hr = this.mediaSeeking.GetDuration(out cDuration);
                DsError.ThrowExceptionForHR(hr);

                long nPos = 0;

                if (seekType == AMSeekingSeekingFlags.RelativePositioning)
                    nPos = cPos + (changeBy * TICK_MULT);
                else if (seekType == AMSeekingSeekingFlags.AbsolutePositioning)
                    nPos = changeBy;

                if (nPos < cDuration && nPos >= 0)
                {
                    DsLong ndspos = new DsLong(nPos);
                    hr = this.mediaSeeking.SetPositions(nPos, AMSeekingSeekingFlags.AbsolutePositioning, 0, AMSeekingSeekingFlags.NoPositioning);//mediaPosition.put_CurrentPosition(nPos);
                    DsError.ThrowExceptionForHR(hr);
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(string.Format("Error skipping: {0}", ex.Message));
                using (EPDialog ed = new EPDialog())
                    ed.ShowDialog("Error", string.Format("Error skipping: {0}", ex.Message), 10, this);
            }
        }

        private void miSkipBack1_Click(object sender, EventArgs e)
        {
            //if (ps.SkipBack1 < 0)
            //    ChangePosition(ps.SkipBack1, AMSeekingSeekingFlags.RelativePositioning);
            //else
            //    ChangePosition(ps.SkipBack1 * -1, AMSeekingSeekingFlags.RelativePositioning);
            if (ps.SkipBack1 < 0)
                SkipBack(ps.SkipBack1);//, AMSeekingSeekingFlags.RelativePositioning);
            else
                SkipBack(ps.SkipBack1 * -1);//, AMSeekingSeekingFlags.RelativePositioning);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            //if (Environment.OSVersion.Version.Major < 6)
            //{
            //    //MessageBox.Show("This application will only run on Windows Vista or greater");
            //    using (EPDialog ed = new EPDialog())
            //        ed.ShowDialog("Error", "This application requires the Enhanced Video Renderer (EVR)", 10);
            //    this.Close();
            //}

            try
            {
                if (!FilterGraphTools.IsThisComObjectInstalled(new Guid("{FA10746C-9B63-4B6C-BC49-FC300EA5F256}")))
                {
                    using (EPDialog ed = new EPDialog())
                        ed.ShowDialog("Error", "This application requires the Enhanced Video Renderer (EVR).", 10, this);
                    this.Close();
                    return;
                }
            }
            catch (Exception ex)
            {
                using (EPDialog ed = new EPDialog())
                    ed.ShowDialog("Error", string.Format("Error Testing EVR: {0}", ex.Message), 10, this);
                this.Close();
                return;
            }

            RegistryKey runFrom = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\babgvant\EVRPlay", true);
            try
            {
                if (runFrom == null)
                    runFrom = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\babgvant\EVRPlay");

                runFrom.SetValue("LastRunFrom", Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            }
            finally
            {
                if (runFrom != null)
                    runFrom.Close();
            }


            RawDevice.RegisterRawDevices(0xFFBC, 0x88, InputMode.BackgroundMode);
            RawDevice.RegisterRawDevices(0x0C, 0x01, InputMode.BackgroundMode);
            RawDevice.RegisterRawDevices(0x0C, 0x80, InputMode.BackgroundMode);
            
            RawDevice.RawInput += new EventHandler<RawInputEventArgs>(RawDevice_RawInput);

            //messageWindow = new SageMessageListener(this);
            try
            {
                messageWindow = new EVRPlayListener(this);
            }
            catch (Exception ex)
            {
                FileLogger.Log(ex.ToString());
            }

            if (ps.NeedsUpgrade)
            {
                ps.Upgrade();
                ps.NeedsUpgrade = false;
                ps.Save();
            }

            if (fHist.NeedsUpgrade)
            {
                fHist.Upgrade();
                fHist.NeedsUpgrade = false;
                fHist.Save();
            }

            miSkip1.Text = string.Format("Forward {0} Seconds", ps.SkipForward1);
            miSkipForward2.Text = string.Format("Forward {0} Seconds", ps.SkipForward2);
            miSkipBack1.Text = string.Format("Back {0} Seconds", ps.SkipBack1);
            miSkipBack2.Text = string.Format("Back {0} Seconds", ps.SkipBack2);
            BuildHistoryMenu(string.Empty);

            if (fHist.LastWidth == 0 || fHist.LastHeight == 0)
                ToggleFullScreen("Load");
            else
            {
                this.Width = fHist.LastWidth;
                this.Height = fHist.LastHeight;
            }

            pf = new ProgressForm();
            this.AddOwnedForm(pf);
            //pf.Owner = this;
            //pf.Show();

            //wbSageServer.Navigate(ABOUT_BLANK);
            //SHDocVw.WebBrowser axBrowser = (SHDocVw.WebBrowser)this.wbSageServer.ActiveXInstance;

            //axBrowser.NavigateError +=  new SHDocVw.DWebBrowserEvents2_NavigateErrorEventHandler(axBrowser_NavigateError);
            //axBrowser.BeforeNavigate2 += new SHDocVw.DWebBrowserEvents2_BeforeNavigate2EventHandler(axBrowser_BeforeNavigate2);

            if (ps.OpenMediaBrowser)
            {
                ShowMediaBrowser(BrowseMode.Recordings);
            }
            else if (ps.BrowseOnLoad)
            {
                CreateBrowserForm();
                bf.BrowseWebserver();
            }
        }

        void RawDevice_RawInput(object sender, RawInputEventArgs e)
        {
            DeviceData dd = e.GetRawData() as DeviceData;
            if (dd != null)
            {
                try
                {
                    byte[,] ddByte = dd.GetDataBuffer();
                    switch ((RawInput)ddByte[0, 1])
                    {
                        case RawInput.DETAILS:

                            break;
                        case RawInput.GUIDE:

                            break;
                        case RawInput.TVJUMP:
                            //live tv
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
                            ShowMediaBrowser(BrowseMode.Recordings);
                            break;
                        case RawInput.MYVIDEOS:
                            //browse to videos
                            ShowMediaBrowser(BrowseMode.Media);
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
                            ShowDvdMenu(DvdMenuId.Root);
                            break;
                        case RawInput.DVDSUBTITLE:
                            //toggle subtitles
                            ToogleSubtitles();
                            break;
                        case RawInput.INFO:
                            //info key
                            EPInfo ei = new EPInfo();
                            ei.ShowDialog(10, this);
                            break;
                    }
                }
                finally
                {
                    if (dd != null)
                        dd.Dispose();
                }
            }
        }

        private void miFileHistoryClick(object sender, EventArgs e)
        {
            MenuItem mi = (MenuItem)sender;
            string nfPath = (string)mi.Tag;
                
            try
            {
                Uri fUri = new Uri(nfPath);

                if ((fUri.IsFile && File.Exists(nfPath)) || !fUri.IsFile)
                {
                    if (this.currentState != PlayState.Init)
                        CloseClip();

                    filename.Clear();
                    currentIndex = 0;
                    filename.Add(nfPath);

                    // Open the new clip
                    OpenClip(OpenClipMode.None);
                }
                else if (Directory.Exists(nfPath) && nfPath.ToLower().IndexOf("video_ts") > 0)
                {
                    if (this.currentState != PlayState.Init)
                        CloseClip();

                    filename.Clear();
                    currentIndex = 0;
                    filename.Add(nfPath);

                    // Open the new clip
                    OpenClip(OpenClipMode.Folder);
                }
                else
                {
                    //MessageBox.Show(string.Format("{0} could not be found", nfPath));
                    using (EPDialog ed = new EPDialog())
                        ed.ShowDialog("Error", string.Format("{0} could not be found", nfPath), 10, this);
                }
            }
            catch (Exception ex)
            {
                using (EPDialog ed = new EPDialog())
                    ed.ShowDialog("Error", string.Format("Could not open {0}: {1}", nfPath, ex.Message), 10, this);
            }
        }

        private void miSkipForward2_Click(object sender, EventArgs e)
        {
            //ChangePosition(ps.SkipForward2, AMSeekingSeekingFlags.RelativePositioning);
            SkipForward(ps.SkipForward2);//
        }

        private void miSkipBack2_Click(object sender, EventArgs e)
        {
            //if (ps.SkipBack2 < 0)
            //    ChangePosition(ps.SkipBack2, AMSeekingSeekingFlags.RelativePositioning);
            //else
            //    ChangePosition(ps.SkipBack2 * -1, AMSeekingSeekingFlags.RelativePositioning);

            if (ps.SkipBack2 < 0)
                SkipBack(ps.SkipBack2);//, AMSeekingSeekingFlags.RelativePositioning);
            else
                SkipBack(ps.SkipBack2 * -1);//, AMSeekingSeekingFlags.RelativePositioning);
        }

        private void UpdateStatus(object sender, EventArgs e)
        {
            NativeMethods2.SetThreadExecutionState(EXECUTION_STATE.ES_DISPLAY_REQUIRED);
            UpdateMainTitle();
        }

        private void MainForm_DoubleClick(object sender, EventArgs e)
        {
            ToggleFullScreen("double click");
        }

        private void tmPosition_Tick(object sender, EventArgs e)
        {
            if (this.currentState == PlayState.Running)
            {
                long currentPos = 0;
                long duration = 0;

                NativeMethods2.SetThreadExecutionState(EXECUTION_STATE.ES_DISPLAY_REQUIRED);

                if (GetPositionAndDuration(out currentPos, out duration) >= 0)
                {
                    foreach (double[] comm in commList)
                    {
                        long cStart = (long)(comm[0] * TICK_MULT);
                        long cEnd = (long)(comm[1] * TICK_MULT);

                        if (currentPos >= cStart && currentPos < cEnd && currentPos > lastJump)
                        {
                            FileLogger.Log("Skip commercial: {0} - {1}", cStart, cEnd);
                            ChangePosition(cEnd, AMSeekingSeekingFlags.AbsolutePositioning);
                            lastJump = cEnd;
                            break;
                        }
                    }
                }
            }
        }

        public void ChangePosition(MouseEventArgs e)
        {
            if (mediaSeeking != null)
            {
                long currentPos = 0;
                long duration = 0;

                if (GetPositionAndDuration(out currentPos, out duration) >= 0)
                {
                    long nPos = duration / pf.Width * e.X;
                    ChangePosition(nPos, AMSeekingSeekingFlags.AbsolutePositioning);
                }
            }
            else if (dvdCtrl != null)
            {
                DvdHMSFTimeCode tc = new DvdHMSFTimeCode();
                DvdTimeCodeFlags tf;

                int hr = dvdInfo.GetTotalTitleTime(tc, out tf);
                if (hr == 0)
                {
                    TimeSpan tsDuration = new TimeSpan(tc.bHours, tc.bMinutes, tc.bSeconds);
                    double nPos = tsDuration.TotalSeconds / pf.Width * e.X;
                    TimeSpan tsPos = TimeSpan.FromSeconds(nPos);
                    DvdHMSFTimeCode nTc = new DvdHMSFTimeCode();

                    nTc.bHours = (byte)tsPos.Hours;
                    nTc.bMinutes = (byte)tsPos.Minutes;
                    nTc.bSeconds = (byte)tsPos.Seconds;

                    hr = dvdCtrl.PlayAtTime(nTc, DvdCmdFlags.SendEvents, out cmdOption);
                    DsError.ThrowExceptionForHR(hr);

                    if (cmdOption != null)
                    {
                        pendingCmd = true;
                    }
                }
            }
        }

        private void pbPlayback_MouseClick(object sender, MouseEventArgs e)
        {
            ChangePosition(e);
        }

        private int GetPositionAndDuration(out long cPos, out long duration)
        {
            cPos = 0;
            duration = 0;
            int hr = 0;

            if (mediaSeeking != null)
            {
                hr = mediaSeeking.GetDuration(out duration);
                if (hr < 0)
                    return hr;

                hr = mediaSeeking.GetCurrentPosition(out cPos);
                if (hr < 0)
                    return hr;
            }
            return hr;
        }

        Point lastMousePosition = new Point();

        private void MainForm_MouseMove(object sender, MouseEventArgs e)
        {
            MoveMouse(sender, e);
        }

        public void MoveMouse(object sender, MouseEventArgs e)
        {
            if (lastMousePosition.X == 0 && lastMousePosition.Y == 0)
                lastMousePosition = e.Location;

            if (this.isFullScreen)
            {
                if (enteredFullScreen.AddSeconds(1) < DateTime.Now &&
                    (Math.Abs(e.Location.X - lastMousePosition.X) > ps.MouseSensitivity || Math.Abs(e.Location.Y - lastMousePosition.Y) > ps.MouseSensitivity))
                {
                    FileLogger.Log("Mouse Move: {0} {1} Last Move {2} {3}", e.X, e.Y, lastMousePosition.X, lastMousePosition.Y);

                    lastMousePosition = e.Location;
                    ToogleControls(true);

                    lastMove = DateTime.Now;
                    if (!tmMouseMove.Enabled)
                        tmMouseMove.Enabled = true;
                }
            }
            else
            {
                Cursor.Show();
            }

            lastMousePosition = e.Location;

            if ((dvdCtrl == null) || (menuMode != MenuMode.Buttons))
            {
                return;
            }
            Point pt = new Point();
            pt.X = e.X;
            pt.Y = e.Y;
            dvdCtrl.SelectAtPosition(pt);
        }

        private void tmMouseMove_Tick(object sender, EventArgs e)
        {
            FileLogger.Log("tmMouseMove_Tick: {0} {1}", lastMove.AddSeconds(10), DateTime.Now);

            if (lastMove.AddSeconds(ps.FSControlTimeout) < DateTime.Now)
            {
                if (isFullScreen)
                {
                    FileLogger.Log("tmMouseMove_Tick Hide controls");
                    ToogleControls(false);
                }
                else
                    ToogleControls(true);

                tmMouseMove.Enabled = false;
                //showingControls = false;
            }
        }

        private void ToogleControls(bool show)
        {
            FileLogger.Log("ToogleControls: {0}", show);

            if (!show)
            {
                Cursor.Position = new Point(this.Width, this.Height);
                lastMove = DateTime.Now;
            }

            if (show)
                Cursor.Show();
            else
                Cursor.Hide();

            ToogleProgress(show);
            ToggleMenu(show);

            if (show)
                Cursor.Show();
            else
                Cursor.Hide();

            //this.Focus();
        }

        private void ToggleMenu(bool show)
        {
            FileLogger.Log("ToggleMenu: {0}", show);
            foreach (MenuItem mi in mainMenu1.MenuItems)
                mi.Visible = show;
        }

        private void ToogleProgress(bool show)
        {
            FileLogger.Log("ToogleProgress: {0}", show);

            if (currentState == PlayState.Init)
                pf.Visible = false;
            else
                pf.Visible = show;
            //if (show)
            //{
            //    pf.Show();
            //    pf.Activate();
            //}
            //else
            //    pf.Hide();

            //pf.TopMost = show;
        }

        private void menuItemSettings_Click(object sender, EventArgs e)
        {
            SettingsForm sf = new SettingsForm();
            if (sf.ShowDialog() == DialogResult.OK)
                ps = new PlaySettings();
        }

        private void menuItemOpenVideoTS_Click(object sender, EventArgs e)
        {
            // If we have ANY file open, close it and shut down DirectShow
            if (this.currentState != PlayState.Init)
                CloseClip();

            // Open the new clip
            OpenClip(OpenClipMode.Folder);
        }

        private void MainForm_MouseDown(object sender, MouseEventArgs e)
        {
            if ((dvdCtrl == null) || (menuMode != MenuMode.Buttons))
            {
                return;
            }
            Point pt = new Point();
            pt.X = e.X;
            pt.Y = e.Y;
            dvdCtrl.ActivateAtPosition(pt);
        }

        private void menuItemRootMenu_Click(object sender, EventArgs e)
        {
            ShowDvdMenu(DvdMenuId.Root);
        }

        internal bool ShowDvdMenu(DvdMenuId menu)
        {
            IDvdCmd icmd;

            if ((this.currentState != PlayState.Running) || (dvdCtrl == null))
            {
                return false;
            }
            else
            {
                try
                {
                    int hr = dvdCtrl.ShowMenu(menu, DvdCmdFlags.Block | DvdCmdFlags.Flush, out icmd);
                    DsError.ThrowExceptionForHR(hr);
                }
                catch (Exception ex)
                {
                    using (EPDialog ed = new EPDialog())
                        ed.ShowDialog("Error", ex.Message, 10, this);
                }
                return true;
            }
        }

        private void menuItem3_Click(object sender, EventArgs e)
        {
            //IDvdCmd icmd;

            //if ((currentState != PlayState.Running) || (dvdCtrl == null))
            //{
            //    return;
            //}

            //int hr = dvdCtrl.ShowMenu(DvdMenuId.Title, DvdCmdFlags.Block | DvdCmdFlags.Flush, out icmd);
            //DsError.ThrowExceptionForHR(hr);
            ShowDvdMenu(DvdMenuId.Title);
        }

        /// <summary> asynchronous command completed </summary>
        void OnCmdComplete(IntPtr p1, IntPtr hrg)
        {
            // Trace.WriteLine( "DVD OnCmdComplete.........." );
            if ((pendingCmd == false) || (dvdInfo == null))
            {
                return;
            }

            IDvdCmd cmd;
            int hr = dvdInfo.GetCmdFromEvent(p1, out cmd);
            DsError.ThrowExceptionForHR(hr);

            if (cmd == null)
            {
                // DVD OnCmdComplete GetCmdFromEvent failed
                return;
            }

            if (cmd != cmdOption)
            {
                // DVD OnCmdComplete UNKNOWN CMD
                Marshal.ReleaseComObject(cmd);
                cmd = null;
                return;
            }

            Marshal.ReleaseComObject(cmd);
            cmd = null;
            Marshal.ReleaseComObject(cmdOption);
            cmdOption = null;
            pendingCmd = false;
            // Trace.WriteLine( "DVD OnCmdComplete OK." );
            UpdateMainTitle();
        }

        private void menuItemSubtitles_Click(object sender, EventArgs e)
        {
            menuItemSubtitles.Checked = ToogleSubtitles();
        }

        private void wbSageServer_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            if (Regex.IsMatch(e.Url.ToString(), @"PlaylistGenerator\?Command=generate\&pltype=pls", RegexOptions.IgnoreCase))
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(DownloadPls), e.Url);
                e.Cancel = true;
            }
            //else if (Regex.IsMatch(e.Url.ToString(), @"/sagepublic/MediaFile\?", RegexOptions.IgnoreCase))
            //{
            //    ThreadPool.QueueUserWorkItem(new WaitCallback(DownloadFile), e.Url);
            //    e.Cancel = true;
            //}
        }

        private string DownloadString(Uri uriPath)
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    return wc.DownloadString(uriPath);
                }
            }
            catch (Exception ex)
            {
                FileLogger.Log("Error downloading string: {0}", ex.ToString());
                return string.Empty;
            }
        }

        private void DownloadFile(object objFilePath)
        {
            if (this.InvokeRequired)
                this.Invoke(new WaitCallback(DownloadFile), new object[] { objFilePath });
            else
            {
                Uri fileUrl = (Uri)objFilePath;
                string fileText = DownloadString(fileUrl);

                if (!string.IsNullOrEmpty(fileText))
                {
                    // If we have ANY file open, close it and shut down DirectShow
                    if (this.currentState != PlayState.Init)
                        CloseClip();

                    filename.Clear();

                    //ParsePlsText(this, plsText);

                    // Open the new clip
                    OpenClip(OpenClipMode.None);
                }
            }
        }

        internal void DownloadPls(object objPlsUrl)
        {
            if (this.InvokeRequired)
                this.Invoke(new WaitCallback(DownloadPls), new object[] { objPlsUrl });
            else
            {            
                Uri plsUrl = objPlsUrl as Uri;

                if (plsUrl == null)
                    plsUrl = new Uri(objPlsUrl.ToString());

                string plsText = DownloadString(plsUrl);

                if (!string.IsNullOrEmpty(plsText))
                {
                    // If we have ANY file open, close it and shut down DirectShow
                    if (this.currentState != PlayState.Init)
                        CloseClip();

                    ParsePlsText(this, plsText);

                    // Open the new clip
                    OpenClip(OpenClipMode.None);
                }
            }
        }

        private void menuItemWebserver_Click(object sender, EventArgs e)
        {
            if (this.currentState != PlayState.Init)
                CloseClip();

            if (bf == null)
                CreateBrowserForm();

            bf.BrowseWebserver();
        }

        public void ShowMediaBrowser(BrowseMode mode)
        {
            if (mb == null)
            {
                mb = new MediaBrowser(mode);
                this.AddOwnedForm(mb);
            }

            mb.Show();
            mb.ChangeTab(mode);
            mb.Activate();
        }

        private void CreateBrowserForm()
        {
            bf = new BrowserForm();
            this.AddOwnedForm(bf);
            bf.Show();
        }

        private void menuItem3_Click_2(object sender, EventArgs e)
        {
            //EPDialog ep = new EPDialog();
            //if (ep.ShowDialog("Test Dialog", "Hello World", 5, this) == DialogResult.OK)
            //{
            //    //do something
            //}
            OtherVideo ov = new OtherVideo();
            this.AddOwnedForm(ov);
            ov.Show();
        }

        private void MainForm_MouseDoubleClick(object sender, MouseEventArgs e)
        {
           // ToggleFullScreen("mouse double click");
        }

        private void MainForm_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                EPInfo ei = new EPInfo();
                ei.ShowDialog(10, this);
            }
        }

        internal void ToogleAspectRatio()
        {
            ToogleAspectRatio(null);
        }

        internal bool ToogleCommSkip()
        {
            return ToogleCommSkip(null);
        }

        internal float ChangeZoom(float adjustAmount)
        {
            if (evrDisplay != null)
            {
                zoomAmt += adjustAmount;

                if (zoomAmt < 1.0f)
                    zoomAmt = 1.0f;

                MFVideoNormalizedRect sRect = new MFVideoNormalizedRect();

                if (zoomAmt == 1.0)
                {
                    sRect.top = 0;
                    sRect.left = 0;
                    sRect.right = 1;
                    sRect.bottom = 1;
                }
                else
                {
                    float fMargin = (0.5f - (0.5f / zoomAmt));

                    sRect.top = fMargin;
                    sRect.left = fMargin;
                    sRect.right = (1.0f - fMargin);
                    sRect.bottom = (1.0f - fMargin);
                }

                this.evrDisplay.SetVideoPosition(sRect, null);
            }
            return zoomAmt;
        }

        internal float ZoomIn()
        {
            return ChangeZoom(ps.ZoomIncrement);
        }

        internal float ZoomOut()
        {
            return ChangeZoom(ps.ZoomIncrement*-1f);
        }

        private void menuItemZoomOut_Click(object sender, EventArgs e)
        {
            ZoomOut();
        }

        private void menuItemZoomIn_Click(object sender, EventArgs e)
        {
            ZoomIn();
        }

        private void menuItemMediaBrowser_Click(object sender, EventArgs e)
        {
            ShowMediaBrowser(BrowseMode.Recordings);
        }

        private void MainForm_Activated(object sender, EventArgs e)
        {
            if (mb != null && mb.Visible)
            {
                if (Cursor.Position.X < mb.Location.X || Cursor.Position.X > mb.Location.X + mb.Size.Width)
                    return;
                else if (Cursor.Position.Y < mb.Location.Y || Cursor.Position.Y > mb.Location.Y + mb.Size.Height)
                    return;
                else
                    mb.Activate();
            }
        }

        private void MainForm_Click(object sender, EventArgs e)
        {

        }

        private void miOpenUrl_Click(object sender, EventArgs e)
        {
            if (this.currentState != PlayState.Init)
                CloseClip();

            OpenClip(OpenClipMode.Url);
        }

        private void menuItem8_Click(object sender, EventArgs e)
        {
            MediaFoundation.Misc.BitmapInfoHeader pBih = new MediaFoundation.Misc.BitmapInfoHeader();
            IntPtr pDib = IntPtr.Zero;
            int pcbDib = 0;
            long pTimeStamp = 0;

            try
            {
                pBih.Size = Marshal.SizeOf(pBih);

                int hr = evrDisplay.GetCurrentImage(pBih, out pDib, out pcbDib, out pTimeStamp);
                DsError.ThrowExceptionForHR(hr);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK);
            }
            finally
            {
                if (pDib != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(pDib);
            }
        }

        [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleDC", SetLastError = true)]
        static extern IntPtr CreateCompatibleDC([In] IntPtr hdc);

        [DllImport("gdi32.dll", EntryPoint = "DeleteDC")]
        static extern bool DeleteDC([In] IntPtr hdc);

        [DllImport("gdi32.dll", EntryPoint = "SelectObject", SetLastError = true)]
        static extern IntPtr SelectObject([In] IntPtr hdc, [In] IntPtr hgdiobj);

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);

        private void menuItem14_Click(object sender, EventArgs e)
        {
            MFVideoAlphaBitmap alphaBmp = new MFVideoAlphaBitmap();

            using (Bitmap alphaBitmap = new Bitmap("epsonproj.png"))
            {

                //alphaBitmap is a 32bit semitransparent Bitmap
                Graphics g = Graphics.FromImage(alphaBitmap);

                // get pointer to needed objects
                IntPtr hdc = g.GetHdc();
                IntPtr memDC = CreateCompatibleDC(hdc);
                IntPtr hBitmap = alphaBitmap.GetHbitmap();
                IntPtr hOld = SelectObject(memDC, hBitmap);

                alphaBmp.GetBitmapFromDC = true;
                alphaBmp.stru = memDC;
                alphaBmp.paras = new MFVideoAlphaBitmapParams();
                alphaBmp.paras.dwFlags = MFVideoAlphaBitmapFlags.Alpha | MFVideoAlphaBitmapFlags.DestRect;

                // calculate destination rectangle
                MFVideoNormalizedRect mfNRect = new MFVideoNormalizedRect();
                //NormalizedRect nRect = GetDestRectangle(width, height, subtitleLines);

                mfNRect.top = 0.5f;// nRect.top;
                mfNRect.left = 0.5f;// nRect.left;
                mfNRect.right = 1.0f;//nRect.right;
                mfNRect.bottom = 1.0f;// nRect.bottom;

                // used when viewing half side by side anaglyph video that is stretched to full width
                //if (FrameMode == Mars.FrameMode.HalfSideBySide)
                //{
                //    mfNRect.left /= 2;
                //    mfNRect.right /= 2;
                //}

                alphaBmp.paras.nrcDest = mfNRect;

                // calculate source rectangle (full subtitle bitmap)
                MFRect rcSrc = new MFRect();
                rcSrc.bottom = alphaBitmap.Height;
                rcSrc.right = alphaBitmap.Width;
                rcSrc.top = 0;
                rcSrc.left = 0;

                alphaBmp.paras.rcSrc = rcSrc;

                // apply 1-bit transparency 
                //System.Drawing.Color colorKey = System.Drawing.Color.White;
                //alphaBmp.paras.clrSrcKey = ColorTranslator.ToWin32(colorKey);

                // 90% visible
                alphaBmp.paras.fAlpha = 0.5F;

                // set the bitmap to the evr mixer
                mixBmp.SetAlphaBitmap(alphaBmp);

                // cleanup
                SelectObject(memDC, hOld);
                DeleteDC(memDC);
                g.ReleaseHdc();
            }
        }

        private void menuItem15_Click(object sender, EventArgs e)
        {
            mixBmp.ClearAlphaBitmap();
        }

        private void menuItem16_Click(object sender, EventArgs e)
        {
            if (cpsett != null)
            {
                int range;
                int hr = cpsett.GetInt(EVRCPSetting.NOMINAL_RANGE, out range);
                if (hr >= 0)
                {
                    if ((MFNominalRange)range == MFNominalRange.MFNominalRange_16_235)
                        cpsett.SetInt(EVRCPSetting.NOMINAL_RANGE, (int)MFNominalRange.MFNominalRange_0_255);
                    else
                        cpsett.SetInt(EVRCPSetting.NOMINAL_RANGE, (int)MFNominalRange.MFNominalRange_16_235);
                }
            }
        }
    }

    internal enum PlayState
    {
        Stopped,
        Paused,
        Running,
        Init
    };

    internal enum MediaType
    {
        Audio,
        Video
    }

    public enum EXECUTION_STATE : uint
    {
        ES_SYSTEM_REQUIRED = 0x00000001,
        ES_DISPLAY_REQUIRED = 0x00000002,
        ES_USER_PRESENT = 0x00000004,
        ES_AWAYMODE_REQUIRED = 0x00000040,
        ES_CONTINUOUS = 0x80000000
    }

    public enum OpenClipMode
    {
        None,
        File,
        Folder,
        Url
    }

    internal enum MenuMode
    {
        No, Buttons, Still
    }
}