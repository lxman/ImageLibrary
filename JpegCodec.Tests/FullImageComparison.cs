using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;
using Xunit.Abstractions;

namespace JpegCodec.Tests;

public class FullImageComparison
{
    private readonly ITestOutputHelper _output;

    public FullImageComparison(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void CompareEntireBackhoe()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";

        var ourImage = JpegDecoder.DecodeFile(path);
        using var isImage = Image.Load<L8>(path);

        _output.WriteLine($"Image dimensions: {ourImage.Width}x{ourImage.Height}");

        int totalPixels = 0;
        int matchingPixels = 0;
        int maxDiff = 0;
        int diffSum = 0;

        for (int y = 0; y < ourImage.Height; y++)
        {
            for (int x = 0; x < ourImage.Width; x++)
            {
                var (ourVal, _, _) = ourImage.GetPixel(x, y);
                byte isVal = isImage[x, y].PackedValue;

                int diff = Math.Abs(ourVal - isVal);
                diffSum += diff;
                maxDiff = Math.Max(maxDiff, diff);

                if (diff <= 1)
                {
                    matchingPixels++;
                }

                totalPixels++;
            }
        }

        double avgDiff = (double)diffSum / totalPixels;
        double matchPercent = 100.0 * matchingPixels / totalPixels;

        _output.WriteLine($"Total pixels: {totalPixels}");
        _output.WriteLine($"Matching (diff <= 1): {matchingPixels} ({matchPercent:F2}%)");
        _output.WriteLine($"Max difference: {maxDiff}");
        _output.WriteLine($"Average difference: {avgDiff:F3}");
    }

    [Fact]
    public void CompareFirstMismatch()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";

        var ourImage = JpegDecoder.DecodeFile(path);
        using var isImage = Image.Load<L8>(path);

        _output.WriteLine("Looking for first significant mismatch (diff > 5)...");

        for (int y = 0; y < ourImage.Height; y++)
        {
            for (int x = 0; x < ourImage.Width; x++)
            {
                var (ourVal, _, _) = ourImage.GetPixel(x, y);
                byte isVal = isImage[x, y].PackedValue;

                int diff = Math.Abs(ourVal - isVal);
                if (diff > 5)
                {
                    _output.WriteLine($"First mismatch at ({x}, {y}): ours={ourVal}, IS={isVal}, diff={diff}");
                    return;
                }
            }
        }

        _output.WriteLine("No significant mismatch found!");
    }
}
