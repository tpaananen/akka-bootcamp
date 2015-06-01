using System;
using System.IO;
using System.Text;
using Akka.Actor;

namespace WinTail
{
    /// <summary>
    /// Monitors the file at <see cref="_filePath"/> for changes and sends file updates to console.
    /// </summary>
    public class TailActor : UntypedActor, IDisposable
    {
        #region Message types

        /// <summary>
        /// Signal that the file has changed, and we need to read the next line of the file.
        /// </summary>
        public class FileWriteMessage
        {
            public FileWriteMessage(string fileName)
            {
                FileName = fileName;
            }

            public string FileName { get; private set; }
        }

        /// <summary>
        /// Signal that the OS had an error accessing the file.
        /// </summary>
        public class FileErrorMessage
        {
            public FileErrorMessage(string fileName, string reason)
            {
                FileName = fileName;
                Reason = reason;
            }

            public string FileName { get; private set; }

            public string Reason { get; private set; }
        }

        public class FileRenamedMessage : FileErrorMessage
        {
            public FileRenamedMessage(string oldFileName, string newFileName, string reason) 
                : base(oldFileName, reason)
            {
                NewFileName = newFileName;
            }

            public string NewFileName { get; private set; }
        }

        /// <summary>
        /// Signal to read the initial contents of the file at actor startup.
        /// </summary>
        public class InitialReadMessage
        {
            public InitialReadMessage(string fileName, string text)
            {
                FileName = fileName;
                Text = text;
            }

            public string FileName { get; private set; }
            public string Text { get; private set; }
        }

        #endregion

        private string _filePath;
        private readonly IActorRef _reporterActor;
        private FileObserver _observer;
        private Stream _fileStream;
        private StreamReader _fileStreamReader;

        public TailActor(IActorRef reporterActor, string filePath)
        {
            _reporterActor = reporterActor;
            _filePath = filePath;
        }

        protected override void PostRestart(Exception reason)
        {
            if (reason is FileRenamedException)
            {
                var ex = (FileRenamedException) reason;
                _filePath = ex.NewFileName;
            }
            base.PostRestart(reason);
        }

        protected override void PreStart()
        {
            // start watching file for changes
            _observer = new FileObserver(Self, Path.GetFullPath(_filePath));
            _observer.Start();

            // open the file stream with shared read/write permissions (so file can be written to while open)
            _fileStream = new FileStream(Path.GetFullPath(_filePath), FileMode.Open, FileAccess.Read,
                                         FileShare.ReadWrite | FileShare.Delete);
            _fileStreamReader = new StreamReader(_fileStream, Encoding.UTF8);

            // read the initial contents of the file and send it to console as first message
            var text = _fileStreamReader.ReadToEnd();
            Self.Tell(new InitialReadMessage(_filePath, text));
        }

        protected override void OnReceive(object message)
        {
            if (message is FileWriteMessage)
            {
                // move file cursor forward
                // pull results from cursor to end of file and write to output
                // (this is assuming a log file type format that is append-only)
                var text = _fileStreamReader.ReadToEnd();
                if (!string.IsNullOrEmpty(text))
                {
                    _reporterActor.Tell(text);
                }
            }
            else if (message is FileRenamedMessage)
            {
                var fe = (FileRenamedMessage)message;
                _reporterActor.Tell(string.Format("Tailed file renamed: {0} to {1}", fe.FileName, fe.NewFileName));
                throw new FileRenamedException(fe.FileName, fe.NewFileName, fe.Reason);
            }
            else if (message is FileErrorMessage)
            {
                var fe = (FileErrorMessage) message;
                _reporterActor.Tell(string.Format("Tail error: {0}", fe.Reason));
                throw new IOException(fe.Reason);
            }
            else if (message is InitialReadMessage)
            {
                var ir = (InitialReadMessage) message;
                _reporterActor.Tell(ir.Text);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _observer.Dispose();
                _fileStreamReader.Dispose();
                _fileStream.Dispose();
            }
        }
    }
}
