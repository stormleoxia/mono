namespace System.Web
{
    internal enum FileAction
    {
        Dispose = -2,
        Error = -1,
        Overwhelming = 0,
        Added = 1,
        Removed = 2,
        Modified = 3,
        RenamedOldName = 4,
        RenamedNewName = 5
    }

    internal sealed class FileChangeEvent : EventArgs
    {
        internal FileAction Action;
        internal string FileName;

        internal FileChangeEvent(FileAction action, string fileName)
        {
            Action = action;
            FileName = fileName;
        }
    }
}