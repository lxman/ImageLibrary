using CoreJ2K;
using Xunit;
using Xunit.Abstractions;

namespace Jp2Codec.Tests;

/// <summary>
/// Tests comparing our decoder output against Melville.CSJ2K reference implementation.
/// </summary>
public class ReferenceComparisonTests
{
    private readonly ITestOutputHelper _output;

    public ReferenceComparisonTests(ITestOutputHelper output)
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

    /// <summary>
    /// Helper to get a pixel value from PortableImage using GetComponent.
    /// </summary>
    private static int GetPixel(CoreJ2K.Util.PortableImage image, int component, int x, int y)
    {
        var componentData = image.GetComponent(component);
        int pixelIndex = y * image.Width + x;
        return componentData[pixelIndex];
    }

    [Fact]
    public void DecodeWithReference_8x8()
    {
        var path = Path.Combine(GetTestImagesPath(), "test_8x8.jp2");
        var data = File.ReadAllBytes(path);

        // Decode with CoreJ2K
        var refImage = J2kImage.FromBytes(data);

        _output.WriteLine($"Reference decode: {refImage.Width}x{refImage.Height}, {refImage.NumberOfComponents} components");

        // Show some pixel values
        var comp0 = refImage.GetComponent(0);
        _output.WriteLine("Reference pixel values (first row):");
        var row = string.Join(" ", Enumerable.Range(0, Math.Min(8, refImage.Width)).Select(x => comp0[x].ToString()));
        _output.WriteLine(row);

        // Compare with our parsed header info
        var fileReader = new Jp2FileReader(data);
        var fileInfo = fileReader.Read();
        var codestreamReader = new CodestreamReader(fileInfo.CodestreamData!);
        var codestream = codestreamReader.ReadMainHeader();

        _output.WriteLine($"\nOur parser: {codestream.Frame.Width}x{codestream.Frame.Height}, {codestream.Frame.ComponentCount} components");

        Assert.Equal(refImage.Width, codestream.Frame.Width);
        Assert.Equal(refImage.Height, codestream.Frame.Height);
        Assert.Equal(refImage.NumberOfComponents, codestream.Frame.ComponentCount);
    }

    [Fact]
    public void DecodeWithReference_16x16()
    {
        var path = Path.Combine(GetTestImagesPath(), "test_16x16.jp2");
        var data = File.ReadAllBytes(path);

        // Decode with CoreJ2K
        var refImage = J2kImage.FromBytes(data);

        _output.WriteLine($"Reference decode: {refImage.Width}x{refImage.Height}");
        _output.WriteLine($"Components: {refImage.NumberOfComponents}");

        // Show decomposition info from our parser
        var fileReader = new Jp2FileReader(data);
        var fileInfo = fileReader.Read();
        var codestreamReader = new CodestreamReader(fileInfo.CodestreamData!);
        var codestream = codestreamReader.ReadMainHeader();

        _output.WriteLine($"Decomposition levels: {codestream.CodingParameters.DecompositionLevels}");
        _output.WriteLine($"Wavelet: {codestream.CodingParameters.WaveletType}");

        Assert.Equal(refImage.Width, codestream.Frame.Width);
        Assert.Equal(refImage.Height, codestream.Frame.Height);
    }

    [Fact]
    public void DecodeWithReference_Conformance()
    {
        var path = Path.Combine(GetTestImagesPath(), "conformance_test.jp2");
        if (!File.Exists(path))
        {
            _output.WriteLine("Conformance test file not found, skipping");
            return;
        }

        var data = File.ReadAllBytes(path);

        // Decode with CoreJ2K
        var refImage = J2kImage.FromBytes(data);

        _output.WriteLine($"Reference decode: {refImage.Width}x{refImage.Height}, {refImage.NumberOfComponents} components");

        // Sample some pixels
        _output.WriteLine("\nSample pixels at (0,0), (100,100), (200,200):");
        for (int c = 0; c < refImage.NumberOfComponents; c++)
        {
            _output.WriteLine($"  Component {c}: ({GetPixel(refImage, c, 0, 0)}, {GetPixel(refImage, c, 100, 100)}, {GetPixel(refImage, c, 200, 200)})");
        }

        // Compare with our parsed header info
        var fileReader = new Jp2FileReader(data);
        var fileInfo = fileReader.Read();
        var codestreamReader = new CodestreamReader(fileInfo.CodestreamData!);
        var codestream = codestreamReader.ReadMainHeader();

        _output.WriteLine($"\nOur parser:");
        _output.WriteLine($"  Size: {codestream.Frame.Width}x{codestream.Frame.Height}");
        _output.WriteLine($"  Components: {codestream.Frame.ComponentCount}");
        _output.WriteLine($"  Decomposition: {codestream.CodingParameters.DecompositionLevels} levels");
        _output.WriteLine($"  Wavelet: {codestream.CodingParameters.WaveletType}");

        Assert.Equal(refImage.Width, codestream.Frame.Width);
        Assert.Equal(refImage.Height, codestream.Frame.Height);
        Assert.Equal(refImage.NumberOfComponents, codestream.Frame.ComponentCount);
    }

    [Fact]
    public void ComparePixelValues_8x8()
    {
        var path = Path.Combine(GetTestImagesPath(), "test_8x8.jp2");
        var data = File.ReadAllBytes(path);

        // Decode with reference
        var refImage = J2kImage.FromBytes(data);

        var comp0 = refImage.GetComponent(0);
        _output.WriteLine("Reference decoded pixel values:");
        for (int y = 0; y < refImage.Height; y++)
        {
            var rowValues = Enumerable.Range(0, refImage.Width)
                .Select(x => comp0[y * refImage.Width + x].ToString().PadLeft(4));
            _output.WriteLine(string.Join("", rowValues));
        }

        // These are the expected values from our original PGM
        var expected = new int[,]
        {
            {0, 32, 64, 96, 128, 160, 192, 255},
            {32, 64, 96, 128, 160, 192, 255, 0},
            {64, 96, 128, 160, 192, 255, 0, 32},
            {96, 128, 160, 192, 255, 0, 32, 64},
            {128, 160, 192, 255, 0, 32, 64, 96},
            {160, 192, 255, 0, 32, 64, 96, 128},
            {192, 255, 0, 32, 64, 96, 128, 160},
            {255, 0, 32, 64, 96, 128, 160, 192},
        };

        _output.WriteLine("\nExpected values:");
        for (int y = 0; y < 8; y++)
        {
            var rowValues = Enumerable.Range(0, 8)
                .Select(x => expected[y, x].ToString().PadLeft(4));
            _output.WriteLine(string.Join("", rowValues));
        }

        // Verify lossless decode
        int mismatches = 0;
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                int refVal = GetPixel(refImage, 0, x, y);
                if (refVal != expected[y, x])
                {
                    mismatches++;
                    _output.WriteLine($"Mismatch at ({x},{y}): expected {expected[y, x]}, got {refVal}");
                }
            }
        }

        _output.WriteLine($"\nTotal mismatches: {mismatches}");
        Assert.Equal(0, mismatches);
    }
}
