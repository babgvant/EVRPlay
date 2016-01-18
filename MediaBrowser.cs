using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;

using DirectShowLib;

namespace babgvant.EVRPlay
{
    public partial class MediaBrowser : ClientForm, IMessageFilter
    {
        public const string BUTTON_BACK = "btnBack";

        private CachedData data = null;
        private string baseUrl = string.Empty;
        private BrowseMode lastMode = BrowseMode.None;
        private bool disposed = false;

        public MediaBrowser(BrowseMode mode)
        {
            InitializeComponent();
            lastMode = mode;
        }

        private void MediaBrowser_Load(object sender, EventArgs e)
        {
            Cursor.Position = new Point(this.Location.X + 10, this.Location.Y + 10);//position the cursor w/ in the bounds of the form

            Match m = Regex.Match(mfOwner.ps.SageWebserver, @"https?://[\w|.]+(?::\d+)/sage");

            if (m.Success)
                baseUrl = m.Value;
            else
                using (EPDialog ed = new EPDialog())
                    ed.ShowDialog("Error", "Could not determin base url", 10, mfOwner);


            tcBrowse.SelectedIndex = 0;
            BuildList(lastMode);
        }

        private bool GetXmlFromSage(BrowseMode mode)
        {
            string Url = string.Empty;
            int cacheFor = 0;

            if (lastMode != mode || data == null || data.HasExpired)
            {
                lastMode = mode;
                switch (mode)
                {
                    case BrowseMode.Recordings:
                        Url = string.Format("{0}{1}", baseUrl, mfOwner.ps.SageRecording);
                        cacheFor = mfOwner.ps.CacheRecording;
                        break;
                    case BrowseMode.Media:
                        Url = string.Format("{0}{1}", baseUrl, mfOwner.ps.SageMedia);
                        cacheFor = mfOwner.ps.CacheMedia;
                        break;
                }

                if (MainForm.DebugMode)
                    Url = "http://microsoft.com";

                using (DownloadForm df = new DownloadForm(Url, this.mfOwner.ps.BrowseAttempts))
                {
                    if (df.ShowDialog(this) == DialogResult.OK || MainForm.DebugMode)
                    {
                        //get xml document
                        if (MainForm.DebugMode)
                        {
                            using (StreamReader sr = new StreamReader(@"C:\temp\recordings.xml"))
                            {
                                data = new CachedData(sr.ReadToEnd(), cacheFor);
                            }
                        }
                        else
                        {
                            data = new CachedData(df.Result, cacheFor);
                        }

                        return true;
                    }
                    else
                    {
                        //display the error
                        using (EPDialog ed = new EPDialog())
                            ed.ShowDialog("Error", df.Result, 10, mfOwner);
                        return false;
                    }
                }
            }
            else
                return true;
        }

        private void BuildList(BrowseMode mode)
        {
            if (GetXmlFromSage(mode))
            {
                DataView showView = null;
                DataTable dtRec = null;

                switch (mode)
                {
                    case BrowseMode.Recordings:
                        tcBrowse.SelectedIndex = 0;
                        showView = data.Data.DefaultViewManager.CreateDataView(data.Data.Tables["show"]);
                        showView.Sort = "title";
                        dtRec = showView.ToTable(true, "title");
                        CreateControls(dtRec, new BrowserState("title, originalAirDate", "title", 1, BrowseMode.Recordings, flpRecItems));
                        break;
                    case BrowseMode.Media:
                        tcBrowse.SelectedIndex = 1;
                        showView = data.Data.DefaultViewManager.CreateDataView(data.Data.Tables["show"]);
                        showView.Sort = "episode";
                        dtRec = showView.ToTable(true, "episode");
                        CreateControls(dtRec, new BrowserState("episode", "episode", 1, BrowseMode.Media, flpMedia));
                        break;
                }
            }
        }

