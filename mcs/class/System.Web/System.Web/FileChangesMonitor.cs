//
// System.Web.FileChangesMonitor.cs
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
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Web.Configuration;
using System.Web.Util;

namespace System.Web
{
    // Contains information about the target of a file change notification

#if !FEATURE_PAL // FEATURE_PAL does not enable access control

    // holds information about a single file and the targets of change notification
    internal sealed class FileMonitor
    {
        internal readonly DirectoryMonitor DirectoryMonitor; // the parent
        internal readonly HybridDictionary Aliases; // aliases for this file
        private readonly HybridDictionary _targets; // targets of notification
        private FileAttributesData _fad; // file attributes

        internal FileMonitor(
            DirectoryMonitor dirMon, string fileNameLong, string fileNameShort,
            bool exists, FileAttributesData fad, FileSystemSecurity dacl)
        {
            DirectoryMonitor = dirMon;
            FileNameLong = fileNameLong;
            FileNameShort = fileNameShort;
            Exists = exists;
            _fad = fad;
            Dacl = dacl;
            _targets = new HybridDictionary();
            Aliases = new HybridDictionary(true);
        }

        internal string FileNameLong { get; private set; }
        internal string FileNameShort { get; private set; }
        internal bool Exists { get; private set; }

        internal bool IsDirectory
        {
            get { return (FileNameLong == null); }
        }

        internal FileAction LastAction { get; set; }

        internal DateTime UtcLastCompletion { get; set; }

        // Returns the attributes of a file, updating them if the file has changed.
        internal FileAttributesData Attributes
        {
            get { return _fad; }
        }

        internal FileSystemSecurity Dacl { get; private set; }

        internal void ResetCachedAttributes()
        {
            _fad = null;
            Dacl = null;
        }

        internal void UpdateCachedAttributes()
        {
            var path = Path.Combine(DirectoryMonitor.Directory, FileNameLong);
            FileAttributesData.GetFileAttributes(path, out _fad);
            Dacl = FileSecurity.GetDacl(path);
        }

        // Set new file information when a file comes into existence
        internal void MakeExist(FindFileData ffd, FileSystemSecurity dacl)
        {
            FileNameLong = ffd.FileNameLong;
            FileNameShort = ffd.FileNameShort;
            _fad = ffd.FileAttributesData;
            Dacl = dacl;
            Exists = true;
        }

        // Remove a file from existence
        internal void MakeExtinct()
        {
            _fad = null;
            Dacl = null;
            Exists = false;
        }

        internal void RemoveFileNameShort()
        {
            FileNameShort = null;
        }

        internal ICollection Targets
        {
            get { return _targets.Values; }
        }

        // Add delegate for this file.
        internal void AddTarget(FileChangeEventHandler callback, string alias, bool newAlias)
        {
            var target = (FileMonitorTarget) _targets[callback.Target];
            if (target != null)
            {
                target.AddRef();
            }
            else
            {
#if DEBUG
    // Needs the lock to [....] with DebugDescription
                lock (_targets) {
#endif
                _targets.Add(callback.Target, new FileMonitorTarget(callback, alias));
#if DEBUG
                }
#endif
            }

            if (newAlias)
            {
                Aliases[alias] = alias;
            }
        }


        // Remove delegate for this file given the target object.
        internal int RemoveTarget(object callbackTarget)
        {
            var target = (FileMonitorTarget) _targets[callbackTarget];
#if DEBUG            
            if (FileChangesMonitor.s_enableRemoveTargetAssert) {
                Debug.Assert(target != null, "removing file monitor target that was never added or already been removed");
            }
#endif
            if (target != null && target.Release() == 0)
            {
#if DEBUG
    // Needs the lock to [....] with DebugDescription
                lock (_targets) {
#endif
                _targets.Remove(callbackTarget);
#if DEBUG
                }
#endif
            }

            return _targets.Count;
        }

#if DEBUG
        internal string DebugDescription(string indent) {
            StringBuilder   sb = new StringBuilder(200);
            string          i2 = indent + "    ";
            string          i3 = i2 + "    ";
            DictionaryEntryTypeComparer detcomparer = new DictionaryEntryTypeComparer();

            sb.Append(indent + "System.Web.FileMonitor: ");
            if (FileNameLong != null) {
                sb.Append(FileNameLong);
                if (FileNameShort != null) {
                    sb.Append("; ShortFileName=" + FileNameShort);
                }

                sb.Append("; FileExists="); sb.Append(Exists);                
            }
            else {
                sb.Append("<ANY>");
            }
            sb.Append("\n");
            sb.Append(i2 + "LastAction="); sb.Append(LastAction);
            sb.Append("; LastCompletion="); sb.Append(Debug.FormatUtcDate(UtcLastCompletion));
            sb.Append("\n");

            if (_fad != null) {
                sb.Append(_fad.DebugDescription(i2));
            }
            else {
                sb.Append(i2 + "FileAttributesData = <null>\n");
            }

            DictionaryEntry[] delegateEntries;

            lock (_targets) {
                sb.Append(i2 + _targets.Count + " delegates...\n");

                delegateEntries = new DictionaryEntry[_targets.Count];
                _targets.CopyTo(delegateEntries, 0);
            }
            
            Array.Sort(delegateEntries, detcomparer);
            
            foreach (DictionaryEntry d in delegateEntries) {
                sb.Append(i3 + "Delegate " + d.Key.GetType() + "(HC=" + d.Key.GetHashCode().ToString("x", NumberFormatInfo.InvariantInfo) + ")\n");
            }

            return sb.ToString();
        }
#endif
    }

