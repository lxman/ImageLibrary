using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;
using Xunit.Abstractions;

namespace JpegCodec.Tests;

public class CompareDeepBlock
{
    private readonly ITestOutputHelper _output;

    public CompareDeepBlock(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void FindNonWhiteBlock()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";

        using var isImage = Image.Load<L8>(path);

        // Find first block that's not all white
        int blocksX = (isImage.Width + 7) / 8;
        int blocksY = (isImage.Height + 7) / 8;

        for (int by = 0; by < blocksY; by++)
        {
            for (int bx = 0; bx < blocksX; bx++)
            {
                int startX = bx * 8;
                int startY = by * 8;

                bool allWhite = true;
                for (int y = 0; y < 8 && startY + y < isImage.Height && allWhite; y++)
                {
                    for (int x = 0; x < 8 && startX + x < isImage.Width && allWhite; x++)
                    {
                        if (isImage[startX + x, startY + y].PackedValue < 250)
                        {
                            allWhite = false;
                        }
                    }
                }

                if (!allWhite)
                {
                    _output.WriteLine($"First non-white block at ({bx}, {by}), pixel ({startX}, {startY})");
                    _output.WriteLine($"MCU: ({bx / 2}, {by / 2})");

                    // Show ImageSharp values
                    _output.WriteLine("\nImageSharp values:");
                    for (int y = 0; y < 8; y++)
                    {
                        string row = "  ";
                        for (int x = 0; x < 8; x++)
                        {
                            if (startX + x < isImage.Width && startY + y < isImage.Height)
                                row += $"{isImage[startX + x, startY + y].PackedValue,4}";
                        }
                        _output.WriteLine(row);
                    }

                    // Show our values
                    var ourImage = JpegDecoder.DecodeFile(path);
                    _output.WriteLine("\nOur values:");
                    for (int y = 0; y < 8; y++)
                    {
                        string row = "  ";
                        for (int x = 0; x < 8; x++)
                        {
                            if (startX + x < ourImage.Width && startY + y < ourImage.Height)
                            {
                                var (v, _, _) = ourImage.GetPixel(startX + x, startY + y);
                                row += $"{v,4}";
                            }
                        }
                        _output.WriteLine(row);
                    }

                    // Calculate block index
                    int blocksPerRow = 38; // From our calculation
                    int blockIndex = by * blocksPerRow + bx;
                    _output.WriteLine($"\nBlock index: {blockIndex}");

                    return;
                }
            }
        }
    }

    [Fact]
    public void CompareEntireFirstRowOfBlocks()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";

        var ourImage = JpegDecoder.DecodeFile(path);
        using var isImage = Image.Load<L8>(path);

        // For each block in first row, compare sum of pixels
        _output.WriteLine("Block-by-block comparison, first row (y=0):");
        _output.WriteLine("BlockX | OurSum | ISSum  | Match?");
        _output.WriteLine("-------|--------|--------|-------");

        int blocksPerRow = 38;
        for (int bx = 0; bx < blocksPerRow; bx++)
        {
            int startX = bx * 8;
            int ourSum = 0, isSum = 0;

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    if (startX + x < ourImage.Width && y < ourImage.Height)
                    {
                        var (v, _, _) = ourImage.GetPixel(startX + x, y);
                        ourSum += v;
                        isSum += isImage[startX + x, y].PackedValue;
                    }
                }
            }

            bool match = Math.Abs(ourSum - isSum) < 100;
            _output.WriteLine($"  {bx,4} | {ourSum,6} | {isSum,6} | {(match ? "YES" : "NO")}");
        }
    }

    [Fact]
    public void CompareRowByRow()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";

        var ourImage = JpegDecoder.DecodeFile(path);
        using var isImage = Image.Load<L8>(path);

        // For each row of blocks, compare and find where they diverge
        _output.WriteLine("Row-by-row comparison (block rows):");
        _output.WriteLine("Row | OurAvg | ISAvg  | Diff");
        _output.WriteLine("----|--------|--------|-----");

        int blocksPerRow = 38;
        int blockRows = 40;

        for (int blockRow = 0; blockRow < blockRows; blockRow++)
        {
            int startY = blockRow * 8;
            double ourSum = 0, isSum = 0;
            int count = 0;

            for (int x = 0; x < ourImage.Width; x++)
            {
                for (int y = 0; y < 8 && startY + y < ourImage.Height; y++)
                {
                    var (v, _, _) = ourImage.GetPixel(x, startY + y);
                    ourSum += v;
                    isSum += isImage[x, startY + y].PackedValue;
                    count++;
                }
            }

            double ourAvg = ourSum / count;
            double isAvg = isSum / count;
            double diff = Math.Abs(ourAvg - isAvg);

            _output.WriteLine($" {blockRow,2} | {ourAvg,6:F1} | {isAvg,6:F1} | {diff,5:F1}{(diff > 5 ? " ***" : "")}");
        }
    }
}
