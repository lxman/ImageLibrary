namespace Jp2Codec;

/// <summary>
/// Represents the main JPEG2000 frame parameters extracted from the SIZ marker.
/// This is the entry point for all decoder stages.
/// </summary>
public class Jp2Frame
{
    /// <summary>Reference grid width (Xsiz).</summary>
    public int Width { get; init; }

    /// <summary>Reference grid height (Ysiz).</summary>
    public int Height { get; init; }

    /// <summary>Horizontal offset from grid origin to left edge of image (XOsiz).</summary>
    public int XOffset { get; init; }

    /// <summary>Vertical offset from grid origin to top edge of image (YOsiz).</summary>
    public int YOffset { get; init; }

    /// <summary>Tile width (XTsiz).</summary>
    public int TileWidth { get; init; }

    /// <summary>Tile height (YTsiz).</summary>
    public int TileHeight { get; init; }

    /// <summary>Horizontal offset of first tile (XTOsiz).</summary>
    public int TileXOffset { get; init; }

    /// <summary>Vertical offset of first tile (YTOsiz).</summary>
    public int TileYOffset { get; init; }

    /// <summary>Number of components (Csiz).</summary>
    public int ComponentCount { get; init; }

    /// <summary>Component parameters.</summary>
    public Jp2Component[] Components { get; init; } = [];

    /// <summary>Number of tiles in horizontal direction.</summary>
    public int NumTilesX => (Width - TileXOffset + TileWidth - 1) / TileWidth;

    /// <summary>Number of tiles in vertical direction.</summary>
    public int NumTilesY => (Height - TileYOffset + TileHeight - 1) / TileHeight;

    /// <summary>Total number of tiles.</summary>
    public int TileCount => NumTilesX * NumTilesY;
}

/// <summary>
/// Component parameters from SIZ marker.
/// </summary>
public class Jp2Component
{
    /// <summary>Bit depth (Ssiz & 0x7F), 0-based (actual bits = BitDepth + 1).</summary>
    public int BitDepth { get; init; }

    /// <summary>Whether the component is signed (Ssiz & 0x80).</summary>
    public bool IsSigned { get; init; }

    /// <summary>Horizontal subsampling factor (XRsiz).</summary>
    public int XSubsampling { get; init; }

    /// <summary>Vertical subsampling factor (YRsiz).</summary>
    public int YSubsampling { get; init; }

    /// <summary>Actual bit depth (BitDepth + 1).</summary>
    public int Precision => BitDepth + 1;
}
