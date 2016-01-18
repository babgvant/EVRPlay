using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;


namespace babgvant.EVRPlay
{
    public class EpServer : IDisposable
    {
        IpcServerChannel ipcServerChannel = null;
        private bool disposed = false;
        EpRemote remote;

        internal static string GetNTAccountName(WellKnownSidType knownSid)
        {
            SecurityIdentifier sid = new SecurityIdentifier(knownSid, null);
            NTAccount account = (NTAccount)sid.Translate(typeof(NTAccount));
            return account.Value;
        }

        [SecurityPermission(SecurityAction.Demand)]
        public EpServer()
        {
            IDictionary prop = new Hashtable();
                        
            //prop["port"] = fSett.Port;
            prop["portName"] = string.Format("{0}:EVRPlay", Environment.MachineName);
            prop["authorizedGroup"] = GetNTAccountName(WellKnownSidType.WorldSid);
            prop["impersonationLevel"] = "None";

            BinaryServerFormatterSinkProvider serverProvider = new BinaryServerFormatterSinkProvider();
            serverProvider.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;

            ipcServerChannel = new IpcServerChannel(prop, serverProvider);
           
            ChannelServices.RegisterChannel(ipcServerChannel, false);
            
            // Expose an object for remote calls.
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(EpRemote), "EpRemote.rem", System.Runtime.Remoting.WellKnownObjectMode.Singleton);
            ipcServerChannel.StartListening(null);
            FileLogger.Log("Ipc Server Listening on {0}", ipcServerChannel.GetChannelUri());
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
                    try
                    {
                        if (ipcServerChannel != null)
                        {
                            ipcServerChannel.StopListening(null);
                        }
                    }
                    catch
                    {
                        //do nothing
                    }
                }
            }
            disposed = true;
        }

        #endregion
    }
}
