namespace Jp2Codec.Pipeline;

/// <summary>
/// Output from Tier-2 decoding: code-block bitstreams organized by resolution level and subband.
/// </summary>
public class Tier2Output
{
    /// <summary>Tile index this output belongs to.</summary>
    public int TileIndex { get; init; }

    /// <summary>Component index.</summary>
    public int ComponentIndex { get; init; }

    /// <summary>Number of resolution levels.</summary>
    public int ResolutionLevels { get; init; }

    /// <summary>
    /// Code-blocks organized by [resolution][subband][codeblock].
    /// Resolution 0 has only LL subband.
    /// Resolution r > 0 has HL, LH, HH subbands.
    /// </summary>
    public CodeBlockBitstream[][][] CodeBlocks { get; init; } = [];
}

/// <summary>
/// Bitstream data for a single code-block, with context information for decoding.
/// </summary>
public class CodeBlockBitstream
{
    /// <summary>Code-block X index within the subband.</summary>
    public int BlockX { get; init; }

    /// <summary>Code-block Y index within the subband.</summary>
    public int BlockY { get; init; }

    /// <summary>Width of the code-block in samples.</summary>
    public int Width { get; init; }

    /// <summary>Height of the code-block in samples.</summary>
    public int Height { get; init; }

    /// <summary>Number of coding passes included.</summary>
    public int CodingPasses { get; init; }

    /// <summary>Number of zero bit-planes (leading zeros in magnitude).</summary>
    public int ZeroBitPlanes { get; init; }

    /// <summary>The compressed bitstream data.</summary>
    public byte[] Data { get; init; } = [];

    /// <summary>Bit offset within the first byte.</summary>
    public int BitOffset { get; init; }

    /// <summary>Subband type (LL, HL, LH, HH) - affects context selection.</summary>
    public SubbandType SubbandType { get; init; }
}

/// <summary>
/// Quantized subband data ready for dequantization.
/// </summary>
public class QuantizedSubband
{
    /// <summary>Subband type (LL, HL, LH, or HH).</summary>
    public SubbandType Type { get; init; }

    /// <summary>Resolution level (0 = lowest).</summary>
    public int ResolutionLevel { get; init; }

    /// <summary>Width of the subband in samples.</summary>
    public int Width { get; init; }

    /// <summary>Height of the subband in samples.</summary>
    public int Height { get; init; }

    /// <summary>Quantization step size for this subband.</summary>
    public QuantizationStepSize StepSize { get; init; }

    /// <summary>Quantized coefficient values.</summary>
    public int[,] Coefficients { get; init; } = new int[0, 0];
}

/// <summary>
/// Subband types in the DWT decomposition.
/// </summary>
public enum SubbandType
{
    /// <summary>Low-Low (approximation) subband.</summary>
    LL,
    /// <summary>High-Low (horizontal detail) subband.</summary>
    HL,
    /// <summary>Low-High (vertical detail) subband.</summary>
    LH,
    /// <summary>High-High (diagonal detail) subband.</summary>
    HH,
}

/// <summary>
/// DWT coefficients for all subbands of a tile-component, ready for inverse transform.
/// </summary>
public class DwtCoefficients
{
    /// <summary>Component index.</summary>
    public int ComponentIndex { get; init; }

    /// <summary>Number of decomposition levels.</summary>
    public int DecompositionLevels { get; init; }

    /// <summary>Width at full resolution.</summary>
    public int Width { get; init; }

    /// <summary>Height at full resolution.</summary>
    public int Height { get; init; }

    /// <summary>
    /// Subbands organized by [level][subband].
    /// Level 0 contains only LL (the final approximation).
    /// Level n > 0 contains HL, LH, HH from decomposition level n.
    /// </summary>
    public double[][,] Subbands { get; init; } = [];
}

/// <summary>
/// Reconstructed tile data after inverse DWT.
/// </summary>
public class ReconstructedTile
{
    /// <summary>Tile index.</summary>
    public int TileIndex { get; init; }

    /// <summary>Tile X position in the image.</summary>
    public int TileX { get; init; }

    /// <summary>Tile Y position in the image.</summary>
    public int TileY { get; init; }

    /// <summary>Tile width.</summary>
    public int Width { get; init; }

    /// <summary>Tile height.</summary>
    public int Height { get; init; }

    /// <summary>
    /// Component data [component][y, x].
    /// Values are floating-point before final conversion to integers.
    /// </summary>
    public double[][,] Components { get; init; } = [];
}
