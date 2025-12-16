using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;
using Xunit.Abstractions;

namespace JpegCodec.Tests;

/// <summary>
/// Direct comparison of specific 8x8 blocks between our decoder and ImageSharp.
/// </summary>
public class DirectBlockCompare
{
    private readonly ITestOutputHelper _output;

    public DirectBlockCompare(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void CompareFirstMcuBlocks()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";

        var ourImage = JpegDecoder.DecodeFile(path);
        using var isImage = Image.Load<L8>(path);

        _output.WriteLine($"Image: {ourImage.Width}x{ourImage.Height}");
        _output.WriteLine("");

        // For each of the first 4 blocks (MCU 0), show which pixel region they cover
        // MCU 0 with 2x2 sampling covers pixels (0,0)-(15,15)
        // Block (0,0): pixels (0,0)-(7,7)
        // Block (1,0): pixels (8,0)-(15,7)
        // Block (0,1): pixels (0,8)-(7,15)
        // Block (1,1): pixels (8,8)-(15,15)

        var blockRegions = new[]
        {
            ("Block(0,0)", 0, 0),
            ("Block(1,0)", 8, 0),
            ("Block(0,1)", 0, 8),
            ("Block(1,1)", 8, 8),
        };

        foreach (var (name, startX, startY) in blockRegions)
        {
            _output.WriteLine($"=== {name} at pixel ({startX},{startY}) ===");

            // Calculate sums to see overall difference
            int ourSum = 0, isSum = 0;
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    var (v, _, _) = ourImage.GetPixel(startX + x, startY + y);
                    ourSum += v;
                    isSum += isImage[startX + x, startY + y].PackedValue;
                }
            }
            _output.WriteLine($"Sum: ours={ourSum}, IS={isSum}, diff={ourSum - isSum}");

