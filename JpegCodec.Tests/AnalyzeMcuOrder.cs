using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;
using Xunit.Abstractions;

namespace JpegCodec.Tests;

/// <summary>
/// Analyzes MCU ordering to find the scrambling pattern.
/// </summary>
public class AnalyzeMcuOrder
{
    private readonly ITestOutputHelper _output;

    public AnalyzeMcuOrder(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void AnalyzeBlockOrderingPattern()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";

        DecodedImage ourImage = JpegDecoder.DecodeFile(path);
        using Image<L8> isImage = Image.Load<L8>(path);

        // For each 8x8 block in our image, find where that block appears in ImageSharp
        // This will reveal the scrambling pattern

        _output.WriteLine("Analyzing block ordering pattern...");
        _output.WriteLine("For each block in OUR image, finding its location in ImageSharp");
        _output.WriteLine("");

        int blocksX = (ourImage.Width + 7) / 8;
        int blocksY = (ourImage.Height + 7) / 8;

        // Just check first few rows of blocks
        for (var ourBlockY = 0; ourBlockY < Math.Min(4, blocksY); ourBlockY++)
        {
            for (var ourBlockX = 0; ourBlockX < Math.Min(8, blocksX); ourBlockX++)
            {
                // Get the signature (average and variance) of our block
                (double avg, double variance, byte[] samples) ourSig = GetBlockSignature(ourImage, ourBlockX * 8, ourBlockY * 8);

                // Find matching block in ImageSharp
                (int x, int y)? match = FindMatchingBlock(isImage, ourSig, blocksX, blocksY);

                string matchStr = match.HasValue
                    ? $"IS block ({match.Value.x}, {match.Value.y})"
                    : "NO MATCH";

                _output.WriteLine($"Our block ({ourBlockX,2}, {ourBlockY,2}) -> {matchStr}");
            }
            _output.WriteLine("");
        }
    }

    private (double avg, double variance, byte[] samples) GetBlockSignature(DecodedImage img, int startX, int startY)
    {
        var samples = new byte[64];
        double sum = 0;
        var count = 0;

        for (var y = 0; y < 8 && startY + y < img.Height; y++)
        {
            for (var x = 0; x < 8 && startX + x < img.Width; x++)
            {
                (byte r, _, _) = img.GetPixel(startX + x, startY + y);
                samples[y * 8 + x] = r;
                sum += r;
                count++;
            }
        }

        double avg = count > 0 ? sum / count : 0;

        double variance = 0;
        for (var i = 0; i < count; i++)
        {
            variance += (samples[i] - avg) * (samples[i] - avg);
        }
        variance = count > 0 ? variance / count : 0;

        return (avg, variance, samples);
    }

    private (double avg, double variance, byte[] samples) GetBlockSignatureIS(Image<L8> img, int startX, int startY)
    {
        var samples = new byte[64];
        double sum = 0;
        var count = 0;

        for (var y = 0; y < 8 && startY + y < img.Height; y++)
        {
            for (var x = 0; x < 8 && startX + x < img.Width; x++)
            {
                byte val = img[startX + x, startY + y].PackedValue;
                samples[y * 8 + x] = val;
                sum += val;
                count++;
            }
        }

        double avg = count > 0 ? sum / count : 0;

        double variance = 0;
        for (var i = 0; i < count; i++)
        {
            variance += (samples[i] - avg) * (samples[i] - avg);
        }
        variance = count > 0 ? variance / count : 0;

        return (avg, variance, samples);
    }

    private (int x, int y)? FindMatchingBlock(Image<L8> img, (double avg, double variance, byte[] samples) target, int blocksX, int blocksY)
    {
        // If the block is uniform (low variance), skip matching - too many similar blocks
        if (target.variance < 100)
        {
            return null;
        }

        var bestScore = double.MaxValue;
        (int x, int y)? bestMatch = null;

        for (var blockY = 0; blockY < blocksY; blockY++)
        {
            for (var blockX = 0; blockX < blocksX; blockX++)
            {
                (double avg, double variance, byte[] samples) sig = GetBlockSignatureIS(img, blockX * 8, blockY * 8);

                // Skip if variance is very different
                if (Math.Abs(sig.variance - target.variance) > target.variance * 0.5)
                    continue;

                // Calculate sample-by-sample difference
                double score = 0;
                for (var i = 0; i < 64; i++)
                {
                    double diff = sig.samples[i] - target.samples[i];
                    score += diff * diff;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    bestMatch = (blockX, blockY);
                }
            }
        }

        // Only return if score is very low (good match)
        if (bestScore < 1000)
        {
            return bestMatch;
        }

        return null;
    }

    [Fact]
    public void CompareSpecificMCU()
    {
        // Look at a specific MCU that has content (not just white)
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";

        DecodedImage ourImage = JpegDecoder.DecodeFile(path);
        using Image<L8> isImage = Image.Load<L8>(path);

        // MCU at position (5, 10) should have some actual image content
        int mcuX = 5, mcuY = 10;
        int pixelX = mcuX * 16, pixelY = mcuY * 16;

        _output.WriteLine($"MCU ({mcuX}, {mcuY}) - pixels starting at ({pixelX}, {pixelY})");
        _output.WriteLine("");

        // Show all 4 blocks within this MCU
        for (var by = 0; by < 2; by++)
        {
            for (var bx = 0; bx < 2; bx++)
            {
                int blockPixelX = pixelX + bx * 8;
                int blockPixelY = pixelY + by * 8;

                _output.WriteLine($"Block ({bx}, {by}) at pixel ({blockPixelX}, {blockPixelY}):");
                _output.WriteLine("OUR:");
                for (var y = 0; y < 8; y++)
                {
                    var row = "  ";
                    for (var x = 0; x < 8; x++)
                    {
                        (byte v, _, _) = ourImage.GetPixel(blockPixelX + x, blockPixelY + y);
                        row += $"{v,4}";
                    }
                    _output.WriteLine(row);
                }

                _output.WriteLine("ImageSharp:");
                for (var y = 0; y < 8; y++)
                {
                    var row = "  ";
                    for (var x = 0; x < 8; x++)
                    {
                        row += $"{isImage[blockPixelX + x, blockPixelY + y].PackedValue,4}";
                    }
                    _output.WriteLine(row);
                }
                _output.WriteLine("");
            }
        }
    }
}
