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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using Microsoft.Win32;

namespace babgvant.EVRPlay
{
    public class Commercials
    {
        public static List<double[]> ReadCommercials(string commFile)
        {
            List<double[]> arCommercials = new List<double[]>();

            if (File.Exists(commFile))
            {
                XmlDocument xComm = new XmlDocument();
                xComm.Load(commFile);

                foreach (XmlNode n in xComm.SelectNodes("/root/commercial"))
                {
                    double[] commercial = new double[2];
                    commercial[0] = XmlConvert.ToDouble(n.Attributes["start"].Value);
                    commercial[1] = XmlConvert.ToDouble(n.Attributes["end"].Value);
                    arCommercials.Add(commercial);
                }
            }

            return arCommercials;
        }

        public static List<double[]> ReadEdlCommercials(string commFile)
        {
            List<double[]> arCommercials = new List<double[]>();

            if (File.Exists(commFile))
            {
                using (StreamReader sr = File.OpenText(commFile))
                {
                    string line = sr.ReadLine();
                    while (!string.IsNullOrEmpty(line))
                    {
                        double[] commercial = new double[2];
                        Match cMatch = Regex.Match(line, @"^(?<start>\d+\.\d+)\t(?<end>\d+\.\d+)\t", RegexOptions.Compiled);
                        if (cMatch.Success)
                        {
                            commercial[0] = XmlConvert.ToDouble(cMatch.Groups["start"].Value);
                            commercial[1] = XmlConvert.ToDouble(cMatch.Groups["end"].Value);
                            if (commercial[0] < commercial[1])
                                arCommercials.Add(commercial);
                        }
                        line = sr.ReadLine();
                    }
                }
            }

            return arCommercials;
        }

        //public static string DtbCommercialPath()
        //{
        //    using (RegistryKey dtbKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\DvrmsToolbox", false))
        //    {
        //        string xmlPath = string.Empty;

        //        if (dtbKey != null)
        //            xmlPath = Convert.ToString(dtbKey.GetValue("ComXmlPath"));

        //        return xmlPath;
        //    }
        //}

        public static string XmlDirectory
        {
            get
            {
                PlaySettings ps = new PlaySettings();

                if (string.IsNullOrEmpty(ps.DtbXmlPath))
                {
                    RegistryKey dtbKey = null;
                    try
                    {
                        dtbKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\DvrmsToolbox", false);
                        if (dtbKey != null)
                            return (string)dtbKey.GetValue("ComXmlPath");
                        else
                            return string.Empty;
                    }
                    finally
                    {
                        if (dtbKey != null)
                            dtbKey.Close();
                    }
                }
                else
                    return ps.DtbXmlPath;
            }
        }

        public static string GetXmlFilename(string mediaFile)
        {
            return Path.GetFileName(Path.ChangeExtension(mediaFile, "xml"));    
        }

        public static string GetEdlFilename(string mediaFile)
        {
            return Path.GetFileName(Path.ChangeExtension(mediaFile, "edl"));
        }
    }
}
