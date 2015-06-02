using System;
using Akka.Actor;

namespace WinTail
{
    public class FileRenamedException : Exception
    {

        public FileRenamedException(string oldFileName, string newFileName, string message, IActorRef reporterActor)
            : base(message)
        {
            OldFileName = oldFileName;
            NewFileName = newFileName;
            ReporterActor = reporterActor;
        }

        public string NewFileName { get; private set; }

        public string OldFileName { get; private set; }

        public IActorRef ReporterActor { get; private set; }
    }
}
