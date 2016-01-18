using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Runtime.CompilerServices;
using SHDocVw;
using System.Runtime.InteropServices.ComTypes;

namespace babgvant.EVRPlay
{
    [ComVisible(true)]
    public class BrowserRemotingProxy : MarshalByRefObject, IAuthenticate, IServiceProvider, IDisposable, IOleClientSite
    {
        private Guid SID_SProfferService = new Guid("cb728b20-f786-11ce-92ad-00aa00a74cd0");
        private Guid IID_IProfferService = typeof(IProfferService).GUID;
        private Guid IID_IAuthenticate = typeof(IServiceProvider).GUID;
        public const int INET_E_DEFAULT_ACTION = unchecked((int)0x800C0011);
        public const int S_OK = unchecked((int)0x00000000); 

        private string _userName = string.Empty;
        private string _password = string.Empty;
        private bool _disposed = false;
        IProfferService theProfferService = null;
        private System.Windows.Forms.WebBrowser browserControl;
        private InternetExplorer IE;

        public BrowserRemotingProxy(System.Windows.Forms.WebBrowser browserControl, string userName, string password)
        {
            this.browserControl = browserControl;
            this.IE = browserControl.ActiveXInstance as InternetExplorer;
            IServiceProvider sp = this.IE as IServiceProvider;

            IOleObject oc = IE as IOleObject;
            oc.SetClientSite(this as IOleClientSite); 
            
            IntPtr objectProffer = IntPtr.Zero;
            uint cookie = 0;

            _userName = userName;
            _password = password;

            sp.QueryService(ref SID_SProfferService, ref IID_IProfferService, out objectProffer);
            theProfferService = Marshal.GetObjectForIUnknown(objectProffer) as IProfferService;
            theProfferService.ProfferService(ref IID_IAuthenticate, this, out cookie);
        }

        #region IServiceProvider Members

        int IServiceProvider.QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject)
        {
            ////int hr = HRESULT.E_NOINTERFACE;

            //if (guidService == IID_IAuthenticate && riid == IID_IAuthenticate)
            //{
            //    ppvObject = Marshal.GetComInterfaceForObject(this, typeof(IAuthenticate)); // this as IAuthenticate; //
            //    //if (ppvObject != null) {
            //    //    hr = HRESULT.S_OK;
            //    //}
            //}
            //else
            //{
            //    ppvObject = IntPtr.Zero;
            //}

            ////return hr;

            int nRet = guidService.CompareTo(IID_IAuthenticate);        // Zero
            //returned if the compared objects are equal
            if (nRet == 0)
            {
                nRet = riid.CompareTo(IID_IAuthenticate);                       // Zero
                //returned if the compared objects are equal
                if (nRet == 0)
                {
                    ppvObject = Marshal.GetComInterfaceForObject(this, typeof(IAuthenticate));
                    return S_OK;
                }
            }
            ppvObject = new IntPtr();
            return INET_E_DEFAULT_ACTION;
        }

        #endregion

        #region IAuthenticate Members

        void IAuthenticate.Authenticate(out IntPtr phwnd, out string pszUsername, out string pszPassword)
        {
            phwnd = IntPtr.Zero;

            pszUsername = _userName;
            pszPassword = _password;
        }

        #endregion

        #region IOleClientSite Members

        public void SaveObject()
        {
            // TODO:  Add Form1.SaveObject implementation
        }

        public void GetMoniker(uint dwAssign, uint dwWhichMoniker, object ppmk)
        {
            // TODO:  Add Form1.GetMoniker implementation
        }

        public void GetContainer(object ppContainer)
        {
            ppContainer = this;
        }

        public void ShowObject()
        {
            // TODO:  Add Form1.ShowObject implementation
        }

        public void OnShowWindow(bool fShow)
        {
            // TODO:  Add Form1.OnShowWindow implementation
        }

        public void RequestNewObjectLayout()
        {
            // TODO:  Add Form1.RequestNewObjectLayout implementation
        }

        #endregion 

