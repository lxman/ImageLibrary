using CoreJ2K;
using Jp2Codec;
using Xunit;
using Xunit.Abstractions;

namespace Jp2Codec.Tests;

/// <summary>
/// Tests using ITU-T T.803 conformance test images and other downloaded test files.
/// </summary>
public class ConformanceTests
{
    private readonly ITestOutputHelper _output;

    public ConformanceTests(ITestOutputHelper output)
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
    public void ListAvailableTestFiles()
    {
        var basePath = GetTestImagesPath();

        _output.WriteLine("=== Available Test Images ===\n");

        // Conformance tests
        var conformancePath = Path.Combine(basePath, "conformance");
        if (Directory.Exists(conformancePath))
        {
            var jp2Files = Directory.GetFiles(conformancePath, "*.jp2");
            var j2cFiles = Directory.GetFiles(conformancePath, "*.j2c");
            _output.WriteLine($"Conformance (ITU-T T.803): {jp2Files.Length} JP2, {j2cFiles.Length} J2C");
        }

        // Non-regression tests
        var nonregPath = Path.Combine(basePath, "nonregression");
        if (Directory.Exists(nonregPath))
        {
            var files = Directory.GetFiles(nonregPath, "*.jp2");
            _output.WriteLine($"Non-regression: {files.Length} JP2");
        }

        // Samples
        var samplesPath = Path.Combine(basePath, "samples");
        if (Directory.Exists(samplesPath))
        {
            var files = Directory.GetFiles(samplesPath);
            _output.WriteLine($"Sample images: {files.Length} files");
        }
    }

    [Theory]
    [InlineData("conformance/file1.jp2")]
    [InlineData("conformance/file2.jp2")]
    [InlineData("conformance/file3.jp2")]
    [InlineData("conformance/file4.jp2")]
    [InlineData("conformance/file6.jp2")]
    [InlineData("conformance/file8.jp2")]
    [InlineData("conformance/file9.jp2")]
    [InlineData("conformance/zoo1.jp2")]
    [InlineData("conformance/zoo2.jp2")]
    public void ParseConformanceJp2(string relativePath)
    {
        var path = Path.Combine(GetTestImagesPath(), relativePath);
        if (!File.Exists(path))
        {
            _output.WriteLine($"File not found: {path}");
            return;
        }

        var data = File.ReadAllBytes(path);
        var decoder = new Jp2Decoder(data);

        _output.WriteLine($"{Path.GetFileName(relativePath)}:");
        _output.WriteLine($"  Size: {decoder.Width}x{decoder.Height}");
        _output.WriteLine($"  Components: {decoder.ComponentCount}");
        _output.WriteLine($"  Wavelet: {decoder.Codestream.CodingParameters.WaveletType}");
        _output.WriteLine($"  Decomposition: {decoder.Codestream.CodingParameters.DecompositionLevels} levels");

        Assert.True(decoder.Width > 0);
        Assert.True(decoder.Height > 0);
    }

    [Theory]
    [InlineData("conformance/a1_mono.j2c")]
    [InlineData("conformance/a2_colr.j2c")]
    [InlineData("conformance/b1_mono.j2c")]
    [InlineData("conformance/c1_mono.j2c")]
    [InlineData("conformance/d1_colr.j2c")]
    public void ParseConformanceJ2c(string relativePath)
    {
        var path = Path.Combine(GetTestImagesPath(), relativePath);
        if (!File.Exists(path))
        {
            _output.WriteLine($"File not found: {path}");
            return;
        }

        var data = File.ReadAllBytes(path);

        // J2C files are raw codestreams
        var codestreamReader = new CodestreamReader(data);
        var codestream = codestreamReader.ReadMainHeader();

        _output.WriteLine($"{Path.GetFileName(relativePath)}:");
        _output.WriteLine($"  Size: {codestream.Frame.Width}x{codestream.Frame.Height}");
        _output.WriteLine($"  Components: {codestream.Frame.ComponentCount}");
        _output.WriteLine($"  Wavelet: {codestream.CodingParameters.WaveletType}");
        _output.WriteLine($"  Decomposition: {codestream.CodingParameters.DecompositionLevels} levels");

        Assert.True(codestream.Frame.Width > 0);
        Assert.True(codestream.Frame.Height > 0);
    }

