namespace TheDialgaTeam.Worktips.Explorer.Server.Discord.Command;

public class ExampleAttribute : Attribute
{
    public string Message { get; }

    public ExampleAttribute(string message)
    {
        Message = message;
    }
}