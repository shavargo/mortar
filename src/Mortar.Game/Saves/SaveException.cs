namespace Mortar.Game.Saves;

internal sealed class SaveException : Exception
{
    public SaveException(string message)
        : base(message)
    {
    }
}

internal static class Check
{
    public static void That(bool condition, string message)
    {
        if (!condition) throw new SaveException(message);
    }

    public static T Present<T>(T? value, string name)
        where T : class =>
        value ?? throw new SaveException($"{name} is missing");

    public static string Text(string? value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new SaveException($"{name} is missing") : value!;

    public static int Range(int value, int min, int max, string name)
    {
        That(value >= min && value <= max, $"{name} must be between {min} and {max}");
        return value;
    }

    public static TEnum EnumValue<TEnum>(string? value, string name)
        where TEnum : struct, Enum
    {
        var valid = !int.TryParse(value, out _)
            && Enum.TryParse<TEnum>(value, ignoreCase: false, out var parsed)
            && Enum.IsDefined(typeof(TEnum), parsed);
        return valid ? Enum.Parse<TEnum>(value!) : throw new SaveException($"{name} '{value}' is not valid");
    }
}
