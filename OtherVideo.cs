using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace babgvant.EVRPlay
{
    public partial class OtherVideo : EPTimedForm
    {
        Player p = null;

        public OtherVideo()
        {
            InitializeComponent();
        }

        private void OtherVideo_Load(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                p = new Player(this);
                p.PlayMovieInWindow(openFileDialog1.FileName);
            }
        }

        private void OtherVideo_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
    }
}