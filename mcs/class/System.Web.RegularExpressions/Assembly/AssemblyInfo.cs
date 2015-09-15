using System;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("System.Web.RegularExpressions-gen-net_4_5")]
[assembly: AssemblyDescription("System.Web.RegularExpressions-gen-net_4_5")]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("d47730a8-fb93-4a5b-803f-1f92e2e4a82b")]


[assembly: AssemblyCompany(Consts.MonoCompany)]
[assembly: AssemblyProduct(Consts.MonoProduct)]
[assembly: AssemblyCopyright(Consts.MonoCopyright)]

#if MOBILE
[assembly: AssemblyVersion ("4.0.0.0")]
[assembly: SatelliteContractVersion ("4.0.0.0")]
[assembly: AssemblyInformationalVersion ("4.0.50524.0")]
[assembly: AssemblyFileVersion ("4.0.50524.0")]
#else
[assembly: AssemblyVersion(Consts.FxVersion)]
[assembly: SatelliteContractVersion(Consts.FxVersion)]
[assembly: AssemblyInformationalVersion(Consts.FxFileVersion)]
[assembly: AssemblyFileVersion(Consts.FxFileVersion)]
#endif

[assembly: NeutralResourcesLanguage("en-US")]
[assembly: CLSCompliant(true)]

[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]

[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]

[assembly: AllowPartiallyTrustedCallers]
[assembly: SecurityRules(SecurityRuleSet.Level1, SkipVerificationInFullTrust = true)]
[assembly: SecurityTransparent]

[assembly: AssemblyDelaySign (true)]
[assembly: AssemblyKeyFile ("../winfx.pub")]

[assembly: ComVisible (false)]