    // Change notifications delegate from native code.
    internal delegate void NativeFileChangeNotification(
        FileAction action, [In, MarshalAs(UnmanagedType.LPWStr)] string fileName, long ticks);


    internal sealed class NotificationQueueItem
    {
        internal readonly FileAction Action;
        internal readonly FileChangeEventHandler Callback;
        internal readonly string Filename;

        internal NotificationQueueItem(FileChangeEventHandler callback, FileAction action, string filename)
        {
            Callback = callback;
            Action = action;
            Filename = filename;
        }
    }

    //
    // Monitor changes in a single directory.
    //
#endif // !FEATURE_PAL

    //
    // Manager for directory monitors.                       
    // Provides file change notification services in ASP.NET 
    //
    internal sealed class FileChangesMonitor
    {
#if !FEATURE_PAL // FEATURE_PAL does not enable file change notification

        internal static string[] s_dirsToMonitor =
        {
            HttpRuntime.BinDirectoryName,
            HttpRuntime.ResourcesDirectoryName,
            HttpRuntime.CodeDirectoryName,
            HttpRuntime.WebRefDirectoryName,
            HttpRuntime.BrowsersDirectoryName
        };

        internal const int MAX_PATH = 260;

#pragma warning disable 0649
        private ReadWriteSpinLock _lockDispose; // spinlock for coordinating dispose
#pragma warning restore 0649

        private bool _disposed; // have we disposed?
        private readonly Hashtable _aliases; // alias -> FileMonitor
        private readonly Hashtable _dirs; // dir -> DirectoryMonitor
        private DirectoryMonitor _dirMonSubdirs; // subdirs monitor for renames
        private readonly Hashtable _subDirDirMons; // Hashtable of DirectoryMonitor used in ListenToSubdirectoryChanges
        private ArrayList _dirMonSpecialDirs; // top level dirs we monitor
        private FileChangeEventHandler _callbackRenameOrCriticaldirChange; // event handler for renames and bindir
        private int _activeCallbackCount; // number of callbacks currently executing

        private readonly DirectoryMonitor _dirMonAppPathInternal;
            // watches all files and subdirectories (at any level) beneath HttpRuntime.AppDomainAppPathInternal

        private readonly string _appPathInternal; // HttpRuntime.AppDomainAppPathInternal
        private readonly int _FCNMode; // from registry, controls how we monitor directories

#if DEBUG
        internal static bool    s_enableRemoveTargetAssert;
#endif

        // Dev10 927283: We were appending to HttpRuntime._shutdownMessage in DirectoryMonitor.OnFileChange when
        // we received overwhelming changes and errors, but not all overwhelming file change notifications result
        // in a shutdown.  The fix is to only append to _shutdownMessage when the domain is being shutdown.
        internal static string GenerateErrorMessage(FileAction action, string fileName = null)
        {
            string message = null;
            if (action == FileAction.Overwhelming)
            {
                message = "Overwhelming Change Notification in ";
            }
            else if (action == FileAction.Error)
            {
                message = "File Change Notification Error in ";
            }
            else
            {
                return null;
            }
            return (fileName != null) ? message + Path.GetDirectoryName(fileName) : message;
        }

        internal static HttpException CreateFileMonitoringException(int hr, string path)
        {
            string message;
            var logEvent = false;

            switch (hr)
            {
                case Util.HResults.E_FILENOTFOUND:
                case Util.HResults.E_PATHNOTFOUND:
                    message = SR.Directory_does_not_exist_for_monitoring;
                    break;

                case Util.HResults.E_ACCESSDENIED:
                    message = SR.Access_denied_for_monitoring;
                    logEvent = true;
                    break;

                case Util.HResults.E_INVALIDARG:
                    message = SR.Invalid_file_name_for_monitoring;
                    break;

                case Util.HResults.ERROR_TOO_MANY_CMDS:
                    message = SR.NetBios_command_limit_reached;
                    logEvent = true;
                    break;

                default:
                    message = SR.Failed_to_start_monitoring;
                    break;
            }


            if (logEvent)
            {
                // Need to raise an eventlog too.
                // NOT SUPPORTED on all platforms
                NotSupportedMethods.RaiseFileMonitoringEventlogEvent(
                    SR.GetString(message, HttpRuntime.GetSafePath(path)) +
                    @"\n\r" +
                    SR.GetString(SR.App_Virtual_Path, HttpRuntime.AppDomainAppVirtualPath),
                    path, HttpRuntime.AppDomainAppVirtualPath, hr);
            }

            return new HttpException(SR.GetString(message, HttpRuntime.GetSafePath(path)), hr);
        }

        internal static string GetFullPath(string alias)
        {
            // Assert PathDiscovery before call to Path.GetFullPath
            try
            {
                new FileIOPermission(FileIOPermissionAccess.PathDiscovery, alias).Assert();
            }
            catch
            {
                throw CreateFileMonitoringException(Util.HResults.E_INVALIDARG, alias);
            }

            var path = Path.GetFullPath(alias);
            path = FileUtil.RemoveTrailingDirectoryBackSlash(path);

            return path;
        }

        private bool IsBeneathAppPathInternal(string fullPathName)
        {
            if (_appPathInternal != null
                && fullPathName.Length > _appPathInternal.Length + 1
                && fullPathName.IndexOf(_appPathInternal, StringComparison.OrdinalIgnoreCase) > -1
                && fullPathName[_appPathInternal.Length] == Path.DirectorySeparatorChar)
            {
                return true;
            }
            return false;
        }

        private bool IsFCNDisabled
        {
            get { return _FCNMode == 1; }
        }

