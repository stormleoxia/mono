//
// System.Web.Util.FileUtil.cs
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

using System.Globalization;
using System.IO;
using System.Security.AccessControl;
using System.Security.Permissions;

namespace System.Web.Util
{
    public static class FileUtil
    {
        private static readonly int _maxPathLength = 259;

        private static readonly char[] s_invalidPathChars = Path.GetInvalidPathChars();
        private static readonly char[] _invalidFileNameChars = Path.GetInvalidFileNameChars();


        public static bool FileExists(string fileName)
        {
            return File.Exists(fileName);
        }

        internal static bool DirectoryExists(string dirname)
        {
            var exists = false;
            dirname = RemoveTrailingDirectoryBackSlash(dirname);
            if (HasInvalidLastChar(dirname))
                return false;

            try
            {
                exists = Directory.Exists(dirname);
            }
            catch
            {
            }

            return exists;
        }

        // If the path is longer than the maximum length
        // Trim the end and append the hashcode to it.
        internal static string TruncatePathIfNeeded(string path, int reservedLength)
        {
            var maxPathLength = _maxPathLength - reservedLength;
            if (path.Length > maxPathLength)
            {
                // 

                path = path.Substring(0, maxPathLength - 13) +
                       path.GetHashCode().ToString(CultureInfo.InvariantCulture);
            }

            return path;
        }

        [MonoTODO("Unsafe method replacement")]
        public static void DeleteShadowCache(string cachePath, string applicationName)
        {
            // Not Implemented
        }

        // Remove the final backslash from a directory path, unless it's something like c:\
        internal static string RemoveTrailingDirectoryBackSlash(string path)
        {
            if (path == null)
                return null;
            var length = path.Length;
            if (length > 3 && path[length - 1] == '\\')
                path = path.Substring(0, length - 1);
            return path;
        }

        // Fail if the physical path is not canonical
        internal static void CheckSuspiciousPhysicalPath(string physicalPath)
        {
            if (IsSuspiciousPhysicalPath(physicalPath))
            {
                throw new HttpException(404, string.Empty);
            }
        }

        // Check whether the physical path is not canonical
        // NOTE: this API throws if we don't have permission to the file.
        // NOTE: The compare needs to be case insensitive (VSWhidbey 444513)
        internal static bool IsSuspiciousPhysicalPath(string physicalPath)
        {
            bool pathTooLong;

            if (!IsSuspiciousPhysicalPath(physicalPath, out pathTooLong))
            {
                return false;
            }

            if (!pathTooLong)
            {
                return true;
            }

            // physical path too long -> not good because we still need to make
            // it work for virtual path provider scenarios

            // first a few simple checks:
            if (physicalPath.IndexOf('/') >= 0)
            {
                return true;
            }

            var slashDots = "\\..";
            var idxSlashDots = physicalPath.IndexOf(slashDots, StringComparison.Ordinal);
            if (idxSlashDots >= 0
                && (physicalPath.Length == idxSlashDots + slashDots.Length
                    || physicalPath[idxSlashDots + slashDots.Length] == '\\'))
            {
                return true;
            }

            // the real check is to go right to left until there is no longer path-too-long
            // and see if the canonicalization check fails then

            var pos = physicalPath.LastIndexOf('\\');

            while (pos >= 0)
            {
                var path = physicalPath.Substring(0, pos);

                if (!IsSuspiciousPhysicalPath(path, out pathTooLong))
                {
                    // reached a non-suspicious path that is not too long
                    return false;
                }

                if (!pathTooLong)
                {
                    // reached a suspicious path that is not too long
                    return true;
                }

                // trim the path some more
                pos = physicalPath.LastIndexOf('\\', pos - 1);
            }

            // backtracted to the end without reaching a non-suspicious path
            // this is suspicious (should happen because app root at least should be ok)
            return true;
        }