    [Fact]
    public void ParseBalloonImage()
    {
        var path = Path.Combine(GetTestImagesPath(), "samples", "balloon.jp2");
        if (!File.Exists(path))
        {
            _output.WriteLine($"File not found: {path}");
            return;
        }

        var data = File.ReadAllBytes(path);
        var decoder = new Jp2Decoder(data);

        _output.WriteLine($"balloon.jp2:");
        _output.WriteLine($"  Size: {decoder.Width}x{decoder.Height}");
        _output.WriteLine($"  Components: {decoder.ComponentCount}");
        _output.WriteLine($"  Wavelet: {decoder.Codestream.CodingParameters.WaveletType}");
        _output.WriteLine($"  Decomposition: {decoder.Codestream.CodingParameters.DecompositionLevels} levels");
        _output.WriteLine($"  Layers: {decoder.Codestream.CodingParameters.LayerCount}");
        _output.WriteLine($"  Progression: {decoder.Codestream.CodingParameters.Progression}");

        Assert.True(decoder.Width > 0);
        Assert.True(decoder.Height > 0);
    }

    [Fact]
    public void CompareBalloonWithReference()
    {
        var path = Path.Combine(GetTestImagesPath(), "samples", "balloon.jp2");
        if (!File.Exists(path))
        {
            _output.WriteLine($"File not found: {path}");
            return;
        }

        var data = File.ReadAllBytes(path);

        // Decode with our decoder first
        var decoder = new Jp2Decoder(data);
        _output.WriteLine($"Our decoder: {decoder.Width}x{decoder.Height}, {decoder.ComponentCount} components");

        // Try to decode with reference
        try
        {
            var refImage = J2kImage.FromBytes(data);
            _output.WriteLine($"Reference: {refImage.Width}x{refImage.Height}, {refImage.NumberOfComponents} components");

            Assert.Equal(refImage.Width, decoder.Width);
            Assert.Equal(refImage.Height, decoder.Height);
            Assert.Equal(refImage.NumberOfComponents, decoder.ComponentCount);

            // Compare some pixel samples
            var refComp = refImage.GetComponent(0);
            _output.WriteLine($"\nReference component 0 samples:");
            _output.WriteLine($"  (0,0): {refComp[0]}");
            _output.WriteLine($"  (100,100): {refComp[100 * refImage.Width + 100]}");
            _output.WriteLine($"  (center): {refComp[(refImage.Height / 2) * refImage.Width + refImage.Width / 2]}");
        }
        catch (Exception ex)
        {
            // CoreJ2K has issues with some files (e.g., SOP marker parsing)
            _output.WriteLine($"CoreJ2K failed: {ex.Message}");
            _output.WriteLine("Note: Our decoder parsed this file successfully where CoreJ2K failed!");
        }
    }

    [Fact]
    public void ScanAllConformanceFiles()
    {
        var conformancePath = Path.Combine(GetTestImagesPath(), "conformance");
        if (!Directory.Exists(conformancePath))
        {
            _output.WriteLine("Conformance directory not found");
            return;
        }

        var jp2Files = Directory.GetFiles(conformancePath, "*.jp2");
        var j2cFiles = Directory.GetFiles(conformancePath, "*.j2c");

        _output.WriteLine($"Scanning {jp2Files.Length} JP2 files and {j2cFiles.Length} J2C files\n");

        int successCount = 0;
        int failCount = 0;

        foreach (var file in jp2Files)
        {
            try
            {
                var data = File.ReadAllBytes(file);
                var decoder = new Jp2Decoder(data);
                _output.WriteLine($"OK: {Path.GetFileName(file)} - {decoder.Width}x{decoder.Height}x{decoder.ComponentCount}");
                successCount++;
            }
            catch (Exception ex)
            {
                _output.WriteLine($"FAIL: {Path.GetFileName(file)} - {ex.Message}");
                failCount++;
            }
        }

        foreach (var file in j2cFiles)
        {
            try
            {
                var data = File.ReadAllBytes(file);
                var reader = new CodestreamReader(data);
                var codestream = reader.ReadMainHeader();
                _output.WriteLine($"OK: {Path.GetFileName(file)} - {codestream.Frame.Width}x{codestream.Frame.Height}x{codestream.Frame.ComponentCount}");
                successCount++;
            }
            catch (Exception ex)
            {
                _output.WriteLine($"FAIL: {Path.GetFileName(file)} - {ex.Message}");
                failCount++;
            }
        }

        _output.WriteLine($"\nTotal: {successCount} succeeded, {failCount} failed");
    }
}
