using Microsoft.Extensions.DependencyInjection;
using SocketServer.BusinessLogic.Contracts;
using SocketServer.BusinessLogic.Extensions;
using System;

namespace SocketServer.Impl;

internal sealed class Program
{
    static void Main(string[] args)
    {
        int port = 0;

        if (!(args.Length > 0 && int.TryParse(args[0], out port)))
        {
            Console.WriteLine("You need to specify the port when starting the application");
            Console.WriteLine("Run application by specifying the port in the arguments, example: server.exe 5000");
            Exit(1);
        }

        IServiceCollection services = new ServiceCollection();
        services.AddBusinessLogicServices();
        IServiceProvider serviceProvider = services.BuildServiceProvider();
        IServer server = serviceProvider.GetRequiredService<IServer>();

        Console.CancelKeyPress += (s, e) =>
        {
            server.Stop();
            Console.WriteLine($"Server stoped");
            Exit(0);
        };

        try
        {
            server.Start(port);

            Console.WriteLine($"Server working at {server.GetEndpoint()}\nPress Ctrl+C to exit");

            while (true) { }
        }
        catch (Exception e)
        {
            Console.WriteLine("Unable to start server, error:");
            Console.WriteLine(e.Message);
            Exit(1);
        }
    }

    static void Exit(int exitCode)
    {
        Console.WriteLine("Press enter to close window...");
        Console.ReadLine();
        Environment.Exit(exitCode);
    }
}