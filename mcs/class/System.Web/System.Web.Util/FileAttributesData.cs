//
// System.Web.Util.FileAttributesData.cs
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

using System.IO;
using System.Runtime.InteropServices;

namespace System.Web.Util
{
//
// Wraps the Win32 API GetFileAttributesEx
// We use this api in addition to FindFirstFile because FindFirstFile
// does not work for volumes (e.g. "c:\")
//
    internal sealed class FileAttributesData
    {
        internal readonly FileAttributes FileAttributes;
        internal readonly DateTime UtcCreationTime;
        internal readonly DateTime UtcLastAccessTime;
        internal readonly DateTime UtcLastWriteTime;
        internal readonly long FileSize;
        internal readonly string Name;
        

        internal static FileAttributesData NonExistantAttributesData
        {
            get { return new FileAttributesData(); }
        }

        internal static int GetFileAttributes(string path, out FileAttributesData fad)
        {
            fad = null;

            FileAttributes attributes;
            Exception innerException;
            int errorCode;
            var result = FileUtil.TryGetAttributes(path, out attributes, out innerException, out errorCode);
            if (result != FileAccessResult.Ok)
            {
                return errorCode;
            }
            var info = GetSystemInfo(path, attributes);
            fad = new FileAttributesData(info, attributes);
            return HResults.S_OK;
        }

        internal static int GetFileAttributes(FileData data, out FileAttributesData fad)
        {
            var info = GetSystemInfo(data.FullName, data.Attributes);
            fad = new FileAttributesData(info, data.Attributes);
            return HResults.S_OK;
        }

        private static FileSystemInfo GetSystemInfo(string path, FileAttributes attributes)
        {
            FileSystemInfo info;
            if (attributes.HasFlag(FileAttributes.Directory))
            {
                info = new DirectoryInfo(path);
            }
            else
            {
                info = new FileInfo(path);
            }
            return info;
        }

        private FileAttributesData()
        {
            FileSize = -1;
        }

        private FileAttributesData(FileSystemInfo info, FileAttributes attributes)
        {
            Name = info.Name;
            FileAttributes = (FileAttributes) attributes;
            UtcCreationTime = info.CreationTimeUtc;
            UtcLastAccessTime = info.LastAccessTimeUtc;
            UtcLastWriteTime = info.LastWriteTimeUtc;
            var file = info as FileInfo;
            if (file != null)
            {
                FileSize = file.Length;
            }
        }


#if DBG
    internal string DebugDescription(string indent) {
        StringBuilder   sb = new StringBuilder(200);
        string          i2 = indent + "    ";

        sb.Append(indent + "FileAttributesData\n");
        sb.Append(i2 + "FileAttributes: " + FileAttributes + "\n");
        sb.Append(i2 + "  CreationTime: " + Debug.FormatUtcDate(UtcCreationTime) + "\n");
        sb.Append(i2 + "LastAccessTime: " + Debug.FormatUtcDate(UtcLastAccessTime) + "\n");
        sb.Append(i2 + " LastWriteTime: " + Debug.FormatUtcDate(UtcLastWriteTime) + "\n");
        sb.Append(i2 + "      FileSize: " + FileSize.ToString("n0", NumberFormatInfo.InvariantInfo) + "\n");

        return sb.ToString();
    }
#endif
    }
}