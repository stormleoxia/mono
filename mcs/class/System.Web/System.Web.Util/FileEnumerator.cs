//
// System.Web.Util.FileEnumerator.cs
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
using System.Collections.Generic;
using System.IO;

namespace System.Web.Util
{
    internal class FileEnumerator : FileData, IEnumerable, IEnumerator, IDisposable, IEnumerable<FileData>, IEnumerator<FileData>
    {
        private bool first = true;
        private IEnumerable<string> _innerEnumeration;
        private IEnumerator<string> _innerEnumerator;
        private string _current;

        object IEnumerator.Current
        {
            get { return this.Current; }
        }

        private FileEnumerator(string path)
        {
            this._path = Path.GetFullPath(path);

        }

        /// <summary>
        /// Retourne un énumérateur qui itère au sein de la collection.
        /// </summary>
        /// <returns>
        /// Énumérateur permettant d'effectuer une itération au sein de la collection.
        /// </returns>
        public IEnumerator<FileData> GetEnumerator()
        {
            return this;
        }

        ~FileEnumerator()
        {
            try
            {
                ((IDisposable) this).Dispose();
            }
            finally
            {

            }
        }

        internal static FileEnumerator Create(string path)
        {
            return new FileEnumerator(path);
        }

        private bool SkipCurrent()
        {
            return _current == "." || _current == "..";
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }

        bool IEnumerator.MoveNext()
        {
            if (first)
            {
                _innerEnumeration = Directory.EnumerateFileSystemEntries(_path);
                _innerEnumerator = _innerEnumeration.GetEnumerator();
            }
            do
            {
                if (_innerEnumerator.MoveNext())
                {
                    _current = _innerEnumerator.Current;
                    Init(_current);
                }
                else
                {
                    return false;
                }
            } while (this.SkipCurrent());
            return true;
        }

        void IEnumerator.Reset()
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Obtient l'élément de la collection situé à la position actuelle de l'énumérateur.
        /// </summary>
        /// <returns>
        /// Élément dans la collection à la position actuelle de l'énumérateur.
        /// </returns>
        public FileData Current
        {
            get { return this; }
        }

        void IDisposable.Dispose()
        {
            _innerEnumerator.Dispose();
            _innerEnumeration = null;
            GC.SuppressFinalize((object) this);
        }
    }
}