        private void CreateControls(DataTable dtPivot, BrowserState tag)
        {
            tag.Panel.Controls.Clear();

            switch (tag.Mode)
            {
                case BrowseMode.Media:
                    flpMediaTop.Controls.Clear();
                    switch (tag.Level)
                    {
                        case 1:
                            scMedia.SplitterDistance = 1;

                            if (tag.Panel.Controls.Count > 0)
                                tag.Panel.Controls[0].Select();

                            break;
                        case 2:
                            scMedia.SplitterDistance = 100;

                            Button btnBack = new Button();
                            btnBack.Width = flpMediaTop.Width / 8 - 10;
                            btnBack.Text = "Back";
                            btnBack.Height = flpMediaTop.Height - 10;
                            btnBack.Click += new EventHandler(btnBack_Click);
                            btnBack.Tag = BrowseMode.Media;
                            btnBack.Name = BUTTON_BACK;
                            btnBack.GotFocus += new EventHandler(b_GotFocus);
                            btnBack.LostFocus += new EventHandler(b_LostFocus); 
                            flpMediaTop.Controls.Add(btnBack);
                            break;
                    }
                    break;
                case BrowseMode.Recordings:
                    flpRecordingTop.Controls.Clear();
                    switch (tag.Level)
                    {
                        case 1:
                            scRecording.SplitterDistance = 1;

                            if (tag.Panel.Controls.Count > 0)
                                tag.Panel.Controls[0].Select();

                            break;
                        case 2:
                            scRecording.SplitterDistance = 100;

                            Button btnBack = new Button();
                            btnBack.Width = flpRecordingTop.Width / 8 - 10;
                            btnBack.Text = "Back";
                            btnBack.Height = flpRecordingTop.Height - 10;
                            btnBack.Click += new EventHandler(btnBack_Click);
                            btnBack.ForeColor = System.Drawing.Color.Black;
                            btnBack.Tag = BrowseMode.Recordings;
                            btnBack.Name = BUTTON_BACK;
                            btnBack.GotFocus += new EventHandler(b_GotFocus);
                            btnBack.LostFocus += new EventHandler(b_LostFocus); 
                            flpRecordingTop.Controls.Add(btnBack);

                            Label lblTitle = new Label();
                            lblTitle.Text = EscapeControlText(tag.Value);
                            lblTitle.Width = flpRecordingTop.Width / 2 - 10;
                            lblTitle.Height = flpRecordingTop.Height - 10;
                            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
                            lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                            flpRecordingTop.Controls.Add(lblTitle);

                            break;
                    }
                    break;
            }

            for (int i = 0; i < dtPivot.Rows.Count; i++)
            {
                DataRow dr = dtPivot.Rows[i];
                BrowserState nBs = new BrowserState(tag.Sort, tag.Filter, tag.Level, tag.Mode, tag.Panel);


                StringBuilder sbText = new StringBuilder();
                Control b = null;

                switch (tag.Mode)
                {
                    case BrowseMode.Recordings:
                        b = new Button();
                        switch (tag.Level)
                        {
                            case 1:
                                sbText.AppendFormat("{0} - ", dr["title"]);
                                nBs.Value = dr["title"].ToString();
                                b.Size = new Size(tag.Panel.Width / 4 - 10, mfOwner.ps.MBButtonHeight);
                                break;
                            case 2:
                                if (dtPivot.Columns.Contains("episode") && !string.IsNullOrEmpty(dr["episode"].ToString()))
                                    sbText.AppendFormat("{0} - ", dr["episode"]);
                                if (dtPivot.Columns.Contains("description") && !string.IsNullOrEmpty(dr["description"].ToString()))
                                    sbText.AppendFormat("{0} - ", dr["description"]);
                                if (sbText.Length == 0)
                                {
                                    sbText.AppendFormat("{0} - ", dr["title"]);
                                    DataRow[] drs = data.Data.Tables["show"].Select(string.Format("epgId = '{0}'", dr["epgId"]));

                                    if (drs.Length > 0)
                                    {
                                        DataRow[] sa = drs[0].GetChildRows("show_airing");
                                        if (sa.Length > 0)
                                        {
                                            sbText.AppendFormat("{0} - ", sa[0]["startTime"]);
                                        }
                                    }
                                }
                                nBs.Filter = "epgId";
                                nBs.Value = dr["epgId"].ToString();
                                b.Size = new Size(tag.Panel.Width / 2 - 10, mfOwner.ps.MBButtonHeight);
                                break;
                        }
                        b.Click += new EventHandler(b_Click);
                        b.GotFocus += new EventHandler(b_GotFocus);
                        b.LostFocus += new EventHandler(b_LostFocus);  
                        break;
                    case BrowseMode.Media:
                        switch (tag.Level)
                        {
                            case 1:
                                b = new Button();

                                sbText.AppendFormat("{0} - ", dr["episode"]);
                                nBs.Value = dr["episode"].ToString();
                                b.Size = new Size(tag.Panel.Width / 4 - 10, mfOwner.ps.MBButtonHeight);
                                b.Click += new EventHandler(b_Click);
                                break;
                            case 2:
                                nBs.Filter = "epgId";
                                nBs.Value = dr["epgId"].ToString();

                                Button btnPlay = new Button();
                                btnPlay.Width = flpMediaTop.Width / 8 - 10;
                                btnPlay.Text = "Watch";
                                btnPlay.Click += new EventHandler(b_Click);
                                btnPlay.Height = flpMediaTop.Height - 10;
                                btnPlay.Tag = nBs;
                                btnPlay.Name = "btnPlay";
                                btnPlay.GotFocus += new EventHandler(b_GotFocus);
                                btnPlay.LostFocus += new EventHandler(b_LostFocus); 
                                flpMediaTop.Controls.Add(btnPlay);
                                btnPlay.Select();

                                Label lblTitle = new Label();
                                lblTitle.Text = EscapeControlText(tag.Value);
                                lblTitle.Width = flpMediaTop.Width / 2 - 10;
                                lblTitle.Height = flpMediaTop.Height - 10;
                                lblTitle.TextAlign = ContentAlignment.MiddleCenter;

                                lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                                flpMediaTop.Controls.Add(lblTitle);

                                try
                                {
                                    List<string> fPath = GetSegmentValue(nBs.Filter, nBs.Value, "filePath");
                                    if (fPath.Count > 0)
                                    {
                                        string imgPath = Regex.Replace(mfOwner.ps.SageWebserver, "/sage/home", string.Format(@"/sage/MediaFileThumbnail?FileName={0}", fPath[0]), RegexOptions.IgnoreCase);
                                        byte[] imgBytes = DownloadFile(imgPath);

                                        if (imgBytes != null && imgBytes.Length > 0)
                                        {
                                            PictureBox pb = new PictureBox();
                                            //pb.ImageLocation = imgPath;
                                            //pb.Height = flpMediaTop.Height - 10;
                                            using (MemoryStream ms = new MemoryStream(imgBytes))
                                            {
                                                //using (Bitmap bmp = new Bitmap(ms))
                                                //{
                                                Bitmap bmp = new Bitmap(ms);
                                                pb.Image = bmp;
                                                pb.Height = bmp.Height;
                                                pb.Width = bmp.Width;
                                                //b.Name = string.Format("{0}_{1}", tag.Panel.Name, tag.Panel.Controls.Count);
                                                tag.Panel.Controls.Add(pb);
                                                //}
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    FileLogger.Log(string.Format("Error: {0}", ex.Message));
                                }

                                b = new Label();

                                if (dtPivot.Columns.Contains("episode") && !string.IsNullOrEmpty(dr["episode"].ToString()))
                                    sbText.AppendFormat("{0} - ", dr["episode"]);
                                if (dtPivot.Columns.Contains("description") && !string.IsNullOrEmpty(dr["description"].ToString()))
                                    sbText.AppendFormat("{0} - ", dr["description"].ToString());

                                b.Size = new Size(tag.Panel.Width - 50, tag.Panel.Height - 20);
                                break;
                        }
                        break;
                }

                if (sbText.Length > 0)
                    b.Text = EscapeControlText(sbText.ToString().Substring(0, sbText.Length - 3));
                b.Tag = nBs;

                b.ForeColor = SystemColors.ControlText;
                b.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

                b.Name = string.Format("{0}_{1}", tag.Panel.Name, tag.Panel.Controls.Count);
                b.GotFocus += new EventHandler(b_GotFocus);
                b.LostFocus += new EventHandler(b_LostFocus);        
                tag.Panel.Controls.Add(b);
            }

            switch (tag.Mode)
            {
                case BrowseMode.Media:
                    this.ActiveControl = scMedia;
                    switch (tag.Level)
                    {
                        case 1:
                            if (tag.Panel.Controls.Count > 0)
                            {
                                tag.Panel.Controls[0].Select();
                            }
                            break;
                        case 2:

                            break;
                    }
                    
                    break;
                case BrowseMode.Recordings:
                    this.ActiveControl = scRecording;
                    scRecording.Select();
                    tag.Panel.Focus();
                    //switch (tag.Level)
                    //{
                    //    case 1:
                            if (tag.Panel.Controls.Count > 0)
                                tag.Panel.Controls[0].Select();

                    //        break;
                    //    case 2:

                    //        break;
                    //}
                    
                    break;
            }
            //this.Focus();
            //tag.Panel.Focus();
        }

        void b_LostFocus(object sender, EventArgs e)
        {
            Control c = sender as Control;
            if (c != null)
            {
                c.BackColor = System.Drawing.Color.Transparent;
            }
        }

        void b_GotFocus(object sender, EventArgs e)
        {
            Control c = sender as Control;
            if (c != null)
            {
                c.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            }
        }

        private string FormatUrl(string nUrl)
        {
            string serverPath = Regex.Replace(mfOwner.ps.SageWebserver, @"https?://", "", RegexOptions.IgnoreCase);
            string formatedPath = Regex.Replace(serverPath, "/sage/home", nUrl, RegexOptions.IgnoreCase);
            Match mProtocol = Regex.Match(mfOwner.ps.SageWebserver, @"https?://", RegexOptions.IgnoreCase);
            if (mProtocol.Success)
                //return string.Format("{3}{0}:{1}@{2}", mfOwner.ps.SWUsername, mfOwner.ps.SWPassword, formatedPath, mProtocol.Value);
                return string.Format("{1}{0}", formatedPath, mProtocol.Value);
            else
                return string.Empty;
        }

        private byte[] DownloadFile(string imgUrl)
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    wc.Credentials = new NetworkCredential(mfOwner.ps.SWUsername, mfOwner.ps.SWPassword);
                    return wc.DownloadData(imgUrl);
                }
            }
            catch (Exception ex)
            {
                FileLogger.Log("Error DownloadFile: {0}", ex.Message);
                return null;
            }
        }

