using System;
using System.Security;
using System.Text;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Runtime.Hosting {

    /// <summary>
    /// The methods here are designed to aid in transition from the v2 StrongName APIs on mscoree.dll to the
    /// v4 metahost APIs (which are in-proc SxS aware).
    /// </summary>
    internal static class StrongNameHelpers {

        [ThreadStatic]
        private static int ts_LastStrongNameHR;

        [System.Security.SecurityCritical]
        [ThreadStatic]
        private static IClrStrongName s_StrongName;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "[....]: This file is included in a lot of projects some of which only use a subset of the functions.")]
        private static IClrStrongName StrongName {
            [System.Security.SecurityCritical]
            get {
                if (s_StrongName == null) {
                    s_StrongName = (IClrStrongName)RuntimeEnvironment.GetRuntimeInterfaceAsObject(
                        new Guid("B79B0ACD-F5CD-409b-B5A5-A16244610B92"), 
                        new Guid("9FD93CCF-3280-4391-B3A9-96E1CDE77C8D"));
                }
                return s_StrongName;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "[....]: This file is included in a lot of projects some of which only use a subset of the functions.")]
        private static IClrStrongNameUsingIntPtr StrongNameUsingIntPtr {
            [System.Security.SecurityCritical]
            get {
                return (IClrStrongNameUsingIntPtr)StrongName;
            }
        }

        [System.Security.SecurityCritical]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "[....]: This file is included in a lot of projects some of which only use a subset of the functions.")]
        public static int StrongNameErrorInfo() {
            return ts_LastStrongNameHR;
        }

        [System.Security.SecurityCritical]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "[....]: This file is included in a lot of projects some of which only use a subset of the functions.")]
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "Microsoft.Runtime.Hosting.IClrStrongNameUsingIntPtr.StrongNameFreeBuffer(System.IntPtr)", Justification = "StrongNameFreeBuffer returns void but the new runtime wrappers return an HRESULT.")]
        public static void StrongNameFreeBuffer(IntPtr pbMemory) {
            StrongNameUsingIntPtr.StrongNameFreeBuffer(pbMemory);
        }


        [System.Security.SecurityCritical]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "[....]: This file is included in a lot of projects some of which only use a subset of the functions.")]
        public static bool StrongNameKeyGen(string pwzKeyContainer, int dwFlags, out IntPtr ppbKeyBlob, out int pcbKeyBlob) {
            int hr = StrongName.StrongNameKeyGen(pwzKeyContainer, dwFlags, out ppbKeyBlob, out pcbKeyBlob);
            if( hr < 0 )
            {
                ts_LastStrongNameHR = hr;
                ppbKeyBlob = IntPtr.Zero;
                pcbKeyBlob = 0;
                return false;
            }
            return true;
        }

    }

    /// <summary>
    /// This is a managed wrapper for the IClrStrongName interface defined in metahost.idl
    /// This uses IntPtrs in some places where you'd normally expect a byte[] in order to
    /// be compatible with callers who wrote their PInvoke signatures that way.
    /// Ideally we'd probably just simplify all such callers to using byte[] and remove this
    /// version of the interface.
    /// </summary>
    [System.Security.SecurityCritical]
    [ComImport, ComConversionLoss, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("9FD93CCF-3280-4391-B3A9-96E1CDE77C8D")]
    internal interface IClrStrongNameUsingIntPtr {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int GetHashFromAssemblyFile(
            [In, MarshalAs(UnmanagedType.LPStr)] string pszFilePath,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pbHash,
            [In, MarshalAs(UnmanagedType.U4)] int cchHash,
            [MarshalAs(UnmanagedType.U4)] out int pchHash);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int GetHashFromAssemblyFileW(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pbHash,
            [In, MarshalAs(UnmanagedType.U4)] int cchHash,
            [MarshalAs(UnmanagedType.U4)] out int pchHash);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int GetHashFromBlob(
            [In] IntPtr pbBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cchBlob,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] pbHash,
            [In, MarshalAs(UnmanagedType.U4)] int cchHash,
            [MarshalAs(UnmanagedType.U4)] out int pchHash);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int GetHashFromFile(
            [In, MarshalAs(UnmanagedType.LPStr)] string pszFilePath,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pbHash,
            [In, MarshalAs(UnmanagedType.U4)] int cchHash,
            [MarshalAs(UnmanagedType.U4)] out int pchHash);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int GetHashFromFileW(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pbHash,
            [In, MarshalAs(UnmanagedType.U4)] int cchHash,
            [MarshalAs(UnmanagedType.U4)] out int pchHash);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int GetHashFromHandle(
            [In] IntPtr hFile,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pbHash,
            [In, MarshalAs(UnmanagedType.U4)] int cchHash,
            [MarshalAs(UnmanagedType.U4)] out int pchHash);

        [return: MarshalAs(UnmanagedType.U4)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameCompareAssemblies(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzAssembly1,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzAssembly2,
            [MarshalAs(UnmanagedType.U4)] out int dwResult);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameFreeBuffer(
            [In] IntPtr pbMemory);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameGetBlob(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pbBlob,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int pcbBlob);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameGetBlobFromImage(
            [In] IntPtr pbBase,
            [In, MarshalAs(UnmanagedType.U4)] int dwLength,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pbBlob,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int pcbBlob);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameGetPublicKey(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer,
            [In] IntPtr pbKeyBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cbKeyBlob,
            out IntPtr ppbPublicKeyBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbPublicKeyBlob);

        [return: MarshalAs(UnmanagedType.U4)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameHashSize(
            [In, MarshalAs(UnmanagedType.U4)] int ulHashAlg,
            [MarshalAs(UnmanagedType.U4)] out int cbSize);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameKeyDelete(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameKeyGen(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer,
            [In, MarshalAs(UnmanagedType.U4)] int dwFlags,
            out IntPtr ppbKeyBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbKeyBlob);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameKeyGenEx(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer,
            [In, MarshalAs(UnmanagedType.U4)] int dwFlags,
            [In, MarshalAs(UnmanagedType.U4)] int dwKeySize,
            out IntPtr ppbKeyBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbKeyBlob);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameKeyInstall(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer,
            [In] IntPtr pbKeyBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cbKeyBlob);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameSignatureGeneration(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer,
            [In] IntPtr pbKeyBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cbKeyBlob,
            [In, Out] IntPtr ppbSignatureBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbSignatureBlob);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameSignatureGenerationEx(
            [In, MarshalAs(UnmanagedType.LPWStr)] string wszFilePath,
            [In, MarshalAs(UnmanagedType.LPWStr)] string wszKeyContainer,
            [In] IntPtr pbKeyBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cbKeyBlob,
            [In, Out] IntPtr ppbSignatureBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbSignatureBlob,
            [In, MarshalAs(UnmanagedType.U4)] int dwFlags);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameSignatureSize(
            [In] IntPtr pbPublicKeyBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cbPublicKeyBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbSize);

        [return: MarshalAs(UnmanagedType.U4)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameSignatureVerification(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [In, MarshalAs(UnmanagedType.U4)] int dwInFlags,
            [MarshalAs(UnmanagedType.U4)] out int dwOutFlags);

        [return: MarshalAs(UnmanagedType.U4)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameSignatureVerificationEx(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [In, MarshalAs(UnmanagedType.I1)] bool fForceVerification,
            [MarshalAs(UnmanagedType.I1)] out bool fWasVerified);

        [return: MarshalAs(UnmanagedType.U4)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameSignatureVerificationFromImage(
            [In] IntPtr pbBase,
            [In, MarshalAs(UnmanagedType.U4)] int dwLength,
            [In, MarshalAs(UnmanagedType.U4)] int dwInFlags,
            [MarshalAs(UnmanagedType.U4)] out int dwOutFlags);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameTokenFromAssembly(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            out IntPtr ppbStrongNameToken,
            [MarshalAs(UnmanagedType.U4)] out int pcbStrongNameToken);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameTokenFromAssemblyEx(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            out IntPtr ppbStrongNameToken,
            [MarshalAs(UnmanagedType.U4)] out int pcbStrongNameToken,
            out IntPtr ppbPublicKeyBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbPublicKeyBlob);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameTokenFromPublicKey(
            [In] IntPtr pbPublicKeyBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cbPublicKeyBlob,
            out IntPtr ppbStrongNameToken,
            [MarshalAs(UnmanagedType.U4)] out int pcbStrongNameToken);
    }

    /// <summary>
    /// This is a managed wrapper for the IClrStrongName interface defined in metahost.idl
    /// This is very similar to the standard RCWs provided in 
    /// ndp/fx/src/hosting/interop/microsoft/runtime/hosting/interop, but we don't want to
    /// reference that assembly (part of the SDK only, not .NET redist).  Also, our version
    /// is designed specifically for easy migration from the old mscoree APIs, for example
    /// all APIs return HResults rather than throw exceptions.
    /// </summary> 
    [System.Security.SecurityCritical]
    [ComImport, ComConversionLoss, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("9FD93CCF-3280-4391-B3A9-96E1CDE77C8D")]
    internal interface IClrStrongName {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int GetHashFromAssemblyFile(
            [In, MarshalAs(UnmanagedType.LPStr)] string pszFilePath,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pbHash,
            [In, MarshalAs(UnmanagedType.U4)] int cchHash,
            [MarshalAs(UnmanagedType.U4)] out int pchHash);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int GetHashFromAssemblyFileW(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pbHash,
            [In, MarshalAs(UnmanagedType.U4)] int cchHash,
            [MarshalAs(UnmanagedType.U4)] out int pchHash);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int GetHashFromBlob(
            [In] IntPtr pbBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cchBlob,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] pbHash,
            [In, MarshalAs(UnmanagedType.U4)] int cchHash,
            [MarshalAs(UnmanagedType.U4)] out int pchHash);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int GetHashFromFile(
            [In, MarshalAs(UnmanagedType.LPStr)] string pszFilePath,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pbHash,
            [In, MarshalAs(UnmanagedType.U4)] int cchHash,
            [MarshalAs(UnmanagedType.U4)] out int pchHash);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int GetHashFromFileW(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pbHash,
            [In, MarshalAs(UnmanagedType.U4)] int cchHash,
            [MarshalAs(UnmanagedType.U4)] out int pchHash);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int GetHashFromHandle(
            [In] IntPtr hFile,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pbHash,
            [In, MarshalAs(UnmanagedType.U4)] int cchHash,
            [MarshalAs(UnmanagedType.U4)] out int pchHash);

        [return: MarshalAs(UnmanagedType.U4)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameCompareAssemblies(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzAssembly1,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzAssembly2,
            [MarshalAs(UnmanagedType.U4)] out int dwResult);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameFreeBuffer(
            [In] IntPtr pbMemory);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameGetBlob(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pbBlob,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int pcbBlob);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameGetBlobFromImage(
            [In] IntPtr pbBase,
            [In, MarshalAs(UnmanagedType.U4)] int dwLength,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pbBlob,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int pcbBlob);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameGetPublicKey(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pbKeyBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cbKeyBlob,
            out IntPtr ppbPublicKeyBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbPublicKeyBlob);

        [return: MarshalAs(UnmanagedType.U4)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameHashSize(
            [In, MarshalAs(UnmanagedType.U4)] int ulHashAlg,
            [MarshalAs(UnmanagedType.U4)] out int cbSize);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameKeyDelete(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameKeyGen(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer,
            [In, MarshalAs(UnmanagedType.U4)] int dwFlags,
            out IntPtr ppbKeyBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbKeyBlob);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameKeyGenEx(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer,
            [In, MarshalAs(UnmanagedType.U4)] int dwFlags,
            [In, MarshalAs(UnmanagedType.U4)] int dwKeySize,
            out IntPtr ppbKeyBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbKeyBlob);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameKeyInstall(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pbKeyBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cbKeyBlob);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameSignatureGeneration(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pbKeyBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cbKeyBlob,
            [In, Out] IntPtr ppbSignatureBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbSignatureBlob);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameSignatureGenerationEx(
            [In, MarshalAs(UnmanagedType.LPWStr)] string wszFilePath,
            [In, MarshalAs(UnmanagedType.LPWStr)] string wszKeyContainer,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pbKeyBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cbKeyBlob,
            [In, Out] IntPtr ppbSignatureBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbSignatureBlob,
            [In, MarshalAs(UnmanagedType.U4)] int dwFlags);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameSignatureSize(
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] pbPublicKeyBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cbPublicKeyBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbSize);

        [return: MarshalAs(UnmanagedType.U4)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameSignatureVerification(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [In, MarshalAs(UnmanagedType.U4)] int dwInFlags,
            [MarshalAs(UnmanagedType.U4)] out int dwOutFlags);

        [return: MarshalAs(UnmanagedType.U4)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameSignatureVerificationEx(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [In, MarshalAs(UnmanagedType.I1)] bool fForceVerification,
            [MarshalAs(UnmanagedType.I1)] out bool fWasVerified);

        [return: MarshalAs(UnmanagedType.U4)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameSignatureVerificationFromImage(
            [In] IntPtr pbBase,
            [In, MarshalAs(UnmanagedType.U4)] int dwLength,
            [In, MarshalAs(UnmanagedType.U4)] int dwInFlags,
            [MarshalAs(UnmanagedType.U4)] out int dwOutFlags);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameTokenFromAssembly(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            out IntPtr ppbStrongNameToken,
            [MarshalAs(UnmanagedType.U4)] out int pcbStrongNameToken);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameTokenFromAssemblyEx(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            out IntPtr ppbStrongNameToken,
            [MarshalAs(UnmanagedType.U4)] out int pcbStrongNameToken,
            out IntPtr ppbPublicKeyBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbPublicKeyBlob);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int StrongNameTokenFromPublicKey(
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] pbPublicKeyBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cbPublicKeyBlob,
            out IntPtr ppbStrongNameToken,
            [MarshalAs(UnmanagedType.U4)] out int pcbStrongNameToken);
    }
}