        internal FileChangesMonitor(FcnMode mode)
        {
            // Possible values for DWORD FCNMode:
            //       does not exist == default behavior (create DirectoryMonitor for each subdir)
            //              0 or >2 == default behavior (create DirectoryMonitor for each subdir)
            //                    1 == disable File Change Notifications (FCN)
            //                    2 == create 1 DirectoryMonitor for AppPathInternal and watch subtrees
            switch (mode)
            {
                case FcnMode.NotSet:
                    // If the mode is not set, we use the registry key's value
                    if (NotSupportedMethods.GetDirMonConfiguration(out _FCNMode) != Util.HResults.S_OK)
                    {
                        _FCNMode = 0;
                    }
                    break;
                case FcnMode.Disabled:
                    _FCNMode = 1;
                    break;
                case FcnMode.Single:
                    _FCNMode = 2;
                    break;
                case FcnMode.Default:
                default:
                    _FCNMode = 0;
                    break;
            }

            if (IsFCNDisabled)
            {
                return;
            }

            _aliases = Hashtable.Synchronized(new Hashtable(StringComparer.OrdinalIgnoreCase));
            _dirs = new Hashtable(StringComparer.OrdinalIgnoreCase);
            _subDirDirMons = new Hashtable(StringComparer.OrdinalIgnoreCase);

            if (_FCNMode == 2 && HttpRuntime.AppDomainAppPathInternal != null)
            {
                _appPathInternal = GetFullPath(HttpRuntime.AppDomainAppPathInternal);
                _dirMonAppPathInternal = new DirectoryMonitor(_appPathInternal, _FCNMode);
            }

#if DEBUG
            if ((int)Misc.GetAspNetRegValue(null /*subKey*/, "FCMRemoveTargetAssert", 0) > 0) {
                s_enableRemoveTargetAssert = true;
            }
#endif
        }

        internal bool IsDirNameMonitored(string fullPath, string dirName)
        {
            // is it one of the not-so-special directories we're monitoring?
            if (_dirs.ContainsKey(fullPath))
            {
                return true;
            }
            // is it one of the special directories (bin, App_Code, etc) or a subfolder?
            foreach (var specialDirName in s_dirsToMonitor)
            {
                if (StringUtil.StringStartsWithIgnoreCase(dirName, specialDirName))
                {
                    // a special directory?
                    if (dirName.Length == specialDirName.Length)
                    {
                        return true;
                    }
                        // a subfolder?
                    if (dirName.Length > specialDirName.Length &&
                        dirName[specialDirName.Length] == Path.DirectorySeparatorChar)
                    {
                        return true;
                    }
                }
            }
            // Dev10 Bug 663511: Deletes, moves, and renames of the App_LocalResources folder may be ignored
            if (dirName.IndexOf(HttpRuntime.LocalResourcesDirectoryName, StringComparison.OrdinalIgnoreCase) > -1)
            {
                return true;
            }
            // we're not monitoring it
            return false;
        }

        //
        // Find the directory monitor. If not found, maybe add it.
        // If the directory is not actively monitoring, ensure that
        // it still represents an accessible directory.
        //
        private DirectoryMonitor FindDirectoryMonitor(string dir, bool addIfNotFound, bool throwOnError)
        {
            DirectoryMonitor dirMon;
            FileAttributesData fad = null;
            int hr;

            dirMon = (DirectoryMonitor) _dirs[dir];
            if (dirMon != null)
            {
                if (!dirMon.IsMonitoring())
                {
                    hr = FileAttributesData.GetFileAttributes(dir, out fad);
                    if (hr != Util.HResults.S_OK || (fad.FileAttributes & FileAttributes.Directory) == 0)
                    {
                        dirMon = null;
                    }
                }
            }

            if (dirMon != null || !addIfNotFound)
            {
                return dirMon;
            }

            lock (_dirs.SyncRoot)
            {
                // Check again, this time under synchronization.
                dirMon = (DirectoryMonitor) _dirs[dir];
                if (dirMon != null)
                {
                    if (!dirMon.IsMonitoring())
                    {
                        // Fail if it's not a directory or inaccessible.
                        hr = FileAttributesData.GetFileAttributes(dir, out fad);
                        if (hr == Util.HResults.S_OK && (fad.FileAttributes & FileAttributes.Directory) == 0)
                        {
                            // Fail if it's not a directory.
                            hr = Util.HResults.E_INVALIDARG;
                        }

                        if (hr != Util.HResults.S_OK)
                        {
                            // Not accessible or a dir, so stop monitoring and remove.
                            _dirs.Remove(dir);
                            dirMon.StopMonitoring();
                            if (addIfNotFound && throwOnError)
                            {
                                throw CreateFileMonitoringException(hr, dir);
                            }

                            return null;
                        }
                    }
                }
                else if (addIfNotFound)
                {
                    // Fail if it's not a directory or inaccessible.
                    hr = FileAttributesData.GetFileAttributes(dir, out fad);
                    if (hr == Util.HResults.S_OK && (fad.FileAttributes & FileAttributes.Directory) == 0)
                    {
                        hr = Util.HResults.E_INVALIDARG;
                    }

                    if (hr == Util.HResults.S_OK)
                    {
                        // Add a new directory monitor.
                        dirMon = new DirectoryMonitor(dir, false, DirectoryMonitor.RDCW_FILTER_FILE_AND_DIR_CHANGES,
                            _FCNMode);
                        _dirs.Add(dir, dirMon);
                    }
                    else if (throwOnError)
                    {
                        throw CreateFileMonitoringException(hr, dir);
                    }
                }
            }

            return dirMon;
        }

