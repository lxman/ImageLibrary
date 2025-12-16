using Xunit;
using Xunit.Abstractions;

namespace Jp2Codec.Tests;

/// <summary>
/// Tests for the JPEG2000 codestream reader (Stage 1 of pipeline).
/// </summary>
public class CodestreamReaderTests
{
    private readonly ITestOutputHelper _output;

    public CodestreamReaderTests(ITestOutputHelper output)
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
    public void ParseJp2File_8x8_ReadsCorrectDimensions()
    {
        var path = Path.Combine(GetTestImagesPath(), "test_8x8.jp2");
        var data = File.ReadAllBytes(path);

        Assert.True(Jp2FileReader.IsJp2File(data), "Should be recognized as JP2 file");

        var fileReader = new Jp2FileReader(data);
        var fileInfo = fileReader.Read();

        _output.WriteLine($"JP2 File Info:");
        _output.WriteLine($"  Brand: {fileInfo.Brand}");
        _output.WriteLine($"  Dimensions: {fileInfo.Width}x{fileInfo.Height}");
        _output.WriteLine($"  Components: {fileInfo.ComponentCount}");
        _output.WriteLine($"  Bit Depth: {fileInfo.BitDepth}");
        _output.WriteLine($"  Color Method: {fileInfo.ColorMethod}");
        _output.WriteLine($"  Color Space: {fileInfo.ColorSpace}");
        _output.WriteLine($"  Codestream Size: {fileInfo.CodestreamData?.Length ?? 0} bytes");

        Assert.Equal(8, fileInfo.Width);
        Assert.Equal(8, fileInfo.Height);
        Assert.Equal(1, fileInfo.ComponentCount);
    }

    [Fact]
    public void ParseCodestream_8x8_ReadsMainHeader()
    {
        var path = Path.Combine(GetTestImagesPath(), "test_8x8.jp2");
        var data = File.ReadAllBytes(path);

        var fileReader = new Jp2FileReader(data);
        var fileInfo = fileReader.Read();

        var codestreamReader = new CodestreamReader(fileInfo.CodestreamData!);
        var codestream = codestreamReader.ReadMainHeader();

        _output.WriteLine($"Codestream Info:");
        _output.WriteLine($"  Image Size: {codestream.Frame.Width}x{codestream.Frame.Height}");
        _output.WriteLine($"  Tile Size: {codestream.Frame.TileWidth}x{codestream.Frame.TileHeight}");
        _output.WriteLine($"  Components: {codestream.Frame.ComponentCount}");
        _output.WriteLine($"  Tiles: {codestream.Frame.NumTilesX}x{codestream.Frame.NumTilesY}");

        _output.WriteLine($"\nCoding Parameters:");
        _output.WriteLine($"  Progression: {codestream.CodingParameters.Progression}");
        _output.WriteLine($"  Layers: {codestream.CodingParameters.LayerCount}");
        _output.WriteLine($"  Decomposition Levels: {codestream.CodingParameters.DecompositionLevels}");
        _output.WriteLine($"  Code Block Size: {codestream.CodingParameters.CodeBlockWidth}x{codestream.CodingParameters.CodeBlockHeight}");
        _output.WriteLine($"  Wavelet: {codestream.CodingParameters.WaveletType}");
        _output.WriteLine($"  MCT: {codestream.CodingParameters.MultipleComponentTransform}");

        _output.WriteLine($"\nQuantization:");
        _output.WriteLine($"  Style: {codestream.QuantizationParameters.Style}");
        _output.WriteLine($"  Guard Bits: {codestream.QuantizationParameters.GuardBits}");
        _output.WriteLine($"  Step Sizes: {codestream.QuantizationParameters.StepSizes.Length}");

        foreach (var comment in codestream.Comments)
        {
            _output.WriteLine($"\nComment: {comment}");
        }

        Assert.Equal(8, codestream.Frame.Width);
        Assert.Equal(8, codestream.Frame.Height);
        Assert.Equal(1, codestream.Frame.ComponentCount);
    }

    [Fact]
    public void ParseCodestream_16x16_ReadsDecompositionLevels()
    {
        var path = Path.Combine(GetTestImagesPath(), "test_16x16.jp2");
        var data = File.ReadAllBytes(path);

        var fileReader = new Jp2FileReader(data);
        var fileInfo = fileReader.Read();

        var codestreamReader = new CodestreamReader(fileInfo.CodestreamData!);
        var codestream = codestreamReader.ReadMainHeader();

        _output.WriteLine($"16x16 Image:");
        _output.WriteLine($"  Decomposition Levels: {codestream.CodingParameters.DecompositionLevels}");
        _output.WriteLine($"  Wavelet: {codestream.CodingParameters.WaveletType}");

        Assert.Equal(16, codestream.Frame.Width);
        Assert.Equal(16, codestream.Frame.Height);
        Assert.True(codestream.CodingParameters.DecompositionLevels >= 1);
    }

    [Fact]
    public void ParseTilePart_8x8_ReadsBitstream()
    {
        var path = Path.Combine(GetTestImagesPath(), "test_8x8.jp2");
        var data = File.ReadAllBytes(path);

        var fileReader = new Jp2FileReader(data);
        var fileInfo = fileReader.Read();

        var codestreamReader = new CodestreamReader(fileInfo.CodestreamData!);
        var codestream = codestreamReader.ReadMainHeader();

        // Read tile-parts
        Jp2TilePart? tilePart;
        while ((tilePart = codestreamReader.ReadTilePart()) != null)
        {
            _output.WriteLine($"Tile Part {tilePart.TileIndex}.{tilePart.TilePartIndex}:");
            _output.WriteLine($"  Bitstream Size: {tilePart.BitstreamData.Length} bytes");

            // Show first few bytes of bitstream
            var preview = string.Join(" ", tilePart.BitstreamData.Take(16).Select(b => $"{b:X2}"));
            _output.WriteLine($"  First bytes: {preview}");

            codestream.TileParts.Add(tilePart);
        }

        Assert.Single(codestream.TileParts);
        Assert.True(codestream.TileParts[0].BitstreamData.Length > 0);
    }
}
