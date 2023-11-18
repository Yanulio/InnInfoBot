using System.Collections.Concurrent;

namespace innBot.Data;

public class LastCommandRepository
{
    private ConcurrentDictionary<long, string> lastCommands = new ConcurrentDictionary<long, string>();

    public void SaveCommand(string command, long senderId)
    {
        lastCommands.AddOrUpdate(senderId, command, (key, oldValue) => command);
    }

    public string GetLastCommand(long senderId)
    {
        if (lastCommands.TryGetValue(senderId, out string lastCommand))
        {
            return lastCommand;
        }
        else
        {
            return null;
        }
    }
}