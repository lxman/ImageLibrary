using System;

namespace ImageLibrary.Jbig2;

/// <summary>
/// Base exception for all JBIG2 decoding errors.
/// </summary>
public class Jbig2Exception : Exception
{
    public Jbig2Exception(string message) : base(message) { }
    public Jbig2Exception(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when JBIG2 data is malformed or invalid.
/// </summary>
public class Jbig2DataException : Jbig2Exception
{
    public Jbig2DataException(string message) : base(message) { }
    public Jbig2DataException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when a JBIG2 feature is not supported by this decoder.
/// </summary>
public class Jbig2UnsupportedException : Jbig2Exception
{
    public Jbig2UnsupportedException(string message) : base(message) { }
}

/// <summary>
/// Thrown when resource limits are exceeded during decoding.
/// </summary>
public class Jbig2ResourceException : Jbig2Exception
{
    public Jbig2ResourceException(string message) : base(message) { }
}
