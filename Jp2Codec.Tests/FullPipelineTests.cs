using Jp2Codec.Pipeline;
using Xunit.Abstractions;

namespace Jp2Codec.Tests;

/// <summary>
/// End-to-end tests for the complete JPEG2000 decoder pipeline.
/// </summary>
public class FullPipelineTests
{
    private readonly ITestOutputHelper _output;

    public FullPipelineTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string GetTestImagesPath()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "TestImages", "jp2_test")))
        {
            dir = dir.Parent;
        }

        return dir != null
            ? Path.Combine(dir.FullName, "TestImages", "jp2_test")
            : "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jp2_test";
    }

    [Fact]
    public void Decoder_ParsesImage_8x8()
    {
        string path = Path.Combine(GetTestImagesPath(), "test_8x8.jp2");
        byte[] data = File.ReadAllBytes(path);

        var decoder = new Jp2Decoder(data);

        _output.WriteLine($"Image: {decoder.Width}x{decoder.Height}, {decoder.ComponentCount} components");
        _output.WriteLine($"Wavelet: {decoder.Codestream.CodingParameters.WaveletType}");
        _output.WriteLine($"Decomposition: {decoder.Codestream.CodingParameters.DecompositionLevels} levels");

        Assert.Equal(8, decoder.Width);
        Assert.Equal(8, decoder.Height);
        Assert.Equal(1, decoder.ComponentCount);
    }

    [Fact]
    public void Decoder_DecodesImage_8x8()
    {
        string path = Path.Combine(GetTestImagesPath(), "test_8x8.jp2");
        byte[] data = File.ReadAllBytes(path);

        var decoder = new Jp2Decoder(data);
        byte[] pixels = decoder.DecodeGrayscale();

        _output.WriteLine($"Decoded {pixels.Length} bytes");

        // Show decoded values
        _output.WriteLine("\nDecoded pixel values:");
        for (var y = 0; y < 8; y++)
        {
            string row = string.Join(" ", Enumerable.Range(0, 8)
                .Select(x => pixels[y * 8 + x].ToString().PadLeft(4)));
            _output.WriteLine(row);
        }

        Assert.Equal(64, pixels.Length);
    }

    [Fact]
    public void Decoder_ShowsIntermediateData_8x8()
    {
        string path = Path.Combine(GetTestImagesPath(), "test_8x8.jp2");
        byte[] data = File.ReadAllBytes(path);

        var decoder = new Jp2Decoder(data);
        IntermediateData intermediate = decoder.GetIntermediateData();

        _output.WriteLine("Intermediate data:");

        if (intermediate.Tier2Output != null)
        {
            _output.WriteLine($"  Tier-2: {intermediate.Tier2Output.ResolutionLevels} resolution levels");
            int totalBlocks = intermediate.Tier2Output.CodeBlocks.Sum(r => r.Sum(s => s.Length));
            _output.WriteLine($"          {totalBlocks} code-blocks");
        }

        if (intermediate.Subbands != null)
        {
            _output.WriteLine($"  Subbands: {intermediate.Subbands.Length}");
            foreach (QuantizedSubband sub in intermediate.Subbands)
            {
                _output.WriteLine($"    {sub.Type}: {sub.Width}x{sub.Height}");
            }
        }

        if (intermediate.DwtCoefficients != null)
        {
            _output.WriteLine($"  DWT: {intermediate.DwtCoefficients.Width}x{intermediate.DwtCoefficients.Height}");
            _output.WriteLine($"       {intermediate.DwtCoefficients.DecompositionLevels} decomposition levels");
        }
    }

    [Fact]
    public void Decoder_DecodesImage_16x16()
    {
        string path = Path.Combine(GetTestImagesPath(), "test_16x16.jp2");
        byte[] data = File.ReadAllBytes(path);

        var decoder = new Jp2Decoder(data);
        byte[] pixels = decoder.DecodeGrayscale();

        _output.WriteLine($"Image: {decoder.Width}x{decoder.Height}");
        _output.WriteLine($"Decomposition: {decoder.Codestream.CodingParameters.DecompositionLevels} levels");
        _output.WriteLine($"Decoded {pixels.Length} bytes");

        // Show a subset
        _output.WriteLine("\nTop-left 8x8 of decoded image:");
        for (var y = 0; y < Math.Min(8, decoder.Height); y++)
        {
            string row = string.Join(" ", Enumerable.Range(0, Math.Min(8, decoder.Width))
                .Select(x => pixels[y * decoder.Width + x].ToString().PadLeft(4)));
            _output.WriteLine(row);
        }

        Assert.Equal(256, pixels.Length);
    }

    [Fact]
    public void Decoder_DecodesConformanceImage()
    {
        string path = Path.Combine(GetTestImagesPath(), "conformance_test.jp2");
        if (!File.Exists(path))
        {
            _output.WriteLine("Conformance test file not found, skipping");
            return;
        }

        byte[] data = File.ReadAllBytes(path);

        var decoder = new Jp2Decoder(data);

        _output.WriteLine($"Conformance image:");
        _output.WriteLine($"  Size: {decoder.Width}x{decoder.Height}");
        _output.WriteLine($"  Components: {decoder.ComponentCount}");
        _output.WriteLine($"  Wavelet: {decoder.Codestream.CodingParameters.WaveletType}");
        _output.WriteLine($"  Decomposition: {decoder.Codestream.CodingParameters.DecompositionLevels} levels");

        // Try to decode
        byte[] pixels = decoder.Decode();

        _output.WriteLine($"  Decoded: {pixels.Length} bytes");

        Assert.True(pixels.Length > 0);
    }
}
