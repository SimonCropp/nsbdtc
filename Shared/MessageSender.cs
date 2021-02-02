using System;
using System.Threading.Tasks;
using NServiceBus;

public class MessageSender
{
    public static async Task StartLoop(IMessageSession messageSession)
    {
        Console.WriteLine("Press [c] to send a command. Press [Esc] to exit.");
        while (true)
        {
            var input = Console.ReadKey();
            Console.WriteLine();

            switch (input.Key)
            {
                case ConsoleKey.C:
                    await messageSession.SendLocal(new MyCommand());
                    break;
                case ConsoleKey.Escape:
                    return;
            }
        }
    }
}