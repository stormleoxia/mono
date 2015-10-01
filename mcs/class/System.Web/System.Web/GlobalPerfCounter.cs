﻿using System;
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