        // VSWhidbey 609102 - Medium trust apps may hit this method, and if the physical path exists,
        // Path.GetFullPath will seek PathDiscovery permissions and throw an exception.
        [FileIOPermission(SecurityAction.Assert, AllFiles = FileIOPermissionAccess.PathDiscovery)]
        internal static bool IsSuspiciousPhysicalPath(string physicalPath, out bool pathTooLong)
        {
            bool isSuspicious;

            // DevDiv 340712: GetConfigPathData generates n^2 exceptions where n is number of incorrectly placed '/'
            // Explicitly prevent frequent exception cases since this method is called a few times per url segment
            if ((physicalPath != null) &&
                (physicalPath.Length > _maxPathLength ||
                 physicalPath.IndexOfAny(s_invalidPathChars) != -1 ||
                 // Contains ':' at any position other than 2nd char
                 (physicalPath.Length > 0 && physicalPath[0] == ':') ||
                 (physicalPath.Length > 2 && physicalPath.IndexOf(':', 2) > 0)))
            {
                // see comment below
                pathTooLong = true;
                return true;
            }

            try
            {
                isSuspicious = !string.IsNullOrEmpty(physicalPath) &&
                               string.Compare(physicalPath, Path.GetFullPath(physicalPath),
                                   StringComparison.OrdinalIgnoreCase) != 0;
                pathTooLong = false;
            }
            catch (PathTooLongException)
            {
                isSuspicious = true;
                pathTooLong = true;
            }
            catch (NotSupportedException)
            {
                // see comment below -- we do the same for ':'
                isSuspicious = true;
                pathTooLong = true;
            }
            catch (ArgumentException)
            {
                // DevDiv Bugs 152256:  Illegal characters {",|} in path prevent configuration system from working.
                // We need to catch this exception and conservatively assume that the path is suspicious in 
                // such a case.
                // We also set pathTooLong to true because at this point we do not know if the path is too long
                // or not. If we assume that pathTooLong is false, it means that our path length enforcement
                // is bypassed by using URLs with illegal characters. We do not want that. Moreover, returning 
                // pathTooLong = true causes the current logic to peel of URL fragments, which can also find a 
                // path without illegal characters to retrieve the config.
                isSuspicious = true;
                pathTooLong = true;
            }

            return isSuspicious;
        }

        // this code is called by config that doesn't have AspNetHostingPermission
        internal static void PhysicalPathStatus(string physicalPath, bool directoryExistsOnError, bool fileExistsOnError,
            out bool exists, out bool isDirectory)
        {
            exists = false;
            isDirectory = true;

            Debug.Assert(!(directoryExistsOnError && fileExistsOnError),
                "!(directoryExistsOnError && fileExistsOnError)");

            if (string.IsNullOrEmpty(physicalPath))
                return;

            using (new ApplicationImpersonationContext())
            {
                //UnsafeNativeMethods.WIN32_FILE_ATTRIBUTE_DATA data;            
                var ok = DirectoryExists(physicalPath);
                if (ok)
                {
                    exists = true;
                    if (HasInvalidLastChar(physicalPath))
                    {
                        exists = false;
                    }
                }
                else
                {
                    exists = !File.Exists(physicalPath);
                    isDirectory = false;
                }
            }
        }

        private static bool HasInvalidLastChar(string physicalPath)
        {
            // see VSWhidbey #108945
            // We need to filter out directory names which end
            // in " " or ".".  We want to treat path names that 
            // end in these characters as files - however, Windows
            // will strip these characters off the end of the name,
            // which may result in the name being treated as a 
            // directory instead.

            if (string.IsNullOrEmpty(physicalPath))
            {
                return false;
            }

            var lastChar = physicalPath[physicalPath.Length - 1];
            return lastChar == ' ' || lastChar == '.';
        }

        internal static FileAccessResult TryGetAttributes(string fileName, out FileAttributes attributes)
        {
            Exception discardedException;
            int discardedCode;
            return TryGetAttributes(fileName, out attributes, out discardedException, out discardedCode);
        }

        internal static FileAccessResult TryGetAttributes(string fileName, out FileAttributes attributes,
            out Exception innerException, out int errorCode)
        {
            var result = FileAccessResult.Unknown;
            attributes = FileAttributes.Offline;
            try
            {
                attributes = File.GetAttributes(fileName);
                innerException = null;
                errorCode = HResults.S_OK;
                return FileAccessResult.Ok;
            }
            catch (ArgumentException e)
            {
                innerException = e;
                errorCode = HResults.E_INVALIDARG;
                result = FileAccessResult.InvalidPath;
            }
            catch (PathTooLongException e)
            {
                innerException = e;
                errorCode = HResults.E_INVALID_DATA;
                result = FileAccessResult.InvalidPath;
            }
            catch (NotSupportedException e)
            {
                innerException = e;
                errorCode = HResults.ERROR_NOT_SUPPORTED;
                result = FileAccessResult.InvalidPath;
            }
            catch (DirectoryNotFoundException e)
            {
                innerException = e;
                errorCode = HResults.E_PATHNOTFOUND;
                result = FileAccessResult.DirectoryNotExist;
            }
            catch (FileNotFoundException e)
            {
                innerException = e;
                errorCode = HResults.E_FILENOTFOUND;
                result = FileAccessResult.FileNotExist;
            }
            catch (IOException e)
            {
                innerException = e;
                errorCode = HResults.E_FAIL;
                result = FileAccessResult.NotAvailable;
            }
            catch (UnauthorizedAccessException e)
            {
                innerException = e;
                errorCode = HResults.E_ACCESSDENIED;
                result = FileAccessResult.Unauthorized;
            }
            catch (Exception e)
            {
                errorCode = HResults.E_FAIL;
                innerException = e;
            }
            return result;
        }

