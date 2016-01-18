namespace babgvant.EVRPlay
{
    partial class BrowserForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!disposed)
                {
                    //System.Windows.Forms.Application.RemoveMessageFilter(this);
                    if (components != null)
                        components.Dispose();
                    disposed = true;
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.wbSageServer = new babgvant.EVRPlay.WebBrowser2();
            this.SuspendLayout();
            // 
            // wbSageServer
            // 
            this.wbSageServer.AllowWebBrowserDrop = false;
            this.wbSageServer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.wbSageServer.Location = new System.Drawing.Point(0, 0);
            this.wbSageServer.MinimumSize = new System.Drawing.Size(20, 20);
            this.wbSageServer.Name = "wbSageServer";
            this.wbSageServer.Size = new System.Drawing.Size(379, 325);
            this.wbSageServer.TabIndex = 0;
            this.wbSageServer.WebBrowserShortcutsEnabled = false;
            this.wbSageServer.NavigateError += new babgvant.EVRPlay.WebBrowserNavigateErrorEventHandler(this.wbSageServer_NavigateError);
            this.wbSageServer.Navigating += new System.Windows.Forms.WebBrowserNavigatingEventHandler(this.wbSageServer_Navigating);
            this.wbSageServer.NewWindow += new System.ComponentModel.CancelEventHandler(this.wbSageServer_NewWindow);
            this.wbSageServer.Navigated += new System.Windows.Forms.WebBrowserNavigatedEventHandler(this.wbSageServer_Navigated);
            // 
            // BrowserForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(379, 325);
            this.ControlBox = false;
            this.Controls.Add(this.wbSageServer);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BrowserForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "BrowserForm";
            this.Deactivate += new System.EventHandler(this.BrowserForm_Deactivate);
            this.Load += new System.EventHandler(this.BrowserForm_Load);
            this.DoubleClick += new System.EventHandler(this.BrowserForm_DoubleClick);
            this.Activated += new System.EventHandler(this.BrowserForm_Activated);
            this.ResumeLayout(false);

        }

        #endregion

        private WebBrowser2 wbSageServer;
    }
}