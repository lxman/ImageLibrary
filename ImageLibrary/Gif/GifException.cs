using System;

namespace ImageLibrary.Gif;

/// <summary>
/// Exception thrown when GIF decoding or encoding fails.
/// </summary>
public class GifException : Exception
{
    public GifException(string message) : base(message) { }
    public GifException(string message, Exception innerException) : base(message, innerException) { }
}
