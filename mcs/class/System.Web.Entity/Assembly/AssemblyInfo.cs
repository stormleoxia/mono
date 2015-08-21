//
// AssemblyInfo.cs
//
// Author:
//   Marek Habersack <mhabersack@novell.com>
//
// (C) 2007 Novell, Inc.
//
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Resources;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime;
using System.Web.UI;
using System.Security;

// General Information about the System.Web.Extensions.Design assembly
// v3.5 Assembly

[assembly: AssemblyTitle ("System.Web.Entity.dll")]
[assembly: AssemblyDescription("System.Web.Entity.dll")]
[assembly: AssemblyDefaultAlias("System.Web.Entity.dll")]

[assembly: AssemblyCompany (Consts.MonoCompany)]
[assembly: AssemblyProduct (Consts.MonoProduct)]
[assembly: AssemblyCopyright (Consts.MonoCopyright)]
[assembly: AssemblyVersion (Consts.FxVersion)]
[assembly: SatelliteContractVersion (Consts.FxVersion)]
[assembly: AssemblyInformationalVersion (Consts.FxFileVersion)]
[assembly: AssemblyFileVersion (Consts.FxFileVersion)]


[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: BitmapSuffixInSameAssembly]

[assembly: AssemblyTargetedPatchBand("1.0.23-139944600")]

[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: ComCompatibleVersion(1, 0, 3300, 0)]

[assembly: AllowPartiallyTrustedCallers]
[assembly: SecurityRules(SecurityRuleSet.Level1, SkipVerificationInFullTrust = true)]
[assembly: SecurityTransparent]
[assembly: TagPrefix("System.Web.UI.WebControls", "asp")]

[assembly: NeutralResourcesLanguage ("en-US")]

#if !(TARGET_DOTNET)
	[assembly: CLSCompliant (true)]
	[assembly: AssemblyDelaySign (true)]
	[assembly: AssemblyKeyFile ("../winfx.pub")]
#endif

[assembly: ComVisible (false)]

[assembly: CompilationRelaxations (CompilationRelaxations.NoStringInterning)]
