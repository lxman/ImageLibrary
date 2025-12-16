namespace PngCodec;

/// <summary>
/// Exception thrown when PNG decoding or encoding fails.
/// </summary>
public class PngException : Exception
{
    public PngException(string message) : base(message) { }
    public PngException(string message, Exception innerException) : base(message, innerException) { }
}