        // Remove the aliases of a file monitor.
        internal void RemoveAliases(FileMonitor fileMon)
        {
            if (IsFCNDisabled)
            {
                return;
            }

            foreach (DictionaryEntry entry in fileMon.Aliases)
            {
                if (_aliases[entry.Key] == fileMon)
                {
                    _aliases.Remove(entry.Key);
                }
            }
        }

        //
        // Request to monitor a file, which may or may not exist.
        //
        internal DateTime StartMonitoringFile(string alias, FileChangeEventHandler callback)
        {
            Debug.Trace("FileChangesMonitor",
                "StartMonitoringFile\n" + "\tArgs: File=" + alias + "; Callback=" + callback.Target + "(HC=" +
                callback.Target.GetHashCode().ToString("x", NumberFormatInfo.InvariantInfo) + ")");

            FileMonitor fileMon;
            DirectoryMonitor dirMon;
            string fullPathName, dir, file;
            var addAlias = false;

            if (alias == null)
            {
                throw CreateFileMonitoringException(Util.HResults.E_INVALIDARG, alias);
            }

            if (IsFCNDisabled)
            {
                fullPathName = GetFullPath(alias);
                FindFileData ffd = null;
                var hr = FindFileData.FindFile(fullPathName, out ffd);
                if (hr == Util.HResults.S_OK)
                {
                    return ffd.FileAttributesData.UtcLastWriteTime;
                }
                return DateTime.MinValue;
            }

            using (new ApplicationImpersonationContext())
            {
                _lockDispose.AcquireReaderLock();
                try
                {
                    // Don't start monitoring if disposed.
                    if (_disposed)
                    {
                        return DateTime.MinValue;
                    }

                    fileMon = (FileMonitor) _aliases[alias];
                    if (fileMon != null)
                    {
                        // Used the cached directory monitor and file name.
                        dirMon = fileMon.DirectoryMonitor;
                        file = fileMon.FileNameLong;
                    }
                    else
                    {
                        addAlias = true;

                        if (alias.Length == 0 || !UrlPath.IsAbsolutePhysicalPath(alias))
                        {
                            throw CreateFileMonitoringException(Util.HResults.E_INVALIDARG, alias);
                        }

                        //
                        // Get the directory and file name, and lookup 
                        // the directory monitor.
                        //
                        fullPathName = GetFullPath(alias);

                        if (IsBeneathAppPathInternal(fullPathName))
                        {
                            dirMon = _dirMonAppPathInternal;
                            file = fullPathName.Substring(_appPathInternal.Length + 1);
                        }
                        else
                        {
                            dir = UrlPath.GetDirectoryOrRootName(fullPathName);
                            file = Path.GetFileName(fullPathName);
                            if (string.IsNullOrEmpty(file))
                            {
                                // not a file
                                throw CreateFileMonitoringException(Util.HResults.E_INVALIDARG, alias);
                            }
                            dirMon = FindDirectoryMonitor(dir, true /*addIfNotFound*/, true /*throwOnError*/);
                        }
                    }

                    fileMon = dirMon.StartMonitoringFileWithAssert(file, callback, alias);
                    if (addAlias)
                    {
                        _aliases[alias] = fileMon;
                    }
                }
                finally
                {
                    _lockDispose.ReleaseReaderLock();
                }

                FileAttributesData fad;
                fileMon.DirectoryMonitor.GetFileAttributes(file, out fad);

                Debug.Dump("FileChangesMonitor", this);

                if (fad != null)
                {
                    return fad.UtcLastWriteTime;
                }
                return DateTime.MinValue;
            }
        }

