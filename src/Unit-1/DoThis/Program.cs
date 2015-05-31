using System;
﻿using Akka.Actor;

namespace WinTail
{
    #region Program
    class Program
    {
        public static ActorSystem MyActorSystem;

        static void Main(string[] args)
        {
            // initialize MyActorSystem
            MyActorSystem = ActorSystem.Create("MyActoSystem");

            // time to make your first actors!
            var writer = MyActorSystem.ActorOf(Props.Create(() => new ConsoleWriterActor()), "writer");
            var reader = MyActorSystem.ActorOf(Props.Create(() => new ConsoleReaderActor(writer)), "reader");

            // tell console reader to begin
            reader.Tell(ConsoleReaderActor.StartCommand);

            // blocks the main thread from exiting until the actor system is shut down
            MyActorSystem.AwaitTermination();
        }
    }
    #endregion
}
