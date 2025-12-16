using Xunit;
using Xunit.Abstractions;

namespace JpegCodec.Tests;

/// <summary>
/// Verify bitstream positioning and early decoded values.
/// </summary>
public class VerifyBitstream
{
    private readonly ITestOutputHelper _output;

    public VerifyBitstream(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void CheckEntropyOffset()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        _output.WriteLine($"File size: {data.Length} bytes");
        _output.WriteLine($"Entropy data offset: {frame.EntropyDataOffset}");
        _output.WriteLine($"Entropy data length: {frame.EntropyDataLength}");
        _output.WriteLine("");

        // Show bytes around the entropy start
        _output.WriteLine("Bytes before entropy data:");
        int startShow = Math.Max(0, frame.EntropyDataOffset - 20);
        for (int i = startShow; i < frame.EntropyDataOffset; i++)
        {
            _output.WriteLine($"  [{i}] = 0x{data[i]:X2}");
        }

        _output.WriteLine("");
        _output.WriteLine("First bytes of entropy data:");
        for (int i = 0; i < Math.Min(20, frame.EntropyDataLength); i++)
        {
            _output.WriteLine($"  [{frame.EntropyDataOffset + i}] = 0x{data[frame.EntropyDataOffset + i]:X2}");
        }
    }

    [Fact]
    public void ManuallyDecodeFirstFewBlocks()
    {
        // Try to manually decode the first few DC coefficients to verify
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        _output.WriteLine("SOS marker info:");
        _output.WriteLine($"  Component count: {frame.ComponentCount}");
        foreach (var comp in frame.Components)
        {
            _output.WriteLine($"  Component {comp.Id}: DC table {comp.DcTableId}, AC table {comp.AcTableId}");
        }
        _output.WriteLine("");

        // Create decoder and get first few DC values
        var decoder = new EntropyDecoder(frame, data);
        decoder.Reset();

        _output.WriteLine("First 10 decoded DC values (sequential decode order):");
        for (int i = 0; i < 10; i++)
        {
            var block = decoder.DecodeSingleBlock(0);
            _output.WriteLine($"  Block {i}: DC = {block[0]}");
        }
    }

    [Fact]
    public void CheckDcTableContents()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        _output.WriteLine("DC Huffman Table 0:");
        var dcSpec = frame.DcHuffmanTables[0];
        if (dcSpec != null)
        {
            _output.WriteLine("  Code counts by length:");
            for (int i = 0; i < 16; i++)
            {
                if (dcSpec.CodeCounts[i] > 0)
                {
                    _output.WriteLine($"    {i + 1} bits: {dcSpec.CodeCounts[i]} codes");
                }
            }
            _output.WriteLine($"  Total symbols: {dcSpec.Symbols.Length}");
            _output.WriteLine($"  Symbols: [{string.Join(", ", dcSpec.Symbols.Take(20))}...]");
        }
    }

    [Fact]
    public void CompareDcSequences()
    {
        // Compare DC values from decode-order vs storage-index
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        // Get blocks from DecodeAllBlocks (stored by spatial position)
        var decoder1 = new EntropyDecoder(frame, data);
        var spatialBlocks = decoder1.DecodeAllBlocks();

        // Decode sequentially to get decode-order
        var decoder2 = new EntropyDecoder(frame, data);
        decoder2.Reset();

        _output.WriteLine("Comparing decode-order DC vs spatial-storage DC:");
        _output.WriteLine("DecodeIdx | DecodeOrderDC | SpatialIdx | SpatialDC | Match?");
        _output.WriteLine("----------|---------------|------------|-----------|-------");

        int decodeIdx = 0;
        for (int mcuY = 0; mcuY < 3; mcuY++)  // First 3 MCU rows
        {
            for (int mcuX = 0; mcuX < frame.McuCountX; mcuX++)
            {
                for (int subY = 0; subY < 2; subY++)
                {
                    for (int subX = 0; subX < 2; subX++)
                    {
                        var block = decoder2.DecodeSingleBlock(0);
                        short decodeOrderDc = block[0];

                        int gx = mcuX * 2 + subX;
                        int gy = mcuY * 2 + subY;
                        int spatialIdx = gy * 38 + gx;
                        short spatialDc = spatialBlocks[0][spatialIdx][0];

                        bool match = decodeOrderDc == spatialDc;
                        if (!match || decodeIdx < 20 || (decodeIdx >= 40 && decodeIdx < 50))
                        {
                            _output.WriteLine($"    {decodeIdx,5} |          {decodeOrderDc,4} |       {spatialIdx,4} |      {spatialDc,4} | {(match ? "YES" : "NO ***")}");
                        }

                        decodeIdx++;
                    }
                }
            }
        }
    }
}
