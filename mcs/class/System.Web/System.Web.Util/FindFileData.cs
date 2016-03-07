//
// System.Web.Util.FindFileData.cs
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
using System.Web.UI.WebControls.WebParts;

namespace System.Web.Util
{
    internal sealed class FindFileData
    {
        private FindFileData(FileAttributesData result)
        {
            FileAttributesData = result;
            FileNameLong = result.Name;
            // Ignore the 8.3 Short filenames;
        }


        internal FindFileData(FileData data)
        {
            FileAttributesData result;
            FileAttributesData.GetFileAttributes(data, out result);
            FileAttributesData = result;
            FileNameLong = data.Name;
            // Ignore the 8.3 Short filenames;
        }

        internal FileAttributesData FileAttributesData { get; private set; }

        internal string FileNameLong { get; private set; }

        internal string FileNameShort { get { return string.Empty; } }

        internal static int FindFile(string path, out FindFileData data)
        {
            data = null;
            path = FileUtil.RemoveTrailingDirectoryBackSlash(path);
            FileAttributesData attributes;
            var errorCode = FileAttributesData.GetFileAttributes(path, out attributes);
            if (errorCode != HResults.S_OK)
            {
                return errorCode;
            }
            data = new FindFileData(attributes);
            return HResults.S_OK;
        }

        internal static int FindFile(string fullPath, string rootDirectoryPath, out FindFileData data)
        {
            var file = FindFile(fullPath, out data);
            if (file != 0 || string.IsNullOrEmpty(rootDirectoryPath))
                return file;
            rootDirectoryPath = FileUtil.RemoveTrailingDirectoryBackSlash(rootDirectoryPath);
            var relativePathLong = string.Empty;
            var relativePathShort = string.Empty;
            for (var directoryName = Path.GetDirectoryName(fullPath);
                directoryName != null && directoryName.Length > rootDirectoryPath.Length + 1 &&
                directoryName.IndexOf(rootDirectoryPath, StringComparison.OrdinalIgnoreCase) == 0;
                directoryName = Path.GetDirectoryName(directoryName))
            {
                FindFileData findFileData;
                int errorCode = FindFile(directoryName, out findFileData);
                if (errorCode != HResults.S_OK)
                    return errorCode;
                relativePathLong = findFileData.FileNameLong + Path.DirectorySeparatorChar + relativePathLong;                
            }
            if (!string.IsNullOrEmpty(relativePathLong))
                data.PrependRelativePath(relativePathLong);
            return file;
        }

        private void PrependRelativePath(string relativePathLong)
        {
            FileNameLong = relativePathLong + FileNameLong;
        }
    }
}