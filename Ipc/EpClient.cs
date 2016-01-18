using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;

namespace babgvant.EVRPlay
{
    public class EpClient : IDisposable
    {
        private IpcChannel _icpChannel = null;
        private bool disposed = false;
        EpRemote ep = null;

        public EpClient()
        {
            BinaryClientFormatterSinkProvider clientProvider = new BinaryClientFormatterSinkProvider();
            BinaryServerFormatterSinkProvider serverProvider = new BinaryServerFormatterSinkProvider();
            serverProvider.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;

            IDictionary props = new Hashtable();
            string s = System.Guid.NewGuid().ToString();
            props["name"] = s;
            props["typeFilterLevel"] = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;

            _icpChannel = new IpcChannel(props, clientProvider, serverProvider);
            ChannelServices.RegisterChannel(_icpChannel, false);

            string location = string.Format(@"ipc://{0}:EVRPlay/EpRemote.rem", Environment.MachineName);
            ep = (EpRemote)Activator.GetObject(typeof(EpRemote), location);
            FileLogger.Log("EpRemote obtained");
        }

        public void PlayFile(string filePath)
        {
            ep.PlayFile(filePath);
        }

        public void Focus()
        {
            ep.Focus();
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed 
                // and unmanaged resources.
                if (disposing)
                {                    
                    if (_icpChannel != null)
                    {
                        ChannelServices.UnregisterChannel(_icpChannel);
                    }                    
                }
            }
            disposed = true;
        }
        #endregion

        
    }
}
