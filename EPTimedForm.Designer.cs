namespace babgvant.EVRPlay
{
    partial class EPTimedForm
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
            if (disposing && (components != null))
            {
                components.Dispose();
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
            this.tmTimeout = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // tmTimeout
            // 
            this.tmTimeout.Tick += new System.EventHandler(this.tmTimeout_Tick);
            // 
            // EPTimedForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 264);
            this.ControlBox = false;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "EPTimedForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "EPTimedForm";
            this.Load += new System.EventHandler(this.EPTimedForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        protected System.Windows.Forms.Timer tmTimeout;
    }
}