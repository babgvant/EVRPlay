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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MediaFoundation.EVR;
using CoreAudioApi;

namespace babgvant.EVRPlay
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                PlaySettings ps = new PlaySettings();

                ps.MaxInitialHeight = Convert.ToInt32(txtMaxHeight.Text);
                ps.MaxInitialWidth = Convert.ToInt32(txtMaxWidth.Text);
                ps.MouseSensitivity = Convert.ToInt32(txtMouseSensitivity.Text);
                ps.SkipBack1 = Convert.ToInt32(txtSkipBack1.Text);
                ps.SkipBack2 = Convert.ToInt32(txtSkipBack2.Text);
                ps.SkipForward1 = Convert.ToInt32(txtSkipForward1.Text);
                ps.SkipForward2 = Convert.ToInt32(txtSkipForward2.Text);
                ps.FSControlTimeout = Convert.ToInt32(txtFSControlTimeout.Text);
                ps.FileHistory = Convert.ToInt32(txtFileHistory.Text);
                ps.OverscanHeight = Convert.ToInt32(txtOverscanHeight.Text);
                ps.OverscanWidth = Convert.ToInt32(txtOverscanWidth.Text);
                ps.BrowseAttempts = Convert.ToInt32(txtBrowseAttempts.Text);
                ps.Zoom = Convert.ToSingle(txtZoom.Text);
                ps.ZoomIncrement = Convert.ToSingle(txtZoomIncrement.Text);

                ps.SageWebserver = txtSageWebserver.Text;
                ps.SWUsername = txtUsername.Text;
                ps.SWPassword = txtPassword.Text;
                ps.WOLMac = txtSageMac.Text;

                if (!string.IsNullOrEmpty(txtBookmarkDir.Text) && System.IO.Directory.Exists(txtBookmarkDir.Text))
                    ps.BookmarkDir = txtBookmarkDir.Text;

                ps.PublishGraph = chkPublishGraph.Checked;
                ps.WriteLog = chkWriteLog.Checked;
                ps.SingleInstance = chkSingleInstance.Checked;
                ps.BrowseOnLoad = chkAutoBrowse.Checked;

                ps.BlockedFilters = ConvertLines(txtBlockedFilters);
                ps.PreferredDecoders = ConvertLines(txtPreferedFilters);
                ps.ReplacePaths = ConvertLines(txtReplacePaths);

                ps.OpenMediaBrowser = chkOpenMediaBrower.Checked;
                ps.UseDtbXml = chkUseDtbXml.Checked;
                ps.PriorityClass = (int)Enum.Parse(typeof(ProcessPriorityClass), cbPriorityClass.Text);
                ps.DtbXmlPath = txtDtbXmlPath.Text;
                ps.MultiChannelWMA = chkMultichannelWMA.Checked;
                ps.CustomPresenterEnabled = chkCustomPresenter.Checked;
                ps.CustomPresenter = txtCustomPresenter.Text;
                ps.CustomAudioRender = txtCustomAudio.Text;
                ps.UseCustomAudioRenderer = chkCustomAudio.Checked;
                ps.VideoAspectRatioMode = (int)Enum.Parse(typeof(MFVideoAspectRatioMode), cbArMode.Text);
                ps.UseBookmarks = chkUseBookmarks.Checked;

                ps.AudioPlaybackDevice = (cbAudioDevice.SelectedItem as AudioDevice).Value;  

                ps.Save();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving: {0}", ex.Message);
            }
        }

        private void txtBookmarkDir_DoubleClick(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                txtBookmarkDir.Text = folderBrowserDialog1.SelectedPath;
        }

        private class AudioDevice
        {
            public string Text { get; set; }
            public string Value { get; set; }
        }

        private void GetAudioDevices(ComboBox box, string audDevice)
        {
            box.Items.Clear();
            box.DisplayMember = "Text";
            box.ValueMember = "Value";
            box.Items.Add(new AudioDevice() { Text = "Default Device", Value = string.Empty });
            box.SelectedIndex = 0;

            MMDeviceEnumerator DevEnum = new MMDeviceEnumerator();
            MMDeviceCollection dc = DevEnum.EnumerateAudioEndPoints(EDataFlow.eRender, EDeviceState.DEVICE_STATE_ACTIVE);
            for (int i = 0; i < dc.Count; i++)
            {
                box.Items.Add(new AudioDevice() { Text = dc[i].FriendlyName, Value = dc[i].ID });
                if(audDevice == dc[i].ID)
                    box.SelectedIndex = i+1;
            }
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            cbPriorityClass.DataSource = Enum.GetNames(typeof(ProcessPriorityClass));
            cbArMode.DataSource = Enum.GetNames(typeof(MFVideoAspectRatioMode));

            PlaySettings ps = new PlaySettings();
            GetAudioDevices(cbAudioDevice, ps.AudioPlaybackDevice);
            AddLines(txtBlockedFilters, ps.BlockedFilters);
            AddLines(txtPreferedFilters, ps.PreferredDecoders);
            AddLines(txtReplacePaths, ps.ReplacePaths);

            txtMaxHeight.Text = ps.MaxInitialHeight.ToString();
            txtMaxWidth.Text = ps.MaxInitialWidth.ToString();
            txtMouseSensitivity.Text = ps.MouseSensitivity.ToString();
            txtSkipBack1.Text = ps.SkipBack1.ToString();
            txtSkipBack2.Text = ps.SkipBack2.ToString();
            txtSkipForward1.Text = ps.SkipForward1.ToString();
            txtSkipForward2.Text = ps.SkipForward2.ToString();
            txtFSControlTimeout.Text = ps.FSControlTimeout.ToString();
            chkPublishGraph.Checked = ps.PublishGraph;
            chkWriteLog.Checked = ps.WriteLog;
            txtFileHistory.Text = ps.FileHistory.ToString();
            txtOverscanHeight.Text = ps.OverscanHeight.ToString();
            txtOverscanWidth.Text = ps.OverscanWidth.ToString();
            chkSingleInstance.Checked = ps.SingleInstance;
            txtSageWebserver.Text = ps.SageWebserver;
            txtUsername.Text = ps.SWUsername;
            txtPassword.Text = ps.SWPassword;
            chkAutoBrowse.Checked = ps.BrowseOnLoad;
            txtSageMac.Text = ps.WOLMac;
            txtBrowseAttempts.Text = ps.BrowseAttempts.ToString();
            txtZoom.Text = ps.Zoom.ToString();
            txtZoomIncrement.Text = ps.ZoomIncrement.ToString();
            txtCustomPresenter.Text = ps.CustomPresenter;
            chkCustomPresenter.Checked = ps.CustomPresenterEnabled;
            txtCustomAudio.Text = ps.CustomAudioRender;
            chkCustomAudio.Checked = ps.UseCustomAudioRenderer;

            chkOpenMediaBrower.Checked = ps.OpenMediaBrowser;
            chkUseDtbXml.Checked = ps.UseDtbXml;
            chkMultichannelWMA.Checked = ps.MultiChannelWMA;
            chkUseBookmarks.Checked = ps.UseBookmarks;

            if (ps.PriorityClass > 0)
                cbPriorityClass.SelectedItem = Enum.GetName(typeof(ProcessPriorityClass), ps.PriorityClass);
            else
                cbPriorityClass.SelectedItem = Enum.GetName(typeof(ProcessPriorityClass), ProcessPriorityClass.Normal);

            cbArMode.SelectedItem = Enum.GetName(typeof(MFVideoAspectRatioMode), ps.VideoAspectRatioMode);

            if (!string.IsNullOrEmpty(ps.BookmarkDir))
                txtBookmarkDir.Text = ps.BookmarkDir;
            else
                txtBookmarkDir.Text = MainForm.BaseBookmarkPath;

            txtDtbXmlPath.Text = ps.DtbXmlPath;
        }

        private StringCollection ConvertLines(TextBox t)
        {
            StringCollection sc = new StringCollection();
            sc.AddRange(t.Lines);
            return sc;
        }

        private void AddLines(TextBox t, StringCollection c)
        {
            List<string> l = new List<string>();
            foreach (string s in c)
                l.Add(s);
            t.Lines = l.ToArray();
        }

        private void tabPage2_Click(object sender, EventArgs e)
        {

        }
    }
}