        private string EscapeControlText(string inText)
        {
            if (!string.IsNullOrEmpty(inText))
                return inText.Replace("&", "&&");
            else
                return inText;
        }

        void btnBack_Click(object sender, EventArgs e)
        {
            Button b = sender as Button;

            BuildList((BrowseMode)b.Tag);
        }

        void b_Click(object sender, EventArgs e)
        {
            Button b = sender as Button;
            if (b != null)
            {
                BrowserState tState = b.Tag as BrowserState;
                switch (tState.Level)
                {
                    case 1:
                        tState.Level++;
                        DataView showView = data.Data.DefaultViewManager.CreateDataView(data.Data.Tables["show"]);
                        showView.Sort = tState.Sort;
                        showView.RowFilter = string.Format("{0} = '{1}'", tState.Filter, tState.Value.Replace("'", "''"));
                        DataTable dtDetail = showView.ToTable();//true, tState.SecondLevel);
                        CreateControls(dtDetail, tState);
                        break;
                    case 2:
                        List<string> fPath = GetSegmentValue(tState.Filter, tState.Value, "filePath");
                        StringBuilder sbPls = new StringBuilder();

                        for (int i = 0; i < fPath.Count; i++)
                            sbPls.AppendFormat("File{0}={1}{2}", i, fPath[i], Environment.NewLine);

                        MainForm.ParsePlsText(mfOwner, sbPls.ToString());
                        if (mfOwner.OpenClip(OpenClipMode.None))
                            this.Hide();
                        else
                        {
                            using (EPDialog ed = new EPDialog())
                                ed.ShowDialog("Error", "Could not play file", 20, mfOwner);
                        }
                        break;
                }
            }
        }

