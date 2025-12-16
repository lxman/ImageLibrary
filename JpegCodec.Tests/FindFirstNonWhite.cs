using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;
using Xunit.Abstractions;

namespace JpegCodec.Tests;

public class FindFirstNonWhite
{
    private readonly ITestOutputHelper _output;

    public FindFirstNonWhite(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void FindFirstNonWhiteDC()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        byte[] data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        JpegFrame frame = reader.ReadFrame();

        var decoder = new EntropyDecoder(frame, data);
        short[][][] blocks = decoder.DecodeAllBlocks();

        _output.WriteLine("Looking for first block with DC != 73...");

        for (var i = 0; i < blocks[0].Length; i++)
        {
            short dc = blocks[0][i][0];
            if (dc != 73)
            {
                int blockX = i % 38;
                int blockY = i / 38;
                int pixelX = blockX * 8;
                int pixelY = blockY * 8;

                int mcuX = blockX / 2;
                int mcuY = blockY / 2;
                int subX = blockX % 2;
                int subY = blockY % 2;

                _output.WriteLine($"Block {i}: DC = {dc}");
                _output.WriteLine($"  Global block: ({blockX}, {blockY})");
                _output.WriteLine($"  Pixel region: ({pixelX}, {pixelY}) to ({pixelX + 7}, {pixelY + 7})");
                _output.WriteLine($"  MCU: ({mcuX}, {mcuY}), sub-block: ({subX}, {subY})");
                _output.WriteLine("");

                if (i > 100) break; // Limit output
            }
        }
    }

    [Fact]
    public void CheckImageSharpTopRegion()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        using Image<L8> isImage = Image.Load<L8>(path);

        _output.WriteLine("ImageSharp pixel values at specific locations:");

        // Check top-left corner
        _output.WriteLine("\nTop-left 16x16 (MCU 0,0):");
        for (var y = 0; y < 16; y += 4)
        {
            var row = $"y={y,2}: ";
            for (var x = 0; x < 16; x += 4)
            {
                row += $"{isImage[x, y].PackedValue,4}";
            }
            _output.WriteLine(row);
        }

        // Check region around (16,8) - where first mismatch was
        _output.WriteLine("\n(16,8) region (MCU 1,0):");
        for (var y = 8; y < 16; y++)
        {
            var row = $"y={y,2}: ";
            for (var x = 16; x < 24; x++)
            {
                row += $"{isImage[x, y].PackedValue,4}";
            }
            _output.WriteLine(row);
        }

        // Find first ImageSharp pixel that's not 255
        _output.WriteLine("\nFirst non-255 ImageSharp pixel:");
        for (var y = 0; y < isImage.Height; y++)
        {
            for (var x = 0; x < isImage.Width; x++)
            {
                byte val = isImage[x, y].PackedValue;
                if (val < 250)
                {
                    _output.WriteLine($"Pixel ({x}, {y}) = {val}");
                    int blockX = x / 8;
                    int blockY = y / 8;
                    _output.WriteLine($"  Block: ({blockX}, {blockY})");
                    _output.WriteLine($"  MCU: ({blockX / 2}, {blockY / 2})");
                    return;
                }
            }
        }
        _output.WriteLine("All pixels are 255!");
    }

    [Fact]
    public void CompareSpecificRegion()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";

        DecodedImage ourImage = JpegDecoder.DecodeFile(path);
        using Image<L8> isImage = Image.Load<L8>(path);

        // Let's check where OUR image first has non-white
        _output.WriteLine("Finding first non-white pixel in OUR image:");
        for (var y = 0; y < ourImage.Height; y++)
        {
            for (var x = 0; x < ourImage.Width; x++)
            {
                (byte v, _, _) = ourImage.GetPixel(x, y);
                if (v < 250)
                {
                    _output.WriteLine($"Our first non-white: ({x}, {y}) = {v}");
                    int blockX = x / 8;
                    int blockY = y / 8;
                    _output.WriteLine($"  Block: ({blockX}, {blockY}), MCU: ({blockX / 2}, {blockY / 2})");
                    break;
                }
            }
            (byte v2, _, _) = ourImage.GetPixel(0, y);
            if (v2 < 250) break;
        }

        _output.WriteLine("\nFinding first non-white pixel in ImageSharp:");
        for (var y = 0; y < isImage.Height; y++)
        {
            for (var x = 0; x < isImage.Width; x++)
            {
                byte val = isImage[x, y].PackedValue;
                if (val < 250)
                {
                    _output.WriteLine($"IS first non-white: ({x}, {y}) = {val}");
                    int blockX = x / 8;
                    int blockY = y / 8;
                    _output.WriteLine($"  Block: ({blockX}, {blockY}), MCU: ({blockX / 2}, {blockY / 2})");
                    return;
                }
            }
        }
    }
}
