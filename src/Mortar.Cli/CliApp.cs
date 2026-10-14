namespace Mortar.Cli;

public static class CliApp
{
    private const string Usage = """
        Usage:
          mortar generate --area <area.json> --seed <n> --out <layout.json>
          mortar preview --layout <layout.json> --out <map.png> [--scale <pixels per cell>]
        """;

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 0)
        {
            error.WriteLine(Usage);
            return ExitCodes.InvalidInput;
        }
        if (args[0] is "help" or "--help" or "-h")
        {
            output.WriteLine(Usage);
            return ExitCodes.Success;
        }
        var options = CliOptions.Parse(args, 1, error);
        if (options is null) return ExitCodes.InvalidInput;
        return args[0] switch
        {
            "generate" => GenerateCommand.Run(options, output, error),
            "preview" => PreviewCommand.Run(options, output, error),
            _ => UnknownCommand(args[0], error),
        };
    }

    private static int UnknownCommand(string name, TextWriter error)
    {
        error.WriteLine($"error: unknown command '{name}'");
        error.WriteLine(Usage);
        return ExitCodes.InvalidInput;
    }
}
