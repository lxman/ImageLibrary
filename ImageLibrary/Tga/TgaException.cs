using System;

namespace ImageLibrary.Tga;

/// <summary>
/// Exception thrown when TGA decoding or encoding fails.
/// </summary>
public class TgaException : Exception
{
    public TgaException(string message) : base(message) { }
    public TgaException(string message, Exception innerException) : base(message, innerException) { }
}
