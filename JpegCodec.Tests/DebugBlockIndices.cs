using Xunit;
using Xunit.Abstractions;

namespace JpegCodec.Tests;

public class DebugBlockIndices
{
    private readonly ITestOutputHelper _output;

    public DebugBlockIndices(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TraceBlockAllocation()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        byte[] data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        JpegFrame frame = reader.ReadFrame();

        _output.WriteLine($"Image: {frame.Width}x{frame.Height}");
        _output.WriteLine($"MaxHSamp: {frame.MaxHorizontalSamplingFactor}, MaxVSamp: {frame.MaxVerticalSamplingFactor}");
        _output.WriteLine($"MCUs: {frame.McuCountX}x{frame.McuCountY}");

        JpegComponent comp = frame.Components[0];
        _output.WriteLine($"Component 0: hSamp={comp.HorizontalSamplingFactor}, vSamp={comp.VerticalSamplingFactor}");

        // Calculate blocks per row as done in EntropyDecoder
        int hBlocks = (frame.Width + frame.MaxHorizontalSamplingFactor * 8 - 1)
                      / (frame.MaxHorizontalSamplingFactor * 8) * comp.HorizontalSamplingFactor;
        int vBlocks = (frame.Height + frame.MaxVerticalSamplingFactor * 8 - 1)
                      / (frame.MaxVerticalSamplingFactor * 8) * comp.VerticalSamplingFactor;
        _output.WriteLine($"Blocks: {hBlocks}x{vBlocks} = {hBlocks * vBlocks}");

        // Trace first few MCUs - show which block indices they write to
        _output.WriteLine("\n=== Block allocation by MCU ===");
        for (var mcuY = 0; mcuY < 3; mcuY++)
        {
            for (var mcuX = 0; mcuX < 3; mcuX++)
            {
                _output.WriteLine($"\nMCU ({mcuX}, {mcuY}):");
                for (var blockY = 0; blockY < comp.VerticalSamplingFactor; blockY++)
                {
                    for (var blockX = 0; blockX < comp.HorizontalSamplingFactor; blockX++)
                    {
                        int globalBlockX = mcuX * comp.HorizontalSamplingFactor + blockX;
                        int globalBlockY = mcuY * comp.VerticalSamplingFactor + blockY;
                        int blockIndex = globalBlockY * hBlocks + globalBlockX;
                        _output.WriteLine($"  Sub-block ({blockX},{blockY}) -> global ({globalBlockX},{globalBlockY}) -> index {blockIndex}");
                    }
                }
            }
        }

        // Now trace the reverse - for given pixels, which block index are we reading from?
        _output.WriteLine("\n=== Block lookup by pixel ===");
        int blocksPerRow = (frame.Width + frame.MaxHorizontalSamplingFactor * 8 - 1)
                          / (frame.MaxHorizontalSamplingFactor * 8) * comp.HorizontalSamplingFactor;
        _output.WriteLine($"blocksPerRow in ColorConverter: {blocksPerRow}");

        int[] testPixels = [0, 8, 16, 24, 80, 88, 160, 168];
        foreach (int px in testPixels)
        {
            foreach (int py in testPixels)
            {
                if (px < frame.Width && py < frame.Height)
                {
                    int blockX = px / 8;
                    int blockY = py / 8;
                    int blockIndex = blockY * blocksPerRow + blockX;
                    _output.WriteLine($"Pixel ({px,3},{py,3}) -> block ({blockX,2},{blockY,2}) -> index {blockIndex}");
                }
            }
        }
    }

    [Fact]
    public void CompareFirstBlockDCT()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        byte[] data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        JpegFrame frame = reader.ReadFrame();

        var decoder = new EntropyDecoder(frame, data);
        short[][][] blocks = decoder.DecodeAllBlocks();

        _output.WriteLine($"Total blocks decoded for component 0: {blocks[0].Length}");

        // Show first 8 blocks (should be first 2 MCU rows for 2x2 sampling)
        _output.WriteLine("\nFirst 8 blocks (DCT coefficients, DC only):");
        for (var i = 0; i < Math.Min(8, blocks[0].Length); i++)
        {
            _output.WriteLine($"Block {i}: DC = {blocks[0][i][0]}");
        }

        // Show what ImageSharp reports for first blocks
        _output.WriteLine("\nImageSharp reports these DC values for first blocks:");
        _output.WriteLine("Block 0: DC = 73");
        _output.WriteLine("Block 1: DC = 73 (same, diff from prev = 0)");
        _output.WriteLine("(from instrumentation output)");
    }
}