        #region IDisposable Members

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_disposed)
                {
                    if (theProfferService != null)
                        Marshal.ReleaseComObject(theProfferService);
                    _disposed = true;
                }
            }
        }
        #endregion
    }

    #region COM Interfaces import Declarations
    public struct HRESULT
    {
        public const int S_OK = 0;
        public const int S_FALSE = 1;
        public const int E_NOTIMPL = unchecked((int)0x80004001);
        public const int E_INVALIDARG = unchecked((int)0x80070057);
        public const int E_NOINTERFACE = unchecked((int)0x80004002);
        public const int E_FAIL = unchecked((int)0x80004005);
        public const int E_UNEXPECTED = unchecked((int)0x8000FFFF);
    }

    [ComImport,
    Guid("00000112-0000-0000-C000-000000000046"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IOleObject
    {
        void SetClientSite(IOleClientSite pClientSite);
        void GetClientSite(IOleClientSite ppClientSite);
        void SetHostNames(object szContainerApp, object szContainerObj);
        void Close(uint dwSaveOption);
        void SetMoniker(uint dwWhichMoniker, object pmk);
        void GetMoniker(uint dwAssign, uint dwWhichMoniker, object ppmk);
        void InitFromData(IDataObject pDataObject, bool
            fCreation, uint dwReserved);
        void GetClipboardData(uint dwReserved, IDataObject ppDataObject);
        void DoVerb(uint iVerb, uint lpmsg, object pActiveSite,
            uint lindex, uint hwndParent, uint lprcPosRect);
        void EnumVerbs(object ppEnumOleVerb);
        void Update();
        void IsUpToDate();
        void GetUserClassID(uint pClsid);
        void GetUserType(uint dwFormOfType, uint pszUserType);
        void SetExtent(uint dwDrawAspect, uint psizel);
        void GetExtent(uint dwDrawAspect, uint psizel);
        void Advise(object pAdvSink, uint pdwConnection);
        void Unadvise(uint dwConnection);
        void EnumAdvise(object ppenumAdvise);
        void GetMiscStatus(uint dwAspect, uint pdwStatus);
        void SetColorScheme(object pLogpal);
    }

    [ComImport,
    Guid("00000118-0000-0000-C000-000000000046"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IOleClientSite
    {
        void SaveObject();
        void GetMoniker(uint dwAssign, uint dwWhichMoniker, object ppmk);
        void GetContainer(object ppContainer);
        void ShowObject();
        void OnShowWindow(bool fShow);
        void RequestNewObjectLayout();
    } 

    //#region IServiceProvider Interface

    //[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
    //public interface IServiceProvider
    //{
    //    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    //    int QueryService([In] ref Guid guidService, [In] ref Guid riid, [Out] out IntPtr ppvObject);
    //}

    //#endregion

    [ComImport,
   GuidAttribute("6d5140c1-7436-11ce-8034-00aa006009fa"),
   InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown),
   ComVisible(false)]
    public interface IServiceProvider
    {
        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject);
    }

    //[ComImport, GuidAttribute("79EAC9D0-BAF9-11CE-8C82-00AA004BA90B"),
    //InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown),
    //ComVisible(false)]
    //public interface IAuthenticate
    //{
    //    [return: MarshalAs(UnmanagedType.I4)]
    //    [PreserveSig]
    //    int Authenticate(ref IntPtr phwnd,
    //        ref IntPtr pszUsername,
    //        ref IntPtr pszPassword
    //        );
    //} 

    #region IProfferService Interface
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("CB728B20-F786-11CE-92AD-00AA00A74CD0")]
    public interface IProfferService
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void ProfferService([In] ref Guid rguidService, [In, MarshalAs(UnmanagedType.Interface)] IServiceProvider psp, out uint pdwCookie);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void RevokeService([In] uint dwCookie);
    }

    #endregion

    #region IAuthenticate Interface
    //MIDL_INTERFACE("79eac9d0-baf9-11ce-8c82-00aa004ba90b")
    //IAuthenticate : public IUnknown
    //{
    //public:
    //    virtual HRESULT STDMETHODCALLTYPE Authenticate(
    //        /* [out] */ HWND *phwnd,
    //        /* [out] */ LPWSTR *pszUsername,
    //        /* [out] */ LPWSTR *pszPassword) = 0;
    //}
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComConversionLoss, Guid("79EAC9D0-BAF9-11CE-8C82-00AA004BA90B")]
    public interface IAuthenticate
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Authenticate([Out, ComAliasName("UrlMonInterop.wireHWND")] out IntPtr phwnd, [Out, MarshalAs(UnmanagedType.LPWStr)] out string pszUsername, [Out, MarshalAs(UnmanagedType.LPWStr)] out string pszPassword);
    }

    #endregion
    #endregion
}