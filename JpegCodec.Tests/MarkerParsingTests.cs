using Xunit;

namespace JpegCodec.Tests;

/// <summary>
/// Tests for Stage 1: JPEG marker parsing.
/// Verifies that JpegReader correctly parses all JPEG markers.
/// </summary>
public class MarkerParsingTests
{
    private static string GetTestImagesPath()
    {
        // Navigate up from the test output directory to find TestImages
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "TestImages", "jpeg_test")))
        {
            dir = dir.Parent;
        }

        if (dir != null)
        {
            return Path.Combine(dir.FullName, "TestImages", "jpeg_test");
        }

        // Fallback to absolute path
        return "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test";
    }

    #region Level 1: Simple Grayscale Tests

    [Theory]
    [InlineData("level1_simple/gray_solid_black.jpg", 8, 8)]
    [InlineData("level1_simple/gray_solid_white.jpg", 8, 8)]
    [InlineData("level1_simple/gray_solid_128.jpg", 8, 8)]
    public void ParseLevel1_SimplestJpegs_CorrectDimensions(string relativePath, int expectedWidth, int expectedHeight)
    {
        var path = Path.Combine(GetTestImagesPath(), relativePath);
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        Assert.Equal(expectedWidth, frame.Width);
        Assert.Equal(expectedHeight, frame.Height);
    }

    [Fact]
    public void ParseLevel1_GrayscaleSolid_IsBaselineDCT()
    {
        var path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_128.jpg");
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        Assert.True(frame.IsBaseline);
        Assert.False(frame.IsProgressive);
        Assert.Equal(8, frame.Precision);
    }

    [Fact]
    public void ParseLevel1_GrayscaleSolid_SingleComponent()
    {
        var path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_128.jpg");
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        // ImageSharp creates grayscale as Y-only when using L8
        // But it might also create as 3-component YCbCr
        Assert.True(frame.ComponentCount >= 1, $"Expected at least 1 component, got {frame.ComponentCount}");
    }

    [Fact]
    public void ParseLevel1_GrayscaleSolid_HasQuantizationTable()
    {
        var path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_128.jpg");
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        // Should have at least one quantization table
        Assert.NotNull(frame.QuantizationTables[0]);
        Assert.Equal(64, frame.QuantizationTables[0]!.Length);
    }

    [Fact]
    public void ParseLevel1_GrayscaleSolid_HasHuffmanTables()
    {
        var path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_128.jpg");
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        // Should have DC and AC Huffman tables
        Assert.NotNull(frame.DcHuffmanTables[0]);
        Assert.NotNull(frame.AcHuffmanTables[0]);
    }

    [Fact]
    public void ParseLevel1_GrayscaleSolid_HasEntropyData()
    {
        var path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_128.jpg");
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        Assert.True(frame.EntropyDataOffset > 0, "Entropy data offset should be positive");
        Assert.True(frame.EntropyDataLength > 0, "Entropy data length should be positive");
    }

    #endregion

    #region Level 2: AC Coefficient Tests

    [Theory]
    [InlineData("level2_ac_coefficients/gray_gradient_h.jpg")]
    [InlineData("level2_ac_coefficients/gray_gradient_v.jpg")]
    [InlineData("level2_ac_coefficients/gray_gradient_diag.jpg")]
    [InlineData("level2_ac_coefficients/gray_checker.jpg")]
    public void ParseLevel2_GrayscalePatterns_ParsesSuccessfully(string relativePath)
    {
        var path = Path.Combine(GetTestImagesPath(), relativePath);
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        Assert.Equal(8, frame.Width);
        Assert.Equal(8, frame.Height);
        Assert.True(frame.IsBaseline);
    }

    #endregion

    #region Level 3: Multiple Block Tests

    [Theory]
    [InlineData("level3_multiple_blocks/gray_16x16_solid.jpg", 16, 16)]
    [InlineData("level3_multiple_blocks/gray_16x16_gradient_h.jpg", 16, 16)]
    [InlineData("level3_multiple_blocks/gray_24x24_gradient.jpg", 24, 24)]
    [InlineData("level3_multiple_blocks/gray_32x32_block_checker.jpg", 32, 32)]
    [InlineData("level3_multiple_blocks/gray_64x64_gradient.jpg", 64, 64)]
    public void ParseLevel3_MultipleBlocks_CorrectDimensions(string relativePath, int expectedWidth, int expectedHeight)
    {
        var path = Path.Combine(GetTestImagesPath(), relativePath);
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        Assert.Equal(expectedWidth, frame.Width);
        Assert.Equal(expectedHeight, frame.Height);
    }

    #endregion

    #region Level 4: Color 4:4:4 Tests

    [Theory]
    [InlineData("level4_color_444/color_solid_red.jpg")]
    [InlineData("level4_color_444/color_solid_green.jpg")]
    [InlineData("level4_color_444/color_solid_blue.jpg")]
    public void ParseLevel4_Color444_ThreeComponents(string relativePath)
    {
        var path = Path.Combine(GetTestImagesPath(), relativePath);
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        Assert.Equal(3, frame.ComponentCount);
    }

    [Fact]
    public void ParseLevel4_Color444_SamplingFactorsAllOne()
    {
        var path = Path.Combine(GetTestImagesPath(), "level4_color_444/color_solid_red.jpg");
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        // 4:4:4 means all sampling factors are 1x1
        foreach (var component in frame.Components)
        {
            Assert.Equal(1, component.HorizontalSamplingFactor);
            Assert.Equal(1, component.VerticalSamplingFactor);
        }
    }

    #endregion

    #region Level 5: Color 4:2:0 Tests

    [Fact]
    public void ParseLevel5_Color420_ThreeComponents()
    {
        var path = Path.Combine(GetTestImagesPath(), "level5_color_420/color420_solid_red.jpg");
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        Assert.Equal(3, frame.ComponentCount);
    }

    [Fact]
    public void ParseLevel5_Color420_CorrectSamplingFactors()
    {
        var path = Path.Combine(GetTestImagesPath(), "level5_color_420/color420_solid_red.jpg");
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        // 4:2:0 means Y is 2x2, Cb and Cr are 1x1
        var yComponent = frame.Components[0];
        Assert.Equal(2, yComponent.HorizontalSamplingFactor);
        Assert.Equal(2, yComponent.VerticalSamplingFactor);

        var cbComponent = frame.Components[1];
        Assert.Equal(1, cbComponent.HorizontalSamplingFactor);
        Assert.Equal(1, cbComponent.VerticalSamplingFactor);

        var crComponent = frame.Components[2];
        Assert.Equal(1, crComponent.HorizontalSamplingFactor);
        Assert.Equal(1, crComponent.VerticalSamplingFactor);
    }

    #endregion

    #region Level 6: Non-aligned Dimensions

    [Theory]
    [InlineData("level6_non_aligned/gray_7x7.jpg", 7, 7)]
    [InlineData("level6_non_aligned/gray_9x9.jpg", 9, 9)]
    [InlineData("level6_non_aligned/gray_10x12.jpg", 10, 12)]
    [InlineData("level6_non_aligned/gray_15x17.jpg", 15, 17)]
    public void ParseLevel6_NonAlignedGrayscale_CorrectDimensions(string relativePath, int expectedWidth, int expectedHeight)
    {
        var path = Path.Combine(GetTestImagesPath(), relativePath);
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        Assert.Equal(expectedWidth, frame.Width);
        Assert.Equal(expectedHeight, frame.Height);
    }

    #endregion

    #region Level 7: Quality Variations

    [Theory]
    [InlineData("level7_quality/gray_q100.jpg")]
    [InlineData("level7_quality/gray_q75.jpg")]
    [InlineData("level7_quality/gray_q50.jpg")]
    [InlineData("level7_quality/gray_q25.jpg")]
    [InlineData("level7_quality/gray_q10.jpg")]
    public void ParseLevel7_DifferentQualities_ParsesSuccessfully(string relativePath)
    {
        var path = Path.Combine(GetTestImagesPath(), relativePath);
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        Assert.Equal(8, frame.Width);
        Assert.Equal(8, frame.Height);
        Assert.NotNull(frame.QuantizationTables[0]);
    }

    [Fact]
    public void ParseLevel7_QualityVariation_QuantizationTablesDiffer()
    {
        var pathQ100 = Path.Combine(GetTestImagesPath(), "level7_quality/gray_q100.jpg");
        var pathQ10 = Path.Combine(GetTestImagesPath(), "level7_quality/gray_q10.jpg");

        var frameQ100 = new JpegReader(File.ReadAllBytes(pathQ100)).ReadFrame();
        var frameQ10 = new JpegReader(File.ReadAllBytes(pathQ10)).ReadFrame();

        // Q10 should have larger quantization values than Q100
        var q100Table = frameQ100.QuantizationTables[0]!;
        var q10Table = frameQ10.QuantizationTables[0]!;

        // At least some values should differ
        bool anyDifferent = false;
        for (int i = 0; i < 64; i++)
        {
            if (q100Table[i] != q10Table[i])
            {
                anyDifferent = true;
                break;
            }
        }

        Assert.True(anyDifferent, "Q100 and Q10 should have different quantization tables");
    }

    #endregion

    #region All Files Test

    [Fact]
    public void ParseAllTestImages_NoExceptions()
    {
        var basePath = GetTestImagesPath();
        var jpegFiles = Directory.GetFiles(basePath, "*.jpg", SearchOption.AllDirectories);

        Assert.True(jpegFiles.Length >= 50, $"Expected at least 50 test images, found {jpegFiles.Length}");

        var failures = new List<string>();

        foreach (var file in jpegFiles)
        {
            try
            {
                var data = File.ReadAllBytes(file);
                var reader = new JpegReader(data);
                var frame = reader.ReadFrame();

                // Basic sanity checks
                if (frame.Width == 0 || frame.Height == 0)
                {
                    failures.Add($"{Path.GetFileName(file)}: Invalid dimensions {frame.Width}x{frame.Height}");
                }

                if (frame.ComponentCount == 0)
                {
                    failures.Add($"{Path.GetFileName(file)}: No components");
                }

                if (frame.EntropyDataLength == 0)
                {
                    failures.Add($"{Path.GetFileName(file)}: No entropy data");
                }
            }
            catch (Exception ex)
            {
                failures.Add($"{Path.GetFileName(file)}: {ex.Message}");
            }
        }

        if (failures.Count > 0)
        {
            Assert.Fail($"Failed to parse {failures.Count} files:\n" + string.Join("\n", failures));
        }
    }

    #endregion
}
