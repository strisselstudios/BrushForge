namespace BrushForge.MapFormat.Model;

/// <summary>
/// One ordered quoted key/value property in a Valve map entity.
/// </summary>
public sealed record MapProperty
{
    public MapProperty(
        string key,
        string value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        string normalizedKey = key.Trim();

        if (normalizedKey.Length == 0) {
            throw new ArgumentException(
                "A map property key is required.",
                nameof(key));
        }

        ValidateText(
            normalizedKey,
            nameof(key));

        ValidateText(
            value,
            nameof(value));

        Key = normalizedKey;
        Value = value;
    }

    public string Key { get; }

    public string Value { get; }

    private static void ValidateText(
        string value,
        string parameterName)
    {
        if (
            value.Any(
                character =>
                    character is '\0' or '\r' or '\n')
        ) {
            throw new ArgumentException(
                "Map property text cannot contain null characters or line breaks.",
                parameterName);
        }
    }
}