        //
        // Request to monitor a path, which may be file, directory, or non-existent
        // file.
        //
        internal DateTime StartMonitoringPath(string alias, FileChangeEventHandler callback, out FileAttributesData fad)
        {
            Debug.Trace("FileChangesMonitor",
                "StartMonitoringPath\n" + "\tArgs: File=" + alias + "; Callback=" + callback.Target + "(HC=" +
                callback.Target.GetHashCode().ToString("x", NumberFormatInfo.InvariantInfo) + ")");

            FileMonitor fileMon = null;
            DirectoryMonitor dirMon = null;
            string fullPathName, dir, file = null;
            var addAlias = false;

            fad = null;

            if (alias == null)
            {
                throw new HttpException(SR.GetString(SR.Invalid_file_name_for_monitoring, string.Empty));
            }

            if (IsFCNDisabled)
            {
                fullPathName = GetFullPath(alias);
                FindFileData ffd = null;
                var hr = FindFileData.FindFile(fullPathName, out ffd);
                if (hr == Util.HResults.S_OK)
                {
                    fad = ffd.FileAttributesData;
                    return ffd.FileAttributesData.UtcLastWriteTime;
                }
                return DateTime.MinValue;
            }

            using (new ApplicationImpersonationContext())
            {
                _lockDispose.AcquireReaderLock();
                try
                {
                    if (_disposed)
                    {
                        return DateTime.MinValue;
                    }

                    // do/while loop once to make breaking out easy
                    do
                    {
                        fileMon = (FileMonitor) _aliases[alias];
                        if (fileMon != null)
                        {
                            // Used the cached directory monitor and file name.
                            file = fileMon.FileNameLong;
                            fileMon = fileMon.DirectoryMonitor.StartMonitoringFileWithAssert(file, callback, alias);
                            continue;
                        }

                        addAlias = true;

                        if (alias.Length == 0 || !UrlPath.IsAbsolutePhysicalPath(alias))
                        {
                            throw new HttpException(SR.GetString(SR.Invalid_file_name_for_monitoring,
                                HttpRuntime.GetSafePath(alias)));
                        }

                        fullPathName = GetFullPath(alias);

                        // see if the path is beneath HttpRuntime.AppDomainAppPathInternal
                        if (IsBeneathAppPathInternal(fullPathName))
                        {
                            dirMon = _dirMonAppPathInternal;
                            file = fullPathName.Substring(_appPathInternal.Length + 1);
                            fileMon = dirMon.StartMonitoringFileWithAssert(file, callback, alias);
                            continue;
                        }

                        // try treating the path as a directory
                        dirMon = FindDirectoryMonitor(fullPathName, false, false);
                        if (dirMon != null)
                        {
                            fileMon = dirMon.StartMonitoringFileWithAssert(null, callback, alias);
                            continue;
                        }

                        // try treaing the path as a file
                        dir = UrlPath.GetDirectoryOrRootName(fullPathName);
                        file = Path.GetFileName(fullPathName);
                        if (!string.IsNullOrEmpty(file))
                        {
                            dirMon = FindDirectoryMonitor(dir, false, false);
                            if (dirMon != null)
                            {
                                // try to add it - a file is the common case,
                                // and we avoid hitting the disk twice
                                try
                                {
                                    fileMon = dirMon.StartMonitoringFileWithAssert(file, callback, alias);
                                }
                                catch
                                {
                                }

                                if (fileMon != null)
                                {
                                    continue;
                                }
                            }
                        }

                        // We aren't monitoring this path or its parent directory yet. 
                        // Hit the disk to determine if it's a directory or file.
                        dirMon = FindDirectoryMonitor(fullPathName, true, false);
                        if (dirMon != null)
                        {
                            // It's a directory, so monitor all changes in it
                            file = null;
                        }
                        else
                        {
                            // It's not a directory, so treat as file
                            if (string.IsNullOrEmpty(file))
                            {
                                throw CreateFileMonitoringException(Util.HResults.E_INVALIDARG, alias);
                            }

                            dirMon = FindDirectoryMonitor(dir, true, true);
                        }

                        fileMon = dirMon.StartMonitoringFileWithAssert(file, callback, alias);
                    } while (false);

                    if (!fileMon.IsDirectory)
                    {
                        fileMon.DirectoryMonitor.GetFileAttributes(file, out fad);
                    }

                    if (addAlias)
                    {
                        _aliases[alias] = fileMon;
                    }
                }
                finally
                {
                    _lockDispose.ReleaseReaderLock();
                }

                Debug.Dump("FileChangesMonitor", this);

                if (fad != null)
                {
                    return fad.UtcLastWriteTime;
                }
                return DateTime.MinValue;
            }
        }

        //
        // Request to monitor the bin directory and directory renames anywhere under app
        //

        internal void StartMonitoringDirectoryRenamesAndBinDirectory(string dir, FileChangeEventHandler callback)
        {
            Debug.Trace("FileChangesMonitor",
                "StartMonitoringDirectoryRenamesAndBinDirectory\n" + "\tArgs: File=" + dir + "; Callback=" +
                callback.Target + "(HC=" + callback.Target.GetHashCode().ToString("x", NumberFormatInfo.InvariantInfo) +
                ")");

            if (string.IsNullOrEmpty(dir))
            {
                throw new HttpException(SR.GetString(SR.Invalid_file_name_for_monitoring, string.Empty));
            }

            if (IsFCNDisabled)
            {
                return;
            }

#if DEBUG
            Debug.Assert(_dirs.Count == 0, "This function must be called before monitoring other directories, otherwise monitoring of UNC directories will be unreliable on Windows2000 Server.");
#endif
            using (new ApplicationImpersonationContext())
            {
                _lockDispose.AcquireReaderLock();
                try
                {
                    if (_disposed)
                    {
                        return;
                    }

                    _callbackRenameOrCriticaldirChange = callback;

                    var dirRoot = GetFullPath(dir);

                    // Monitor bin directory and app directory (for renames only) separately
                    // to avoid overwhelming changes when the user writes to a subdirectory
                    // of the app directory.

                    _dirMonSubdirs = new DirectoryMonitor(dirRoot, true, DirectoryMonitor.RDCW_FILTER_DIR_RENAMES,
                        true, _FCNMode);
                    try
                    {
                        _dirMonSubdirs.StartMonitoringFileWithAssert(null, OnSubdirChange, dirRoot);
                    }
                    catch
                    {
                        ((IDisposable) _dirMonSubdirs).Dispose();
                        _dirMonSubdirs = null;
                        throw;
                    }

                    _dirMonSpecialDirs = new ArrayList();
                    for (var i = 0; i < s_dirsToMonitor.Length; i++)
                    {
                        _dirMonSpecialDirs.Add(ListenToSubdirectoryChanges(dirRoot, s_dirsToMonitor[i]));
                    }
                }
                finally
                {
                    _lockDispose.ReleaseReaderLock();
                }
            }
        }

