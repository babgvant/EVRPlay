using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace babgvant.EVRPlay
{
    public partial class ClientForm : Form
    {
        protected MainForm mfOwner
        {
            get
            {
                return Owner as MainForm;
            }
        }

        public ClientForm()
        {
            InitializeComponent();
        }

        private void ClientForm_Load(object sender, EventArgs e)
        {
            if (mfOwner != null)
            {
                this.Owner.Resize += new EventHandler(Owner_Resize);
                this.Owner.LocationChanged += new EventHandler(Owner_LocationChanged);
                ChangeSize();
                ChangeLocation();
            }
        }

        private void ChangeSize()
        {
            this.Width = Owner.ClientRectangle.Width;
            this.Height = Owner.ClientRectangle.Height;
        }

        public void ChangeLocation()
        {
            if (Owner != null)
            {
                if (mfOwner.isFullScreen)
                    this.Location = new Point(Owner.Location.X, Owner.Location.Y + 20);
                else
                    this.Location = new Point(Owner.Location.X + 8, Owner.Location.Y + 48);

                ChangeSize();
            }
        }

        void Owner_LocationChanged(object sender, EventArgs e)
        {
            ChangeLocation();
        }

        void Owner_Resize(object sender, EventArgs e)
        {
            ChangeLocation();
        }
    }
}