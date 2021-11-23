using SocketServer.DataObjects;
using System.Collections.Generic;
using System.Net.Sockets;

namespace SocketServer.BusinessLogic.Contracts;

public interface ISessionStorage
{
    IEnumerable<SessionData> GetAll();
    SessionData Get(Socket socket);
    void AddOrUpdate(SessionData sessionData);
    bool Delete(Socket socket);
    void DeleteAll();
}