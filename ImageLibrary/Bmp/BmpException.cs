using System;

namespace ImageLibrary.Bmp;

/// <summary>
/// Exception thrown when BMP encoding or decoding fails.
/// </summary>
public class BmpException : Exception
{
    public BmpException(string message) : base(message)
    {
    }

    public BmpException(string message, Exception innerException) : base(message, innerException)
    {
    }
}