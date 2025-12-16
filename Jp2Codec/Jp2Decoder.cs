using Jp2Codec.Dequantization;
using Jp2Codec.Pipeline;
using Jp2Codec.PostProcess;
using Jp2Codec.Tier1;
using Jp2Codec.Tier2;
using Jp2Codec.Wavelet;

namespace Jp2Codec;

/// <summary>
/// Main JPEG2000 decoder that orchestrates all pipeline stages.
/// </summary>
public class Jp2Decoder
{
    private readonly Jp2Codestream _codestream;
    private readonly byte[] _codestreamData;
    private readonly int _jp2ColorSpace;
    private readonly Jp2Palette? _palette;
    private readonly ComponentMapping[]? _componentMappings;

    // Pipeline stages
    private readonly Tier2Decoder _tier2;
    private readonly Tier1Decoder _tier1;
    private readonly Dequantizer _dequantizer;
    private readonly InverseDwt _inverseDwt;
    private readonly PostProcessor _postProcessor;

    // JP2 color space constants
    public const int ColorSpaceSRGB = 16;
    public const int ColorSpaceGreyscale = 17;
    public const int ColorSpaceYCC = 18;

    public Jp2Decoder(byte[] data)
    {
        // Check if this is a JP2 file or raw codestream
        Jp2FileInfo? fileInfo = null;
        if (Jp2FileReader.IsRawCodestream(data))
        {
            // Raw codestream - use data directly
            _codestreamData = data;
            _jp2ColorSpace = 0; // Unknown
            _palette = null;
            _componentMappings = null;
        }
        else
        {
            // JP2 file - extract codestream from boxes
            var fileReader = new Jp2FileReader(data);
            fileInfo = fileReader.Read();

            if (fileInfo.CodestreamData == null)
            {
                throw new Jp2Exception("No codestream found in data");
            }

            _codestreamData = fileInfo.CodestreamData;
            _jp2ColorSpace = fileInfo.ColorSpace;
            _palette = fileInfo.Palette;
            _componentMappings = fileInfo.ComponentMappings;
        }

        // Parse main header
        var codestreamReader = new CodestreamReader(_codestreamData);
        _codestream = codestreamReader.ReadMainHeader();

        // Read tile-parts
        while (true)
        {
            var tilePart = codestreamReader.ReadTilePart();
            if (tilePart == null) break;
            _codestream.TileParts.Add(tilePart);
        }

        // Initialize pipeline stages
        _tier2 = new Tier2Decoder(_codestream);
        _tier1 = new Tier1Decoder(_codestream);
        _dequantizer = new Dequantizer(_codestream);
        _inverseDwt = new InverseDwt(_codestream.CodingParameters.WaveletType);
        _postProcessor = new PostProcessor(_codestream, _jp2ColorSpace, fileInfo?.ChannelDefinitions);
    }

    /// <summary>
    /// Gets the image width.
    /// </summary>
    public int Width => _codestream.Frame.Width;

    /// <summary>
    /// Gets the image height.
    /// </summary>
    public int Height => _codestream.Frame.Height;

    /// <summary>
    /// Gets the number of components.
    /// </summary>
    public int ComponentCount => _codestream.Frame.ComponentCount;

    /// <summary>
    /// Gets the parsed codestream info.
    /// </summary>
    public Jp2Codestream Codestream => _codestream;

    /// <summary>
    /// Gets the JP2 color space (16=sRGB, 17=Greyscale, 18=sYCC).
    /// </summary>
    public int Jp2ColorSpace => _jp2ColorSpace;

    /// <summary>
    /// Gets a palette entry value for debugging.
    /// </summary>
    public int GetPaletteEntry(int index, int column)
    {
        if (_palette == null) return -1;
        if (index >= _palette.NumEntries || column >= _palette.NumColumns) return -1;
        return _palette.Entries[index, column];
    }

