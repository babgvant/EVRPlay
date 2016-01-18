using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace babgvant.EVRPlay
{
    public partial class DownloadForm : Form
    {
        PlaySettings ps = new PlaySettings();
        string _url = string.Empty;
        private int _maxAttempts = 0;
        private int _attemptsMade = 0;
        private string _result = string.Empty;

        public string Result
        {
            get
            {
                return _result;
            }
        }

        public DownloadForm(string Url, int maxAttempts)
        {
            _url = Url;
            _maxAttempts = maxAttempts;

            InitializeComponent();
        }

        private void pbDownload_Click(object sender, EventArgs e)
        {

        }

        private void DownloadForm_Load(object sender, EventArgs e)
        {
            Download();
        }

        private void Download()
        {
            _attemptsMade++;

            if (MainForm.DebugMode)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                if (!string.IsNullOrEmpty(ps.WOLMac))
                    BrowserForm.WakeUp(ps.WOLMac);

                using (WebClient wc = new WebClient())
                {
                    try
                    {
                        wc.Credentials = new NetworkCredential(ps.SWUsername, ps.SWPassword);
                        //wc.DownloadProgressChanged += new DownloadProgressChangedEventHandler(wc_DownloadProgressChanged);
                        wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadStringCompleted);
                        wc.DownloadStringAsync(new Uri(_url));
                    }
                    catch (Exception ex)
                    {
                        if (_attemptsMade < _maxAttempts)
                        {
                            Download();
                            return;
                        }

                        _result = ex.Message;
                        this.DialogResult = DialogResult.Abort;
                        this.Close();
                    }
                }
            }
        }

        void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            //throw new Exception("The method or operation is not implemented.");
            if (this.InvokeRequired)
                this.Invoke(new DownloadStringCompletedEventHandler(wc_DownloadStringCompleted), new object[] { sender, e });

            if (e.Error == null)
            {
                _result = e.Result;
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                if (_attemptsMade < _maxAttempts)
                {
                    Download();
                    return;
                }
                else
                {
                    _result = e.Error.Message;
                    this.DialogResult = DialogResult.Abort;
                }

            }
            this.Close();
        }

        void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            //throw new Exception("The method or operation is not implemented.");
            if (this.InvokeRequired)
                this.Invoke(new DownloadProgressChangedEventHandler(wc_DownloadProgressChanged), new object[] { sender, e });

            pbDownload.Value = e.ProgressPercentage;
        }

        private void tmStep_Tick(object sender, EventArgs e)
        {
            //if(pbDownload.Value > pbDownload.Maximum)
            //    pbDownload.res
            pbDownload.PerformStep();
        }
    }
}