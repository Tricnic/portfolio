using BSCore.Chat;
using System.Collections.Generic;
using System.Text;

public class ChatCommandController
{
    private static readonly string[] HELP_MODES = new string[] { "<command> - Displays detailed information about this command" };

    private class CommandHandler
    {
        public CommandHandler(System.Func<string[], bool> handler, string description, string[] modes)
        {
            Handler = handler;
            Description = description;
            Modes = modes;
        }

        public System.Func<string[], bool> Handler;
        public string Description;
        public string[] Modes;
    }

    public ChatCommandController(ChatClient chatClient)
    {
        ChatClient = chatClient;
        RegisterChatCommand("help", HelpCommandHandler, "Lists available commands", HELP_MODES);
    }

    public readonly ChatClient ChatClient;

    private readonly Dictionary<string, CommandHandler> _commandHandlers = new Dictionary<string, CommandHandler>();

    public void RegisterChatCommand(string command, System.Func<string[], bool> handler, string description, string[] modes)
    {
        _commandHandlers.Add(command, new CommandHandler(handler, description, modes));
    }

    public void HandleChatCommand(string message)
    {
        string[] parts = message.Split(' ');
        string command = parts[0].TrimStart('/').ToLower();
        string[] args;
        if (parts.Length > 1)
        {
            args = new string[parts.Length - 1];
            for (int i = 1; i < parts.Length; i++)
            {
                args[i - 1] = parts[i];
            }
        }
        else
        {
            args = new string[0];
        }
        HandleCommand(command, args);
    }

    private void SendSystemMessage(string message)
    {
        ChatClient.SystemChatRoom.OnMessageReceived(new SystemMessage(message));
    }

    private void HandleCommand(string command, string[] args)
    {
        if (_commandHandlers.TryGetValue(command, out CommandHandler commandHandler))
        {
            //SendSystemMessage($"Running command \"{command}\" with arguments: ({string.Join(", ", args)})");
            if (!commandHandler.Handler(args))
            {
                SendSystemMessage($"Error running \"/{command}\" command. Type \"/help {command}\" for more info.");
            }
        }
        else
        {
            SendSystemMessage($"Unknown command: \"{command}\". Type /help for a list of available commands.");
        }
    }

    private bool HelpCommandHandler(string[] args)
    {
        if (args.Length > 0)
        {
            if (_commandHandlers.TryGetValue(args[0], out CommandHandler commandHandler))
            {
                StringBuilder sb = new StringBuilder($"/{args[0]} - {commandHandler.Description}");
                if (commandHandler.Modes.Length > 0)
                {
                    sb.Append($"\n{string.Join("\n", commandHandler.Modes)}");
                }
                SendSystemMessage(sb.ToString());
            }
            else
            {
                SendSystemMessage($"Unknown command: {args[0]}. Type \"/help\" for a list of available commands.");
            }
        }
        else
        {
            SendSystemMessage("Available commands. Type \"/help command\" for more information on a specific command.");
            foreach (var kvp in _commandHandlers)
            {
                SendSystemMessage($"/{kvp.Key} {kvp.Value.Description}");
            }
        }
        return true;
    }
}
