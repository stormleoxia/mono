//
// System.Web.GlobalPerfCounter.cs
//
// Authors:
//	Fabien Bondi (fbondi@leoxia.com)
//
// Copyright (C) 2015-2016 Leoxia, Inc (http://www.leoxia.com)
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
//

namespace System.Web
{
    internal enum GlobalPerfCounter
    {
        APPLICATION_RESTARTS,
        APPLICATIONS_RUNNING,
        REQUESTS_DISCONNECTED,
        REQUEST_EXECUTION_TIME,
        REQUESTS_REJECTED,
        REQUESTS_QUEUED,
        WPS_RUNNING,
        WPS_RESTARTS,
        REQUEST_WAIT_TIME,
        STATE_SERVER_SESSIONS_ACTIVE,
        STATE_SERVER_SESSIONS_ABANDONED,
        STATE_SERVER_SESSIONS_TIMED_OUT,
        STATE_SERVER_SESSIONS_TOTAL,
        REQUESTS_CURRENT,
        GLOBAL_AUDIT_SUCCESS,
        GLOBAL_AUDIT_FAIL,
        GLOBAL_EVENTS_ERROR,
        GLOBAL_EVENTS_HTTP_REQ_ERROR,
        GLOBAL_EVENTS_HTTP_INFRA_ERROR,
        REQUESTS_IN_NATIVE_QUEUE
    }
}