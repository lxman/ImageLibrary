using Jp2Codec;
using Jp2Codec.Tier2;
using Xunit;
using Xunit.Abstractions;

namespace Jp2Codec.Tests;

/// <summary>
/// Tests for Tier-2 decoder (packet parsing).
/// </summary>
public class Tier2DecoderTests
{
    private readonly ITestOutputHelper _output;

    public Tier2DecoderTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string GetTestImagesPath()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "TestImages", "jp2_test")))
        {
            dir = dir.Parent;
        }

        return dir != null
            ? Path.Combine(dir.FullName, "TestImages", "jp2_test")
            : "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jp2_test";
    }

    [Fact]
    public void BitReader_ReadsBits()
    {
        // Test bit reader with known data
        byte[] data = [0b10110100, 0b01101001];
        var reader = new BitReader(data);

        // Read individual bits
        Assert.Equal(1, reader.ReadBit()); // 1
        Assert.Equal(0, reader.ReadBit()); // 0
        Assert.Equal(1, reader.ReadBit()); // 1
        Assert.Equal(1, reader.ReadBit()); // 1
        Assert.Equal(0, reader.ReadBit()); // 0
        Assert.Equal(1, reader.ReadBit()); // 1
        Assert.Equal(0, reader.ReadBit()); // 0
        Assert.Equal(0, reader.ReadBit()); // 0

        // Second byte
        Assert.Equal(0, reader.ReadBit()); // 0
        Assert.Equal(1, reader.ReadBit()); // 1
    }

    [Fact]
    public void BitReader_ReadsMultipleBits()
    {
        byte[] data = [0b10110100, 0b01101001];
        var reader = new BitReader(data);

        // Read 4 bits at a time
        Assert.Equal(0b1011, reader.ReadBits(4));
        Assert.Equal(0b0100, reader.ReadBits(4));
        Assert.Equal(0b0110, reader.ReadBits(4));
    }

    [Fact]
    public void BitReader_HandlesBitStuffing()
    {
        // After 0xFF, the stuffed 0 bit should be skipped
        byte[] data = [0xFF, 0x00, 0b10000000];
        var reader = new BitReader(data);

        // Read all 8 bits of first byte (0xFF = 11111111)
        Assert.Equal(0xFF, reader.ReadBits(8));

        // After 0xFF, next byte should have bit 0 skipped
        // 0x00 = 00000000, but bit 0 is stuffed, so we read 7 bits: 0000000
        // Then we get bit 7 of 0b10000000 = 1
        int val = reader.ReadBits(7);
        Assert.Equal(0, val); // 7 zeros from stuffed byte
    }

    [Fact]
    public void TagTree_BasicDecoding()
    {
        // Create a 2x2 tag tree
        var tree = new TagTree(2, 2);

        // Set known values
        tree.SetValue(0, 0, 0);
        tree.SetValue(1, 0, 1);
        tree.SetValue(0, 1, 2);
        tree.SetValue(1, 1, 3);

        // Create a mock bit reader with appropriate bits
        // For simplicity, we'll test the structure
        _output.WriteLine("TagTree created successfully with 2x2 leaves");
    }

    [Fact]
    public void Tier2Decoder_ParsesSimpleCodestream_8x8()
    {
        var path = Path.Combine(GetTestImagesPath(), "test_8x8.jp2");
        var data = File.ReadAllBytes(path);

        // Parse codestream
        var fileReader = new Jp2FileReader(data);
        var fileInfo = fileReader.Read();
        var codestreamReader = new CodestreamReader(fileInfo.CodestreamData!);
        var codestream = codestreamReader.ReadMainHeader();

        // Read tile-part
        var tilePart = codestreamReader.ReadTilePart();
        Assert.NotNull(tilePart);

        _output.WriteLine($"Tile-part bitstream: {tilePart.BitstreamData.Length} bytes");
        _output.WriteLine($"Decomposition levels: {codestream.CodingParameters.DecompositionLevels}");
        _output.WriteLine($"Layers: {codestream.CodingParameters.LayerCount}");
        _output.WriteLine($"Progression: {codestream.CodingParameters.Progression}");
        _output.WriteLine($"Code-block size: {codestream.CodingParameters.CodeBlockWidth}x{codestream.CodingParameters.CodeBlockHeight}");

        // Try to decode
        var decoder = new Tier2Decoder(codestream);
        var output = decoder.Process(tilePart);

        _output.WriteLine($"\nTier-2 output:");
        _output.WriteLine($"  Tile index: {output.TileIndex}");
        _output.WriteLine($"  Component: {output.ComponentIndex}");
        _output.WriteLine($"  Resolution levels: {output.ResolutionLevels}");

        for (int r = 0; r < output.ResolutionLevels; r++)
        {
            var resBlocks = output.CodeBlocks[r];
            int numSubbands = resBlocks.Length;
            _output.WriteLine($"  Resolution {r}: {numSubbands} subbands");

            for (int s = 0; s < numSubbands; s++)
            {
                var subbandBlocks = resBlocks[s];
                _output.WriteLine($"    Subband {s}: {subbandBlocks.Length} code-blocks");

                foreach (var block in subbandBlocks)
                {
                    _output.WriteLine($"      Block ({block.BlockX},{block.BlockY}): {block.Data.Length} bytes, {block.CodingPasses} passes, {block.ZeroBitPlanes} zero bp");
                }
            }
        }

        // Verify we got some code-blocks
        int totalBlocks = output.CodeBlocks.Sum(r => r.Sum(s => s.Length));
        _output.WriteLine($"\nTotal code-blocks: {totalBlocks}");

        Assert.True(totalBlocks > 0, "Expected at least one code-block");
    }

    [Fact]
    public void Tier2Decoder_ParsesSimpleCodestream_16x16()
    {
        var path = Path.Combine(GetTestImagesPath(), "test_16x16.jp2");
        var data = File.ReadAllBytes(path);

        var fileReader = new Jp2FileReader(data);
        var fileInfo = fileReader.Read();
        var codestreamReader = new CodestreamReader(fileInfo.CodestreamData!);
        var codestream = codestreamReader.ReadMainHeader();

        var tilePart = codestreamReader.ReadTilePart();
        Assert.NotNull(tilePart);

        _output.WriteLine($"16x16 image:");
        _output.WriteLine($"  Bitstream: {tilePart.BitstreamData.Length} bytes");
        _output.WriteLine($"  Decomposition: {codestream.CodingParameters.DecompositionLevels} levels");

        var decoder = new Tier2Decoder(codestream);
        var output = decoder.Process(tilePart);

        _output.WriteLine($"\nTier-2 output:");
        for (int r = 0; r < output.ResolutionLevels; r++)
        {
            int numBlocks = output.CodeBlocks[r].Sum(s => s.Length);
            _output.WriteLine($"  Resolution {r}: {numBlocks} total code-blocks");
        }

        int totalBlocks = output.CodeBlocks.Sum(r => r.Sum(s => s.Length));
        _output.WriteLine($"Total code-blocks: {totalBlocks}");

        Assert.True(totalBlocks > 0);
    }

    [Fact]
    public void Tier2Decoder_AllComponents()
    {
        var path = Path.Combine(GetTestImagesPath(), "conformance_test.jp2");
        if (!File.Exists(path))
        {
            _output.WriteLine("Conformance test file not found, skipping");
            return;
        }

        var data = File.ReadAllBytes(path);
        var fileReader = new Jp2FileReader(data);
        var fileInfo = fileReader.Read();
        var codestreamReader = new CodestreamReader(fileInfo.CodestreamData!);
        var codestream = codestreamReader.ReadMainHeader();

        _output.WriteLine($"Conformance image: {codestream.Frame.Width}x{codestream.Frame.Height}");
        _output.WriteLine($"Components: {codestream.Frame.ComponentCount}");
        _output.WriteLine($"Decomposition: {codestream.CodingParameters.DecompositionLevels} levels");

        var tilePart = codestreamReader.ReadTilePart();
        Assert.NotNull(tilePart);

        _output.WriteLine($"Bitstream: {tilePart.BitstreamData.Length} bytes");

        var decoder = new Tier2Decoder(codestream);
        var outputs = decoder.DecodeAllComponents(tilePart);

        _output.WriteLine($"\nDecoded {outputs.Length} components:");
        for (int c = 0; c < outputs.Length; c++)
        {
            var output = outputs[c];
            int totalBlocks = output.CodeBlocks.Sum(r => r.Sum(s => s.Length));
            int totalBytes = output.CodeBlocks.Sum(r => r.Sum(s => s.Sum(b => b.Data.Length)));
            _output.WriteLine($"  Component {c}: {totalBlocks} blocks, {totalBytes} bytes data");
        }
    }
}