            // Show first row of each block
            _output.WriteLine("First row:");
            string ourRow = "  Ours: ";
            string isRow = "  IS:   ";
            for (int x = 0; x < 8; x++)
            {
                var (v, _, _) = ourImage.GetPixel(startX + x, startY);
                ourRow += $"{v,4}";
                isRow += $"{isImage[startX + x, startY].PackedValue,4}";
            }
            _output.WriteLine(ourRow);
            _output.WriteLine(isRow);
            _output.WriteLine("");
        }
    }

    [Fact]
    public void CompareSecondMcuRow()
    {
        // MCU row 1 (mcuY=1) covers pixel rows 16-31
        // This is where the comparison showed differences
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";

        var ourImage = JpegDecoder.DecodeFile(path);
        using var isImage = Image.Load<L8>(path);

        _output.WriteLine("MCU row 1 (pixel rows 16-31):");
        _output.WriteLine("");

        // First MCU in row 1 is at mcuX=0, mcuY=1
        // Its blocks cover:
        // Block (0,2): pixels (0,16)-(7,23)
        // Block (1,2): pixels (8,16)-(15,23)
        // Block (0,3): pixels (0,24)-(7,31)
        // Block (1,3): pixels (8,24)-(15,31)

        var blockRegions = new[]
        {
            ("Block(0,2)", 0, 16),
            ("Block(1,2)", 8, 16),
            ("Block(0,3)", 0, 24),
            ("Block(1,3)", 8, 24),
        };

        foreach (var (name, startX, startY) in blockRegions)
        {
            _output.WriteLine($"=== {name} at pixel ({startX},{startY}) ===");

            int ourSum = 0, isSum = 0;
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    var (v, _, _) = ourImage.GetPixel(startX + x, startY + y);
                    ourSum += v;
                    isSum += isImage[startX + x, startY + y].PackedValue;
                }
            }
            _output.WriteLine($"Sum: ours={ourSum}, IS={isSum}, diff={ourSum - isSum}");
            _output.WriteLine("");
        }
    }

    [Fact]
    public void FindExactMismatch()
    {
        // Look for the first pixel that differs significantly
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";

        var ourImage = JpegDecoder.DecodeFile(path);
        using var isImage = Image.Load<L8>(path);

        _output.WriteLine("Looking for first significant mismatch...");

        for (int y = 0; y < Math.Min(40, ourImage.Height); y++)
        {
            for (int x = 0; x < Math.Min(40, ourImage.Width); x++)
            {
                var (ourVal, _, _) = ourImage.GetPixel(x, y);
                var isVal = isImage[x, y].PackedValue;
                int diff = Math.Abs(ourVal - isVal);

                if (diff > 20)
                {
                    int blockX = x / 8;
                    int blockY = y / 8;
                    int mcuX = blockX / 2;
                    int mcuY = blockY / 2;

                    _output.WriteLine($"Mismatch at pixel ({x},{y}): ours={ourVal}, IS={isVal}, diff={diff}");
                    _output.WriteLine($"  Block: ({blockX},{blockY}), MCU: ({mcuX},{mcuY})");
                    _output.WriteLine($"  Sub-block within MCU: ({blockX % 2},{blockY % 2})");
                    _output.WriteLine("");

                    // Show the full block
                    int baseX = blockX * 8;
                    int baseY = blockY * 8;
                    _output.WriteLine($"Full block at ({baseX},{baseY}):");
                    _output.WriteLine("Ours:");
                    for (int by = 0; by < 8; by++)
                    {
                        string row = "  ";
                        for (int bx = 0; bx < 8; bx++)
                        {
                            var (v, _, _) = ourImage.GetPixel(baseX + bx, baseY + by);
                            row += $"{v,4}";
                        }
                        _output.WriteLine(row);
                    }
                    _output.WriteLine("ImageSharp:");
                    for (int by = 0; by < 8; by++)
                    {
                        string row = "  ";
                        for (int bx = 0; bx < 8; bx++)
                        {
                            row += $"{isImage[baseX + bx, baseY + by].PackedValue,4}";
                        }
                        _output.WriteLine(row);
                    }

                    return;
                }
            }
        }

        _output.WriteLine("No significant mismatch found in first 40x40 pixels");
    }

    [Fact]
    public void ShowBlockIndexMapping()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        _output.WriteLine($"Image: {frame.Width}x{frame.Height}");
        _output.WriteLine($"MCUs: {frame.McuCountX}x{frame.McuCountY}");
        _output.WriteLine($"Sampling: {frame.Components[0].HorizontalSamplingFactor}x{frame.Components[0].VerticalSamplingFactor}");
        _output.WriteLine("");

        int hSamp = frame.Components[0].HorizontalSamplingFactor;
        int vSamp = frame.Components[0].VerticalSamplingFactor;
        int blocksPerRow = (frame.Width + frame.MaxHorizontalSamplingFactor * 8 - 1)
                          / (frame.MaxHorizontalSamplingFactor * 8) * hSamp;

        _output.WriteLine($"Blocks per row: {blocksPerRow}");
        _output.WriteLine("");

        // Show how EntropyDecoder assigns block indices for first 2 MCUs
        _output.WriteLine("EntropyDecoder block assignment for first 2 MCUs:");
        for (int mcuY = 0; mcuY < 2; mcuY++)
        {
            for (int mcuX = 0; mcuX < 2; mcuX++)
            {
                _output.WriteLine($"\nMCU ({mcuX},{mcuY}):");
                int decodeOrder = 0;
                for (int blockY = 0; blockY < vSamp; blockY++)
                {
                    for (int blockX = 0; blockX < hSamp; blockX++)
                    {
                        int globalBlockX = mcuX * hSamp + blockX;
                        int globalBlockY = mcuY * vSamp + blockY;
                        int blockIndex = globalBlockY * blocksPerRow + globalBlockX;
                        int pixelX = globalBlockX * 8;
                        int pixelY = globalBlockY * 8;
                        _output.WriteLine($"  Decode #{decodeOrder}: sub-block ({blockX},{blockY}) -> global ({globalBlockX},{globalBlockY}) -> index {blockIndex} -> pixels ({pixelX},{pixelY})");
                        decodeOrder++;
                    }
                }
            }
        }

        _output.WriteLine("");
        _output.WriteLine("ColorConverter lookup for specific pixels:");
        var pixels = new[] { (0, 0), (8, 0), (0, 8), (8, 8), (16, 0), (16, 8), (0, 16), (8, 16) };
        foreach (var (px, py) in pixels)
        {
            int blockX = px / 8;
            int blockY = py / 8;
            int blockIndex = blockY * blocksPerRow + blockX;
            _output.WriteLine($"  Pixel ({px},{py}) -> block ({blockX},{blockY}) -> index {blockIndex}");
        }
    }
}
