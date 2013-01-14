namespace Platform.Node.Messages
{
    /// <summary>
    /// Core messages related to server lifetime
    /// </summary>
    public static class SystemMessage
    {
        public sealed class Init : Message
        {
            
        }

        public sealed class Start : Message {}
        public sealed class StartShutdown : Message
        {
            
        }

        public class StorageWriterInitializationDone : Message
        {
        }
        public sealed class BecameShutdown : Message{}

        public sealed class BecameWorking : Message{}

    }
}