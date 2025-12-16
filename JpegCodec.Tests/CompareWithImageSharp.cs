using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;
using Xunit.Abstractions;

namespace JpegCodec.Tests;

/// <summary>
/// Compares our decoder output with ImageSharp's output to find discrepancies.
/// </summary>
public class CompareWithImageSharp
{
    private readonly ITestOutputHelper _output;

    public CompareWithImageSharp(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void CompareBackhoe_PixelByPixel()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";

        // Decode with our decoder
        var ourImage = JpegDecoder.DecodeFile(path);
        _output.WriteLine($"Our decoder: {ourImage.Width}x{ourImage.Height}");

        // Decode with ImageSharp
        using var isImage = Image.Load<L8>(path); // L8 = grayscale
        _output.WriteLine($"ImageSharp: {isImage.Width}x{isImage.Height}");

        // Compare dimensions
        Assert.Equal(isImage.Width, ourImage.Width);
        Assert.Equal(isImage.Height, ourImage.Height);

        // Find first mismatch
        int mismatches = 0;
        int firstMismatchX = -1, firstMismatchY = -1;

        for (int y = 0; y < ourImage.Height && mismatches < 100; y++)
        {
            for (int x = 0; x < ourImage.Width && mismatches < 100; x++)
            {
                var (ourR, _, _) = ourImage.GetPixel(x, y);
                byte isVal = isImage[x, y].PackedValue;

                int diff = Math.Abs(ourR - isVal);
                if (diff > 2) // Allow small rounding differences
                {
                    if (firstMismatchX < 0)
                    {
                        firstMismatchX = x;
                        firstMismatchY = y;
                    }
                    mismatches++;
                    if (mismatches <= 20)
                    {
                        _output.WriteLine($"Mismatch at ({x},{y}): ours={ourR}, ImageSharp={isVal}, diff={diff}");
                    }
                }
            }
        }

        _output.WriteLine($"Total mismatches (first 100 checked): {mismatches}");

        if (firstMismatchX >= 0)
        {
            // Analyze the area around first mismatch
            _output.WriteLine($"\nFirst mismatch at ({firstMismatchX}, {firstMismatchY})");
            _output.WriteLine($"Block position: ({firstMismatchX / 8}, {firstMismatchY / 8})");
            _output.WriteLine($"MCU position (2x2): ({firstMismatchX / 16}, {firstMismatchY / 16})");
        }

        // Sample specific blocks to understand the pattern
        _output.WriteLine("\n=== Block-by-block comparison (first 4 MCUs) ===");
        for (int mcuY = 0; mcuY < 2; mcuY++)
        {
            for (int mcuX = 0; mcuX < 2; mcuX++)
            {
                _output.WriteLine($"\nMCU ({mcuX}, {mcuY}):");
                for (int blockY = 0; blockY < 2; blockY++)
                {
                    for (int blockX = 0; blockX < 2; blockX++)
                    {
                        int pixelX = mcuX * 16 + blockX * 8;
                        int pixelY = mcuY * 16 + blockY * 8;

                        if (pixelX < ourImage.Width && pixelY < ourImage.Height)
                        {
                            var (ourVal, _, _) = ourImage.GetPixel(pixelX, pixelY);
                            byte isVal = isImage[pixelX, pixelY].PackedValue;
                            string match = Math.Abs(ourVal - isVal) <= 2 ? "OK" : "DIFF";
                            _output.WriteLine($"  Block ({blockX},{blockY}) at pixel ({pixelX},{pixelY}): ours={ourVal}, IS={isVal} [{match}]");
                        }
                    }
                }
            }
        }
    }

    [Fact]
    public void FindOurPixelInImageSharp()
    {
        // For a specific pixel in our output, find where that value appears in ImageSharp
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";

        var ourImage = JpegDecoder.DecodeFile(path);
        using var isImage = Image.Load<L8>(path);

        // Get our pixel at (0,0)
        var (ourVal, _, _) = ourImage.GetPixel(0, 0);
        _output.WriteLine($"Our pixel at (0,0) = {ourVal}");
        _output.WriteLine($"ImageSharp pixel at (0,0) = {isImage[0, 0].PackedValue}");

        // Search for where our (0,0) value appears in ImageSharp
        _output.WriteLine($"\nSearching for value {ourVal} in ImageSharp (first 10 matches):");
        int found = 0;
        for (int y = 0; y < isImage.Height && found < 10; y++)
        {
            for (int x = 0; x < isImage.Width && found < 10; x++)
            {
                if (Math.Abs(isImage[x, y].PackedValue - ourVal) <= 1)
                {
                    _output.WriteLine($"  Found at ({x}, {y}) - block ({x/8}, {y/8}), MCU ({x/16}, {y/16})");
                    found++;
                }
            }
        }

        // Also check what block patterns look like
        _output.WriteLine("\n=== First 8x8 block comparison ===");
        _output.WriteLine("Our block at (0,0):");
        for (int y = 0; y < 8; y++)
        {
            string row = "  ";
            for (int x = 0; x < 8; x++)
            {
                var (v, _, _) = ourImage.GetPixel(x, y);
                row += $"{v,4}";
            }
            _output.WriteLine(row);
        }

        _output.WriteLine("\nImageSharp block at (0,0):");
        for (int y = 0; y < 8; y++)
        {
            string row = "  ";
            for (int x = 0; x < 8; x++)
            {
                row += $"{isImage[x, y].PackedValue,4}";
            }
            _output.WriteLine(row);
        }
    }

    [Fact]
    public void SaveBothImages_ForVisualComparison()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        var outDir = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test";

        // Save ImageSharp version
        using var isImage = Image.Load<L8>(path);
        isImage.SaveAsPng(Path.Combine(outDir, "backhoe-imagesharp.png"));
        _output.WriteLine($"Saved ImageSharp version to backhoe-imagesharp.png");

        // Our version is already saved by BackhoeDecodeTest
        _output.WriteLine($"Our version is at backhoe-decoded.png");

        // Create a diff image
        var ourImage = JpegDecoder.DecodeFile(path);
        using var diffImage = new Image<L8>(ourImage.Width, ourImage.Height);

        for (int y = 0; y < ourImage.Height; y++)
        {
            for (int x = 0; x < ourImage.Width; x++)
            {
                var (ourVal, _, _) = ourImage.GetPixel(x, y);
                byte isVal = isImage[x, y].PackedValue;
                int diff = Math.Abs(ourVal - isVal);
                diffImage[x, y] = new L8((byte)Math.Min(255, diff * 4)); // Amplify diff for visibility
            }
        }
        diffImage.SaveAsPng(Path.Combine(outDir, "backhoe-diff.png"));
        _output.WriteLine($"Saved diff image to backhoe-diff.png");
    }
}
