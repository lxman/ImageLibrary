using Xunit;

namespace JpegCodec.Tests;

/// <summary>
/// Tests for Stage 6: Color conversion and image assembly.
/// </summary>
public class ColorConverterTests
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

    #region YCbCr to RGB Conversion Tests

    [Fact]
    public void YCbCrToRgb_Gray128_ReturnsGray()
    {
        // Y=128, Cb=128, Cr=128 should be neutral gray
        (byte r, byte g, byte b) = ColorConverter.YCbCrToRgb(128, 128, 128);

        Assert.InRange(r, (byte)125, (byte)131);
        Assert.InRange(g, (byte)125, (byte)131);
        Assert.InRange(b, (byte)125, (byte)131);
    }

    [Fact]
    public void YCbCrToRgb_Black_ReturnsBlack()
    {
        // Y=0, Cb=128, Cr=128 should be black
        (byte r, byte g, byte b) = ColorConverter.YCbCrToRgb(0, 128, 128);

        Assert.InRange(r, (byte)0, (byte)5);
        Assert.InRange(g, (byte)0, (byte)5);
        Assert.InRange(b, (byte)0, (byte)5);
    }

    [Fact]
    public void YCbCrToRgb_White_ReturnsWhite()
    {
        // Y=255, Cb=128, Cr=128 should be white
        (byte r, byte g, byte b) = ColorConverter.YCbCrToRgb(255, 128, 128);

        Assert.InRange(r, (byte)250, (byte)255);
        Assert.InRange(g, (byte)250, (byte)255);
        Assert.InRange(b, (byte)250, (byte)255);
    }

    [Fact]
    public void YCbCrToRgb_Red_ReturnsRedish()
    {
        // Pure red in YCbCr is approximately Y=76, Cb=84, Cr=255
        (byte r, byte g, byte b) = ColorConverter.YCbCrToRgb(76, 84, 255);

        // Should have high red, low green, low blue
        Assert.True(r > 200, $"Red component should be high, got {r}");
        Assert.True(g < 50, $"Green component should be low, got {g}");
        Assert.True(b < 50, $"Blue component should be low, got {b}");
    }

    [Fact]
    public void YCbCrToRgb_Green_ReturnsGreenish()
    {
        // Pure green in YCbCr is approximately Y=149, Cb=43, Cr=21
        (byte r, byte g, byte b) = ColorConverter.YCbCrToRgb(149, 43, 21);

        // Should have low red, high green, low blue
        Assert.True(r < 50, $"Red component should be low, got {r}");
        Assert.True(g > 200, $"Green component should be high, got {g}");
        Assert.True(b < 50, $"Blue component should be low, got {b}");
    }

    [Fact]
    public void YCbCrToRgb_Blue_ReturnsBlueish()
    {
        // Pure blue in YCbCr is approximately Y=29, Cb=255, Cr=107
        (byte r, byte g, byte b) = ColorConverter.YCbCrToRgb(29, 255, 107);

        // Should have low red, low green, high blue
        Assert.True(r < 50, $"Red component should be low, got {r}");
        Assert.True(g < 50, $"Green component should be low, got {g}");
        Assert.True(b > 200, $"Blue component should be high, got {b}");
    }

    #endregion

    #region Grayscale Assembly Tests

    [Fact]
    public void AssembleGrayscale_SolidGray_UniformOutput()
    {
        string path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_128.jpg");
        byte[] data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        JpegFrame frame = reader.ReadFrame();

        var decoder = new EntropyDecoder(frame, data);
        short[][][] blocks = decoder.DecodeAllBlocks();

        var dequantizer = new Dequantizer(frame);
        int[][][] dequantized = dequantizer.DequantizeAll(blocks);

        byte[][][] pixels = InverseDct.TransformAll(dequantized);

        var colorConverter = new ColorConverter(frame);
        byte[] rgb = colorConverter.AssembleImage(pixels);

        Assert.Equal(frame.Width * frame.Height * 3, rgb.Length);

        // For grayscale, R = G = B for each pixel
        for (var i = 0; i < rgb.Length; i += 3)
        {
            Assert.Equal(rgb[i], rgb[i + 1]);
            Assert.Equal(rgb[i], rgb[i + 2]);
        }
    }

    [Fact]
    public void AssembleGrayscale_CorrectDimensions()
    {
        string path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_128.jpg");
        byte[] data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        JpegFrame frame = reader.ReadFrame();

        var decoder = new EntropyDecoder(frame, data);
        short[][][] blocks = decoder.DecodeAllBlocks();

        var dequantizer = new Dequantizer(frame);
        int[][][] dequantized = dequantizer.DequantizeAll(blocks);

        byte[][][] pixels = InverseDct.TransformAll(dequantized);

        var colorConverter = new ColorConverter(frame);
        byte[] rgb = colorConverter.AssembleImage(pixels);

        // RGB data should be Width * Height * 3
        Assert.Equal(frame.Width * frame.Height * 3, rgb.Length);
    }

    #endregion

    #region Color Assembly Tests

    [Fact]
    public void AssembleColor_SolidRed_RedPixels()
    {
        string path = Path.Combine(GetTestImagesPath(), "level4_color_444/color_solid_red.jpg");
        byte[] data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        JpegFrame frame = reader.ReadFrame();

        var decoder = new EntropyDecoder(frame, data);
        short[][][] blocks = decoder.DecodeAllBlocks();

        var dequantizer = new Dequantizer(frame);
        int[][][] dequantized = dequantizer.DequantizeAll(blocks);

        byte[][][] pixels = InverseDct.TransformAll(dequantized);

        var colorConverter = new ColorConverter(frame);
        byte[] rgb = colorConverter.AssembleImage(pixels);

        // Check center pixel should be red
        int centerX = frame.Width / 2;
        int centerY = frame.Height / 2;
        int offset = (centerY * frame.Width + centerX) * 3;

        byte r = rgb[offset];
        byte g = rgb[offset + 1];
        byte b = rgb[offset + 2];

        Assert.True(r > 200, $"Red should be high, got {r}");
        Assert.True(g < 80, $"Green should be low, got {g}");
        Assert.True(b < 80, $"Blue should be low, got {b}");
    }

    [Fact]
    public void AssembleColor_SolidGreen_GreenPixels()
    {
        string path = Path.Combine(GetTestImagesPath(), "level4_color_444/color_solid_green.jpg");
        byte[] data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        JpegFrame frame = reader.ReadFrame();

        var decoder = new EntropyDecoder(frame, data);
        short[][][] blocks = decoder.DecodeAllBlocks();

        var dequantizer = new Dequantizer(frame);
        int[][][] dequantized = dequantizer.DequantizeAll(blocks);

        byte[][][] pixels = InverseDct.TransformAll(dequantized);

        var colorConverter = new ColorConverter(frame);
        byte[] rgb = colorConverter.AssembleImage(pixels);

        // Check center pixel should be green
        int centerX = frame.Width / 2;
        int centerY = frame.Height / 2;
        int offset = (centerY * frame.Width + centerX) * 3;

        byte r = rgb[offset];
        byte g = rgb[offset + 1];
        byte b = rgb[offset + 2];

        Assert.True(r < 80, $"Red should be low, got {r}");
        Assert.True(g > 200, $"Green should be high, got {g}");
        Assert.True(b < 80, $"Blue should be low, got {b}");
    }

    [Fact]
    public void AssembleColor_SolidBlue_BluePixels()
    {
        string path = Path.Combine(GetTestImagesPath(), "level4_color_444/color_solid_blue.jpg");
        byte[] data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        JpegFrame frame = reader.ReadFrame();

        var decoder = new EntropyDecoder(frame, data);
        short[][][] blocks = decoder.DecodeAllBlocks();

        var dequantizer = new Dequantizer(frame);
        int[][][] dequantized = dequantizer.DequantizeAll(blocks);

        byte[][][] pixels = InverseDct.TransformAll(dequantized);

        var colorConverter = new ColorConverter(frame);
        byte[] rgb = colorConverter.AssembleImage(pixels);

        // Check center pixel should be blue
        int centerX = frame.Width / 2;
        int centerY = frame.Height / 2;
        int offset = (centerY * frame.Width + centerX) * 3;

        byte r = rgb[offset];
        byte g = rgb[offset + 1];
        byte b = rgb[offset + 2];

        Assert.True(r < 80, $"Red should be low, got {r}");
        Assert.True(g < 80, $"Green should be low, got {g}");
        Assert.True(b > 200, $"Blue should be high, got {b}");
    }

    #endregion

    #region 4:2:0 Subsampling Tests

    [Fact]
    public void AssembleColor420_SolidRed_RedPixels()
    {
        string path = Path.Combine(GetTestImagesPath(), "level5_color_420/color420_solid_red.jpg");
        byte[] data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        JpegFrame frame = reader.ReadFrame();

        var decoder = new EntropyDecoder(frame, data);
        short[][][] blocks = decoder.DecodeAllBlocks();

        var dequantizer = new Dequantizer(frame);
        int[][][] dequantized = dequantizer.DequantizeAll(blocks);

        byte[][][] pixels = InverseDct.TransformAll(dequantized);

        var colorConverter = new ColorConverter(frame);
        byte[] rgb = colorConverter.AssembleImage(pixels);

        // Check center pixel
        int centerX = frame.Width / 2;
        int centerY = frame.Height / 2;
        int offset = (centerY * frame.Width + centerX) * 3;

        byte r = rgb[offset];
        byte g = rgb[offset + 1];
        byte b = rgb[offset + 2];

        Assert.True(r > 200, $"Red should be high, got {r}");
        Assert.True(g < 80, $"Green should be low, got {g}");
        Assert.True(b < 80, $"Blue should be low, got {b}");
    }

    #endregion

    #region All Files Test

    [Fact]
    public void AssembleAllTestImages_NoExceptions()
    {
        string basePath = GetTestImagesPath();
        string[] jpegFiles = Directory.GetFiles(basePath, "*.jpg", SearchOption.AllDirectories);

        var failures = new List<string>();

        foreach (string file in jpegFiles)
        {
            try
            {
                byte[] data = File.ReadAllBytes(file);
                var reader = new JpegReader(data);
                JpegFrame frame = reader.ReadFrame();

                var decoder = new EntropyDecoder(frame, data);
                short[][][] blocks = decoder.DecodeAllBlocks();

                var dequantizer = new Dequantizer(frame);
                int[][][] dequantized = dequantizer.DequantizeAll(blocks);

                byte[][][] pixels = InverseDct.TransformAll(dequantized);

                var colorConverter = new ColorConverter(frame);
                byte[] rgb = colorConverter.AssembleImage(pixels);

                // Basic sanity checks
                int expectedLength = frame.Width * frame.Height * 3;
                if (rgb.Length != expectedLength)
                {
                    failures.Add($"{Path.GetFileName(file)}: Expected {expectedLength} bytes, got {rgb.Length}");
                }
            }
            catch (Exception ex)
            {
                failures.Add($"{Path.GetFileName(file)}: {ex.Message}");
            }
        }

        if (failures.Count > 0)
        {
            Assert.Fail($"Failed to assemble {failures.Count} files:\n" + string.Join("\n", failures.Take(10)));
        }
    }

    #endregion
}
