using Xunit;
using Xunit.Abstractions;

namespace JpegCodec.Tests;

/// <summary>
/// Trace actual decoded DCT coefficients for specific blocks.
/// </summary>
public class TraceDecodedBlocks
{
    private readonly ITestOutputHelper _output;

    public TraceDecodedBlocks(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TraceFirstFewBlocks()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        _output.WriteLine($"Image: {frame.Width}x{frame.Height}");
        _output.WriteLine($"MCUs: {frame.McuCountX}x{frame.McuCountY}");
        _output.WriteLine($"Blocks per row: 38, total blocks: {38 * 40}");
        _output.WriteLine("");

        var decoder = new EntropyDecoder(frame, data);
        var blocks = decoder.DecodeAllBlocks();

        _output.WriteLine($"Decoded {blocks[0].Length} blocks for component 0");
        _output.WriteLine("");

        // Show DC values for first 20 blocks
        _output.WriteLine("First 20 blocks DC values:");
        for (int i = 0; i < Math.Min(20, blocks[0].Length); i++)
        {
            _output.WriteLine($"  Block {i}: DC = {blocks[0][i][0]}");
        }

        // Show DC for blocks around index 40 (where first mismatch is)
        _output.WriteLine("");
        _output.WriteLine("Blocks around index 40:");
        for (int i = 38; i < Math.Min(45, blocks[0].Length); i++)
        {
            int blockX = i % 38;
            int blockY = i / 38;
            int pixelX = blockX * 8;
            int pixelY = blockY * 8;
            _output.WriteLine($"  Block {i} (block {blockX},{blockY}, pixel {pixelX},{pixelY}): DC = {blocks[0][i][0]}");
        }

        // Dequantize and IDCT block 40 to see actual pixel values
        _output.WriteLine("");
        _output.WriteLine("Block 40 after full pipeline:");

        var dequant = new Dequantizer(frame);
        var dequantBlocks = dequant.DequantizeAll(blocks);

        var pixelBlocks = new byte[frame.ComponentCount][][];
        for (int c = 0; c < frame.ComponentCount; c++)
        {
            pixelBlocks[c] = new byte[dequantBlocks[c].Length][];
            for (int b = 0; b < dequantBlocks[c].Length; b++)
            {
                pixelBlocks[c][b] = InverseDct.Transform(dequantBlocks[c][b]);
            }
        }

        _output.WriteLine("Block 40 pixel values (should be for pixels 16,8 to 23,15):");
        for (int y = 0; y < 8; y++)
        {
            string row = "  ";
            for (int x = 0; x < 8; x++)
            {
                row += $"{pixelBlocks[0][40][y * 8 + x],4}";
            }
            _output.WriteLine(row);
        }
    }

    [Fact]
    public void TraceBlocksByMcu()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        var decoder = new EntropyDecoder(frame, data);
        var blocks = decoder.DecodeAllBlocks();

        // For MCU (1,0), show which block indices and their DC values
        _output.WriteLine("MCU (1,0) - expected to cover pixels (16,0)-(31,15):");
        _output.WriteLine("  Should have blocks at indices 2, 3, 40, 41");

        int[] indices = [2, 3, 40, 41];
        foreach (int i in indices)
        {
            int blockX = i % 38;
            int blockY = i / 38;
            _output.WriteLine($"  Index {i} (block {blockX},{blockY}): DC = {blocks[0][i][0]}");
        }

        // Also show MCU (0,1)
        _output.WriteLine("");
        _output.WriteLine("MCU (0,1) - expected to cover pixels (0,16)-(15,31):");
        _output.WriteLine("  Should have blocks at indices 76, 77, 114, 115");

        indices = [76, 77, 114, 115];
        foreach (int i in indices)
        {
            int blockX = i % 38;
            int blockY = i / 38;
            _output.WriteLine($"  Index {i} (block {blockX},{blockY}): DC = {blocks[0][i][0]}");
        }
    }

    [Fact]
    public void CompareDecodeOrderWithLayout()
    {
        // Show the actual order blocks are decoded vs where they're stored
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        _output.WriteLine("Block decode order for first 3 MCU rows:");
        _output.WriteLine("Format: MCU(x,y) sub-block(x,y) -> stored at index N");
        _output.WriteLine("");

        int hSamp = 2, vSamp = 2;
        int blocksPerRow = 38;
        int decodeSeq = 0;

        for (int mcuY = 0; mcuY < 3; mcuY++)
        {
            _output.WriteLine($"=== MCU Row {mcuY} ===");
            for (int mcuX = 0; mcuX < 3; mcuX++)
            {
                var storedAt = new List<int>();
                for (int subY = 0; subY < vSamp; subY++)
                {
                    for (int subX = 0; subX < hSamp; subX++)
                    {
                        int gx = mcuX * hSamp + subX;
                        int gy = mcuY * vSamp + subY;
                        int idx = gy * blocksPerRow + gx;
                        storedAt.Add(idx);
                        decodeSeq++;
                    }
                }
                _output.WriteLine($"  MCU({mcuX},{mcuY}): [{string.Join(", ", storedAt)}]");
            }
            _output.WriteLine("");
        }

        // Now verify by looking at actual decoded DC values
        var decoder = new EntropyDecoder(frame, data);
        var blocks = decoder.DecodeAllBlocks();

        _output.WriteLine("Verifying: Unique DC values in first MCU row (MCUs 0-18):");
        var dcValues = new HashSet<short>();
        for (int mcuX = 0; mcuX < 19; mcuX++)
        {
            for (int subY = 0; subY < 2; subY++)
            {
                for (int subX = 0; subX < 2; subX++)
                {
                    int gx = mcuX * 2 + subX;
                    int gy = subY;
                    int idx = gy * 38 + gx;
                    dcValues.Add(blocks[0][idx][0]);
                }
            }
        }
        _output.WriteLine($"  Found {dcValues.Count} unique DC values: [{string.Join(", ", dcValues.OrderBy(v => v))}]");
    }
}
