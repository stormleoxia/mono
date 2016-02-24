//
// RouteCollection.cs
//
// Author:
//	Atsushi Enomoto <atsushi@ximian.com>
//
// Copyright (C) 2008 Novell Inc. http://novell.com
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using System.Threading;
using System.Web;
using System.Web.Hosting;

namespace System.Web.Routing
{
	[TypeForwardedFrom ("System.Web.Routing, Version=3.5.0.0, Culture=Neutral, PublicKeyToken=31bf3856ad364e35")]
	[AspNetHostingPermission (SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	[AspNetHostingPermission (SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	public class RouteCollection : Collection<RouteBase>
	{
	    private readonly VirtualPathProvider _provider;
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

		public RouteCollection () : 
            this (null)
		{
		}

		public RouteCollection (VirtualPathProvider virtualPathProvider)
		{
		    if (virtualPathProvider == null) {
		        _provider = HostingEnvironment.VirtualPathProvider;
		    }
		    else {
                _provider = virtualPathProvider;		        
		    }
		}

		private readonly Dictionary<string,RouteBase> _namedMap = new Dictionary<string,RouteBase> ();

		public RouteBase this [string name] {
			get {
                if (String.IsNullOrEmpty(name)) {
                    return null;
                }
			    using (GetReadLock())
			    {
			        RouteBase route;
			        if (_namedMap.TryGetValue(name, out route)) {
			            return route;
			        }
			    }
			    return null;
			}
		}

		public bool LowercaseUrls { get; set; }
		public bool AppendTrailingSlash { get; set; }
		public bool RouteExistingFiles { get; set; }

		public void Add (string name, RouteBase item)
		{
            if (item == null) {
                throw new ArgumentNullException("item");
            }
			using (GetWriteLock ()) {
                // It could contains the same item with different names                
                AssertDoesNotContain(name);
				base.Add (item);
				if (!String.IsNullOrEmpty (name))
					_namedMap.Add(name, item);
			}
		}

	    private void AssertDoesNotContain(string name)
	    {
            if (!String.IsNullOrEmpty(name)) {
	            if (_namedMap.ContainsKey(name)) {
	                throw new ArgumentException(
	                    String.Format(
	                        CultureInfo.CurrentUICulture,
	                        "There is already a route for {0}",
	                        name),
	                    "name");
	            }
	        }
	    }


        private void AssertDoesNotContain(RouteBase item)
	    {
            // Insertions by index forbid several occurence of the same item
	        if (Contains(item)) {
                throw new ArgumentException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        "Route is already in the current RouteCollection"),
                    "item");
            }
	    }

	    protected override void ClearItems ()
		{
			using (GetWriteLock ())
				base.ClearItems ();
		}

		public IDisposable GetReadLock ()
		{
			_rwLock.EnterReadLock();
            return new ReadLockDisposable(_rwLock);
		}

		public RouteData GetRouteData (HttpContextBase httpContext)
		{
			if (httpContext == null)
				throw new ArgumentNullException ("httpContext");

			if (httpContext.Request == null)
				throw new ArgumentException ("The context does not contain any request data.", "httpContext");
			if (Count == 0)
				return null;
			if (!RouteExistingFiles) {
				if (IsRouteToExistingFile(httpContext))
					return null;
			}
		    using (GetReadLock()) {
		        foreach (RouteBase rb in this) {
		            var routeData = rb.GetRouteData(httpContext);
		            if (routeData != null) {
		                return routeData;
		            }
		        }
		    }
		    return null;
		}

	    private bool IsRouteToExistingFile(HttpContextBase httpContext)
	    {
	        var path = httpContext.Request.AppRelativeCurrentExecutionFilePath;
	        VirtualPathProvider vpp = _provider;
	        return path != "~/" && vpp != null && (vpp.FileExists(path) || vpp.DirectoryExists(path));
	    }

	    public VirtualPathData GetVirtualPath (RequestContext requestContext, RouteValueDictionary values)
		{
			return GetVirtualPath (requestContext, null, values);
		}

		public VirtualPathData GetVirtualPath (RequestContext requestContext, string name, RouteValueDictionary values)
		{
			if (requestContext == null)
				throw new ArgumentNullException ("httpContext");
			VirtualPathData vp = null;
			if (!String.IsNullOrEmpty (name)) {
				RouteBase rb = this [name];
				if (rb != null)
					vp = rb.GetVirtualPath (requestContext, values);
				else
					throw new ArgumentException ("A route named '" + name + "' could not be found in the route collection.", "name");
			} else {
			    using (GetReadLock()) {
			        foreach (RouteBase rb in this) {
			            vp = rb.GetVirtualPath(requestContext, values);
			            if (vp != null)
			                break;
			        }
			    }
			}

			if (vp != null) {
				string appPath = requestContext.HttpContext.Request.ApplicationPath;
				if (appPath != null && (appPath.Length == 0 || !appPath.EndsWith ("/", StringComparison.Ordinal)))
					appPath += "/";
				
				string pathWithApp = String.Concat (appPath, vp.VirtualPath);
				vp.VirtualPath = requestContext.HttpContext.Response.ApplyAppPathModifier (pathWithApp);
				return vp;
			}

			return null;
		}

		public IDisposable GetWriteLock ()
		{
			_rwLock.EnterWriteLock();
            return new WriteLockDisposable(_rwLock);
		}
		public void Ignore (string url)
		{
			Ignore (url, null);
		}

		public void Ignore (string url, object constraints)
		{
			if (url == null)
				throw new ArgumentNullException ("url");

			Add (new Route (url, null, new RouteValueDictionary (constraints), new StopRoutingHandler ()));
		}
		
		public Route MapPageRoute (string routeName, string routeUrl, string physicalFile)
		{
			return MapPageRoute (routeName, routeUrl, physicalFile, true, null, null, null);
		}

		public Route MapPageRoute (string routeName, string routeUrl, string physicalFile, bool checkPhysicalUrlAccess)
		{
			return MapPageRoute (routeName, routeUrl, physicalFile, checkPhysicalUrlAccess, null, null, null);
		}

		public Route MapPageRoute (string routeName, string routeUrl, string physicalFile, bool checkPhysicalUrlAccess,
					   RouteValueDictionary defaults)
		{
			return MapPageRoute (routeName, routeUrl, physicalFile, checkPhysicalUrlAccess, defaults, null, null);
		}

		public Route MapPageRoute (string routeName, string routeUrl, string physicalFile, bool checkPhysicalUrlAccess,
					   RouteValueDictionary defaults, RouteValueDictionary constraints)
		{
			return MapPageRoute (routeName, routeUrl, physicalFile, checkPhysicalUrlAccess, defaults, constraints, null);
		}

		public Route MapPageRoute (string routeName, string routeUrl, string physicalFile, bool checkPhysicalUrlAccess,
					   RouteValueDictionary defaults, RouteValueDictionary constraints, RouteValueDictionary dataTokens)
		{
			if (routeUrl == null)
				throw new ArgumentNullException ("routeUrl");
			
			var route = new Route (routeUrl, defaults, constraints, dataTokens, new PageRouteHandler (physicalFile, checkPhysicalUrlAccess));
			Add (routeName, route);

			return route;
		}

        // Called by collection (so already locked)
	    protected override void InsertItem(int index, RouteBase item)
	    {
	        if (item == null) {
	            throw new ArgumentNullException("item");
	        }
	        AssertDoesNotContain(item);
	        // FIXME: what happens wrt its name?
	        base.InsertItem(index, item);

	    }

	    private void RemoveRouteNames(int index)
	    {
	        RouteBase route = this[index];
	        foreach (KeyValuePair<string, RouteBase> namedRoute in _namedMap) {
	            if (namedRoute.Value == route)
	            {
	                _namedMap.Remove(namedRoute.Key);
	                break;
	            }
	        }
	    }

        // Called by collection (so already locked)
	    protected override void RemoveItem (int index)
		{
			// FIXME: what happens wrt its name?
			using (GetWriteLock ()) {
                RemoveRouteNames(index);
				base.RemoveItem (index);
			}
		}

        // Called by collection (so already locked)
	    protected override void SetItem(int index, RouteBase item)
	    {
	        if (item == null) {
	            throw new ArgumentNullException("item");
	        }
	        AssertDoesNotContain(item);
	        // FIXME: what happens wrt its name?
	        RemoveRouteNames(index);
	        base.SetItem(index, item);
	    }

	    private struct WriteLockDisposable : IDisposable
        {
            private readonly ReaderWriterLockSlim _rwLock;

            public WriteLockDisposable(ReaderWriterLockSlim rwLock)
            {
                _rwLock = rwLock;
            }

            public void Dispose()
            {
                _rwLock.ExitWriteLock();
            }
        }

        private struct ReadLockDisposable : IDisposable
        {
            private readonly ReaderWriterLockSlim _rwLock;

            public ReadLockDisposable(ReaderWriterLockSlim rwLock)
            {
                _rwLock = rwLock;
            }

            public void Dispose()
            {
                _rwLock.ExitReadLock();
            }
        }
	}

}
