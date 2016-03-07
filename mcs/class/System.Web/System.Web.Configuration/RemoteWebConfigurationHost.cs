using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Configuration.Internal;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace System.Web.Configuration
{
  [SecurityPermission(SecurityAction.Demand, Unrestricted = true)]
  internal sealed class RemoteWebConfigurationHost : DelegatingConfigHost
  {
    private static object s_version = new object();
    private const string KEY_MACHINE = "MACHINE";
    private string _Server;
    private string _Username;
    private string _Domain;
    private string _Password;
    private WindowsIdentity _Identity;
    private Hashtable _PathMap;
    private string _ConfigPath;

    public override bool IsRemote
    {
      get
      {
        return true;
      }
    }

    internal RemoteWebConfigurationHost()
    {
    }

    public override void Init(IInternalConfigRoot configRoot, params object[] hostInitParams)
    {
      throw System.Web.Util.ExceptionUtil.UnexpectedError("RemoteWebConfigurationHost::Init");
    }

    public override void InitForConfiguration(ref string locationSubPath, out string configPath, out string locationConfigPath, IInternalConfigRoot root, params object[] hostInitConfigurationParams)
    {
      WebLevel webLevel = (WebLevel) hostInitConfigurationParams[0];
      string path1 = (string) hostInitConfigurationParams[2];
      string site = (string) hostInitConfigurationParams[3];
      if (locationSubPath == null)
        locationSubPath = (string) hostInitConfigurationParams[4];
      string server = (string) hostInitConfigurationParams[5];
      string fullUserName = (string) hostInitConfigurationParams[6];
      string password = (string) hostInitConfigurationParams[7];
      IntPtr userToken = (IntPtr) hostInitConfigurationParams[8];
      configPath = (string) null;
      locationConfigPath = (string) null;
      this._Server = server;
      this._Username = RemoteWebConfigurationHost.GetUserNameFromFullName(fullUserName);
      this._Domain = RemoteWebConfigurationHost.GetDomainFromFullName(fullUserName);
      this._Password = password;
      this._Identity = userToken == IntPtr.Zero ? (WindowsIdentity) null : new WindowsIdentity(userToken);
      this._PathMap = new Hashtable((IEqualityComparer) StringComparer.OrdinalIgnoreCase);
      string filePaths;
      try
      {
        WindowsImpersonationContext impersonationContext = this._Identity != null ? this._Identity.Impersonate() : (WindowsImpersonationContext) null;
        try
        {
          IRemoteWebConfigurationHostServer remoteObject = RemoteWebConfigurationHost.CreateRemoteObject(server, this._Username, this._Domain, password);
          try
          {
            filePaths = remoteObject.GetFilePaths((int) webLevel, path1, site, locationSubPath);
          }
          finally
          {
            do
              ;
            while (Marshal.ReleaseComObject((object) remoteObject) > 0);
          }
        }
        finally
        {
          if (impersonationContext != null)
            impersonationContext.Undo();
        }
      }
      catch
      {
        throw;
      }
      if (filePaths == null)
        throw System.Web.Util.ExceptionUtil.UnexpectedError("RemoteWebConfigurationHost::InitForConfiguration");
      string[] strArray = filePaths.Split(RemoteWebConfigurationHostServer.FilePathsSeparatorParams);
      if (strArray.Length < 7 || (strArray.Length - 5) % 2 != 0)
        throw System.Web.Util.ExceptionUtil.UnexpectedError("RemoteWebConfigurationHost::InitForConfiguration");
      for (int index = 0; index < strArray.Length; ++index)
      {
        if (strArray[index].Length == 0)
          strArray[index] = (string) null;
      }
      string virtualPath = strArray[0];
      string str1 = strArray[1];
      string str2 = strArray[2];
      configPath = strArray[3];
      locationConfigPath = strArray[4];
      this._ConfigPath = configPath;
      WebConfigurationFileMap configurationFileMap = new WebConfigurationFileMap();
      VirtualPath absoluteAllowNull = VirtualPath.CreateAbsoluteAllowNull(virtualPath);
      configurationFileMap.Site = str2;
      int index1 = 5;
      while (index1 < strArray.Length)
      {
        string configPath1 = strArray[index1];
        string path2 = strArray[index1 + 1];
        this._PathMap.Add((object) configPath1, (object) path2);
        if (WebConfigurationHost.IsMachineConfigPath(configPath1))
        {
          configurationFileMap.MachineConfigFilename = path2;
        }
        else
        {
          string virtualDirectory;
          bool isAppRoot;
          if (WebConfigurationHost.IsRootWebConfigPath(configPath1))
          {
            virtualDirectory = (string) null;
            isAppRoot = false;
          }
          else
          {
            string siteID;
            VirtualPath vpath;
            WebConfigurationHost.GetSiteIDAndVPathFromConfigPath(configPath1, out siteID, out vpath);
            virtualDirectory = VirtualPath.GetVirtualPathString(vpath);
            isAppRoot = vpath == absoluteAllowNull;
          }
          configurationFileMap.VirtualDirectories.Add(virtualDirectory, new VirtualDirectoryMapping(Path.GetDirectoryName(path2), isAppRoot));
        }
        index1 += 2;
      }
      WebConfigurationHost configurationHost = new WebConfigurationHost();
      configurationHost.Init(root, (object) true, (object) new UserMapPath((ConfigurationFileMap) configurationFileMap, false), null, (object) virtualPath, (object) str1, (object) str2);
      this.Host = (IInternalConfigHost) configurationHost;
    }

    public override bool IsConfigRecordRequired(string configPath)
    {
      return configPath.Length <= this._ConfigPath.Length;
    }

    public override string GetStreamName(string configPath)
    {
      return (string) this._PathMap[(object) configPath];
    }

    public override object GetStreamVersion(string streamName)
    {
      WindowsImpersonationContext impersonationContext = (WindowsImpersonationContext) null;
      bool exists;
      long size;
      long createDate;
      long lastWriteDate;
      try
      {
        if (this._Identity != null)
          impersonationContext = this._Identity.Impersonate();
        try
        {
          IRemoteWebConfigurationHostServer remoteObject = RemoteWebConfigurationHost.CreateRemoteObject(this._Server, this._Username, this._Domain, this._Password);
          try
          {
            remoteObject.GetFileDetails(streamName, out exists, out size, out createDate, out lastWriteDate);
          }
          finally
          {
            do
              ;
            while (Marshal.ReleaseComObject((object) remoteObject) > 0);
          }
        }
        finally
        {
          if (impersonationContext != null)
            impersonationContext.Undo();
        }
      }
      catch
      {
        throw;
      }
      return (object) new FileDetails(exists, size, DateTime.FromFileTimeUtc(createDate), DateTime.FromFileTimeUtc(lastWriteDate));
    }

    public override Stream OpenStreamForRead(string streamName)
    {
      RemoteWebConfigurationHostStream configurationHostStream = new RemoteWebConfigurationHostStream(false, this._Server, streamName, (string) null, this._Username, this._Domain, this._Password, this._Identity);
      if (configurationHostStream == null || configurationHostStream.Length < 1L)
        return (Stream) null;
      return (Stream) configurationHostStream;
    }

    public override Stream OpenStreamForWrite(string streamName, string templateStreamName, ref object writeContext)
    {
      RemoteWebConfigurationHostStream configurationHostStream = new RemoteWebConfigurationHostStream(true, this._Server, streamName, templateStreamName, this._Username, this._Domain, this._Password, this._Identity);
      writeContext = (object) configurationHostStream;
      return (Stream) configurationHostStream;
    }

    public override void DeleteStream(string StreamName)
    {
    }

    public override void WriteCompleted(string streamName, bool success, object writeContext)
    {
      if (!success)
        return;
      ((RemoteWebConfigurationHostStream) writeContext).FlushForWriteCompleted();
    }

    public override bool IsFile(string StreamName)
    {
      return false;
    }

    public override bool PrefetchAll(string configPath, string StreamName)
    {
      return true;
    }

    public override bool PrefetchSection(string sectionGroupName, string sectionName)
    {
      return true;
    }

    public override void GetRestrictedPermissions(IInternalConfigRecord configRecord, out PermissionSet permissionSet, out bool isHostReady)
    {
      WebConfigurationHost.StaticGetRestrictedPermissions(configRecord, out permissionSet, out isHostReady);
    }

    public override string DecryptSection(string encryptedXmlString, ProtectedConfigurationProvider protectionProvider, ProtectedConfigurationSection protectedConfigSection)
    {
      return this.CallEncryptOrDecrypt(false, encryptedXmlString, protectionProvider, protectedConfigSection);
    }

    public override string EncryptSection(string clearTextXmlString, ProtectedConfigurationProvider protectionProvider, ProtectedConfigurationSection protectedConfigSection)
    {
      return this.CallEncryptOrDecrypt(true, clearTextXmlString, protectionProvider, protectedConfigSection);
    }

    private string CallEncryptOrDecrypt(bool doEncrypt, string xmlString, ProtectedConfigurationProvider protectionProvider, ProtectedConfigurationSection protectedConfigSection)
    {
      string str = (string) null;
      WindowsImpersonationContext impersonationContext = (WindowsImpersonationContext) null;
      string assemblyQualifiedName = protectionProvider.GetType().AssemblyQualifiedName;
      ProviderSettings providerSettings = protectedConfigSection.Providers[protectionProvider.Name];
      if (providerSettings == null)
        throw System.Web.Util.ExceptionUtil.ParameterInvalid("protectionProvider");
      NameValueCollection nameValueCollection = providerSettings.Parameters ?? new NameValueCollection();
      string[] allKeys = nameValueCollection.AllKeys;
      string[] parameterValues = new string[allKeys.Length];
      for (int index = 0; index < allKeys.Length; ++index)
        parameterValues[index] = nameValueCollection[allKeys[index]];
      if (this._Identity != null)
        impersonationContext = this._Identity.Impersonate();
      try
      {
        try
        {
          IRemoteWebConfigurationHostServer remoteObject = RemoteWebConfigurationHost.CreateRemoteObject(this._Server, this._Username, this._Domain, this._Password);
          try
          {
            str = remoteObject.DoEncryptOrDecrypt(doEncrypt, xmlString, protectionProvider.Name, assemblyQualifiedName, allKeys, parameterValues);
          }
          finally
          {
            do
              ;
            while (Marshal.ReleaseComObject((object) remoteObject) > 0);
          }
        }
        finally
        {
          if (impersonationContext != null)
            impersonationContext.Undo();
        }
      }
      catch
      {
      }
      return str;
    }

    private static string GetUserNameFromFullName(string fullUserName)
    {
      if (string.IsNullOrEmpty(fullUserName))
        return (string) null;
      if (fullUserName.Contains("@"))
        return fullUserName;
      string[] strArray = fullUserName.Split('\\');
      if (strArray.Length == 1)
        return fullUserName;
      return strArray[1];
    }

    private static string GetDomainFromFullName(string fullUserName)
    {
      if (string.IsNullOrEmpty(fullUserName))
        return (string) null;
      if (fullUserName.Contains("@"))
        return (string) null;
      string[] strArray = fullUserName.Split('\\');
      if (strArray.Length == 1)
        return ".";
      return strArray[0];
    }

    internal static IRemoteWebConfigurationHostServer CreateRemoteObject(string server, string username, string domain, string password)
    {
      try
      {
        if (string.IsNullOrEmpty(username))
          return RemoteWebConfigurationHost.CreateRemoteObjectUsingGetTypeFromCLSID(server);
        if (IntPtr.Size == 8)
          return RemoteWebConfigurationHost.CreateRemoteObjectOn64BitPlatform(server, username, domain, password);
        return RemoteWebConfigurationHost.CreateRemoteObjectOn32BitPlatform(server, username, domain, password);
      }
      catch (COMException ex)
      {
        if (ex.ErrorCode == -2147221164)
          throw new Exception(System.Web.SR.GetString("Make_sure_remote_server_is_enabled_for_config_access"));
        throw;
      }
    }

    private static IRemoteWebConfigurationHostServer CreateRemoteObjectUsingGetTypeFromCLSID(string server)
    {
      return (IRemoteWebConfigurationHostServer) Activator.CreateInstance(Type.GetTypeFromCLSID(typeof (RemoteWebConfigurationHostServer).GUID, server, true));
    }

    private static IRemoteWebConfigurationHostServer CreateRemoteObjectOn32BitPlatform(string server, string username, string domain, string password)
    {
      MULTI_QI[] amqi = new MULTI_QI[1];
      IntPtr num1 = IntPtr.Zero;
      IntPtr num2 = IntPtr.Zero;
      Guid guid = typeof (RemoteWebConfigurationHostServer).GUID;
      IntPtr num3 = IntPtr.Zero;
      try
      {
        num1 = Marshal.AllocCoTaskMem(16);
        Marshal.StructureToPtr((object) typeof (IRemoteWebConfigurationHostServer).GUID, num1, false);
        amqi[0] = new MULTI_QI(num1);
        COAUTHIDENTITY coauthidentity = new COAUTHIDENTITY(username, domain, password);
        num3 = Marshal.AllocCoTaskMem(Marshal.SizeOf((object) coauthidentity));
        IntPtr ptr1 = num3;
        int num4 = 0;
        Marshal.StructureToPtr((object) coauthidentity, ptr1, num4 != 0);
        COAUTHINFO coauthinfo = new COAUTHINFO(RpcAuthent.WinNT, RpcAuthor.None, (string) null, RpcLevel.Default, RpcImpers.Impersonate, num3);
        num2 = Marshal.AllocCoTaskMem(Marshal.SizeOf((object) coauthinfo));
        IntPtr ptr2 = num2;
        int num5 = 0;
        Marshal.StructureToPtr((object) coauthinfo, ptr2, num5 != 0);
        COSERVERINFO srv = new COSERVERINFO(server, num2);
        int instanceEx = System.Web.UnsafeNativeMethods.CoCreateInstanceEx(ref guid, IntPtr.Zero, 16, srv, 1, amqi);
        if (instanceEx == -2147221164)
          throw new Exception(System.Web.SR.GetString("Make_sure_remote_server_is_enabled_for_config_access"));
        if (instanceEx < 0)
          Marshal.ThrowExceptionForHR(instanceEx);
        if (amqi[0].hr < 0)
          Marshal.ThrowExceptionForHR(amqi[0].hr);
        int errorCode = System.Web.UnsafeNativeMethods.CoSetProxyBlanket(amqi[0].pItf, RpcAuthent.WinNT, RpcAuthor.None, (string) null, RpcLevel.Default, RpcImpers.Impersonate, num3, 0);
        if (errorCode < 0)
          Marshal.ThrowExceptionForHR(errorCode);
        return (IRemoteWebConfigurationHostServer) Marshal.GetObjectForIUnknown(amqi[0].pItf);
      }
      finally
      {
        if (amqi[0].pItf != IntPtr.Zero)
        {
          Marshal.Release(amqi[0].pItf);
          amqi[0].pItf = IntPtr.Zero;
        }
        amqi[0].piid = IntPtr.Zero;
        if (num2 != IntPtr.Zero)
        {
          Marshal.DestroyStructure(num2, typeof (COAUTHINFO));
          Marshal.FreeCoTaskMem(num2);
        }
        if (num3 != IntPtr.Zero)
        {
          Marshal.DestroyStructure(num3, typeof (COAUTHIDENTITY));
          Marshal.FreeCoTaskMem(num3);
        }
        if (num1 != IntPtr.Zero)
          Marshal.FreeCoTaskMem(num1);
      }
    }

    private static IRemoteWebConfigurationHostServer CreateRemoteObjectOn64BitPlatform(string server, string username, string domain, string password)
    {
      MULTI_QI_X64[] amqi = new MULTI_QI_X64[1];
      IntPtr num1 = IntPtr.Zero;
      IntPtr num2 = IntPtr.Zero;
      Guid guid = typeof (RemoteWebConfigurationHostServer).GUID;
      IntPtr num3 = IntPtr.Zero;
      try
      {
        num1 = Marshal.AllocCoTaskMem(16);
        Marshal.StructureToPtr((object) typeof (IRemoteWebConfigurationHostServer).GUID, num1, false);
        amqi[0] = new MULTI_QI_X64(num1);
        COAUTHIDENTITY_X64 coauthidentityX64 = new COAUTHIDENTITY_X64(username, domain, password);
        num3 = Marshal.AllocCoTaskMem(Marshal.SizeOf((object) coauthidentityX64));
        IntPtr ptr1 = num3;
        int num4 = 0;
        Marshal.StructureToPtr((object) coauthidentityX64, ptr1, num4 != 0);
        COAUTHINFO_X64 coauthinfoX64 = new COAUTHINFO_X64(RpcAuthent.WinNT, RpcAuthor.None, (string) null, RpcLevel.Default, RpcImpers.Impersonate, num3);
        num2 = Marshal.AllocCoTaskMem(Marshal.SizeOf((object) coauthinfoX64));
        IntPtr ptr2 = num2;
        int num5 = 0;
        Marshal.StructureToPtr((object) coauthinfoX64, ptr2, num5 != 0);
        COSERVERINFO_X64 srv = new COSERVERINFO_X64(server, num2);
        int instanceEx = System.Web.UnsafeNativeMethods.CoCreateInstanceEx(ref guid, IntPtr.Zero, 16, srv, 1, amqi);
        if (instanceEx == -2147221164)
          throw new Exception(System.Web.SR.GetString("Make_sure_remote_server_is_enabled_for_config_access"));
        if (instanceEx < 0)
          Marshal.ThrowExceptionForHR(instanceEx);
        if (amqi[0].hr < 0)
          Marshal.ThrowExceptionForHR(amqi[0].hr);
        int errorCode = System.Web.UnsafeNativeMethods.CoSetProxyBlanket(amqi[0].pItf, RpcAuthent.WinNT, RpcAuthor.None, (string) null, RpcLevel.Default, RpcImpers.Impersonate, num3, 0);
        if (errorCode < 0)
          Marshal.ThrowExceptionForHR(errorCode);
        return (IRemoteWebConfigurationHostServer) Marshal.GetObjectForIUnknown(amqi[0].pItf);
      }
      finally
      {
        if (amqi[0].pItf != IntPtr.Zero)
        {
          Marshal.Release(amqi[0].pItf);
          amqi[0].pItf = IntPtr.Zero;
        }
        amqi[0].piid = IntPtr.Zero;
        if (num2 != IntPtr.Zero)
        {
          Marshal.DestroyStructure(num2, typeof (COAUTHINFO_X64));
          Marshal.FreeCoTaskMem(num2);
        }
        if (num3 != IntPtr.Zero)
        {
          Marshal.DestroyStructure(num3, typeof (COAUTHIDENTITY_X64));
          Marshal.FreeCoTaskMem(num3);
        }
        if (num1 != IntPtr.Zero)
          Marshal.FreeCoTaskMem(num1);
      }
    }
  }
}
