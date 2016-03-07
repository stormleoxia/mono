//
// SystemWebTestShim/UrlUtils.cs
//
// Author:
//   Raja R Harinath (harinath@hurrynot.org)
//
// (C) 2009 Novell, Inc (http://www.novell.com)
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


using System;
using System.Text;
using System.Web;
using System.Web.SessionState;

namespace SystemWebTestShim {
	public class UrlUtils {

		// appRoot + SessionID + vpath
		public static string InsertSessionId (string id, string path)
		{
			string dir = GetDirectory (path);
			if (!dir.EndsWith ("/"))
				dir += "/";

			string appvpath = HttpRuntime.AppDomainAppVirtualPath;
			if (!appvpath.EndsWith ("/"))
				appvpath += "/";

			if (path.StartsWith (appvpath))
				path = path.Substring (appvpath.Length);

			if (path [0] == '/')
				path = path.Length > 1 ? path.Substring (1) : "";

			return Canonic (appvpath + "(" + id + ")/" + path);
		}


    	public static string Combine (string basePath, string relPath)
		{
			if (relPath == null)
				throw new ArgumentNullException ("relPath");

			int rlength = relPath.Length;
			if (rlength == 0)
				return "";

			relPath = relPath.Replace ('\\', '/');
			if (IsRooted (relPath))
				return Canonic (relPath);

			char first = relPath [0];
			if (rlength < 3 || first == '~' || first == '/' || first == '\\') {
				if (basePath == null || (basePath.Length == 1 && basePath [0] == '/'))
					basePath = String.Empty;

				string slash = (first == '/') ? "" : "/";
				if (first == '~') {
					if (rlength == 1) {
						relPath = "";
					} else if (rlength > 1 && relPath [1] == '/') {
						relPath = relPath.Substring (2);
						slash = "/";
					}

					string appvpath = HttpRuntime.AppDomainAppVirtualPath;
					if (appvpath.EndsWith ("/"))
						slash = "";

					return Canonic (appvpath + slash + relPath);
				}

				return Canonic (basePath + slash + relPath);
			}

			if (basePath == null || basePath.Length == 0 || basePath [0] == '~')
				basePath = HttpRuntime.AppDomainAppVirtualPath;

			if (basePath.Length <= 1)
				basePath = String.Empty;

			return Canonic (basePath + "/" + relPath);
		}

		static char [] path_sep = {'\\', '/'};
		
		public static string Canonic (string path)
		{
			bool isRooted = IsRooted(path);
			bool endsWithSlash = path.EndsWith("/");
			string [] parts = path.Split (path_sep);
			int end = parts.Length;
			
			int dest = 0;
			
			for (int i = 0; i < end; i++) {
				string current = parts [i];

				if (current.Length == 0)
					continue;

				if (current == "." )
					continue;

				if (current == "..") {
					dest --;
					continue;
				}
				if (dest < 0)
					if (!isRooted)
						throw new HttpException ("Invalid path.");
					else
						dest = 0;

				parts [dest++] = current;
			}
			if (dest < 0)
				throw new HttpException ("Invalid path.");

			if (dest == 0)
				return "/";

			string str = String.Join ("/", parts, 0, dest);
			str = RemoveDoubleSlashes (str);
			if (isRooted)
				str = "/" + str;
			if (endsWithSlash)
				str = str + "/";

			return str;
		}
		
		public static string GetDirectory (string url)
		{
			url = url.Replace('\\','/');
			int last = url.LastIndexOf ('/');

			if (last > 0) {
				if (last < url.Length)
					last++;
				return RemoveDoubleSlashes (url.Substring (0, last));
			}

			return "/";
		}

		public static string RemoveDoubleSlashes (string input)
		{
			// MS VirtualPathUtility removes duplicate '/'

			int index = -1;
			for (int i = 1; i < input.Length; i++)
				if (input [i] == '/' && input [i - 1] == '/') {
					index = i - 1;
					break;
				}

			if (index == -1) // common case optimization
				return input;

			StringBuilder sb = new StringBuilder (input.Length);
			sb.Append (input, 0, index);

			for (int i = index; i < input.Length; i++) {
				if (input [i] == '/') {
					int next = i + 1;
					if (next < input.Length && input [next] == '/')
						continue;
					sb.Append ('/');
				}
				else {
					sb.Append (input [i]);
				}
			}

			return sb.ToString ();
		}

		public static string GetFile (string url)
		{
			url = url.Replace('\\','/');
			int last = url.LastIndexOf ('/');
			if (last >= 0) {
				if (url.Length == 1) // Empty file name instead of ArgumentOutOfRange
					return "";
				return url.Substring (last+1);
			}

			throw new ArgumentException (String.Format ("GetFile: `{0}' does not contain a /", url));
		}
		
		public static bool IsRooted (string path)
		{
			if (path == null || path.Length == 0)
				return true;

			char c = path [0];
			if (c == '/' || c == '\\')
				return true;

			return false;
		}

		public static bool IsRelativeUrl (string path)
		{
			return (path [0] != '/' && path.IndexOf (':') == -1);
		}

                public static string ResolveVirtualPathFromAppAbsolute (string path)
                {
                        if (path [0] != '~') return path;
                                
                        if (path.Length == 1)
                                return HttpRuntime.AppDomainAppVirtualPath;

                        if (path [1] == '/' || path [1] == '\\') {
                                string appPath = HttpRuntime.AppDomainAppVirtualPath;
                                if (appPath.Length > 1) 
                                        return appPath + "/" + path.Substring (2);
                                return "/" + path.Substring (2);
                        }
                        return path;    
                }

                public static string ResolvePhysicalPathFromAppAbsolute (string path) 
                {
                        if (path [0] != '~') return path;

                        if (path.Length == 1)
                                return HttpRuntime.AppDomainAppPath;

                        if (path [1] == '/' || path [1] == '\\') {
                                string appPath = HttpRuntime.AppDomainAppPath;
                                if (appPath.Length > 1)
                                        return appPath + "/" + path.Substring (2);
                                return "/" + path.Substring (2);
                        }
                        return path;
                }
	}
}

