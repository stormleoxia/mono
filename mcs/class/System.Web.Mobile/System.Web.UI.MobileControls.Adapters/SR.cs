using System;
using System.Globalization;
using System.Resources;
using System.Security.Permissions;
using System.Threading;
namespace System.Web.UI.MobileControls.Adapters
{
	[AspNetHostingPermission(SecurityAction.LinkDemand, Unrestricted = true), AspNetHostingPermission(SecurityAction.InheritanceDemand, Unrestricted = true)]
	public class SR
	{
		public const string CalendarAdapterFirstPrompt = "CalendarAdapterFirstPrompt";
		public const string CalendarAdapterOptionPrompt = "CalendarAdapterOptionPrompt";
		public const string CalendarAdapterOptionType = "CalendarAdapterOptionType";
		public const string CalendarAdapterOptionEra = "CalendarAdapterOptionEra";
		public const string CalendarAdapterOptionChooseDate = "CalendarAdapterOptionChooseDate";
		public const string CalendarAdapterOptionChooseWeek = "CalendarAdapterOptionChooseWeek";
		public const string CalendarAdapterOptionChooseMonth = "CalendarAdapterOptionChooseMonth";
		public const string CalendarAdapterTextBoxErrorMessage = "CalendarAdapterTextBoxErrorMessage";
		public const string ChtmlImageAdapterDecimalCodeExpectedAfterGroupChar = "ChtmlImageAdapterDecimalCodeExpectedAfterGroupChar";
		public const string ChtmlPageAdapterRedirectPageContent = "ChtmlPageAdapterRedirectPageContent";
		public const string ChtmlPageAdapterRedirectLinkLabel = "ChtmlPageAdapterRedirectLinkLabel";
		public const string ControlAdapterBasePagePropertyShouldNotBeSet = "ControlAdapterBasePagePropertyShouldNotBeSet";
		public const string FormAdapterMultiControlsAttemptSecondaryUI = "FormAdapterMultiControlsAttemptSecondaryUI";
		public const string MobileTextWriterNotMultiPart = "MobileTextWriterNotMultiPart";
		public const string ObjectListAdapter_InvalidPostedData = "ObjectListAdapter_InvalidPostedData";
		public const string WmlMobileTextWriterBackLabel = "WmlMobileTextWriterBackLabel";
		public const string WmlMobileTextWriterOKLabel = "WmlMobileTextWriterOKLabel";
		public const string WmlMobileTextWriterGoLabel = "WmlMobileTextWriterGoLabel";
		public const string WmlPageAdapterServerError = "WmlPageAdapterServerError";
		public const string WmlPageAdapterStackTrace = "WmlPageAdapterStackTrace";
		public const string WmlPageAdapterPartialStackTrace = "WmlPageAdapterPartialStackTrace";
		public const string WmlPageAdapterMethod = "WmlPageAdapterMethod";
		public const string WmlObjectListAdapterDetails = "WmlObjectListAdapterDetails";
		public const string XhtmlCssHandler_IdNotPresent = "XhtmlCssHandler_IdNotPresent";
		public const string XhtmlCssHandler_StylesheetNotFound = "XhtmlCssHandler_StylesheetNotFound";
		public const string XhtmlObjectListAdapter_InvalidPostedData = "XhtmlObjectListAdapter_InvalidPostedData";
		public const string XhtmlMobileTextWriter_SessionKeyNotSet = "XhtmlMobileTextWriter_SessionKeyNotSet";
		public const string XhtmlMobileTextWriter_CacheKeyNotSet = "XhtmlMobileTextWriter_CacheKeyNotSet";
		private static SR loader;
		private ResourceManager resources;
		private static CultureInfo Culture
		{
			get
			{
				return null;
			}
		}
		public SR()
		{
			this.resources = new ResourceManager("System.Web.UI.MobileControls.Adapters", base.GetType().Assembly);
		}
		private static SR GetLoader()
		{
			if (SR.loader == null)
			{
				SR value = new SR();
				Interlocked.CompareExchange<SR>(ref SR.loader, value, null);
			}
			return SR.loader;
		}
		public static string GetString(string name, params object[] args)
		{
			return SR.GetString(SR.Culture, name, args);
		}
		public static string GetString(CultureInfo culture, string name, params object[] args)
		{
			SR sR = SR.GetLoader();
			if (sR == null)
			{
				return null;
			}
			string @string = sR.resources.GetString(name, culture);
			if (args != null && args.Length != 0)
			{
				for (int i = 0; i < args.Length; i++)
				{
					string text = args[i] as string;
					if (text != null && text.Length > 1024)
					{
						args[i] = text.Substring(0, 1021) + "...";
					}
				}
				return string.Format(CultureInfo.CurrentCulture, @string, args);
			}
			return @string;
		}
		public static string GetString(string name)
		{
			return SR.GetString(SR.Culture, name);
		}
		public static string GetString(CultureInfo culture, string name)
		{
			SR sR = SR.GetLoader();
			if (sR == null)
			{
				return null;
			}
			return sR.resources.GetString(name, culture);
		}
		public static bool GetBoolean(string name)
		{
			return SR.GetBoolean(SR.Culture, name);
		}
		public static bool GetBoolean(CultureInfo culture, string name)
		{
			bool result = false;
			SR sR = SR.GetLoader();
			if (sR != null)
			{
				object @object = sR.resources.GetObject(name, culture);
				if (@object is bool)
				{
					result = (bool)@object;
				}
			}
			return result;
		}
		public static char GetChar(string name)
		{
			return SR.GetChar(SR.Culture, name);
		}
		public static char GetChar(CultureInfo culture, string name)
		{
			char result = '\0';
			SR sR = SR.GetLoader();
			if (sR != null)
			{
				object @object = sR.resources.GetObject(name, culture);
				if (@object is char)
				{
					result = (char)@object;
				}
			}
			return result;
		}
		public static byte GetByte(string name)
		{
			return SR.GetByte(SR.Culture, name);
		}
		public static byte GetByte(CultureInfo culture, string name)
		{
			byte result = 0;
			SR sR = SR.GetLoader();
			if (sR != null)
			{
				object @object = sR.resources.GetObject(name, culture);
				if (@object is byte)
				{
					result = (byte)@object;
				}
			}
			return result;
		}
		public static short GetShort(string name)
		{
			return SR.GetShort(SR.Culture, name);
		}
		public static short GetShort(CultureInfo culture, string name)
		{
			short result = 0;
			SR sR = SR.GetLoader();
			if (sR != null)
			{
				object @object = sR.resources.GetObject(name, culture);
				if (@object is short)
				{
					result = (short)@object;
				}
			}
			return result;
		}
		public static int GetInt(string name)
		{
			return SR.GetInt(SR.Culture, name);
		}
		public static int GetInt(CultureInfo culture, string name)
		{
			int result = 0;
			SR sR = SR.GetLoader();
			if (sR != null)
			{
				object @object = sR.resources.GetObject(name, culture);
				if (@object is int)
				{
					result = (int)@object;
				}
			}
			return result;
		}
		public static long GetLong(string name)
		{
			return SR.GetLong(SR.Culture, name);
		}
		public static long GetLong(CultureInfo culture, string name)
		{
			long result = 0L;
			SR sR = SR.GetLoader();
			if (sR != null)
			{
				object @object = sR.resources.GetObject(name, culture);
				if (@object is long)
				{
					result = (long)@object;
				}
			}
			return result;
		}
		public static float GetFloat(string name)
		{
			return SR.GetFloat(SR.Culture, name);
		}
		public static float GetFloat(CultureInfo culture, string name)
		{
			float result = 0f;
			SR sR = SR.GetLoader();
			if (sR == null)
			{
				object @object = sR.resources.GetObject(name, culture);
				if (@object is float)
				{
					result = (float)@object;
				}
			}
			return result;
		}
		public static double GetDouble(string name)
		{
			return SR.GetDouble(SR.Culture, name);
		}
		public static double GetDouble(CultureInfo culture, string name)
		{
			double result = 0.0;
			SR sR = SR.GetLoader();
			if (sR == null)
			{
				object @object = sR.resources.GetObject(name, culture);
				if (@object is double)
				{
					result = (double)@object;
				}
			}
			return result;
		}
		public static object GetObject(string name)
		{
			return SR.GetObject(SR.Culture, name);
		}
		public static object GetObject(CultureInfo culture, string name)
		{
			SR sR = SR.GetLoader();
			if (sR == null)
			{
				return null;
			}
			return sR.resources.GetObject(name, culture);
		}
	}
}
