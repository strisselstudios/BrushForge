namespace BrushForge.MapFormat.Serialization;

public sealed record Valve220WriteOptions
{
    public static Valve220WriteOptions Default { get; } =
        new();

    public Valve220WriteOptions(
        string newLine = "\r\n",
        bool validateBeforeWriting = true)
    {
        if (
            newLine is not "\r\n" and not "\n"
        ) {
            throw new ArgumentException(
                "Valve map output must use CRLF or LF line endings.",
                nameof(newLine));
        }

        NewLine = newLine;
        ValidateBeforeWriting =
            validateBeforeWriting;
    }

    public string NewLine { get; }

    public bool ValidateBeforeWriting { get; }
}
