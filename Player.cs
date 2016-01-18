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
using DirectShowLib.Utils;

using MediaFoundation;
using MediaFoundation.EVR;
using DirectShowLib.Dvd;

namespace babgvant.EVRPlay
{
    internal class Player : NativeWindow, IDisposable
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);
        [DllImport("ole32.dll")]
        static extern int OleLoadFromStream(IStream pStm, [In] ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppvObj);
        [DllImport("ole32.dll")]
        static extern int OleSaveToStream(IPersistStream pPStm, IStream pStm);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SystemParametersInfo(SPI uiAction, uint uiParam, IntPtr pvParam, SPIF fWinIni);

        private PlaySettings ps = new PlaySettings();
        private bool disposed = false;

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

        private List<string> filename = new List<string>();
        private int currentIndex = 0;
        private bool isAudioOnly = false;
        public bool isFullScreen = false;
        private int currentVolume = VolumeFull;
        private PlayState currentState = PlayState.Stopped;
        private double currentPlaybackRate = 1.0;
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

        private DvdHMSFTimeCode currnTime = new DvdHMSFTimeCode();		// copy of current playback states, see OnDvdEvent()
        private int currnTitle;
        private int currnChapter;
        private DvdDomain currnDomain;
        private MenuMode menuMode;
        private double dvdPlayRate = 1.0;
        private float zoomAmt = 1.0f;
        private bool disabledScreenSaver = false;
        private Form container = null;

        private string BookmarkFile
        {
            get
            {
                string bookmarkDir = MainForm.BaseBookmarkPath;
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


        public Player(Form container)
        {
            this.container = container;
            this.container.Resize += new EventHandler(container_Resize);
            this.container.Move += new EventHandler(container_Move);
        }

        void container_Move(object sender, EventArgs e)
        {
            MoveVideoWindow();
        }

        void container_Resize(object sender, EventArgs e)
        {
            MoveVideoWindow();
        }

        internal void PlayMovieInWindow(string filename)
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
                IBaseFilter sourceFilter = null;

                try
                {
                    hr = graphBuilder.AddSourceFilter(filename, "Source", out sourceFilter);
                    DsError.ThrowExceptionForHR(hr);

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
                }
                finally
                {
                    if (sourceFilter != null)
                        Marshal.ReleaseComObject(sourceFilter);
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


                // Setup the video window
                //hr = this.videoWindow.put_Owner(this.Handle);
                //DsError.ThrowExceptionForHR(hr);
                this.evrDisplay.SetVideoWindow(container.Handle);

                //hr = this.videoWindow.put_WindowStyle(WindowStyle.Child | WindowStyle.ClipSiblings | WindowStyle.ClipChildren);
                //DsError.ThrowExceptionForHR(hr);

                hr = InitVideoWindow(1, 1);
                DsError.ThrowExceptionForHR(hr);

                GetFrameStepInterface();


                // Complete window initialization
                //CheckSizeMenu(menuFileSizeNormal);
                //this.isFullScreen = false;
                this.currentPlaybackRate = 1.0;
                //UpdateMainTitle();

                container.Focus();

                //pre-roll the graph
                hr = this.mediaControl.Pause();
                DsError.ThrowExceptionForHR(hr);

                // Run the graph to play the media file
                hr = this.mediaControl.Run();
                DsError.ThrowExceptionForHR(hr);

                if (commWatcher != null)
                    commWatcher.Dispose();

                string commPath = Path.Combine(Path.GetDirectoryName(filename), Commercials.GetEdlFilename(filename));

                ReadComm(commPath);

                commWatcher = new FileSystemWatcher(Path.GetDirectoryName(filename), Commercials.GetEdlFilename(filename));
                commWatcher.Changed += new FileSystemEventHandler(commWatcher_Changed);
                commWatcher.Created += new FileSystemEventHandler(commWatcher_Changed);
                //commWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
                commWatcher.EnableRaisingEvents = true;

                MoveToBookmark();

                this.currentState = PlayState.Running;
                //if (isFullScreen)
                //    tmMouseMove.Enabled = true;
            }
            else
            {
                //MessageBox.Show("EVR cannot be loaded on this PC");
                using (EPDialog ed = new EPDialog())
                    ed.ShowDialog("EVR Error", "The Enhanced Video Renderer cannot be loaded on this PC", 30);
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
                //MessageBox.Show("Error skipping: {0}", ex.Message);
                using (EPDialog ed = new EPDialog())
                    ed.ShowDialog("Error", string.Format("Error skipping: {0}", ex.Message), 10);
            }
        }

        void MoveToBookmark()
        {
            if (File.Exists(BookmarkFile))
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

                            hr = OleLoadFromStream(stream, ref stateGuid, out state);
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

                            hr = OleSaveToStream((IPersistStream)pStateData, stream);
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
            //if (this.InvokeRequired)
            //    this.Invoke(new TFS(ReadComm), new object[] { objEdlPath });
            //else
            //{
                string edlPath = (string)objEdlPath;
                try
                {
                    if (File.Exists(edlPath))
                    {
                        FileLogger.Log("Edl file exists: {0}", edlPath);
                        commList = Commercials.ReadEdlCommercials(edlPath);
                        //if (!tmPosition.Enabled)
                        //    tmPosition.Enabled = true;
                    }
                }
                catch (Exception ex)
                {
                    FileLogger.Log("Error reading commmerical segments: {0}", ex.ToString());
                }
            //}
        }

        private void SetupEvrDisplay()
        {
            IMFGetService mfgs = evrRenderer as IMFGetService;
            if (mfgs != null)
            {
                try
                {
                    object objDisplay = null;
                    mfgs.GetService(MFServices.MR_VIDEO_RENDER_SERVICE,
                        typeof(IMFVideoDisplayControl).GUID,
                        out objDisplay
                        );
                    FileLogger.Log("PlayMovieInWindow: MR_VIDEO_RENDER_SERVICE");
                    evrDisplay = objDisplay as IMFVideoDisplayControl;

                    MediaFoundation.Misc.MFSize videoSize = new MediaFoundation.Misc.MFSize();
                    MediaFoundation.Misc.MFSize ar = new MediaFoundation.Misc.MFSize();
                    evrDisplay.GetNativeVideoSize(videoSize, ar);

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

        private int InitVideoWindow(int nMultiplier, int nDivider)
        {
            int hr = 0;
            int lHeight, lWidth;

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
            lWidth = videoSize.cx;
            lHeight = videoSize.cy;

            //EnablePlaybackMenu(true, MediaType.Video);

            if (!this.isFullScreen && ps.MaxInitialHeight > 0 && ps.MaxInitialWidth > 0)
            {
                if ((lWidth > ps.MaxInitialWidth || lHeight > ps.MaxInitialHeight) && (lWidth > container.ClientRectangle.Width || lHeight > container.ClientRectangle.Height))
                {
                    lWidth = ps.MaxInitialWidth;
                    lHeight = ps.MaxInitialHeight;
                }
                //if (lHeight > ps.MaxInitialHeight && lHeight > ClientRectangle.Height)
                //    lHeight = ps.MaxInitialHeight;
            }
            else
            {
                lWidth = container.ClientRectangle.Width;
                lHeight = container.ClientRectangle.Height;
            }

            // Account for requests of normal, half, or double size
            lWidth = lWidth * nMultiplier / nDivider;
            lHeight = lHeight * nMultiplier / nDivider;

            container.ClientSize = new Size(lWidth, lHeight);
            Application.DoEvents();

            //hr = this.videoWindow.SetWindowPosition(0, 0, lWidth, lHeight);
            //MFVideoNormalizedRect sRect = new MFVideoNormalizedRect();
            //sRect.top = 0;
            //sRect.left = 0;
            //sRect.right = 1;
            //sRect.bottom = 1;
            //MediaFoundation.Misc.MFRect dRect = new MediaFoundation.Misc.MFRect();
            //dRect.top = 0 - (ps.OverscanHeight / 2);
            //dRect.left = 0 - (ps.OverscanWidth / 2);
            //dRect.right = lWidth + (ps.OverscanWidth / 2);//this.Width;
            //dRect.bottom = lHeight + (ps.OverscanHeight / 2);//this.Height;
            //this.evrDisplay.SetVideoPosition(sRect, dRect);
            MoveVideoWindow();

            //if (bf != null)
            //    bf.Hide();
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

        private void MoveVideoWindow()
        {
            //int hr = 0;

            // Track the movement of the container window and resize as needed
            if (this.evrDisplay != null)
            {
                MFVideoNormalizedRect sRect = new MFVideoNormalizedRect();
                sRect.top = 0;
                sRect.left = 0;
                sRect.right = 1;
                sRect.bottom = 1;
                MediaFoundation.Misc.MFRect dRect = new MediaFoundation.Misc.MFRect();
                //dRect.top = 0;
                //dRect.left = 0;
                //dRect.right = ClientRectangle.Width;//this.Width;
                //dRect.bottom = ClientRectangle.Height;//this.Height;
                dRect.top = 0 - (ps.OverscanHeight / 2);
                dRect.left = 0 - (ps.OverscanWidth / 2);
                dRect.right = container.ClientRectangle.Width + (ps.OverscanWidth / 2);//this.Width;
                dRect.bottom = container.ClientRectangle.Height + (ps.OverscanHeight / 2);//this.Height;
                this.evrDisplay.SetVideoPosition(sRect, dRect);
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
                    }
                    catch { }
                    //#endif

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
                        if (dvdGraph != null)
                            hr = this.mediaEventEx.SetNotifyWindow(IntPtr.Zero, WM.DVD_EVENT, IntPtr.Zero);
                        else
                            hr = this.mediaEventEx.SetNotifyWindow(IntPtr.Zero, WM.NULL, IntPtr.Zero);
                        //DsError.ThrowExceptionForHR(hr);
                    }

                    if (evrDisplay != null)
                    {
                        //evrDisplay.SetVideoWindow(IntPtr.Zero);
                        Marshal.ReleaseComObject(evrDisplay);
                    }
                    evrDisplay = null;
                    if (this.evrRenderer != null)
                        Marshal.ReleaseComObject(evrRenderer);
                    evrRenderer = null;

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

        private int SetRate(double rate)
        {
            int hr = 0;

            if (dvdCtrl != null)
            {
                dvdPlayRate = rate;
                hr = dvdCtrl.PlayForwards(rate, DvdCmdFlags.SendEvents, out cmdOption);
                DsError.ThrowExceptionForHR(hr);

                if (cmdOption != null)
                {
                    pendingCmd = true;
                }
            }
            else if (this.mediaSeeking != null)
            {
                hr = this.mediaSeeking.SetRate(rate);
                if (hr >= 0)
                {
                    this.currentPlaybackRate = rate;
                    //UpdateMainTitle();
                }
            }

            return hr;
        }

        public void PauseClip()
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

        }

        public void StopClip()
        {
            int hr = 0;
            DsLong pos = new DsLong(0);
            dvdPlayRate = 0.0;
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

            //UpdateMainTitle();

            StopDialog sd = new StopDialog();
            switch (sd.ShowDialog())
            {
                case DialogResult.OK:
                    PauseClip();
                    break;
                case DialogResult.Cancel:
                    container.Close();
                    Application.DoEvents();
                    break;
                case DialogResult.Retry:
                    hr = this.mediaSeeking.SetPositions(pos, AMSeekingSeekingFlags.AbsolutePositioning, null, AMSeekingSeekingFlags.NoPositioning);
                    PauseClip();
                    break;
            }
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
            //UpdateMainTitle();
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
                // Free memory associated with callback, since we're not using it
                hr = this.mediaEventEx.FreeEventParams(evCode, evParam1, evParam2);

                // If this is the end of the clip, reset to beginning
                if (evCode == EventCode.Complete)
                {
                    //DsLong pos = new DsLong(0);
                    // Reset to first frame of movie
                    StopClip();
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
            if (currentIndex < filename.Count)
                PlayMovieInWindow(filename[currentIndex]);
        }

        /*
         * WinForm Related methods
         */

        private int iChar;

        void OnDvdEvent()
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
                            //UpdateMainTitle();
                            break;
                        }
                    case EventCode.DvdChapterStart:
                        {
                            currnChapter = p1.ToInt32();
                            //UpdateMainTitle();
                            break;
                        }
                    case EventCode.DvdTitleChange:
                        {
                            currnTitle = p1.ToInt32();
                            //UpdateMainTitle();
                            break;
                        }
                    case EventCode.DvdDomainChange:
                        {
                            currnDomain = (DvdDomain)p1;
                            //UpdateMainTitle();
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

                switch (m.Msg)
                {
                    case (int)WM.GRAPH_NOTIFY:
                        HandleGraphEvent();
                        break;
                    case (int)WM.DVD_EVENT:
                        OnDvdEvent();
                        break;
                }
            }
            catch (Exception ex)
            {
                FileLogger.Log("Error in WndProc: {0}", ex.ToString());
            }

            base.WndProc(ref m);
        }

        #region IDisposable Members

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!disposed)
                {
                    CloseInterfaces();
                    disposed = true;
                }
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
