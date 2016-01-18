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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace babgvant.EVRPlay
{
    public class FileLogger
    {
        protected static FileLogger _instance = new FileLogger();
        private Queue _wrappedQueue;
        protected Queue _logMe;
        private Thread _logWriter;
        protected ManualResetEvent _mrLogWait;
        protected bool _processMessages = true;
        protected string _logName;
        private PlaySettings ps = new PlaySettings();

        protected virtual string LogName
        {
            get
            {
                return _logName;
            }
        }

        protected FileLogger() {
            if (ps.WriteLog)
            {
                _wrappedQueue = new Queue();
                _logMe = Queue.Synchronized(_wrappedQueue);
                _mrLogWait = new ManualResetEvent(true);
                _logWriter = new Thread(new ThreadStart(ProcessQueue));
                _logWriter.IsBackground = true;
                _logWriter.Start();
                using (Process currentProcess = Process.GetCurrentProcess())
                    _logName = string.Format("{0}_{1}.log", currentProcess.ProcessName, currentProcess.Id);
            }
        }

        public static void Log(string format, params object[] args)
        {
            Log(string.Format(format, args));
        }

        public static void Log(string message)
        {
            if(_instance.ps.WriteLog)
            {
                lock (_instance._logMe.SyncRoot)
                {
                    _instance._logMe.Enqueue(message);
                    // We now have a message in the queue.
                    // signal the manual reset event to unblock the thread
                    _instance._mrLogWait.Set();
                }
            }
        }

        public static void Stop()
        {
            if (_instance.ps.WriteLog)
            {
                _instance._processMessages = false;
                _instance._mrLogWait.Set();
            }
        }

        private void ProcessQueue()
        {
            StreamWriter sr = null;
            string logName = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), LogName);
            try
            {
                sr = File.AppendText(logName);
                sr.WriteLine("------------------------------------------------------------------------");

                while (_processMessages)
                {
                    object message = null;
                    lock (_logMe.SyncRoot)
                    {
                        // if we have messages in the queue
                        // lock the thread, and dequeue the next message
                        if (_logMe.Count > 0)
                        {
                            message = _logMe.Dequeue();
                        }
                        else
                        {
                            // Tell the ManualResetEvent to block the thread
                            // When the WaitOne method is called on the
                            // ManualResetEvent since there are no messages
                            // in the queue.
                            // The thread will become unblocked, when the
                            // ManualResetEvent is signalled to Set.
                            _mrLogWait.Reset();
                        }
                    }
                    if (message != null)
                    {
                        if(sr == null)
                            sr = File.AppendText(logName);

                        sr.WriteLine("{0:yyyy-MM-dd HH:mm:ss.ff} : {1}", DateTime.Now, message);
                    }
                    else
                    {

                        if (sr != null)
                        {
                            sr.Flush();
                            sr.Dispose();
                            sr = null;
                        }
                        // queue is empty, so block thread until Set event is called
                        _mrLogWait.WaitOne();
                    }
                }
            }
            finally
            {
                if (sr != null)
                {
                    sr.Flush();
                    sr.Dispose();
                }
            }
        }
    }
}
