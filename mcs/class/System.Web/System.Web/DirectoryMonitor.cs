using System.Collections;
using System.Globalization;
using System.IO;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Web.Configuration;
using System.Web.Util;

namespace System.Web
{


    internal sealed class DirectoryMonitor : IDisposable
    {
        internal const NotifyFilters RDCW_FILTER_FILE_AND_DIR_CHANGES =
            NotifyFilters.FileName |
            NotifyFilters.DirectoryName |
            NotifyFilters.CreationTime |
            NotifyFilters.Size |
            NotifyFilters.LastWrite |
            NotifyFilters.Security;

        internal const NotifyFilters RDCW_FILTER_DIR_RENAMES =
            NotifyFilters.DirectoryName;
        
        internal const NotifyFilters RDCW_FILTER_FILE_CHANGES =
            NotifyFilters.FileName |
            NotifyFilters.CreationTime |
            NotifyFilters.Size |
            NotifyFilters.LastWrite |
            NotifyFilters.Security;


        private static readonly Queue s_notificationQueue = new Queue();
        private static readonly WorkItemCallback s_notificationCallback = FireNotifications;
        private static int s_inNotificationThread;
        private static int s_notificationBufferSizeIncreased;

        internal readonly string Directory; // directory being monitored
        private readonly Hashtable _fileMons; // fileName -> FileMonitor
        private int _cShortNames; // number of file monitors that are added with their short name
        private FileMonitor _anyFileMon; // special file monitor to watch for any changes in directory
        private readonly bool _watchSubtree; // watch subtree?
        private readonly NotifyFilters _notifyFilter; // the notify filter for the call to ReadDirectoryChangesW

        private readonly bool _ignoreSubdirChange;
        // when a subdirectory is deleted or renamed, ignore the notification if we're not monitoring it

        private readonly bool _isDirMonAppPathInternal;
        private FileSystemWatcher _watcher;
        // special dirmon that monitors all files and subdirectories beneath the vroot (enabled via FCNMode registry key)

        // FcnMode to pass to native code
        internal int FcnMode { get; set; }

        // constructor for special dirmon that monitors all files and subdirectories beneath the vroot (enabled via FCNMode registry key)
        internal DirectoryMonitor(string appPathInternal, int fcnMode)
            : this(appPathInternal, true, RDCW_FILTER_FILE_AND_DIR_CHANGES, fcnMode)
        {
            _isDirMonAppPathInternal = true;
        }

        internal DirectoryMonitor(string dir, bool watchSubtree, NotifyFilters notifyFilter, int fcnMode)
            : this(dir, watchSubtree, notifyFilter, false, fcnMode)
        {
        }

        internal DirectoryMonitor(string dir, bool watchSubtree, NotifyFilters notifyFilter, bool ignoreSubdirChange, int fcnMode)
        {
            Directory = dir;
            _fileMons = new Hashtable(StringComparer.OrdinalIgnoreCase);
            _watchSubtree = watchSubtree;
            _notifyFilter = notifyFilter;
            _ignoreSubdirChange = ignoreSubdirChange;
            FcnMode = fcnMode;
        }

        void IDisposable.Dispose()
        {
            if (_watcher != null)
            {
                ((IDisposable) _watcher).Dispose();
                _watcher = null;
            }

            //
            // Remove aliases to this object in FileChangesMonitor so that
            // it is not rooted.
            //
            if (_anyFileMon != null)
            {
                HttpRuntime.FileChangesMonitor.RemoveAliases(_anyFileMon);
                _anyFileMon = null;
            }

            foreach (DictionaryEntry e in _fileMons)
            {
                var key = (string) e.Key;
                var fileMon = (FileMonitor) e.Value;
                if (fileMon.FileNameLong == key)
                {
                    HttpRuntime.FileChangesMonitor.RemoveAliases(fileMon);
                }
            }

            _fileMons.Clear();
            _cShortNames = 0;
        }

        internal bool IsMonitoring()
        {
            return GetFileMonitorsCount() > 0;
        }

