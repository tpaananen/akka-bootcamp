using System;

namespace WinTail
{
    public class FileRenamedException : Exception
    {

        public FileRenamedException(string oldFileName, string newFileName, string message)
            : base(message)
        {
            OldFileName = oldFileName;
            NewFileName = newFileName;
        }

        public string NewFileName { get; private set; }

        public string OldFileName { get; private set; }
    }
}
