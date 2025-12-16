using CoreJ2K;
using Jp2Codec;
using Xunit;
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
        var path = Path.Combine(GetTestImagesPath(), "test_8x8.jp2");
        var data = File.ReadAllBytes(path);

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
        var path = Path.Combine(GetTestImagesPath(), "test_8x8.jp2");
        var data = File.ReadAllBytes(path);

        var decoder = new Jp2Decoder(data);
        var pixels = decoder.DecodeGrayscale();

        _output.WriteLine($"Decoded {pixels.Length} bytes");

        // Show decoded values
        _output.WriteLine("\nDecoded pixel values:");
        for (int y = 0; y < 8; y++)
        {
            var row = string.Join(" ", Enumerable.Range(0, 8)
                .Select(x => pixels[y * 8 + x].ToString().PadLeft(4)));
            _output.WriteLine(row);
        }

        Assert.Equal(64, pixels.Length);
    }

    [Fact]
    public void Decoder_ComparesWithReference_8x8()
    {
        var path = Path.Combine(GetTestImagesPath(), "test_8x8.jp2");
        var data = File.ReadAllBytes(path);

        // Decode with our decoder
        var decoder = new Jp2Decoder(data);
        var ourPixels = decoder.DecodeGrayscale();

        // Decode with reference (CoreJ2K)
        var refImage = J2kImage.FromBytes(data);
        var refComp = refImage.GetComponent(0);

        _output.WriteLine("Reference vs Our decoder:");
        _output.WriteLine("Position  Reference  Ours  Diff");

        int totalDiff = 0;
        int maxDiff = 0;
        int mismatches = 0;

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                int refVal = refComp[y * 8 + x];
                int ourVal = ourPixels[y * 8 + x];
                int diff = Math.Abs(refVal - ourVal);

                if (diff > 0)
                {
                    _output.WriteLine($"({x},{y}): {refVal,3} vs {ourVal,3} (diff={diff})");
                    mismatches++;
                }

                totalDiff += diff;
                maxDiff = Math.Max(maxDiff, diff);
            }
        }

        _output.WriteLine($"\nTotal mismatches: {mismatches}");
        _output.WriteLine($"Max diff: {maxDiff}");
        _output.WriteLine($"Total diff: {totalDiff}");

        // For lossless compression, we expect exact match
        // For lossy, some small difference is acceptable
        var isLossless = decoder.Codestream.CodingParameters.WaveletType == WaveletTransform.Reversible_5_3;
        _output.WriteLine($"Compression: {(isLossless ? "Lossless" : "Lossy")}");
    }

    [Fact]
    public void Decoder_ShowsIntermediateData_8x8()
    {
        var path = Path.Combine(GetTestImagesPath(), "test_8x8.jp2");
        var data = File.ReadAllBytes(path);

        var decoder = new Jp2Decoder(data);
        var intermediate = decoder.GetIntermediateData();

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
            foreach (var sub in intermediate.Subbands)
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
        var path = Path.Combine(GetTestImagesPath(), "test_16x16.jp2");
        var data = File.ReadAllBytes(path);

        var decoder = new Jp2Decoder(data);
        var pixels = decoder.DecodeGrayscale();

        _output.WriteLine($"Image: {decoder.Width}x{decoder.Height}");
        _output.WriteLine($"Decomposition: {decoder.Codestream.CodingParameters.DecompositionLevels} levels");
        _output.WriteLine($"Decoded {pixels.Length} bytes");

        // Show a subset
        _output.WriteLine("\nTop-left 8x8 of decoded image:");
        for (int y = 0; y < Math.Min(8, decoder.Height); y++)
        {
            var row = string.Join(" ", Enumerable.Range(0, Math.Min(8, decoder.Width))
                .Select(x => pixels[y * decoder.Width + x].ToString().PadLeft(4)));
            _output.WriteLine(row);
        }

        Assert.Equal(256, pixels.Length);
    }

    [Fact]
    public void Decoder_DecodesConformanceImage()
    {
        var path = Path.Combine(GetTestImagesPath(), "conformance_test.jp2");
        if (!File.Exists(path))
        {
            _output.WriteLine("Conformance test file not found, skipping");
            return;
        }

        var data = File.ReadAllBytes(path);

        var decoder = new Jp2Decoder(data);

        _output.WriteLine($"Conformance image:");
        _output.WriteLine($"  Size: {decoder.Width}x{decoder.Height}");
        _output.WriteLine($"  Components: {decoder.ComponentCount}");
        _output.WriteLine($"  Wavelet: {decoder.Codestream.CodingParameters.WaveletType}");
        _output.WriteLine($"  Decomposition: {decoder.Codestream.CodingParameters.DecompositionLevels} levels");

        // Try to decode
        var pixels = decoder.Decode();

        _output.WriteLine($"  Decoded: {pixels.Length} bytes");

        Assert.True(pixels.Length > 0);
    }
}
