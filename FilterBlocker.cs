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
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using DirectShowLib;
using DirectShowLib.Utils;

namespace babgvant.EVRPlay
{
    public class FilterBlocker : IAMGraphBuilderCallback
    {

        private PlaySettings _ps = new PlaySettings();
        private string _fileName = string.Empty;
        private string _fileExt = string.Empty;
        private List<string> _blockedFilters = new List<string>();

        public string FileExt
        {
            get
            {
                return _fileExt;
            }
        }

        public FilterBlocker(string fileName)
        {
            _fileName = fileName;
            _fileExt = Path.GetExtension(fileName).ToLower();
            foreach (string bt in _ps.BlockedFilters)
            {
                string[] tb = bt.Split(';');
                if (_fileExt == tb[0].ToLower())
                {
                    for (int i = 1; i < tb.Length; i++)
                    {
                        if (!_blockedFilters.Contains(tb[i]))
                            _blockedFilters.Add(tb[1].Trim());
                    }
                }
            }
        }

        #region IAMGraphBuilderCallback Members

        public int SelectedFilter(System.Runtime.InteropServices.ComTypes.IMoniker pMon)
        {
            if (_blockedFilters.Count > 0)
            {
                object objBag = null;
                Guid ipGuid = typeof(IPropertyBag).GUID;//new Guid("55272A00-42CB-11CE-8135-00AA004BB851");
                object objClsid = null;
                object objFriendlyName = null;
                try
                {
                    pMon.BindToStorage(null, null, ref ipGuid, out objBag);
                    IPropertyBag bag = objBag as IPropertyBag;
                    if (bag != null)
                    {
                        bag.Read("CLSID", out objClsid, null);
                        bag.Read("FriendlyName", out objFriendlyName, null);

                        if (_blockedFilters.Contains(objClsid.ToString()) || _blockedFilters.Contains(objFriendlyName.ToString()))
                        {
                            FileLogger.Log("Blocked Filter: {0} - {1}", objFriendlyName, objClsid);
                            return -1;
                        }
                    }
                }
                finally
                {
                    if (objBag != null)
                        Marshal.ReleaseComObject(objBag);
                }
            }
            return 0;
        }

        public int CreatedFilter(IBaseFilter pFil)
        {
            //throw new Exception("The method or operation is not implemented.");
            return 0;
        }

        #endregion
    }
}
