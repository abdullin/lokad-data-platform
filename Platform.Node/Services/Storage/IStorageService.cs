using Platform.Messages;

namespace Platform.Node.Services.Storage
{
    public interface IStorageService :
        IHandle<ClientMessage.AppendEvents>,
        IHandle<SystemMessage.Init>,
        IHandle<ClientMessage.ImportEvents>,
        IHandle<ClientMessage.RequestStoreReset>
    {
    }
}