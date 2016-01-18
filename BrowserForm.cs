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
using System.Data;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

using DirectShowLib;

namespace babgvant.EVRPlay
{
    public partial class BrowserForm : ClientForm, IMessageFilter
    {
        public const string ABOUT_BLANK = "about:blank";
        
        private int browseTimes = 0;
        private bool disposed = false;

        public void BrowseWebserver()
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(BrowseWebserver));
        }

        public void BrowseWebserver(object objUrl)
        {
            if (this.InvokeRequired)
                this.Invoke(new WaitCallback(BrowseWebserver), new object[] { null });
            else
            {
                string Url = Convert.ToString(objUrl);

                if (!string.IsNullOrEmpty(mfOwner.ps.WOLMac))
                    WakeUp(mfOwner.ps.WOLMac);

                if (string.IsNullOrEmpty(Url))
                    Url = mfOwner.ps.SageWebserver;

                string serverPath = Regex.Replace(Url, @"https?://", "", RegexOptions.IgnoreCase);
                Match mProtocol = Regex.Match(Url, @"https?://", RegexOptions.IgnoreCase);
                if (mProtocol.Success)
                    wbSageServer.Navigate(string.Format("{3}{0}:{1}@{2}", mfOwner.ps.SWUsername, mfOwner.ps.SWPassword, serverPath, mProtocol.Value));
            }
        }

        public static void WakeUp(string macString)
        {
            try
            {
                string[] macArray = macString.Trim().Split('-', ':');
                byte[] mac = new byte[macArray.Length];

                for (int i = 0; i < macArray.Length; i++)
                {
                    mac[i] = Convert.ToByte(macArray[i], 16);
                }

                //
                // WOL packet is sent over UDP 255.255.255.0:40000.
                //
                UdpClient client = new UdpClient();
                client.Connect(IPAddress.Broadcast, 40000);

                //
                // WOL packet contains a 6-bytes trailer and 16 times a 6-bytes sequence containing the MAC address.
                //
                byte[] packet = new byte[17 * 6];

                //
                // Trailer of 6 times 0xFF.
                //
                for (int i = 0; i < 6; i++)
                    packet[i] = 0xFF;

                //
                // Body of magic packet contains 16 times the MAC address.
                //
                for (int i = 1; i <= 16; i++)
                    for (int j = 0; j < 6; j++)
                        packet[i * 6 + j] = mac[j];

                //
                // Submit WOL packet.
                //
                client.Send(packet, packet.Length);
            }
            catch { }
        }

        public BrowserForm()
        {
            InitializeComponent();
        }

        private void wbSageServer_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            string url = e.Url.ToString();

            if (Regex.IsMatch(url, @"PlaylistGenerator\?Command=generate\&pltype=pls", RegexOptions.IgnoreCase))
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(mfOwner.DownloadPls), e.Url);
                e.Cancel = true;
            }
            else if (Regex.IsMatch(url, @"/sage/DetailedInfo\?MediaFileId=(\d+)#streamingOptions", RegexOptions.IgnoreCase))
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(mfOwner.DownloadPls), Regex.Replace(url, @"/sage/DetailedInfo\?MediaFileId=(\d+)#streamingOptions", @"/sagepublic/PlaylistGenerator?Command=generate&pltype=pls&fntype=filepath&MediaFileId=$1", RegexOptions.IgnoreCase));
                e.Cancel = true;
            }
        }

        private void wbSageServer_NavigateError(object sender, WebBrowserNavigateErrorEventArgs e)
        {
            if (e.StatusCode == -2146697211 && browseTimes < mfOwner.ps.BrowseAttempts)
            {
                browseTimes++;
                Thread.Sleep(1000);
                BrowseWebserver(null);
            }
        }

        private void wbSageServer_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            browseTimes = 0;
        }

        

        private void BrowserForm_Load(object sender, EventArgs e)
        {
            wbSageServer.Navigate(ABOUT_BLANK);
            
            //Application.AddMessageFilter(this);
        }

        

        private void BrowserForm_DoubleClick(object sender, EventArgs e)
        {
            if (Owner != null)
                mfOwner.ToggleFullScreen();
        }

        #region IMessageFilter Members

        public bool PreFilterMessage(ref Message m)
        {
            //if (this.Visible)
            //{
                switch (m.Msg)
                {
                    case (int)WM.LBUTTONDBLCLK:
                    case (int)WM.NCLBUTTONDBLCLK:
                        if (Owner != null)
                            mfOwner.ToggleFullScreen();
                        break;
                }
            //}
            return false;
        }

        #endregion

        private void wbSageServer_NewWindow(object sender, CancelEventArgs e)
        {
            if (Regex.IsMatch(wbSageServer.StatusText, @"/sage/player.html\?.+\&MediaFileId=(?<fileid>\d+)", RegexOptions.IgnoreCase))
            {
                BrowseWebserver(Regex.Replace(wbSageServer.StatusText, @"/sage/player.html\?.+\&MediaFileId=(?<fileid>\d+)", @"/sagepublic/PlaylistGenerator?Command=generate&pltype=pls&fntype=filepath&MediaFileId=$1", RegexOptions.IgnoreCase));
            }
            e.Cancel = true;
        }

        private void BrowserForm_Activated(object sender, EventArgs e)
        {
            Application.AddMessageFilter(this);
        }

        private void BrowserForm_Deactivate(object sender, EventArgs e)
        {
            Application.RemoveMessageFilter(this);
        }
    }
}