        //
        // Monitor a directory that causes an appdomain shutdown when it changes
        //
        internal void StartListeningToLocalResourcesDirectory(VirtualPath virtualDir)
        {
            Debug.Trace("FileChangesMonitor",
                "StartListeningToVirtualSubdirectory\n" + "\tArgs: virtualDir=" + virtualDir);

            if (IsFCNDisabled)
            {
                return;
            }

            // In some situation (not well understood yet), we get here with either
            // _callbackRenameOrCriticaldirChange or _dirMonSpecialDirs being null (VSWhidbey #215040).
            // When that happens, just return.
            //Debug.Assert(_callbackRenameOrCriticaldirChange != null);
            //Debug.Assert(_dirMonSpecialDirs != null);
            if (_callbackRenameOrCriticaldirChange == null || _dirMonSpecialDirs == null)
                return;

            using (new ApplicationImpersonationContext())
            {
                _lockDispose.AcquireReaderLock();
                try
                {
                    if (_disposed)
                    {
                        return;
                    }

                    // Get the physical path, and split it into the parent dir and the dir name
                    var dir = virtualDir.MapPath();
                    dir = FileUtil.RemoveTrailingDirectoryBackSlash(dir);
                    var name = Path.GetFileName(dir);
                    dir = Path.GetDirectoryName(dir);

                    // If the physical parent directory doesn't exist, don't do anything.
                    // This could happen when using a non-file system based VirtualPathProvider
                    if (!Directory.Exists(dir))
                        return;

                    _dirMonSpecialDirs.Add(ListenToSubdirectoryChanges(dir, name));
                }
                finally
                {
                    _lockDispose.ReleaseReaderLock();
                }
            }
        }

        private DirectoryMonitor ListenToSubdirectoryChanges(string dirRoot, string dirToListenTo)
        {
            string dirRootSubDir;
            DirectoryMonitor dirMonSubDir;

            if (StringUtil.StringEndsWith(dirRoot, '\\'))
            {
                dirRootSubDir = dirRoot + dirToListenTo;
            }
            else
            {
                dirRootSubDir = dirRoot + "\\" + dirToListenTo;
            }

            if (IsBeneathAppPathInternal(dirRootSubDir))
            {
                dirMonSubDir = _dirMonAppPathInternal;

                dirToListenTo = dirRootSubDir.Substring(_appPathInternal.Length + 1);
                Debug.Trace("ListenToSubDir", dirRoot + " " + dirToListenTo);
                dirMonSubDir.StartMonitoringFileWithAssert(dirToListenTo, OnCriticaldirChange, dirRootSubDir);
            }
            else if (Directory.Exists(dirRootSubDir))
            {
                dirMonSubDir = new DirectoryMonitor(dirRootSubDir, true, DirectoryMonitor.RDCW_FILTER_FILE_CHANGES,
                    _FCNMode);
                try
                {
                    dirMonSubDir.StartMonitoringFileWithAssert(null, OnCriticaldirChange, dirRootSubDir);
                }
                catch
                {
                    ((IDisposable) dirMonSubDir).Dispose();
                    dirMonSubDir = null;
                    throw;
                }
            }
            else
            {
                dirMonSubDir = (DirectoryMonitor) _subDirDirMons[dirRoot];
                if (dirMonSubDir == null)
                {
                    dirMonSubDir = new DirectoryMonitor(dirRoot, false,
                        DirectoryMonitor.RDCW_FILTER_FILE_AND_DIR_CHANGES, _FCNMode);
                    _subDirDirMons[dirRoot] = dirMonSubDir;
                }

                try
                {
                    dirMonSubDir.StartMonitoringFileWithAssert(dirToListenTo, OnCriticaldirChange, dirRootSubDir);
                }
                catch
                {
                    ((IDisposable) dirMonSubDir).Dispose();
                    dirMonSubDir = null;
                    throw;
                }
            }

            return dirMonSubDir;
        }

        private void OnSubdirChange(object sender, FileChangeEvent e)
        {
            try
            {
                Interlocked.Increment(ref _activeCallbackCount);

                if (_disposed)
                {
                    return;
                }

                Debug.Trace("FileChangesMonitor",
                    "OnSubdirChange\n" + "\tArgs: Action=" + e.Action + "; fileName=" + e.FileName);
                var handler = _callbackRenameOrCriticaldirChange;
                if (handler != null &&
                    (e.Action == FileAction.Error || e.Action == FileAction.Overwhelming ||
                     e.Action == FileAction.RenamedOldName || e.Action == FileAction.Removed))
                {
                    Debug.Trace("FileChangesMonitor",
                        "Firing subdir change event\n" + "\tArgs: Action=" + e.Action + "; fileName=" + e.FileName +
                        "; Target=" + handler.Target + "(HC=" +
                        handler.Target.GetHashCode().ToString("x", NumberFormatInfo.InvariantInfo) + ")");

                    HttpRuntime.SetShutdownMessage(
                        SR.GetString(SR.Directory_rename_notification, e.FileName));

                    handler(this, e);
                }
            }
            finally
            {
                Interlocked.Decrement(ref _activeCallbackCount);
            }
        }

        private void OnCriticaldirChange(object sender, FileChangeEvent e)
        {
            try
            {
                Interlocked.Increment(ref _activeCallbackCount);

                if (_disposed)
                {
                    return;
                }

                Debug.Trace("FileChangesMonitor",
                    "OnCriticaldirChange\n" + "\tArgs: Action=" + e.Action + "; fileName=" + e.FileName);
                HttpRuntime.SetShutdownMessage(SR.GetString(SR.Change_notification_critical_dir));
                var handler = _callbackRenameOrCriticaldirChange;
                if (handler != null)
                {
                    handler(this, e);
                }
            }
            finally
            {
                Interlocked.Decrement(ref _activeCallbackCount);
            }
        }