        private List<string> GetSegmentValue(string filter, string value, string segmentValue)
        {
            List<string> attribute = new List<string>();
            DataRow[] drs = data.Data.Tables["show"].Select(string.Format("{0} = '{1}'", filter, value.Replace("'", "''")));

            if (drs.Length > 0)
            {
                DataRow[] sa = drs[0].GetChildRows("show_airing");
                if (sa.Length > 0)
                {
                    DataRow[] am = sa[0].GetChildRows("airing_mediafile");
                    if (am.Length > 0)
                    {
                        DataRow[] ms = am[0].GetChildRows("mediafile_segmentList");
                        if (ms.Length > 0)
                        {
                            DataRow[] ss = ms[0].GetChildRows("segmentList_segment");
                            if (ss.Length > 0)
                            {
                                for (int i = 0; i < ss.Length; i++)
                                {
                                    string fp = ss[i][segmentValue].ToString();
                                    attribute.Add(fp);
                                }
                            }
                        }
                    }
                }
            }
            return attribute;
        }

        private void tcBrowse_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                switch (tcBrowse.SelectedIndex)
                {
                    case 0:
                        BuildList(BrowseMode.Recordings);
                        break;
                    case 1:
                        BuildList(BrowseMode.Media);
                        break;
                }
            }
            catch (Exception ex)
            {
                FileLogger.Log("Error: {0}", ex.Message);
            }
        }

        public void ChangeTab(BrowseMode mode)
        {
            if (mode == BrowseMode.None)
            {
                switch (tcBrowse.SelectedIndex)
                {
                    case 0:
                        tcBrowse.SelectedIndex = 1;
                        break;
                    case 1:
                        tcBrowse.SelectedIndex = 0;
                        break;
                }
            }
            else
                BuildList(mode);
        }

        private Button FindBackButton()
        {
            Control[] btnBack = this.Controls.Find(BUTTON_BACK, true);
            if (btnBack.Length > 0)
                return btnBack[0] as Button;
            else
                return null;
        }

        private bool BrowseControl(BrowseAction action)
        {
            //Control c = this.ActiveControl;
            //c.Container.Components.
            Control c = FindFocusedControl(this);
            if (c != null)
            {
                if (c.Name == BUTTON_BACK)
                {
                    BrowseMode mode = (BrowseMode)c.Tag;
                    FlowLayoutPanel flp = null;

                    switch (mode)
                    {
                        case BrowseMode.Media:
                            flp = flpMedia;
                            break;
                        case BrowseMode.Recordings:
                            flp = flpRecItems;
                            break;
                    }

                    switch (action)
                    {
                        case BrowseAction.Down:
                        case BrowseAction.PageDown:
                            if (flp != null && flp.Controls.Count > 0)
                            {
                                flp.Controls[0].Select();
                                return true;
                            }
                            break;
                        case BrowseAction.Up:
                        case BrowseAction.PageUp:
                            if (flp != null && flp.Controls.Count > 0)
                            {
                                flp.Controls[flp.Controls.Count-1].Select();
                                return true;
                            } 
                            break;                            
                    }
                }
                else
                {
                    Match nm = Regex.Match(c.Name, @"(?<container>\w+_)(?<index>\d+)", RegexOptions.Compiled);
                    if (nm.Success)
                    {
                        int index = Convert.ToInt32(nm.Groups["index"].Value);
                        string fItem;
                        Control[] fControls;
                        int multiplier = c.Parent.Width / c.Width;

                        switch (action)
                        {
                            case BrowseAction.Down:
                                fItem = string.Format("{0}{1}", nm.Groups["container"].Value, index + multiplier);
                                fControls = c.Parent.Controls.Find(fItem, true);
                                if (fControls.Length > 0)
                                {
                                    fControls[0].Select();
                                    return true;
                                }
                                break;
                            case BrowseAction.Up:
                                if (index == 0)
                                {
                                    Button btnBack = FindBackButton();
                                    if (btnBack != null)
                                    {
                                        //btnBack.Tag = c.Name;
                                        btnBack.Select();
                                        return true;
                                    }
                                }
                                else
                                {
                                    fItem = string.Format("{0}{1}", nm.Groups["container"].Value, index - multiplier);
                                    fControls = c.Parent.Controls.Find(fItem, true);
                                    if (fControls.Length > 0)
                                    {
                                        fControls[0].Select();
                                        return true;
                                    }
                                }
                                break;
                            case BrowseAction.PageDown:
                                fItem = string.Format("{0}{1}", nm.Groups["container"].Value, index + (mfOwner.ps.PageSize * multiplier));
                                fControls = c.Parent.Controls.Find(fItem, true);
                                if (fControls.Length > 0)
                                {
                                    fControls[0].Select();
                                    return true;
                                }
                                else
                                {
                                    if (c.Parent.Controls.Count > 0)
                                    {
                                        if (index < c.Parent.Controls.Count - 1)
                                            c.Parent.Controls[c.Parent.Controls.Count - 1].Select();
                                        else
                                            c.Parent.Controls[0].Select();
                                        return true;
                                    }
                                }
                                break;
                            case BrowseAction.PageUp:
                                if (index == 0)
                                {
                                    Button btnBack = FindBackButton();
                                    if (btnBack != null)
                                    {
                                        //btnBack.Tag = c.Name;                                        
                                        btnBack.Select();
                                        return true;
                                    }
                                }

                                fItem = string.Format("{0}{1}", nm.Groups["container"].Value, index - (mfOwner.ps.PageSize * multiplier));
                                fControls = c.Parent.Controls.Find(fItem, true);
                                if (fControls.Length > 0)
                                {
                                    fControls[0].Select();
                                    return true;
                                }
                                else
                                {
                                    if (c.Parent.Controls.Count > 0)
                                    {
                                        if ((index < multiplier && index != 0) || index == c.Parent.Controls.Count - 1)
                                            c.Parent.Controls[0].Select();
                                        else
                                            c.Parent.Controls[c.Parent.Controls.Count - 1].Select();                                            
                                        return true;
                                    }
                                }

                                break;
                        }
                    }
                }
            }
            return false;
        }

        private Control FindFocusedControl(Control control)
        {
            Control c = null;

            foreach (Control oControl in control.Controls)
            {
                if (oControl.Focused)
                {
                    c = oControl;
                    break;
                }
                else if (oControl.HasChildren)
                {
                    c = FindFocusedControl(oControl);
                    if (c != null)
                        break;
                }
            }
            return c;
        }

        #region IMessageFilter Members

        public bool PreFilterMessage(ref Message m)
        {
            bool handled = false;
            //if (m.Msg == (int)WM.KEYDOWN)
            //{
                //if (Common.ShouldFilter(m.HWnd, this))
                //if (this.Visible)
                //{
                    switch ((WM)m.Msg)
                    {
                        case WM.KEYDOWN:
                            switch ((NativeMethods2.VirtualKeys)m.WParam)
                            {
                                case NativeMethods2.VirtualKeys.DOWN:
                                    //this.Controls.Find(
                                    handled = BrowseControl(BrowseAction.Down);
                                    break;
                                case NativeMethods2.VirtualKeys.UP:
                                    handled = BrowseControl(BrowseAction.Up);
                                    break;
                                case NativeMethods2.VirtualKeys.BROWSER_BACK:
                                case NativeMethods2.VirtualKeys.BACK: //backspace
                                    Button back = FindBackButton();
                                    if (back != null)
                                    {
                                        btnBack_Click(back, null);
                                        handled = true;
                                    }
                                    break;
                                case NativeMethods2.VirtualKeys.PRIOR: //page up
                                    handled = BrowseControl(BrowseAction.PageUp);
                                    break;
                                case NativeMethods2.VirtualKeys.NEXT: //page down
                                    handled = BrowseControl(BrowseAction.PageDown);
                                    break;
                                case NativeMethods2.VirtualKeys.MEDIA_PREV_TRACK:
                                case NativeMethods2.VirtualKeys.MEDIA_NEXT_TRACK:
                                    ChangeTab(BrowseMode.None);
                                    handled = true;
                                    break;
                            }
                            break;
                        case WM.LBUTTONDBLCLK:
                        case WM.NCLBUTTONDBLCLK:
                            mfOwner.ToggleFullScreen();
                            break;
                    }
                //}
            //}
            return handled;
        }

        #endregion

        private void MediaBrowser_Deactivate(object sender, EventArgs e)
        {
            Application.RemoveMessageFilter(this);            
        }

        private void MediaBrowser_Activated(object sender, EventArgs e)
        {
            Application.AddMessageFilter(this);
        }

        private void MediaBrowser_MouseMove(object sender, MouseEventArgs e)
        {
            mfOwner.MoveMouse(sender, e);
        }

        private void MediaBrowser_DoubleClick(object sender, EventArgs e)
        {
            mfOwner.ToggleFullScreen();
            ChangeLocation();
        }
    }

    public class BrowserState
    {
        public string Sort;
        public string Filter;
        public FlowLayoutPanel Panel;
        public int Level = 1;
        public string Value;
        public BrowseMode Mode;

        public BrowserState(string sort, string filter, int Level, BrowseMode mode, FlowLayoutPanel panel)
        {
            this.Sort = sort;
            this.Filter = filter;
            this.Panel = panel;
            this.Level = Level;
            this.Mode = mode;
        }
    }

    public class CachedData
    {
        private DateTime dataLoaded = DateTime.Now;
        //private XmlDocument xmlResult;
        //private XPathNavigator xNav;
        private int cacheFor = 0;
        private DataSet dsResult;

        public bool HasExpired
        {
            get
            {
                if (cacheFor == 0 || dataLoaded.AddMinutes(cacheFor) > DateTime.Now)
                    return false;
                else
                    return true;
            }
        }

        public DataSet Data
        {
            get
            {
                return dsResult;
            }
        }

        public CachedData(string strXml, int cacheFor)
        {
            this.cacheFor = cacheFor;
            dsResult = new DataSet("Media");

            try
            {
                using (StringReader sr = new StringReader(strXml))
                {
                    XmlTextReader xtr = new XmlTextReader(sr);
                    xtr.XmlResolver = null;

                    using (Stream s = this.GetType().Assembly.GetManifestResourceStream("babgvant.EVRPlay.sageShowInfo.xsd"))
                    {
                        dsResult.ReadXmlSchema(s);
                        dsResult.ReadXml(xtr);
                    }
                }

            }
            catch (Exception ex)
            {
                FileLogger.Log("CachedData: {0}", ex.Message);
            }
        }
    }

    public enum BrowseMode
    {
        None,
        Recordings,
        Media
    }

    public enum BrowseAction
    {
        Up,
        Down,
        PageUp,
        PageDown
    }
}