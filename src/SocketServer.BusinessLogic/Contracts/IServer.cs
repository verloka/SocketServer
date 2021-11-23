using System.Net;

namespace SocketServer.BusinessLogic.Contracts;

public interface IServer
{
    void Start(int port);
    void Stop();
    EndPoint GetEndpoint();
}
