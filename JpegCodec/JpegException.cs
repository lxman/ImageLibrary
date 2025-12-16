namespace JpegCodec;

/// <summary>
/// Exception thrown when JPEG decoding fails.
/// </summary>
public class JpegException : Exception
{
    public JpegException(string message) : base(message)
    {
    }

    public JpegException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