    /// <summary>
    /// Decodes the image and returns interleaved pixel data.
    /// </summary>
    public byte[] Decode()
    {
        if (_codestream.TileParts.Count == 0)
        {
            throw new Jp2Exception("No tile-parts found in codestream");
        }

        // For single-tile images, decode directly
        byte[] result;
        if (_codestream.Frame.TileCount == 1)
        {
            result = DecodeTile(0);
        }
        else
        {
            // For multi-tile images, decode each tile and assemble
            result = DecodeAllTiles();
        }

        // Apply palette mapping if present
        if (_palette != null && _componentMappings != null)
        {
            result = ApplyPaletteMapping(result);
        }

        return result;
    }

    /// <summary>
    /// Decodes a single tile and returns pixel data.
    /// </summary>
    public byte[] DecodeTile(int tileIndex)
    {
        var tilePart = _codestream.TileParts.FirstOrDefault(tp => tp.TileIndex == tileIndex);
        if (tilePart == null)
        {
            throw new Jp2Exception($"Tile {tileIndex} not found");
        }

        // Calculate tile dimensions
        int numTilesX = _codestream.Frame.NumTilesX;
        int tileX = tileIndex % numTilesX;
        int tileY = tileIndex / numTilesX;

        int tileStartX = tileX * _codestream.Frame.TileWidth + _codestream.Frame.TileXOffset;
        int tileStartY = tileY * _codestream.Frame.TileHeight + _codestream.Frame.TileYOffset;
        int tileEndX = Math.Min(tileStartX + _codestream.Frame.TileWidth, _codestream.Frame.Width);
        int tileEndY = Math.Min(tileStartY + _codestream.Frame.TileHeight, _codestream.Frame.Height);
        int tileWidth = tileEndX - tileStartX;
        int tileHeight = tileEndY - tileStartY;

        // Decode each component through the pipeline
        var reconstructedComponents = new double[_codestream.Frame.ComponentCount][,];

        var tier2Outputs = _tier2.DecodeAllComponents(tilePart);

        for (int c = 0; c < _codestream.Frame.ComponentCount; c++)
        {
            // Tier-2: packet parsing
            var tier2Output = tier2Outputs[c];

            // Tier-1: EBCOT decoding
            var subbands = _tier1.DecodeToSubbands(tier2Output);

            // Dequantization
            var dwtCoefs = _dequantizer.DequantizeAll(subbands, c);

            // Inverse DWT
            var reconstructed = _inverseDwt.Process(dwtCoefs);

            reconstructedComponents[c] = reconstructed;
        }

        // Create reconstructed tile
        var tile = new ReconstructedTile
        {
            TileIndex = tileIndex,
            TileX = tileX,
            TileY = tileY,
            Width = tileWidth,
            Height = tileHeight,
            Components = reconstructedComponents,
        };

        // Post-processing (color transform, level shift, clamping)
        return _postProcessor.Process(tile);
    }

