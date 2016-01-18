using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace babgvant.EVRPlay
{
    public class Common
    {
        public static bool ShouldFilter(IntPtr HWnd, Control control)
        {
            bool filter = false;

            if (HWnd == control.Handle)
                return true;

            foreach (Control c in control.Controls)
            {
                if (HWnd == c.Handle)
                {
                    filter = true;
                    break;
                }
                if (!filter && c.HasChildren)
                    filter = ShouldFilter(HWnd, c);
                if (filter)
                    break;
            }
            return filter;
        }
    }
}
