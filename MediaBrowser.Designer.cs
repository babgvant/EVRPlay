namespace babgvant.EVRPlay
{
    partial class MediaBrowser
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
                    System.Windows.Forms.Application.RemoveMessageFilter(this);
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
            this.components = new System.ComponentModel.Container();
            this.tcBrowse = new System.Windows.Forms.TabControl();
            this.tpRecording = new System.Windows.Forms.TabPage();
            this.scRecording = new System.Windows.Forms.SplitContainer();
            this.flpRecordingTop = new System.Windows.Forms.FlowLayoutPanel();
            this.flpRecItems = new System.Windows.Forms.FlowLayoutPanel();
            this.tbVideo = new System.Windows.Forms.TabPage();
            this.scMedia = new System.Windows.Forms.SplitContainer();
            this.flpMediaTop = new System.Windows.Forms.FlowLayoutPanel();
            this.flpMedia = new System.Windows.Forms.FlowLayoutPanel();
            this.ttMediaBrowser = new System.Windows.Forms.ToolTip(this.components);
            this.tcBrowse.SuspendLayout();
            this.tpRecording.SuspendLayout();
            this.scRecording.Panel1.SuspendLayout();
            this.scRecording.Panel2.SuspendLayout();
            this.scRecording.SuspendLayout();
            this.tbVideo.SuspendLayout();
            this.scMedia.Panel1.SuspendLayout();
            this.scMedia.Panel2.SuspendLayout();
            this.scMedia.SuspendLayout();
            this.SuspendLayout();
            // 
            // tcBrowse
            // 
            this.tcBrowse.Controls.Add(this.tpRecording);
            this.tcBrowse.Controls.Add(this.tbVideo);
            this.tcBrowse.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tcBrowse.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tcBrowse.Location = new System.Drawing.Point(0, 0);
            this.tcBrowse.Name = "tcBrowse";
            this.tcBrowse.SelectedIndex = 0;
            this.tcBrowse.Size = new System.Drawing.Size(606, 473);
            this.tcBrowse.TabIndex = 2;
            this.tcBrowse.SelectedIndexChanged += new System.EventHandler(this.tcBrowse_SelectedIndexChanged);
            // 
            // tpRecording
            // 
            this.tpRecording.Controls.Add(this.scRecording);
            this.tpRecording.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.tpRecording.Location = new System.Drawing.Point(4, 29);
            this.tpRecording.Name = "tpRecording";
            this.tpRecording.Padding = new System.Windows.Forms.Padding(3);
            this.tpRecording.Size = new System.Drawing.Size(598, 440);
            this.tpRecording.TabIndex = 0;
            this.tpRecording.Tag = "Recording";
            this.tpRecording.Text = "Recordings";
            this.tpRecording.UseVisualStyleBackColor = true;
            // 
            // scRecording
            // 
            this.scRecording.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scRecording.Location = new System.Drawing.Point(3, 3);
            this.scRecording.Name = "scRecording";
            this.scRecording.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // scRecording.Panel1
            // 
            this.scRecording.Panel1.Controls.Add(this.flpRecordingTop);
            // 
            // scRecording.Panel2
            // 
            this.scRecording.Panel2.Controls.Add(this.flpRecItems);
            this.scRecording.Size = new System.Drawing.Size(592, 434);
            this.scRecording.SplitterDistance = 100;
            this.scRecording.TabIndex = 1;
            // 
            // flpRecordingTop
            // 
            this.flpRecordingTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpRecordingTop.Location = new System.Drawing.Point(0, 0);
            this.flpRecordingTop.Name = "flpRecordingTop";
            this.flpRecordingTop.Size = new System.Drawing.Size(592, 100);
            this.flpRecordingTop.TabIndex = 0;
            // 
            // flpRecItems
            // 
            this.flpRecItems.AutoScroll = true;
            this.flpRecItems.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpRecItems.Location = new System.Drawing.Point(0, 0);
            this.flpRecItems.Name = "flpRecItems";
            this.flpRecItems.Size = new System.Drawing.Size(592, 330);
            this.flpRecItems.TabIndex = 0;
            // 
            // tbVideo
            // 
            this.tbVideo.Controls.Add(this.scMedia);
            this.tbVideo.Location = new System.Drawing.Point(4, 29);
            this.tbVideo.Name = "tbVideo";
            this.tbVideo.Padding = new System.Windows.Forms.Padding(3);
            this.tbVideo.Size = new System.Drawing.Size(598, 440);
            this.tbVideo.TabIndex = 1;
            this.tbVideo.Tag = "Media";
            this.tbVideo.Text = "Videos";
            this.tbVideo.UseVisualStyleBackColor = true;
            // 
            // scMedia
            // 
            this.scMedia.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scMedia.Location = new System.Drawing.Point(3, 3);
            this.scMedia.Name = "scMedia";
            this.scMedia.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // scMedia.Panel1
            // 
            this.scMedia.Panel1.Controls.Add(this.flpMediaTop);
            // 
            // scMedia.Panel2
            // 
            this.scMedia.Panel2.AutoScroll = true;
            this.scMedia.Panel2.Controls.Add(this.flpMedia);
            this.scMedia.Size = new System.Drawing.Size(592, 434);
            this.scMedia.SplitterDistance = 100;
            this.scMedia.TabIndex = 0;
            // 
            // flpMediaTop
            // 
            this.flpMediaTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpMediaTop.Location = new System.Drawing.Point(0, 0);
            this.flpMediaTop.Name = "flpMediaTop";
            this.flpMediaTop.Size = new System.Drawing.Size(592, 100);
            this.flpMediaTop.TabIndex = 0;
            // 
            // flpMedia
            // 
            this.flpMedia.AutoScroll = true;
            this.flpMedia.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpMedia.Location = new System.Drawing.Point(0, 0);
            this.flpMedia.Name = "flpMedia";
            this.flpMedia.Size = new System.Drawing.Size(592, 330);
            this.flpMedia.TabIndex = 0;
            // 
            // MediaBrowser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(606, 473);
            this.Controls.Add(this.tcBrowse);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "MediaBrowser";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "MediaBrowser";
            this.TransparencyKey = System.Drawing.Color.Black;
            this.Deactivate += new System.EventHandler(this.MediaBrowser_Deactivate);
            this.Load += new System.EventHandler(this.MediaBrowser_Load);
            this.DoubleClick += new System.EventHandler(this.MediaBrowser_DoubleClick);
            this.Activated += new System.EventHandler(this.MediaBrowser_Activated);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.MediaBrowser_MouseMove);
            this.tcBrowse.ResumeLayout(false);
            this.tpRecording.ResumeLayout(false);
            this.scRecording.Panel1.ResumeLayout(false);
            this.scRecording.Panel2.ResumeLayout(false);
            this.scRecording.ResumeLayout(false);
            this.tbVideo.ResumeLayout(false);
            this.scMedia.Panel1.ResumeLayout(false);
            this.scMedia.Panel2.ResumeLayout(false);
            this.scMedia.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tcBrowse;
        private System.Windows.Forms.TabPage tpRecording;
        private System.Windows.Forms.TabPage tbVideo;
        private System.Windows.Forms.FlowLayoutPanel flpRecItems;
        private System.Windows.Forms.ToolTip ttMediaBrowser;
        private System.Windows.Forms.SplitContainer scMedia;
        private System.Windows.Forms.FlowLayoutPanel flpMedia;
        private System.Windows.Forms.FlowLayoutPanel flpMediaTop;
        private System.Windows.Forms.SplitContainer scRecording;
        private System.Windows.Forms.FlowLayoutPanel flpRecordingTop;
    }
}