    /// <summary>
    /// Decodes all tiles and assembles the complete image.
    /// </summary>
    private byte[] DecodeAllTiles()
    {
        int width = _codestream.Frame.Width;
        int height = _codestream.Frame.Height;
        int numComponents = _codestream.Frame.ComponentCount;
        int numTilesX = _codestream.Frame.NumTilesX;
        int numTilesY = _codestream.Frame.NumTilesY;

        byte[] result = new byte[width * height * numComponents];

        for (int ty = 0; ty < numTilesY; ty++)
        {
            for (int tx = 0; tx < numTilesX; tx++)
            {
                int tileIndex = ty * numTilesX + tx;

                // Decode tile
                byte[] tileData = DecodeTile(tileIndex);

                // Calculate tile position
                int tileStartX = tx * _codestream.Frame.TileWidth + _codestream.Frame.TileXOffset;
                int tileStartY = ty * _codestream.Frame.TileHeight + _codestream.Frame.TileYOffset;
                int tileEndX = Math.Min(tileStartX + _codestream.Frame.TileWidth, width);
                int tileEndY = Math.Min(tileStartY + _codestream.Frame.TileHeight, height);
                int tileWidth = tileEndX - tileStartX;
                int tileHeight = tileEndY - tileStartY;

                // Copy tile data to result
                for (int y = 0; y < tileHeight; y++)
                {
                    int srcOffset = y * tileWidth * numComponents;
                    int dstOffset = ((tileStartY + y) * width + tileStartX) * numComponents;
                    Array.Copy(tileData, srcOffset, result, dstOffset, tileWidth * numComponents);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Decodes to a grayscale image (first component only).
    /// </summary>
    public byte[] DecodeGrayscale()
    {
        if (_codestream.TileParts.Count == 0)
        {
            throw new Jp2Exception("No tile-parts found in codestream");
        }

        var tilePart = _codestream.TileParts[0];

        // Decode first component only
        var tier2Output = _tier2.Process(tilePart);
        var subbands = _tier1.DecodeToSubbands(tier2Output);
        var dwtCoefs = _dequantizer.DequantizeAll(subbands, 0);
        var reconstructed = _inverseDwt.Process(dwtCoefs);

        // Post-process to bytes
        var grayscaleProcessor = new GrayscalePostProcessor(_codestream);
        return grayscaleProcessor.Process(reconstructed);
    }

    /// <summary>
    /// Decodes the image without palette mapping (returns raw indices for palette images).
    /// </summary>
    public byte[] DecodeRawIndices()
    {
        if (_codestream.TileParts.Count == 0)
        {
            throw new Jp2Exception("No tile-parts found in codestream");
        }

        // For single-tile images, decode directly
        if (_codestream.Frame.TileCount == 1)
        {
            return DecodeTile(0);
        }
        else
        {
            return DecodeAllTiles();
        }
    }

    /// <summary>
    /// Gets intermediate data for testing/debugging.
    /// </summary>
    public IntermediateData GetIntermediateData(int tileIndex = 0)
    {
        var tilePart = _codestream.TileParts.FirstOrDefault(tp => tp.TileIndex == tileIndex);
        if (tilePart == null)
        {
            return new IntermediateData();
        }

        var tier2Output = _tier2.Process(tilePart);
        var subbands = _tier1.DecodeToSubbands(tier2Output);
        var dwtCoefs = _dequantizer.DequantizeAll(subbands, 0);

        return new IntermediateData
        {
            Tier2Output = tier2Output,
            Subbands = subbands,
            DwtCoefficients = dwtCoefs,
        };
    }

    /// <summary>
    /// Applies palette mapping to convert palette indices to actual color values.
    /// </summary>
    private byte[] ApplyPaletteMapping(byte[] indexData)
    {
        int width = _codestream.Frame.Width;
        int height = _codestream.Frame.Height;
        int numPixels = width * height;

        // Determine output format based on component mappings
        // Each mapping produces one output component
        int numOutputComponents = _componentMappings!.Length;
        byte[] result = new byte[numPixels * numOutputComponents];

        // For each pixel
        for (int i = 0; i < numPixels; i++)
        {
            // The input is a single-component image containing palette indices
            byte paletteIndex = indexData[i];

            // Apply each component mapping
            for (int m = 0; m < _componentMappings.Length; m++)
            {
                var mapping = _componentMappings[m];

                int value;
                if (mapping.MappingType == 1)
                {
                    // Palette mapping: look up value in palette
                    int col = mapping.PaletteColumn;
                    if (paletteIndex < _palette!.NumEntries && col < _palette.NumColumns)
                    {
                        value = _palette.Entries[paletteIndex, col];
                    }
                    else
                    {
                        value = 0;
                    }
                }
                else
                {
                    // Direct mapping: use the index value directly
                    value = paletteIndex;
                }

                // Clamp to 8-bit
                result[i * numOutputComponents + m] = (byte)Math.Clamp(value, 0, 255);
            }
        }

        return result;
    }
}

/// <summary>
/// Holds intermediate data from decoder stages for testing.
/// </summary>
public class IntermediateData
{
    public Tier2Output? Tier2Output { get; init; }
    public QuantizedSubband[]? Subbands { get; init; }
    public DwtCoefficients? DwtCoefficients { get; init; }
}
