namespace System.Web
{
  public enum ApplicationShutdownReason
  {
    None,
    HostingEnvironment,
    ChangeInGlobalAsax,
    ConfigurationChange,
    UnloadAppDomainCalled,
    ChangeInSecurityPolicyFile,
    BinDirChangeOrDirectoryRename,
    BrowsersDirChangeOrDirectoryRename,
    CodeDirChangeOrDirectoryRename,
    ResourcesDirChangeOrDirectoryRename,
    IdleTimeout,
    PhysicalApplicationPathChanged,
    HttpRuntimeClose,
    InitializationError,
    MaxRecompilationsReached,
    BuildManagerChange,
  }
}
