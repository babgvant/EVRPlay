using System;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Text;
using System.Windows.Forms;

namespace babgvant.EVRPlay
{
    [Serializable()]
    public class EpRemote : MarshalByRefObject
    {
        //private Form parent;

        //public void SetParentForm(Form parentForm)
        //{
        //    this.parent = parentForm;
        //}

        public void PlayFile(string filePath)
        {
            Focus();
            FileLogger.Log("RemotePlay: {0}", filePath);
            MainForm.theForm.PlayFile(filePath);
        }

        public void Focus()
        {
            FileLogger.Log("RemotePlay: focus");
            MainForm.theForm.FocusForm();            
        }
    }
}
