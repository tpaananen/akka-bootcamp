using System;
using Akka.Actor;

namespace WinTail
{
    /// <summary>
    /// Actor responsible for reading FROM the console. 
    /// Also responsible for calling <see cref="ActorSystem.Shutdown"/>.
    /// </summary>
    class ConsoleReaderActor : UntypedActor
    {
        public const string ExitCommand = "exit";
        private readonly IActorRef _consoleWriterActor;

        public ConsoleReaderActor(IActorRef consoleWriterActor)
        {
            _consoleWriterActor = consoleWriterActor;
        }

        protected override void OnReceive(object message)
        {
            var read = Console.ReadLine();
            if (!string.IsNullOrEmpty(read) && String.Equals(read, ExitCommand, StringComparison.OrdinalIgnoreCase))
            {
                // shut down the system (acquire handle to system via
                // this actors context)
                Context.System.Shutdown();
                return;
            }

            // send input to the console writer to process and print
            _consoleWriterActor.Tell(read);

            // continue reading messages from the console
            Self.Tell("continue");
        }

    }
}