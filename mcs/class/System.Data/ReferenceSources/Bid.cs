using System;

static class Bid
{
    //+//////////////////////////////////////////////////////////////////////////////////////////
    //                                                                                         //
    //                                      INTERFACE                                          //
    //                                                                                         //
    //+//////////////////////////////////////////////////////////////////////////////////////////
    //
    //  ApiGroup control flags are accessible from attached diagnostic subsystem via corresponding
    //  delegate, so the output can be enabled/disabled on the fly.
    //
    internal enum ApiGroup : uint
    {
        Off = 0x00000000,

        Default = 0x00000001,   // Bid.TraceEx (Always ON)
        Trace = 0x00000002,   // Bid.Trace, Bid.PutStr
        Scope = 0x00000004,   // Bid.Scope{Enter|Leave|Auto}
        Perf = 0x00000008,   // TBD..
        Resource = 0x00000010,   // TBD..
        Memory = 0x00000020,   // TBD..
        StatusOk = 0x00000040,   // S_OK, STATUS_SUCCESS, etc.
        Advanced = 0x00000080,   // Bid.TraceEx

        Pooling = 0x00001000,
        Dependency = 0x00002000,
        StateDump = 0x00004000,
        Correlation = 0x00040000,

        MaskBid = 0x00000FFF,
        MaskUser = 0xFFFFF000,
        MaskAll = 0xFFFFFFFF
    }

	static IntPtr NoData          = (IntPtr)(-1);

	internal static void Trace(string fmtPrintfW, params object[] args)
	{
	}

	internal static void TraceEx(uint flags, string fmtPrintfW, params object[] args)
	{
	}
#if !MOBILE
	internal static void TraceSqlReturn(string fmtPrintfW, System.Data.Odbc.ODBC32.RetCode a1, string a2)
	{
	}
#endif
	internal static void ScopeEnter(out IntPtr hScp, string fmt, params object[] args) {
		hScp = NoData;
	}

	internal static void ScopeLeave(ref IntPtr hScp) {
		hScp = NoData;
	}

    internal static bool AdvancedOn
    {
        get { return (modFlags & ApiGroup.Advanced) != 0; }
    }

    private static ApiGroup modFlags;

}