        //
        // Use to avoid the perf hit of a Demand when the Demand is not necessary for security.
        //
        // If trueOnError is set, then return true if we cannot confirm that the file does NOT exist.
        //
        internal static bool DirectoryExists(string filename, bool trueOnError)
        {
            filename = RemoveTrailingDirectoryBackSlash(filename);
            if (HasInvalidLastChar(filename))
            {
                return false;
            }
            FileAttributes fileAttributes;
            var result = TryGetAttributes(filename, out fileAttributes);
            if (result == FileAccessResult.Ok)
            {
                // The path exists. Return true if it is a directory, false if a file.
                return fileAttributes.HasFlag(FileAttributes.Directory);
            }
            if (!trueOnError)
            {
                return false;
            }
            // Return true if we cannot confirm that the file does NOT exist.
            if (result == FileAccessResult.Unauthorized ||
                result == FileAccessResult.NotAvailable ||
                result == FileAccessResult.Unknown)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Canonicalize the directory, and makes sure it ends with a '\'
        /// </summary>
        /// <param name="dir">The dir.</param>
        /// <returns></returns>
        internal static string FixUpPhysicalDirectory(string dir)
        {
            if (dir == null)
                return null;

            dir = Path.GetFullPath(dir);

            // Append '\' to the directory if necessary.
            if (!StringUtil.StringEndsWith(dir, @"\"))
                dir = dir + @"\";

            return dir;
        }

        internal static string NormalizePath(string path)
        {
            return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        internal static FileAccessResult TryGetAccessControl(
            string filename,
            out FileSystemSecurity accessControl,
            out Exception exception,
            out int errorCode)
        {
            accessControl = null;
            FileAttributes fileAttributes;
            var result = TryGetAttributes(filename, out fileAttributes, out exception, out errorCode);
            if (result == FileAccessResult.Ok)
            {
                try
                {
                    if (fileAttributes.HasFlag(FileAttributes.Directory))
                    {
                        accessControl = Directory.GetAccessControl(filename);
                    }
                    else
                    {
                        accessControl = File.GetAccessControl(filename);
                    }
                }
                catch (ArgumentNullException e)
                {
                    exception = e;
                    errorCode = HResults.E_INVALIDARG;
                    result = FileAccessResult.InvalidPath;
                }
                catch (IOException e)
                {
                    exception = e;
                    errorCode = HResults.E_FAIL;
                    result = FileAccessResult.NotAvailable;
                }
                catch (PlatformNotSupportedException e)
                {
                    exception = e;
                    errorCode = HResults.ERROR_NOT_SUPPORTED;
                    result = FileAccessResult.NotAvailable;
                }
                catch (UnauthorizedAccessException e)
                {
                    exception = e;
                    errorCode = HResults.E_ACCESSDENIED;
                    result = FileAccessResult.Unauthorized;
                }
                catch (SystemException e)
                {
                    exception = e;
                    if (fileAttributes.HasFlag(FileAttributes.Directory))
                    {
                        errorCode = HResults.E_PATHNOTFOUND;
                        result = FileAccessResult.DirectoryNotExist;
                    }
                    else
                    {
                        errorCode = HResults.E_FILENOTFOUND;
                        result = FileAccessResult.FileNotExist;
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                    errorCode = HResults.E_FAIL;
                    result = FileAccessResult.Unknown;
                }
            }
            return result;
        }

        internal static bool DirectoryAccessible(string dirname)
        {
            var flag = false;
            dirname = RemoveTrailingDirectoryBackSlash(dirname);
            if (HasInvalidLastChar(dirname))
                return false;
            try
            {
                flag = new DirectoryInfo(dirname).Exists;
            }
            catch
            {
            }
            return flag;
        }

        internal static bool IsBeneathAppRoot(string appRoot, string filePath)
        {
            return filePath.Length > appRoot.Length + 1 &&
                   filePath.IndexOf(appRoot, StringComparison.OrdinalIgnoreCase) > -1 &&
                   filePath[appRoot.Length] == Path.DirectorySeparatorChar;
        }

        internal static string GetFirstExistingDirectory(string appRoot, string fileName)
        {
            if (!IsBeneathAppRoot(appRoot, fileName))
                return null;
            var str = appRoot;
            while (true)
            {
                var length = fileName.IndexOf(Path.DirectorySeparatorChar, str.Length + 1);
                if (length > -1)
                {
                    var filename = fileName.Substring(0, length);
                    if (DirectoryExists(filename, false))
                        str = filename;
                    else
                        break;
                }
                else
                    break;
            }
            return str;
        }

        internal static bool IsValidDirectoryName(string name)
        {
            return !string.IsNullOrEmpty(name) && name.IndexOfAny(_invalidFileNameChars, 0) == -1 &&
                   (!name.Equals(".") && !name.Equals(".."));
        }
    }


    internal enum FileAccessResult
    {
        Ok,
        Unknown,
        InvalidPath,
        DirectoryNotExist,
        FileNotExist,
        NotAvailable,
        Unauthorized
    }
}