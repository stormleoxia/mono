//
// System.Web.Util.FileData.cs
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

namespace System.Web.Util
{
    internal abstract class FileData
    {
        protected string _path;
        private string _fileName;
        private FileAttributes _attributes;

        internal string Name
        {
            get { return _fileName; }
        }

        internal string FullName
        {
            get { return _path + "\\" + _fileName; }
        }

        internal bool IsDirectory
        {
            get { return _attributes.HasFlag(FileAttributes.Directory); }
        }

        internal bool IsHidden
        {
            get { return _attributes.HasFlag(FileAttributes.Hidden); }
        }

        public FileAttributes Attributes { get { return _attributes; } }

        internal FindFileData GetFindFileData()
        {
            return new FindFileData(this);
        }

        protected void Init(string path)
        {
            // Path must exists
            _path = Path.GetDirectoryName(path);
            _attributes = File.GetAttributes(path);
            if (!IsDirectory)
            {
                _fileName = Path.GetFileName(path);
            }
            else
            {
                var info = new DirectoryInfo(path);
                _fileName = info.Name;
            }
        }
    }
}
