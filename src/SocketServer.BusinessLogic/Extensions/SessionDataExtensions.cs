using SocketServer.DataObjects;

namespace SocketServer.BusinessLogic.Extensions;

internal static class SessionDataExtensions
{
    public static void AppendReceivedData(this SessionData sessionData, string appendData) => sessionData.ReceivedData = $"{sessionData.ReceivedData}{appendData}";
    public static void ResetReceivedData(this SessionData sessionData) => sessionData.ReceivedData = string.Empty;
    public static void RemoveLastCharacter(this SessionData sessionData) => sessionData.ReceivedData = sessionData.ReceivedData[0..^1];
}