        private void StartMonitoring()
        {
            if (_watcher == null)
            {
                _watcher = new FileSystemWatcher();
                _watcher.Path = Directory;
                _watcher.NotifyFilter = NotifyFilters.LastWrite;
                _watcher.Filter = "*.*";
                _watcher.Changed += new FileSystemEventHandler(OnFileChanged);
                _watcher.EnableRaisingEvents = true;
                _watcher.IncludeSubdirectories = !_ignoreSubdirChange;
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            var action = FileAction.Overwhelming;
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Changed:
                    action = FileAction.Modified;
                    break;
                case WatcherChangeTypes.Created:
                    action = FileAction.Added;
                    break;
                case WatcherChangeTypes.Deleted:
                    action = FileAction.Removed;
                    break;
                case WatcherChangeTypes.Renamed:
                    action = FileAction.RenamedNewName;
                    break;
                case WatcherChangeTypes.All:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            OnFileChange(action, e.FullPath, DateTime.UtcNow);
        }

        internal void StopMonitoring()
        {
            lock (this)
            {
                ((IDisposable) this).Dispose();
            }
        }

        private FileMonitor FindFileMonitor(string file)
        {
            FileMonitor fileMon;

            if (file == null)
            {
                fileMon = _anyFileMon;
            }
            else
            {
                fileMon = (FileMonitor) _fileMons[file];
            }

            return fileMon;
        }

        private FileMonitor AddFileMonitor(string file)
        {
            string path;
            FileMonitor fileMon;
            FindFileData ffd = null;
            int hr;

            if (string.IsNullOrEmpty(file))
            {
                // add as the <ANY> file monitor
                fileMon = new FileMonitor(this, null, null, true, null, null);
                _anyFileMon = fileMon;
            }
            else
            {
                // Get the long and short name of the file
                path = Path.Combine(Directory, file);
                if (_isDirMonAppPathInternal)
                {
                    hr = FindFileData.FindFile(path, Directory, out ffd);
                }
                else
                {
                    hr = FindFileData.FindFile(path, out ffd);
                }
                if (hr == Util.HResults.S_OK)
                {
                    // Unless this is FileChangesMonitor._dirMonAppPathInternal,
                    // don't monitor changes to a directory - this will not pickup changes to files in the directory.
                    if (!_isDirMonAppPathInternal && (ffd.FileAttributesData.FileAttributes & FileAttributes.Directory) != 0)
                    {
                        throw FileChangesMonitor.CreateFileMonitoringException(Util.HResults.E_INVALIDARG, path);
                    }

                    var dacl = FileSecurity.GetDacl(path);
                    fileMon = new FileMonitor(this, ffd.FileNameLong, ffd.FileNameShort, true, ffd.FileAttributesData, dacl);
                    _fileMons.Add(ffd.FileNameLong, fileMon);

                    // Update short name aliases to this file
                    UpdateFileNameShort(fileMon, null, ffd.FileNameShort);
                }
                else if (hr == Util.HResults.E_PATHNOTFOUND || hr == Util.HResults.E_FILENOTFOUND)
                {
                    // Don't allow possible short file names to be added as non-existant,
                    // because it is impossible to track them if they are indeed a short name since
                    // short file names may change.

                    // FEATURE_PAL 


                    if (file.IndexOf('~') != -1)
                    {
                        throw FileChangesMonitor.CreateFileMonitoringException(Util.HResults.E_INVALIDARG, path);
                    }

                    // Add as non-existent file
                    fileMon = new FileMonitor(this, file, null, false, null, null);
                    _fileMons.Add(file, fileMon);
                }
                else
                {
                    throw FileChangesMonitor.CreateFileMonitoringException(hr, path);
                }
            }

            return fileMon;
        }

        //
        // Update short names of a file
        //
        private void UpdateFileNameShort(FileMonitor fileMon, string oldFileNameShort, string newFileNameShort)
        {
            if (oldFileNameShort != null)
            {
                var oldFileMonShort = (FileMonitor) _fileMons[oldFileNameShort];
                if (oldFileMonShort != null)
                {
                    // The old filemonitor no longer has this short file name.
                    // Update the monitor and _fileMons
                    if (oldFileMonShort != fileMon)
                    {
                        oldFileMonShort.RemoveFileNameShort();
                    }


                    _fileMons.Remove(oldFileNameShort);
                    _cShortNames--;
                }
            }

            if (newFileNameShort != null)
            {
                // Add the new short file name.
                _fileMons.Add(newFileNameShort, fileMon);
                _cShortNames++;
            }
        }

        private void RemoveFileMonitor(FileMonitor fileMon)
        {
            if (fileMon == _anyFileMon)
            {
                _anyFileMon = null;
            }
            else
            {
                _fileMons.Remove(fileMon.FileNameLong);
                if (fileMon.FileNameShort != null)
                {
                    _fileMons.Remove(fileMon.FileNameShort);
                    _cShortNames--;
                }
            }

            HttpRuntime.FileChangesMonitor.RemoveAliases(fileMon);
        }

        private int GetFileMonitorsCount()
        {
            var c = _fileMons.Count - _cShortNames;
            if (_anyFileMon != null)
            {
                c++;
            }

            return c;
        }

        // The 4.0 CAS changes made the AppDomain homogenous, so we need to assert
        // FileIOPermission.  Currently this is only exposed publicly via CacheDependency, which
        // already does a PathDiscover check for public callers.
        [FileIOPermission(SecurityAction.Assert, Unrestricted = true)]
        internal FileMonitor StartMonitoringFileWithAssert(string file, FileChangeEventHandler callback, string alias)
        {
            FileMonitor fileMon = null;
            var firstFileMonAdded = false;

            lock (this)
            {
                // Find existing file monitor
                fileMon = FindFileMonitor(file);
                if (fileMon == null)
                {
                    // Add a new monitor
                    fileMon = AddFileMonitor(file);
                    if (GetFileMonitorsCount() == 1)
                    {
                        firstFileMonAdded = true;
                    }
                }

                // Add callback to the file monitor
                fileMon.AddTarget(callback, alias, true);

                // Start directory monitoring when the first file gets added
                if (firstFileMonAdded)
                {
                    StartMonitoring();
                }
            }

            return fileMon;
        }

        //
        // Request to stop monitoring a file.
        //
        internal void StopMonitoringFile(string file, object target)
        {
            FileMonitor fileMon;
            int numTargets;

            lock (this)
            {
                // Find existing file monitor
                fileMon = FindFileMonitor(file);
                if (fileMon != null)
                {
                    numTargets = fileMon.RemoveTarget(target);
                    if (numTargets == 0)
                    {
                        RemoveFileMonitor(fileMon);

                        // last target for the file monitor gone 
                        // -- remove the file monitor
                        if (GetFileMonitorsCount() == 0)
                        {
                            ((IDisposable) this).Dispose();
                        }
                    }
                }
            }

#if DEBUG
            if (fileMon != null)
            {
                Debug.Dump("FileChangesMonitor", HttpRuntime.FileChangesMonitor);
            }
#endif
        }


        internal bool GetFileAttributes(string file, out FileAttributesData fad)
        {
            FileMonitor fileMon = null;
            fad = null;

            lock (this)
            {
                // Find existing file monitor
                fileMon = FindFileMonitor(file);
                if (fileMon != null)
                {
                    // Get the attributes
                    fad = fileMon.Attributes;
                    return true;
                }
            }

            return false;
        }

        //
        // Notes about file attributes:
        // 
        // CreationTime is the time a file entry is added to a directory. 
        //     If file q1 is copied to q2, q2's creation time is updated if it is new to the directory,
        //         else q2's old time is used.
        // 
        //     If a file is deleted, then added, its creation time is preserved from before the delete.
        //     
        // LastWriteTime is the time a file was last written.    
        //     If file q1 is copied to q2, q2's lastWrite time is the same as q1.
        //     Note that this implies that the LastWriteTime can be older than the LastCreationTime,
        //     and that a copy of a file can result in the LastWriteTime being earlier than
        //     its previous value.
        // 
        // LastAccessTime is the time a file was last accessed, such as opened or written to.
        //     Note that if the attributes of a file are changed, its LastAccessTime is not necessarily updated.
        //     
        // If the FileSize, CreationTime, or LastWriteTime have changed, then we know that the 
        //     file has changed in a significant way, and that the LastAccessTime will be greater than
        //     or equal to that time.
        //     
        // If the FileSize, CreationTime, or LastWriteTime have not changed, then the file's
        //     attributes may have changed without changing the LastAccessTime.
        //

        // Confirm that the changes occurred after we started monitoring,
        // to handle the case where:
        //
        //     1. User creates a file.
        //     2. User starts to monitor the file.
        //     3. Change notification is made of the original creation of the file.
        // 
        // Note that we can only approximate when the last change occurred by
        // examining the LastAccessTime. The LastAccessTime will change if the 
        // contents of a file (but not necessarily its attributes) change.
        // The drawback to using the LastAccessTime is that it will also be
        // updated when a file is read.
        //
        // Note that we cannot make this confirmation when only the file's attributes
        // or ACLs change, because changes to attributes and ACLs won't change the LastAccessTime.
        // 
        private bool IsChangeAfterStartMonitoring(FileAttributesData fad, FileMonitorTarget target, DateTime utcCompletion)
        {
            // If the LastAccessTime is more than 60 seconds before we
            // started monitoring, then the change likely did not update
            // the LastAccessTime correctly.
            if (fad.UtcLastAccessTime.AddSeconds(60) < target.UtcStartMonitoring)
            {
#if DEBUG
                Debug.Trace("FileChangesMonitorIsChangeAfterStart", "LastAccessTime is more than 60 seconds before monitoring started.");
#endif
                return true;
            }

            // Check if the notification of the change came after
            // we started monitoring.
            if (utcCompletion > target.UtcStartMonitoring)
            {
#if DEBUG
                Debug.Trace("FileChangesMonitorIsChangeAfterStart", "Notification came after we started monitoring.");
#endif
                return true;
            }

            // Make sure that the LastAccessTime is valid.
            // It must be more recent than the LastWriteTime.
            if (fad.UtcLastAccessTime < fad.UtcLastWriteTime)
            {
#if DEBUG
                Debug.Trace("FileChangesMonitorIsChangeAfterStart", "UtcLastWriteTime is greater then UtcLastAccessTime.");
#endif
                return true;
            }

            // If the LastAccessTime occurs exactly at midnight,
            // then the system is FAT32 and LastAccessTime is unusable.
            if (fad.UtcLastAccessTime.TimeOfDay == TimeSpan.Zero)
            {
#if DEBUG
                Debug.Trace("FileChangesMonitorIsChangeAfterStart", "UtcLastAccessTime is midnight -- FAT32 likely.");
#endif
                return true;
            }

            // Finally, compare LastAccessTime to the time we started monitoring.
            // If the time of the last access was before we started monitoring, then
            // we know a change did not occur to the file contents.
            if (fad.UtcLastAccessTime >= target.UtcStartMonitoring)
            {
#if DEBUG
                Debug.Trace("FileChangesMonitorIsChangeAfterStart", "UtcLastAccessTime is greater than UtcStartMonitoring.");
#endif
                return true;
            }

#if DEBUG
            Debug.Trace("FileChangesMonitorIsChangeAfterStart", "Change is before start of monitoring.  Data:\n FileAttributesData: \nUtcCreationTime: " + fad.UtcCreationTime + " UtcLastAccessTime: " + fad.UtcLastAccessTime + " UtcLastWriteTime: " + fad.UtcLastWriteTime + "\n FileMonitorTarget:\n UtcStartMonitoring: " + target.UtcStartMonitoring + "\nUtcCompletion: " + utcCompletion);
#endif
            return false;
        }

        // If this is a special dirmon that monitors all files and subdirectories 
        // beneath the vroot (enabled via FCNMode registry key), then
        // we need to special case how we lookup the FileMonitor.  For example, nobody has called
        // StartMonitorFile for specific files in the App_LocalResources directory,
        // so we need to see if fileName is in App_LocalResources and then get the FileMonitor for
        // the directory.
        private bool GetFileMonitorForSpecialDirectory(string fileName, ref FileMonitor fileMon)
        {
            // fileName should not be in short form (8.3 format)...it was converted to long form in
            // DirMonCompletion::ProcessOneFileNotification

            // first search for match within s_dirsToMonitor
            for (var i = 0; i < FileChangesMonitor.s_dirsToMonitor.Length; i++)
            {
                if (StringUtil.StringStartsWithIgnoreCase(fileName, FileChangesMonitor.s_dirsToMonitor[i]))
                {
                    fileMon = (FileMonitor) _fileMons[FileChangesMonitor.s_dirsToMonitor[i]];
                    return fileMon != null;
                }
            }

            // if we did not find a match in s_dirsToMonitor, look for LocalResourcesDirectoryName anywhere within fileName
            var indexStart = fileName.IndexOf(HttpRuntime.LocalResourcesDirectoryName, StringComparison.OrdinalIgnoreCase);
            if (indexStart > -1)
            {
                var dirNameLength = indexStart + HttpRuntime.LocalResourcesDirectoryName.Length;

                // fileName should either end with LocalResourcesDirectoryName or include a trailing slash and more characters
                if (fileName.Length == dirNameLength || fileName[dirNameLength] == Path.DirectorySeparatorChar)
                {
                    var dirName = fileName.Substring(0, dirNameLength);
                    fileMon = (FileMonitor) _fileMons[dirName];
                    return fileMon != null;
                }
            }

            return false;
        }


        //
        // Delegate callback from native code.
        //
        internal void OnFileChange(FileAction action, string fileName, DateTime utcCompletion)
        {
            //
            // Use try/catch to prevent runtime exceptions from propagating 
            // into native code.
            //
            try
            {
                FileMonitor fileMon = null;
                ArrayList targets = null;
                int i, n;
                FileMonitorTarget target;
                ICollection col;
                string key;
                FileAttributesData fadOld = null;
                FileAttributesData fadNew = null;
                FileSystemSecurity daclOld = null;
                FileSystemSecurity daclNew = null;
                FileAction lastAction = FileAction.Error;
                var utcLastCompletion = DateTime.MinValue;
                var isSpecialDirectoryChange = false;

#if DEBUG
                string reasonIgnore = string.Empty;
                string reasonFire = string.Empty;
#endif

                // We've already stopped monitoring, but a change completion was
                // posted afterwards. Ignore it.
                if (_watcher == null)
                {
                    return;
                }

                lock (this)
                {
                    if (_fileMons.Count > 0)
                    {
                        if (action == FileAction.Overwhelming)
                        {
                            // Overwhelming change -- notify all file monitors
                            Debug.Assert(fileName == null, "fileName == null");
                            Debug.Assert(action != FileAction.Overwhelming, "action != FileAction.Overwhelming");

                            // Get targets for all files
                            targets = new ArrayList();
                            foreach (DictionaryEntry d in _fileMons)
                            {
                                key = (string) d.Key;
                                fileMon = (FileMonitor) d.Value;
                                if (fileMon.FileNameLong == key)
                                {
                                    fileMon.ResetCachedAttributes();
                                    fileMon.LastAction = action;
                                    fileMon.UtcLastCompletion = utcCompletion;
                                    col = fileMon.Targets;
                                    targets.AddRange(col);
                                }
                            }

                            fileMon = null;
                        }
                        else
                        {
                            Debug.Assert((int) action >= 1 && fileName != null && fileName.Length > 0, "(int) action >= 1 && fileName != null && fileName.Length > 0");

                            // Find the file monitor
                            fileMon = (FileMonitor) _fileMons[fileName];

                            if (_isDirMonAppPathInternal && fileMon == null)
                            {
                                isSpecialDirectoryChange = GetFileMonitorForSpecialDirectory(fileName, ref fileMon);
                            }

                            if (fileMon != null)
                            {
                                // Get the targets
                                col = fileMon.Targets;
                                targets = new ArrayList(col);

                                fadOld = fileMon.Attributes;
                                daclOld = fileMon.Dacl;
                                lastAction = fileMon.LastAction;
                                utcLastCompletion = fileMon.UtcLastCompletion;
                                fileMon.LastAction = action;
                                fileMon.UtcLastCompletion = utcCompletion;

                                if (action == FileAction.Removed || action == FileAction.RenamedOldName)
                                {
                                    // File not longer exists.
                                    fileMon.MakeExtinct();
                                }
                                else if (fileMon.Exists)
                                {
                                    // We only need to update the attributes if this is 
                                    // a different completion, as we retreive the attributes
                                    // after the completion is received.
                                    if (utcLastCompletion != utcCompletion)
                                    {
                                        fileMon.UpdateCachedAttributes();
                                    }
                                }
                                else
                                {
                                    // File now exists - update short name and attributes.
                                    FindFileData ffd = null;
                                    var path = Path.Combine(Directory, fileMon.FileNameLong);
                                    int hr;
                                    if (_isDirMonAppPathInternal)
                                    {
                                        hr = FindFileData.FindFile(path, Directory, out ffd);
                                    }
                                    else
                                    {
                                        hr = FindFileData.FindFile(path, out ffd);
                                    }
                                    if (hr == Util.HResults.S_OK)
                                    {
                                        Debug.Assert(StringUtil.EqualsIgnoreCase(fileMon.FileNameLong, ffd.FileNameLong), "StringUtil.EqualsIgnoreCase(fileMon.FileNameLong, ffd.FileNameLong)");

                                        var oldFileNameShort = fileMon.FileNameShort;
                                        var dacl = FileSecurity.GetDacl(path);
                                        fileMon.MakeExist(ffd, dacl);
                                        UpdateFileNameShort(fileMon, oldFileNameShort, ffd.FileNameShort);
                                    }
                                }

                                fadNew = fileMon.Attributes;
                                daclNew = fileMon.Dacl;
                            }
                        }
                    }

                    // Notify the delegate waiting for any changes
                    if (_anyFileMon != null)
                    {
                        col = _anyFileMon.Targets;
                        if (targets != null)
                        {
                            targets.AddRange(col);
                        }
                        else
                        {
                            targets = new ArrayList(col);
                        }
                    }

                    if (action == FileAction.Error)
                    {
                        // Stop monitoring.
                        ((IDisposable) this).Dispose();
                    }
                }

                // Ignore Modified action for directories (VSWhidbey 295597)
                var ignoreThisChangeNotification = false;

                if (!isSpecialDirectoryChange && fileName != null && action == FileAction.Modified)
                {
                    // check if the file is a directory (reuse attributes if already obtained)
                    var fad = fadNew;

                    if (fad == null)
                    {
                        var path = Path.Combine(Directory, fileName);
                        FileAttributesData.GetFileAttributes(path, out fad);
                    }

                    if (fad != null && ((fad.FileAttributes & FileAttributes.Directory) != 0))
                    {
                        // ignore if directory
                        ignoreThisChangeNotification = true;
                    }
                }

                // Dev10 440497: Don't unload AppDomain when a folder is deleted or renamed, unless we're monitoring files in it
                if (_ignoreSubdirChange && (action == FileAction.Removed || action == FileAction.RenamedOldName) && fileName != null)
                {
                    var fullPath = Path.Combine(Directory, fileName);
                    if (!HttpRuntime.FileChangesMonitor.IsDirNameMonitored(fullPath, fileName))
                    {
#if DEBUG
                        Debug.Trace("FileChangesMonitorIgnoreSubdirChange", "*** Ignoring SubDirChange " + DateTime.Now.ToString("hh:mm:ss.fff", CultureInfo.InvariantCulture) + ": fullPath=" + fullPath + ", action=" + action.ToString());
#endif
                        ignoreThisChangeNotification = true;
                    }
#if DEBUG
                    else
                    {
                        Debug.Trace("FileChangesMonitorIgnoreSubdirChange", "*** SubDirChange " + DateTime.Now.ToString("hh:mm:ss.fff", CultureInfo.InvariantCulture) + ": fullPath=" + fullPath + ", action=" + action.ToString());
                    }
#endif
                }

                // Fire the event
                if (targets != null && !ignoreThisChangeNotification)
                {
                    Debug.Dump("FileChangesMonitor", HttpRuntime.FileChangesMonitor);

                    lock (s_notificationQueue.SyncRoot)
                    {
                        for (i = 0, n = targets.Count; i < n; i++)
                        {
                            //
                            // Determine whether the change is significant, and if so, add it 
                            // to the notification queue.
                            //
                            // - A change is significant if action is other than Added or Modified
                            // - A change is significant if the action is Added and it occurred after
                            //   the target started monitoring.
                            // - If the action is Modified:
                            // -- A change is significant if the file contents were modified
                            //    and it occurred after the target started monitoring.
                            // -- A change is significant if the DACL changed. We cannot check if
                            //    the change was made after the target started monitoring in this case,
                            //    as the LastAccess time may not be updated.
                            //
                            target = (FileMonitorTarget) targets[i];
                            bool isSignificantChange;
                            if ((action != FileAction.Added && action != FileAction.Modified) || fadNew == null)
                            {
                                // Any change other than Added or Modified is significant.
                                // If we have no attributes to examine, the change is significant.
                                isSignificantChange = true;

#if DEBUG
                                reasonFire = "(action != FileAction.Added && action != FileAction.Modified) || fadNew == null";
#endif
                            }
                            else if (action == FileAction.Added)
                            {
                                // Added actions are significant if they occur after we started monitoring.
                                isSignificantChange = IsChangeAfterStartMonitoring(fadNew, target, utcCompletion);

#if DEBUG
                                reasonIgnore = "change occurred before started monitoring";
                                reasonFire = "file added after start of monitoring";
#endif
                            }
                            else
                            {
                                Debug.Assert(action == FileAction.Modified, "action == FileAction.Modified");
                                if (utcCompletion == utcLastCompletion)
                                {
                                    // File attributes and ACLs will not have changed if the completion is the same
                                    // as the last, since we get the attributes after all changes in the completion
                                    // have occurred. Therefore if the previous change was Modified, there
                                    // is no change that we can detect.
                                    //
                                    // Notepad fires such spurious notifications when a file is saved.
                                    // 
                                    isSignificantChange = (lastAction != FileAction.Modified);

#if DEBUG
                                    reasonIgnore = "spurious FileAction.Modified";
                                    reasonFire = "spurious completion where action != modified";
#endif
                                }
                                else if (fadOld == null)
                                {
                                    // There were no attributes before this notification, 
                                    // so assume the change is significant. We cannot check for
                                    // whether the change was after the start of monitoring,
                                    // because we don't know if the content changed, or just
                                    // DACL, in which case the LastAccessTime will not be updated.
                                    isSignificantChange = true;

#if DEBUG
                                    reasonFire = "no attributes before this notification";
#endif
                                }
                                else if (daclOld == null || daclOld != daclNew)
                                {
                                    // The change is significant if the DACL changed. 
                                    // We cannot check if the change is after the start of monitoring,
                                    // as a change in the DACL does not necessarily update the
                                    // LastAccessTime of a file.
                                    // If we cannot access the DACL, then we must assume
                                    // that it is what has changed.
                                    isSignificantChange = true;

#if DEBUG
                                    if (daclOld == null)
                                    {
                                        reasonFire = "unable to access ACL";
                                    }
                                    else
                                    {
                                        reasonFire = "ACL changed";
                                    }
#endif
                                }
                                else
                                {
                                    // The file content was modified. We cannot guarantee that the
                                    // LastWriteTime or FileSize changed when the file changed, as 
                                    // copying a file preserves the LastWriteTime, and the "touch"
                                    // command can reset the LastWriteTime of many files to the same
                                    // time.
                                    //
                                    // If the file content is modified, we can determine if the file
                                    // was not changed after the start of monitoring by looking at 
                                    // the LastAccess time.
                                    isSignificantChange = IsChangeAfterStartMonitoring(fadNew, target, utcCompletion);

#if DEBUG
                                    reasonIgnore = "change occurred before started monitoring";
                                    reasonFire = "file content modified after start of monitoring";
#endif
                                }
                            }

                            if (isSignificantChange)
                            {
#if DEBUG
                                Debug.Trace("FileChangesMonitorCallback", "Firing change event, reason=" + reasonFire + "\n\tArgs: Action=" + action + ";     Completion=" + Debug.FormatUtcDate(utcCompletion) + "; fileName=" + fileName + "\n\t  LastAction=" + lastAction + "; LastCompletion=" + Debug.FormatUtcDate(utcLastCompletion) + "\nfadOld=" + ((fadOld != null) ? fadOld.DebugDescription("\t") : "<null>") + "\nfileMon=" + ((fileMon != null) ? fileMon.DebugDescription("\t") : "<null>") + "\n" + target.DebugDescription("\t"));
#endif

                                s_notificationQueue.Enqueue(new NotificationQueueItem(target.Callback, action, target.Alias));
                            }
#if DEBUG
                            else
                            {
                                Debug.Trace("FileChangesMonitorCallback", "Ignoring change event, reason=" + reasonIgnore + "\n\tArgs: Action=" + action + ";     Completion=" + Debug.FormatUtcDate(utcCompletion) + "; fileName=" + fileName + "\n\t  LastAction=" + lastAction + "; LastCompletion=" + Debug.FormatUtcDate(utcLastCompletion) + "\nfadOld=" + ((fadOld != null) ? fadOld.DebugDescription("\t") : "<null>") + "\nfileMon=" + ((fileMon != null) ? fileMon.DebugDescription("\t") : "<null>") + "\n" + target.DebugDescription("\t"));
                            }
#endif
                        }
                    }

                    if (s_notificationQueue.Count > 0 && s_inNotificationThread == 0 && Interlocked.Exchange(ref s_inNotificationThread, 1) == 0)
                    {
                        WorkItem.PostInternal(s_notificationCallback);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Trace(Debug.TAG_INTERNAL, "Exception thrown processing file change notification" + " action=" + action.ToString() + " fileName" + fileName);

                Debug.TraceException(Debug.TAG_INTERNAL, ex);
            }
        }

        // Fire notifications on a separate thread from that which received the notifications,
        // so that we don't block notification collection.
        private static void FireNotifications()
        {
            try
            {
                // Outer loop: test whether we need to fire notifications and grab the lock
                for (;;)
                {
                    // Inner loop: fire notifications until the queue is emptied
                    for (;;)
                    {
                        // Remove an item from the queue.
                        NotificationQueueItem nqi = null;
                        lock (s_notificationQueue.SyncRoot)
                        {
                            if (s_notificationQueue.Count > 0)
                            {
                                nqi = (NotificationQueueItem) s_notificationQueue.Dequeue();
                            }
                        }

                        if (nqi == null)
                            break;

                        try
                        {
                            Debug.Trace("FileChangesMonitorFireNotification", "Firing change event" + "\n\tArgs: Action=" + nqi.Action + "; fileName=" + nqi.Filename + "; Target=" + nqi.Callback.Target + "(HC=" + nqi.Callback.Target.GetHashCode().ToString("x", NumberFormatInfo.InvariantInfo) + ")");

                            // Call the callback
                            nqi.Callback(null, new FileChangeEvent(nqi.Action, nqi.Filename));
                        }
                        catch (Exception ex)
                        {
                            Debug.Trace(Debug.TAG_INTERNAL, "Exception thrown in file change callback" + " action=" + nqi.Action.ToString() + " fileName" + nqi.Filename);

                            Debug.TraceException(Debug.TAG_INTERNAL, ex);
                        }
                    }

                    // Release the lock
                    Interlocked.Exchange(ref s_inNotificationThread, 0);

                    // We need to test again to avoid ---- where a thread that receives notifications adds to the
                    // queue, but does not spawn a thread because s_inNotificationThread = 1
                    if (s_notificationQueue.Count == 0 || Interlocked.Exchange(ref s_inNotificationThread, 1) != 0)
                        break;
                }
            }
            catch
            {
                Interlocked.Exchange(ref s_inNotificationThread, 0);
            }
        }

#if DEBUG
        internal string DebugDescription(string indent)
        {
            StringBuilder sb = new StringBuilder(200);
            string i2 = indent + "    ";
            DictionaryEntryCaseInsensitiveComparer decomparer = new DictionaryEntryCaseInsensitiveComparer();

            lock (this)
            {
                DictionaryEntry[] fileEntries = new DictionaryEntry[_fileMons.Count];
                _fileMons.CopyTo(fileEntries, 0);
                Array.Sort(fileEntries, decomparer);

                sb.Append(indent + "System.Web.DirectoryMonitor: " + Directory + "\n");
                if (_watcher != null)
                {
                    sb.Append(i2 + "_dirMonCompletion " + _watcher.DebugDescription());
                }
                else
                {
                    sb.Append(i2 + "_dirMonCompletion = <null>\n");
                }

                sb.Append(i2 + GetFileMonitorsCount() + " file monitors...\n");
                if (_anyFileMon != null)
                {
                    sb.Append(_anyFileMon.DebugDescription(i2));
                }

                foreach (DictionaryEntry d in fileEntries)
                {
                    FileMonitor fileMon = (FileMonitor) d.Value;
                    if (fileMon.FileNameShort == (string) d.Key)
                        continue;

                    sb.Append(fileMon.DebugDescription(i2));
                }
            }

            return sb.ToString();
        }
#endif
    }

    internal static class FileSystemWatcherEx
    {
        public static string DebugDescription(this FileSystemWatcher watcher)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("Watcher.Path={0}", watcher.Path);
            builder.AppendLine();
            builder.AppendFormat("Watcher.Filter={0}", watcher.Filter);
            builder.AppendLine();
            builder.AppendFormat("Watcher.IncludeSubDirectories={0}", watcher.IncludeSubdirectories);
            builder.AppendLine();
            return builder.ToString();
        }
    }

    internal static class FileAttributesDataEx
    {
        internal static string DebugDescription(this FileAttributesData data, string separator)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("{1}File.Name={0}", data.Name, separator);
            builder.AppendLine();
            builder.AppendFormat("{1}File.Size={0}", data.FileSize, separator);
            builder.AppendLine();
            builder.AppendFormat("{1}File.Attributes={0}", data.FileAttributes, separator);
            builder.AppendLine();
            builder.AppendFormat("{1}File.Created={0}", data.UtcCreationTime, separator);
            builder.AppendLine();
            builder.AppendFormat("{1}File.Accessed={0}", data.UtcLastAccessTime, separator);
            builder.AppendLine();
            builder.AppendFormat("{1}File.Written={0}", data.UtcLastWriteTime, separator);
            builder.AppendLine();
            return builder.ToString();
        }
    }
}