using Xunit;

namespace JpegCodec.Tests;

/// <summary>
/// Integration tests for the complete JPEG decoder.
/// Tests the full pipeline from JPEG bytes to decoded RGB image.
/// </summary>
public class JpegDecoderTests
{
    private static string GetTestImagesPath()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "TestImages", "jpeg_test")))
        {
            dir = dir.Parent;
        }

        return dir != null
            ? Path.Combine(dir.FullName, "TestImages", "jpeg_test")
            : "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test";
    }

    #region Basic Decoding Tests

    [Fact]
    public void Decode_SimpleGrayscale_NoException()
    {
        var path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_128.jpg");
        var image = JpegDecoder.DecodeFile(path);

        Assert.NotNull(image);
        Assert.True(image.Width > 0);
        Assert.True(image.Height > 0);
        Assert.Equal(image.Width * image.Height * 3, image.RgbData.Length);
    }

    [Fact]
    public void Decode_SimpleColor_NoException()
    {
        var path = Path.Combine(GetTestImagesPath(), "level4_color_444/color_solid_red.jpg");
        var image = JpegDecoder.DecodeFile(path);

        Assert.NotNull(image);
        Assert.True(image.Width > 0);
        Assert.True(image.Height > 0);
    }

    #endregion

    #region Grayscale Value Tests

    [Fact]
    public void Decode_SolidGray128_CenterPixelNear128()
    {
        var path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_128.jpg");
        var image = JpegDecoder.DecodeFile(path);

        var (r, g, b) = image.GetPixel(image.Width / 2, image.Height / 2);

        Assert.InRange(r, (byte)120, (byte)136);
        Assert.Equal(r, g);
        Assert.Equal(r, b);
    }

    [Fact]
    public void Decode_SolidBlack_CenterPixelNearZero()
    {
        var path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_black.jpg");
        var image = JpegDecoder.DecodeFile(path);

        var (r, g, b) = image.GetPixel(image.Width / 2, image.Height / 2);

        Assert.InRange(r, (byte)0, (byte)20);
        Assert.Equal(r, g);
        Assert.Equal(r, b);
    }

    [Fact]
    public void Decode_SolidWhite_CenterPixelNear255()
    {
        var path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_white.jpg");
        var image = JpegDecoder.DecodeFile(path);

        var (r, g, b) = image.GetPixel(image.Width / 2, image.Height / 2);

        Assert.InRange(r, (byte)235, (byte)255);
        Assert.Equal(r, g);
        Assert.Equal(r, b);
    }

    #endregion

    #region Color Value Tests

    [Fact]
    public void Decode_SolidRed_CenterPixelIsRed()
    {
        var path = Path.Combine(GetTestImagesPath(), "level4_color_444/color_solid_red.jpg");
        var image = JpegDecoder.DecodeFile(path);

        var (r, g, b) = image.GetPixel(image.Width / 2, image.Height / 2);

        Assert.True(r > 200, $"Red should be high, got {r}");
        Assert.True(g < 80, $"Green should be low, got {g}");
        Assert.True(b < 80, $"Blue should be low, got {b}");
    }

    [Fact]
    public void Decode_SolidGreen_CenterPixelIsGreen()
    {
        var path = Path.Combine(GetTestImagesPath(), "level4_color_444/color_solid_green.jpg");
        var image = JpegDecoder.DecodeFile(path);

        var (r, g, b) = image.GetPixel(image.Width / 2, image.Height / 2);

        Assert.True(r < 80, $"Red should be low, got {r}");
        Assert.True(g > 200, $"Green should be high, got {g}");
        Assert.True(b < 80, $"Blue should be low, got {b}");
    }

    [Fact]
    public void Decode_SolidBlue_CenterPixelIsBlue()
    {
        var path = Path.Combine(GetTestImagesPath(), "level4_color_444/color_solid_blue.jpg");
        var image = JpegDecoder.DecodeFile(path);

        var (r, g, b) = image.GetPixel(image.Width / 2, image.Height / 2);

        Assert.True(r < 80, $"Red should be low, got {r}");
        Assert.True(g < 80, $"Green should be low, got {g}");
        Assert.True(b > 200, $"Blue should be high, got {b}");
    }

    #endregion

    #region 4:2:0 Subsampling Tests

    [Fact]
    public void Decode_420Red_CenterPixelIsRed()
    {
        var path = Path.Combine(GetTestImagesPath(), "level5_color_420/color420_solid_red.jpg");
        var image = JpegDecoder.DecodeFile(path);

        var (r, g, b) = image.GetPixel(image.Width / 2, image.Height / 2);

        Assert.True(r > 200, $"Red should be high, got {r}");
        Assert.True(g < 80, $"Green should be low, got {g}");
        Assert.True(b < 80, $"Blue should be low, got {b}");
    }

    #endregion

    #region Non-Aligned Dimension Tests

    [Fact]
    public void Decode_NonAligned_CorrectDimensions()
    {
        var path = Path.Combine(GetTestImagesPath(), "level6_non_aligned/gray_7x7.jpg");
        var image = JpegDecoder.DecodeFile(path);

        Assert.Equal(7, image.Width);
        Assert.Equal(7, image.Height);
    }

    #endregion

    #region GetPixel Tests

    [Fact]
    public void GetPixel_ValidPosition_ReturnsValue()
    {
        var path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_128.jpg");
        var image = JpegDecoder.DecodeFile(path);

        var pixel = image.GetPixel(0, 0);
        // Should not throw
        Assert.True(true);
    }

    [Fact]
    public void GetPixel_NegativePosition_ThrowsException()
    {
        var path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_128.jpg");
        var image = JpegDecoder.DecodeFile(path);

        Assert.Throws<ArgumentOutOfRangeException>(() => image.GetPixel(-1, 0));
    }

    [Fact]
    public void GetPixel_OutOfBounds_ThrowsException()
    {
        var path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_128.jpg");
        var image = JpegDecoder.DecodeFile(path);

        Assert.Throws<ArgumentOutOfRangeException>(() => image.GetPixel(image.Width, 0));
    }

    #endregion

    #region All Files Test

    [Fact]
    public void DecodeAllTestImages_NoExceptions()
    {
        var basePath = GetTestImagesPath();
        var jpegFiles = Directory.GetFiles(basePath, "*.jpg", SearchOption.AllDirectories);

        var failures = new List<string>();

        foreach (var file in jpegFiles)
        {
            try
            {
                var image = JpegDecoder.DecodeFile(file);

                // Basic sanity checks
                if (image.Width <= 0 || image.Height <= 0)
                {
                    failures.Add($"{Path.GetFileName(file)}: Invalid dimensions {image.Width}x{image.Height}");
                    continue;
                }

                if (image.RgbData.Length != image.Width * image.Height * 3)
                {
                    failures.Add($"{Path.GetFileName(file)}: RGB data length mismatch");
                    continue;
                }
            }
            catch (Exception ex)
            {
                failures.Add($"{Path.GetFileName(file)}: {ex.Message}");
            }
        }

        if (failures.Count > 0)
        {
            Assert.Fail($"Failed to decode {failures.Count} files:\n" + string.Join("\n", failures.Take(10)));
        }
    }

    [Fact]
    public void DecodeAllTestImages_VerifyDimensions()
    {
        var basePath = GetTestImagesPath();
        var jpegFiles = Directory.GetFiles(basePath, "*.jpg", SearchOption.AllDirectories);

        foreach (var file in jpegFiles)
        {
            var image = JpegDecoder.DecodeFile(file);

            // Verify dimensions match what we read from markers
            var data = File.ReadAllBytes(file);
            var reader = new JpegReader(data);
            var frame = reader.ReadFrame();

            Assert.Equal(frame.Width, image.Width);
            Assert.Equal(frame.Height, image.Height);
        }
    }

    #endregion

    #region Gradient Tests

    [Fact]
    public void Decode_DiagonalGradient_HasVariation()
    {
        var path = Path.Combine(GetTestImagesPath(), "level2_ac_coefficients/gray_gradient_diag.jpg");
        var image = JpegDecoder.DecodeFile(path);

        // Get corners
        var topLeft = image.GetGrayscale(0, 0);
        var bottomRight = image.GetGrayscale(image.Width - 1, image.Height - 1);

        // Diagonal gradient should have different values at corners
        Assert.NotEqual(topLeft, bottomRight);
    }

    [Fact]
    public void Decode_HorizontalGradient_LeftDifferentFromRight()
    {
        var path = Path.Combine(GetTestImagesPath(), "level2_ac_coefficients/gray_gradient_h.jpg");
        var image = JpegDecoder.DecodeFile(path);

        var centerY = image.Height / 2;
        var left = image.GetGrayscale(0, centerY);
        var right = image.GetGrayscale(image.Width - 1, centerY);

        Assert.True(Math.Abs(left - right) > 100, $"Horizontal gradient should vary, left={left}, right={right}");
    }

    [Fact]
    public void Decode_VerticalGradient_TopDifferentFromBottom()
    {
        var path = Path.Combine(GetTestImagesPath(), "level2_ac_coefficients/gray_gradient_v.jpg");
        var image = JpegDecoder.DecodeFile(path);

        var centerX = image.Width / 2;
        var top = image.GetGrayscale(centerX, 0);
        var bottom = image.GetGrayscale(centerX, image.Height - 1);

        Assert.True(Math.Abs(top - bottom) > 100, $"Vertical gradient should vary, top={top}, bottom={bottom}");
    }

    #endregion

    #region Quality Variation Tests

    [Fact]
    public void Decode_Quality10_DecodesSuccessfully()
    {
        var path = Path.Combine(GetTestImagesPath(), "level7_quality/gray_q10.jpg");
        var image = JpegDecoder.DecodeFile(path);

        Assert.NotNull(image);
        Assert.True(image.Width > 0);
    }

    [Fact]
    public void Decode_Quality100_DecodesSuccessfully()
    {
        var path = Path.Combine(GetTestImagesPath(), "level7_quality/gray_q100.jpg");
        var image = JpegDecoder.DecodeFile(path);

        Assert.NotNull(image);
        Assert.True(image.Width > 0);
    }

    #endregion
}