        //
        // Request to stop monitoring a file.
        //
        internal void StopMonitoringFile(string alias, object target)
        {
            Debug.Trace("FileChangesMonitor", "StopMonitoringFile\n" + "File=" + alias + "; Callback=" + target);

            if (IsFCNDisabled)
            {
                return;
            }

            FileMonitor fileMon;
            DirectoryMonitor dirMon = null;
            string fullPathName, file = null, dir;

            if (alias == null)
            {
                throw new HttpException(SR.GetString(SR.Invalid_file_name_for_monitoring, string.Empty));
            }

            using (new ApplicationImpersonationContext())
            {
                _lockDispose.AcquireReaderLock();
                try
                {
                    if (_disposed)
                    {
                        return;
                    }

                    fileMon = (FileMonitor) _aliases[alias];
                    if (fileMon != null && !fileMon.IsDirectory)
                    {
                        // Used the cached directory monitor and file name
                        dirMon = fileMon.DirectoryMonitor;
                        file = fileMon.FileNameLong;
                    }
                    else
                    {
                        if (alias.Length == 0 || !UrlPath.IsAbsolutePhysicalPath(alias))
                        {
                            throw new HttpException(SR.GetString(SR.Invalid_file_name_for_monitoring,
                                HttpRuntime.GetSafePath(alias)));
                        }

                        // Lookup the directory monitor
                        fullPathName = GetFullPath(alias);
                        dir = UrlPath.GetDirectoryOrRootName(fullPathName);
                        file = Path.GetFileName(fullPathName);
                        if (string.IsNullOrEmpty(file))
                        {
                            // not a file
                            throw new HttpException(SR.GetString(SR.Invalid_file_name_for_monitoring,
                                HttpRuntime.GetSafePath(alias)));
                        }

                        dirMon = FindDirectoryMonitor(dir, false, false);
                    }

                    if (dirMon != null)
                    {
                        dirMon.StopMonitoringFile(file, target);
                    }
                }
                finally
                {
                    _lockDispose.ReleaseReaderLock();
                }
            }
        }

        //
        // Request to stop monitoring a file.
        // 
        internal void StopMonitoringPath(string alias, object target)
        {
            Debug.Trace("FileChangesMonitor", "StopMonitoringFile\n" + "File=" + alias + "; Callback=" + target);

            if (IsFCNDisabled)
            {
                return;
            }

            FileMonitor fileMon;
            DirectoryMonitor dirMon = null;
            string fullPathName, file = null, dir;

            if (alias == null)
            {
                throw new HttpException(SR.GetString(SR.Invalid_file_name_for_monitoring, string.Empty));
            }

            using (new ApplicationImpersonationContext())
            {
                _lockDispose.AcquireReaderLock();
                try
                {
                    if (_disposed)
                    {
                        return;
                    }

                    fileMon = (FileMonitor) _aliases[alias];
                    if (fileMon != null)
                    {
                        // Used the cached directory monitor and file name.
                        dirMon = fileMon.DirectoryMonitor;
                        file = fileMon.FileNameLong;
                    }
                    else
                    {
                        if (alias.Length == 0 || !UrlPath.IsAbsolutePhysicalPath(alias))
                        {
                            throw new HttpException(SR.GetString(SR.Invalid_file_name_for_monitoring,
                                HttpRuntime.GetSafePath(alias)));
                        }

                        // try treating the path as a directory
                        fullPathName = GetFullPath(alias);
                        dirMon = FindDirectoryMonitor(fullPathName, false, false);
                        if (dirMon == null)
                        {
                            // try treaing the path as a file
                            dir = UrlPath.GetDirectoryOrRootName(fullPathName);
                            file = Path.GetFileName(fullPathName);
                            if (!string.IsNullOrEmpty(file))
                            {
                                dirMon = FindDirectoryMonitor(dir, false, false);
                            }
                        }
                    }

                    if (dirMon != null)
                    {
                        dirMon.StopMonitoringFile(file, target);
                    }
                }
                finally
                {
                    _lockDispose.ReleaseReaderLock();
                }
            }
        }

        //
        // Returns the last modified time of the file. If the 
        // file does not exist, returns DateTime.MinValue.
        //
        internal FileAttributesData GetFileAttributes(string alias)
        {
            FileMonitor fileMon;
            DirectoryMonitor dirMon = null;
            string fullPathName, file = null, dir;
            FileAttributesData fad = null;

            if (alias == null)
            {
                throw CreateFileMonitoringException(Util.HResults.E_INVALIDARG, alias);
            }

            if (IsFCNDisabled)
            {
                if (alias.Length == 0 || !UrlPath.IsAbsolutePhysicalPath(alias))
                {
                    throw CreateFileMonitoringException(Util.HResults.E_INVALIDARG, alias);
                }

                fullPathName = GetFullPath(alias);
                FindFileData ffd = null;
                var hr = FindFileData.FindFile(fullPathName, out ffd);
                if (hr == Util.HResults.S_OK)
                {
                    return ffd.FileAttributesData;
                }
                return null;
            }

            using (new ApplicationImpersonationContext())
            {
                _lockDispose.AcquireReaderLock();
                try
                {
                    if (!_disposed)
                    {
                        fileMon = (FileMonitor) _aliases[alias];
                        if (fileMon != null && !fileMon.IsDirectory)
                        {
                            // Used the cached directory monitor and file name.
                            dirMon = fileMon.DirectoryMonitor;
                            file = fileMon.FileNameLong;
                        }
                        else
                        {
                            if (alias.Length == 0 || !UrlPath.IsAbsolutePhysicalPath(alias))
                            {
                                throw CreateFileMonitoringException(Util.HResults.E_INVALIDARG, alias);
                            }

                            // Lookup the directory monitor
                            fullPathName = GetFullPath(alias);
                            dir = UrlPath.GetDirectoryOrRootName(fullPathName);
                            file = Path.GetFileName(fullPathName);
                            if (!string.IsNullOrEmpty(file))
                            {
                                dirMon = FindDirectoryMonitor(dir, false, false);
                            }
                        }
                    }
                }
                finally
                {
                    _lockDispose.ReleaseReaderLock();
                }

                // If we're not monitoring the file, get the attributes.
                if (dirMon == null || !dirMon.GetFileAttributes(file, out fad))
                {
                    FileAttributesData.GetFileAttributes(alias, out fad);
                }

                return fad;
            }
        }

