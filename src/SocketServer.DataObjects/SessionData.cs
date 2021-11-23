using System.Net.Sockets;

namespace SocketServer.DataObjects;

public sealed class SessionData
{
    public Socket ClientSocket { get; private set; }

    public int Result { get; set; }

    public string ReceivedData { get; set; }

    public SessionData(Socket ClientSocket)
    {
        this.ClientSocket = ClientSocket;
        Result = 0;
        ReceivedData = string.Empty;
    }
}

