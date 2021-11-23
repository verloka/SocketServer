using SocketServer.BusinessLogic.Contracts;
using SocketServer.DataObjects;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;

namespace SocketServer.BusinessLogic;

internal sealed class InMemorySessionStorage : ISessionStorage
{
    private readonly ConcurrentDictionary<Socket, SessionData> storage;

    public InMemorySessionStorage() => storage = new();

    public void AddOrUpdate(SessionData sessionData) => storage.AddOrUpdate(sessionData.ClientSocket, sessionData, (o, n) => storage[o] = n);

    public bool Delete(Socket socket) => storage.TryRemove(socket, out _);

    public void DeleteAll() => storage.Clear();

    public SessionData Get(Socket socket) => storage.TryGetValue(socket, out SessionData data) ? data : null;

    public IEnumerable<SessionData> GetAll() => storage.Values;
}