        //
        // Request to stop monitoring everything -- release all native resources
        //
        internal void Stop()
        {
            Debug.Trace("FileChangesMonitor", "Stop!");

            if (IsFCNDisabled)
            {
                return;
            }

            using (new ApplicationImpersonationContext())
            {
                _lockDispose.AcquireWriterLock();
                try
                {
                    _disposed = true;
                }
                finally
                {
                    _lockDispose.ReleaseWriterLock();
                }

                // wait for executing callbacks to complete
                while (_activeCallbackCount != 0)
                {
                    Thread.Sleep(250);
                }

                if (_dirMonSubdirs != null)
                {
                    _dirMonSubdirs.StopMonitoring();
                    _dirMonSubdirs = null;
                }

                if (_dirMonSpecialDirs != null)
                {
                    foreach (DirectoryMonitor dirMon in _dirMonSpecialDirs)
                    {
                        if (dirMon != null)
                        {
                            dirMon.StopMonitoring();
                        }
                    }

                    _dirMonSpecialDirs = null;
                }

                _callbackRenameOrCriticaldirChange = null;

                if (_dirs != null)
                {
                    var e = _dirs.GetEnumerator();
                    while (e.MoveNext())
                    {
                        var dirMon = (DirectoryMonitor) e.Value;
                        dirMon.StopMonitoring();
                    }
                }

                _dirs.Clear();
                _aliases.Clear();

                // Don't allow the AppDomain to unload while we have
                // active DirMonCompletions
/*
                while (DirMonCompletion.ActiveDirMonCompletions != 0)
                {
                    Thread.Sleep(10);
                }
*/
            }

            Debug.Dump("FileChangesMonitor", this);
        }

#if DEBUG
        internal string DebugDescription(string indent) {
            StringBuilder   sb = new StringBuilder(200);
            string          i2 = indent + "    ";
            DictionaryEntryCaseInsensitiveComparer  decomparer = new DictionaryEntryCaseInsensitiveComparer();

            sb.Append(indent + "System.Web.FileChangesMonitor\n");
            if (_dirMonSubdirs != null) {
                sb.Append(indent + "_dirMonSubdirs\n");
                sb.Append(_dirMonSubdirs.DebugDescription(i2));
            }

            if (_dirMonSpecialDirs != null) {
                for (int i=0; i<s_dirsToMonitor.Length; i++) {
                    if (_dirMonSpecialDirs[i] != null) {
                        sb.Append(indent + "_dirMon" + s_dirsToMonitor[i] + "\n");
                        sb.Append(((DirectoryMonitor)_dirMonSpecialDirs[i]).DebugDescription(i2));
                    }
                }
            }

            sb.Append(indent + "_dirs " + _dirs.Count + " directory monitors...\n");

            DictionaryEntry[] dirEntries = new DictionaryEntry[_dirs.Count];
            _dirs.CopyTo(dirEntries, 0);
            Array.Sort(dirEntries, decomparer);
            
            foreach (DictionaryEntry d in dirEntries) {
                DirectoryMonitor dirMon = (DirectoryMonitor)d.Value;
                sb.Append(dirMon.DebugDescription(i2));
            }

            return sb.ToString();
        }
#endif

#else // !FEATURE_PAL stubbing

        internal static string[] s_dirsToMonitor = new string[] {
        };

        internal DateTime StartMonitoringFile(string alias, FileChangeEventHandler callback)
        {
            return DateTime.Now;
        }
        
        internal DateTime StartMonitoringPath(string alias, FileChangeEventHandler callback)
        {
            return DateTime.Now;
        }

        internal void StopMonitoringPath(String alias, object target) 
        {
        }

        internal void StartMonitoringDirectoryRenamesAndBinDirectory(string dir, FileChangeEventHandler callback) 
        {
        }
        
        internal void Stop() 
        {
        }                

#endif // !FEATURE_PAL
    }

#if DEBUG
    internal sealed class DictionaryEntryCaseInsensitiveComparer : IComparer {
        IComparer _cicomparer = StringComparer.OrdinalIgnoreCase;

        internal DictionaryEntryCaseInsensitiveComparer() {}
        
        int IComparer.Compare(object x, object y) {
            string a = (string) ((DictionaryEntry) x).Key;
            string b = (string) ((DictionaryEntry) y).Key;

            if (a != null && b != null) {
                return _cicomparer.Compare(a, b);
            }
            else {
                return InvariantComparer.Default.Compare(a, b);            
            }
        }
    }
#endif

#if DEBUG
    internal sealed class DictionaryEntryTypeComparer : IComparer {
        IComparer _cicomparer = StringComparer.OrdinalIgnoreCase;

        internal DictionaryEntryTypeComparer() {}

        int IComparer.Compare(object x, object y) {
            object a = ((DictionaryEntry) x).Key;
            object b = ((DictionaryEntry) y).Key;

            string i = null, j = null;
            if (a != null) {
                i = a.GetType().ToString();
            }

            if (b != null) {
                j = b.GetType().ToString();
            }

            if (i != null && j != null) {
                return _cicomparer.Compare(i, j);
            }
            else {
                return InvariantComparer.Default.Compare(i, j);            
            }
        }
    }
#endif
}