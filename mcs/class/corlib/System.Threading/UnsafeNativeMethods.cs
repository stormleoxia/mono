﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32;

namespace System.Threading
{
    class UnsafeMethods
    {
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurityAttribute()]
        internal static unsafe class ManifestEtw
        {
            //
            // Constants error coded returned by ETW APIs
            //

            // The event size is larger than the allowed maximum (64k - header).
            internal const int ERROR_ARITHMETIC_OVERFLOW = 534;

            // Occurs when filled buffers are trying to flush to disk, 
            // but disk IOs are not happening fast enough. 
            // This happens when the disk is slow and event traffic is heavy. 
            // Eventually, there are no more free (empty) buffers and the event is dropped.
            internal const int ERROR_NOT_ENOUGH_MEMORY = 8;

            internal const int ERROR_MORE_DATA = 0xEA;
            internal const int ERROR_NOT_SUPPORTED = 50;
            internal const int ERROR_INVALID_PARAMETER = 0x57;

            //
            // ETW Methods
            //

            internal const int EVENT_CONTROL_CODE_DISABLE_PROVIDER = 0;
            internal const int EVENT_CONTROL_CODE_ENABLE_PROVIDER = 1;
            internal const int EVENT_CONTROL_CODE_CAPTURE_STATE = 2;

            //
            // Callback
            //
            [SecurityCritical]
            internal unsafe delegate void EtwEnableCallback(
                [In] ref Guid sourceId,
                [In] int isEnabled,
                [In] byte level,
                [In] long matchAnyKeywords,
                [In] long matchAllKeywords,
                [In] EVENT_FILTER_DESCRIPTOR* filterData,
                [In] void* callbackContext
                );

            //
            // Registration APIs
            //
            [SecurityCritical]
            [DllImport(Win32Native.ADVAPI32, ExactSpelling = true, EntryPoint = "EventRegister", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
            internal static extern unsafe uint EventRegister(
                        [In] ref Guid providerId,
                        [In]EtwEnableCallback enableCallback,
                        [In]void* callbackContext,
                        [In][Out]ref long registrationHandle
                        );

            // 
            [SecurityCritical]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
            [DllImport(Win32Native.ADVAPI32, ExactSpelling = true, EntryPoint = "EventUnregister", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
            internal static extern uint EventUnregister([In] long registrationHandle);

            //
            // Writing (Publishing/Logging) APIs
            //
            // 
            [SecurityCritical]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
            [DllImport(Win32Native.ADVAPI32, ExactSpelling = true, EntryPoint = "EventWrite", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
            internal static extern unsafe int EventWrite(
                    [In] long registrationHandle,
                    [In] ref EventDescriptor eventDescriptor,
                    [In] int userDataCount,
                    [In] EventProvider.EventData* userData
                    );

            [SecurityCritical]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
            [DllImport(Win32Native.ADVAPI32, ExactSpelling = true, EntryPoint = "EventWriteString", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
            internal static extern unsafe int EventWriteString(
                    [In] long registrationHandle,
                    [In] byte level,
                    [In] long keyword,
                    [In] string msg
                    );

            [StructLayout(LayoutKind.Sequential)]
            unsafe internal struct EVENT_FILTER_DESCRIPTOR
            {
                public long Ptr;
                public int Size;
                public int Type;
            };

            /// <summary>
            ///  Call the ETW native API EventWriteTransfer and checks for invalid argument error. 
            ///  The implementation of EventWriteTransfer on some older OSes (Windows 2008) does not accept null relatedActivityId.
            ///  So, for these cases we will retry the call with an empty Guid.
            /// </summary>
            internal static int EventWriteTransferWrapper(long registrationHandle,
                                                         ref EventDescriptor eventDescriptor,
                                                         Guid* activityId,
                                                         Guid* relatedActivityId,
                                                         int userDataCount,
                                                         EventProvider.EventData* userData)
            {
                int HResult = EventWriteTransfer(registrationHandle, ref eventDescriptor, activityId, relatedActivityId, userDataCount, userData);
                if (HResult == ERROR_INVALID_PARAMETER && relatedActivityId == null)
                {
                    Guid emptyGuid = Guid.Empty;
                    HResult = EventWriteTransfer(registrationHandle, ref eventDescriptor, activityId, &emptyGuid, userDataCount, userData);
                }

                return HResult;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
            [DllImport(Win32Native.ADVAPI32, ExactSpelling = true, EntryPoint = "EventWriteTransfer", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
            [SuppressUnmanagedCodeSecurityAttribute]        // Don't do security checks 
            private static extern int EventWriteTransfer(
                    [In] long registrationHandle,
                    [In] ref EventDescriptor eventDescriptor,
                    [In] Guid* activityId,
                    [In] Guid* relatedActivityId,
                    [In] int userDataCount,
                    [In] EventProvider.EventData* userData
                    );

            internal enum ActivityControl : uint
            {
                EVENT_ACTIVITY_CTRL_GET_ID = 1,
                EVENT_ACTIVITY_CTRL_SET_ID = 2,
                EVENT_ACTIVITY_CTRL_CREATE_ID = 3,
                EVENT_ACTIVITY_CTRL_GET_SET_ID = 4,
                EVENT_ACTIVITY_CTRL_CREATE_SET_ID = 5
            };

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
            [DllImport(Win32Native.ADVAPI32, ExactSpelling = true, EntryPoint = "EventActivityIdControl", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
            [SuppressUnmanagedCodeSecurityAttribute]        // Don't do security checks 
            internal static extern int EventActivityIdControl([In] ActivityControl ControlCode, [In][Out] ref Guid ActivityId);

            internal enum EVENT_INFO_CLASS
            {
                BinaryTrackInfo,
                SetEnableAllKeywords,
                SetTraits,
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
            [DllImport(Win32Native.ADVAPI32, ExactSpelling = true, EntryPoint = "EventSetInformation", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
            [SuppressUnmanagedCodeSecurityAttribute]        // Don't do security checks 
            internal static extern int EventSetInformation(
                [In] long registrationHandle,
                [In] EVENT_INFO_CLASS informationClass,
                [In] void* eventInformation,
                [In] int informationLength);

            // Support for EnumerateTraceGuidsEx
            internal enum TRACE_QUERY_INFO_CLASS
            {
                TraceGuidQueryList,
                TraceGuidQueryInfo,
                TraceGuidQueryProcess,
                TraceStackTracingInfo,
                MaxTraceSetInfoClass
            };

            internal struct TRACE_GUID_INFO
            {
                public int InstanceCount;
                public int Reserved;
            };

            internal struct TRACE_PROVIDER_INSTANCE_INFO
            {
                public int NextOffset;
                public int EnableCount;
                public int Pid;
                public int Flags;
            };

            internal struct TRACE_ENABLE_INFO
            {
                public int IsEnabled;
                public byte Level;
                public byte Reserved1;
                public ushort LoggerId;
                public int EnableProperty;
                public int Reserved2;
                public long MatchAnyKeyword;
                public long MatchAllKeyword;
            };

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
            [DllImport(Win32Native.ADVAPI32, ExactSpelling = true, EntryPoint = "EnumerateTraceGuidsEx", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
            [SuppressUnmanagedCodeSecurityAttribute]        // Don't do security checks 
            internal static extern int EnumerateTraceGuidsEx(
                TRACE_QUERY_INFO_CLASS TraceQueryInfoClass,
                void* InBuffer,
                int InBufferSize,
                void* OutBuffer,
                int OutBufferSize,
                ref int ReturnLength);

        }
    }
}
