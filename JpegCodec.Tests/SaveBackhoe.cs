using Xunit;
using Xunit.Abstractions;

namespace JpegCodec.Tests;

public class SaveBackhoe
{
    private readonly ITestOutputHelper _output;

    public SaveBackhoe(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void SaveDecodedBackhoe()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        var outputPath = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-decoded.ppm";

        DecodedImage image = JpegDecoder.DecodeFile(path);

        _output.WriteLine($"Decoded: {image.Width}x{image.Height}");

        // Save as PPM
        using FileStream file = File.Create(outputPath);
        using var writer = new StreamWriter(file);

        writer.WriteLine("P3");
        writer.WriteLine($"{image.Width} {image.Height}");
        writer.WriteLine("255");

        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                (byte r, byte g, byte b) = image.GetPixel(x, y);
                writer.Write($"{r} {g} {b} ");
            }
            writer.WriteLine();
        }

        _output.WriteLine($"Saved to: {outputPath}");
    }
}
