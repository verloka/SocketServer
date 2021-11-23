using SocketServer.BusinessLogic.Contracts;
using SocketServer.BusinessLogic.Extensions;
using SocketServer.DataObjects;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketServer.BusinessLogic;

internal sealed class Server : IServer
{
    readonly ISessionStorage sessionStorage;

    Socket serverSocket;
    byte[] buffer { get; set; }

    public Server(ISessionStorage sessionStorage)
    {
        this.sessionStorage = sessionStorage;
    }

    public void Start(int port)
    {
        serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        buffer = new byte[Constants.DATA_SIZE];

        serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
        serverSocket.Listen(0);
        serverSocket.BeginAccept(new AsyncCallback(ProcessIncomingConnection), serverSocket);
    }

    public void Stop()
    {
        if (serverSocket != null)
        {
            serverSocket.Close();
            sessionStorage.DeleteAll();
        }
    }

    public EndPoint GetEndpoint()
    {
        if (serverSocket != null)
        {
            return serverSocket.LocalEndPoint;
        }
        else
        {
            return new IPEndPoint(0, 0);
        }
    }

    private void ProcessIncomingConnection(IAsyncResult result)
    {
        try
        {
            Socket socket = (Socket)result.AsyncState;

            SessionData session = new(socket.EndAccept(result));
            sessionStorage.AddOrUpdate(session);

            SendWelcomeToSocket(session.ClientSocket);

            serverSocket.BeginAccept(new AsyncCallback(ProcessIncomingConnection), serverSocket);
        }

        catch { }
    }

    private void SendWelcomeToSocket(Socket s)
    {
        SendBytesToSocket(
                s,
                new byte[] {
                            0xff, 0xfd, 0x01,   // Do Echo
                            0xff, 0xfd, 0x21,   // Do Remote Flow Control
                            0xff, 0xfb, 0x01,   // Will Echo
                            0xff, 0xfb, 0x03    // Will Supress Go Ahead
                }
            );

        SendMessageToSocket(s, $"Hello traveler!\r\nServer time: {DateTime.UtcNow}\r\n");
    }

    void ClearClientScreen(Socket s)
    {
        SendMessageToSocket(s, "\u001B[1J\u001B[H");
    }

    private void SendMessageToSocket(Socket s, string message)
    {
        byte[] data = Encoding.ASCII.GetBytes(message);
        SendBytesToSocket(s, data);
    }

    private void SendBytesToSocket(Socket s, byte[] data)
    {
        s.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendData), s);
    }

    private void SendData(IAsyncResult result)
    {
        try
        {
            Socket clientSocket = (Socket)result.AsyncState;
            clientSocket.EndSend(result);

            SessionData session = sessionStorage.Get(clientSocket);
            clientSocket.BeginReceive(buffer, 0, Constants.DATA_SIZE, SocketFlags.None, new AsyncCallback(ReceiveData), clientSocket);
        }

        catch { }
    }

    private void ReceiveData(IAsyncResult result)
    {
        try
        {
            Socket clientSocket = (Socket)result.AsyncState;
            SessionData session = sessionStorage.Get(clientSocket);

            int bytesReceived = clientSocket.EndReceive(result);

            if (bytesReceived == 0)
            {
                CloseConnection(clientSocket);
                return;
            }

            if (buffer[0] < Constants.SE)
            {
                if ((buffer[0] == Constants.DOT_CHAR && buffer[1] == Constants.CARRIAGE_RETURN && session.ReceivedData.Length == 0) || 
                    (buffer[0] == Constants.CARRIAGE_RETURN && buffer[1] == Constants.NEW_LINE))
                {
                    HandleReceivedData(session);
                }
                else
                {
                    if (buffer[0] == Constants.BACKSPACE_CHAR)
                    {
                        if (session.ReceivedData.Length > 0)
                        {
                            session.RemoveLastCharacter();
                            SendBytesToSocket(clientSocket, new byte[] { 0x08, 0x20, 0x08 });
                        }
                        else
                        {
                            clientSocket.BeginReceive(buffer, 0, Constants.DATA_SIZE, SocketFlags.None, new AsyncCallback(ReceiveData), clientSocket);
                        }
                    }
                    else if (buffer[0] == Constants.DELETE_CHAR)
                    {
                        clientSocket.BeginReceive(buffer, 0, Constants.DATA_SIZE, SocketFlags.None, new AsyncCallback(ReceiveData), clientSocket);
                    }
                    else
                    {
                        session.AppendReceivedData(Encoding.ASCII.GetString(buffer, 0, bytesReceived));

                        SendBytesToSocket(clientSocket, new byte[] { buffer[0] });

                        clientSocket.BeginReceive(buffer, 0, Constants.DATA_SIZE, SocketFlags.None, new AsyncCallback(ReceiveData), clientSocket);
                    }
                }
            }
            else
            {
                clientSocket.BeginReceive(buffer, 0, Constants.DATA_SIZE, SocketFlags.None, new AsyncCallback(ReceiveData), clientSocket);
            }
        }

        catch { }
    }

    private void CloseConnection(Socket s)
    {
        s.Close();
        sessionStorage.Delete(s);
        serverSocket.BeginAccept(new AsyncCallback(ProcessIncomingConnection), serverSocket);
    }

    private void HandleReceivedData(SessionData session)
    {
        string receivedData = session.ReceivedData;
        session.ResetReceivedData();

        if (int.TryParse(receivedData, out int i))
        {
            session.Result += i;
            SendMessageToSocket(session.ClientSocket, $"\n\rEntered value: {session.Result}");
        }
        else
        {
            switch (receivedData)
            {
                case "list":
                    {
                        session.ResetReceivedData();
                        var clients = sessionStorage.GetAll();
                        SendMessageToSocket(session.ClientSocket, $"\n\rConnected clients - {clients.Count()}:");
                        foreach (var item in clients)
                            SendMessageToSocket(session.ClientSocket, $"\n\r{item.ClientSocket.RemoteEndPoint}{(item.ClientSocket == session.ClientSocket ? " (you)" : "")}: entered value - {item.Result}");
                        break;
                    }
                case "clear":
                    {
                        ClearClientScreen(session.ClientSocket);
                        return;
                    }
                case "exit":
                    {
                        CloseConnection(session.ClientSocket);
                        return;
                    }
                default:
                    {
                        SendMessageToSocket(session.ClientSocket, "\n\rUnknown command, use the following commands:");
                        SendMessageToSocket(session.ClientSocket, "\n\rlist - get a list of connected clients");
                        SendMessageToSocket(session.ClientSocket, "\n\rclear - clear console window");
                        SendMessageToSocket(session.ClientSocket, "\n\rexit - close connection");
                        SendMessageToSocket(session.ClientSocket, "\n\rany integer value - increase the entered value");
                        break;
                    }
            }

        }

        SendMessageToSocket(session.ClientSocket, "\n\r");
    }
}
