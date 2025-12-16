using Xunit;

namespace JpegCodec.Tests;

/// <summary>
/// Tests for Stage 5: Inverse DCT.
/// Verifies that frequency-domain coefficients are correctly transformed to spatial domain.
/// </summary>
public class InverseDctTests
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

    #region Basic IDCT Tests

    [Fact]
    public void Transform_AllZeros_Returns128()
    {
        // All zero coefficients should produce a block of 128s (level shift applied)
        var coefficients = new int[64];

        byte[] result = InverseDct.Transform(coefficients);

        foreach (byte pixel in result)
        {
            Assert.Equal(128, pixel);
        }
    }

    [Fact]
    public void Transform_DCOnly_UniformBlock()
    {
        // A block with only DC coefficient should produce uniform pixels
        var coefficients = new int[64];
        coefficients[0] = 800; // DC coefficient (will be scaled by 1/4 * 1/sqrt(2) * 1/sqrt(2) = 1/8)

        byte[] result = InverseDct.Transform(coefficients);

        // All pixels should be the same
        byte firstPixel = result[0];
        foreach (byte pixel in result)
        {
            Assert.Equal(firstPixel, pixel);
        }
    }

    [Fact]
    public void Transform_PositiveDC_BrightBlock()
    {
        // Large positive DC should produce bright pixels (> 128)
        var coefficients = new int[64];
        coefficients[0] = 1024; // Large positive DC

        byte[] result = InverseDct.Transform(coefficients);

        // Should be brighter than 128
        Assert.True(result[0] > 128, $"Expected bright pixel, got {result[0]}");
    }

    [Fact]
    public void Transform_NegativeDC_DarkBlock()
    {
        // Large negative DC should produce dark pixels (< 128)
        var coefficients = new int[64];
        coefficients[0] = -1024; // Large negative DC

        byte[] result = InverseDct.Transform(coefficients);

        // Should be darker than 128
        Assert.True(result[0] < 128, $"Expected dark pixel, got {result[0]}");
    }

    [Fact]
    public void Transform_OutputIs64Bytes()
    {
        var coefficients = new int[64];
        coefficients[0] = 100;

        byte[] result = InverseDct.Transform(coefficients);

        Assert.Equal(64, result.Length);
    }

    [Fact]
    public void Transform_OutputInValidRange()
    {
        // Random-ish coefficients
        var coefficients = new int[64];
        coefficients[0] = 500;
        coefficients[1] = -50;
        coefficients[8] = 30;

        byte[] result = InverseDct.Transform(coefficients);

        foreach (byte pixel in result)
        {
            Assert.InRange(pixel, (byte)0, (byte)255);
        }
    }

    #endregion

    #region AC Coefficient Tests

    [Fact]
    public void Transform_HorizontalAC_CreatesVerticalPattern()
    {
        // AC coefficient at position (0,1) creates horizontal frequency variation
        // which appears as vertical stripes
        var coefficients = new int[64];
        coefficients[1] = 200; // (0,1) in row-major order

        byte[] result = InverseDct.Transform(coefficients);

        // The result should have variation in the horizontal direction
        // Check that not all values in a row are the same
        var hasRowVariation = false;
        for (var y = 0; y < 8; y++)
        {
            byte first = result[y * 8];
            for (var x = 1; x < 8; x++)
            {
                if (result[y * 8 + x] != first)
                {
                    hasRowVariation = true;
                    break;
                }
            }
            if (hasRowVariation) break;
        }

        Assert.True(hasRowVariation, "Horizontal AC should create variation along rows");
    }

    [Fact]
    public void Transform_VerticalAC_CreatesHorizontalPattern()
    {
        // AC coefficient at position (1,0) creates vertical frequency variation
        // which appears as horizontal stripes
        var coefficients = new int[64];
        coefficients[8] = 200; // (1,0) in row-major order

        byte[] result = InverseDct.Transform(coefficients);

        // The result should have variation in the vertical direction
        // Check that not all values in a column are the same
        var hasColumnVariation = false;
        for (var x = 0; x < 8; x++)
        {
            byte first = result[x];
            for (var y = 1; y < 8; y++)
            {
                if (result[y * 8 + x] != first)
                {
                    hasColumnVariation = true;
                    break;
                }
            }
            if (hasColumnVariation) break;
        }

        Assert.True(hasColumnVariation, "Vertical AC should create variation along columns");
    }

    #endregion

    #region Reference Implementation Tests

    [Fact]
    public void Transform_MatchesReference_AllZeros()
    {
        var coefficients = new int[64];

        byte[] optimized = InverseDct.Transform(coefficients);
        byte[] reference = InverseDct.TransformReference(coefficients);

        for (var i = 0; i < 64; i++)
        {
            Assert.Equal(reference[i], optimized[i]);
        }
    }

    [Fact]
    public void Transform_MatchesReference_DCOnly()
    {
        var coefficients = new int[64];
        coefficients[0] = 512;

        byte[] optimized = InverseDct.Transform(coefficients);
        byte[] reference = InverseDct.TransformReference(coefficients);

        for (var i = 0; i < 64; i++)
        {
            // Allow small rounding differences
            Assert.True(Math.Abs(reference[i] - optimized[i]) <= 1,
                $"Position {i}: reference={reference[i]}, optimized={optimized[i]}");
        }
    }

    [Fact]
    public void Transform_MatchesReference_MixedCoefficients()
    {
        var coefficients = new int[64];
        coefficients[0] = 400;  // DC
        coefficients[1] = -50;  // First AC
        coefficients[8] = 30;   // Second row first
        coefficients[9] = -20;

        byte[] optimized = InverseDct.Transform(coefficients);
        byte[] reference = InverseDct.TransformReference(coefficients);

        var maxDiff = 0;
        for (var i = 0; i < 64; i++)
        {
            int diff = Math.Abs(reference[i] - optimized[i]);
            maxDiff = Math.Max(maxDiff, diff);
        }

        // Allow small rounding differences
        Assert.True(maxDiff <= 1, $"Max difference between reference and optimized: {maxDiff}");
    }

    #endregion

    #region Full Pipeline Tests

    [Fact]
    public void TransformAll_SimpleGrayscale_NoException()
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

        Assert.NotNull(pixels);
        Assert.Equal(dequantized.Length, pixels.Length);
    }

    [Fact]
    public void TransformAll_SolidGray128_PixelsNear128()
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

        // For solid gray 128, all Y pixels should be near 128
        foreach (byte pixel in pixels[0][0])
        {
            Assert.True(pixel >= 120 && pixel <= 136,
                $"Solid gray 128 should have Y pixels near 128, got {pixel}");
        }
    }

    [Fact]
    public void TransformAll_SolidBlack_PixelsNearZero()
    {
        string path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_black.jpg");
        byte[] data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        JpegFrame frame = reader.ReadFrame();

        var decoder = new EntropyDecoder(frame, data);
        short[][][] blocks = decoder.DecodeAllBlocks();

        var dequantizer = new Dequantizer(frame);
        int[][][] dequantized = dequantizer.DequantizeAll(blocks);

        byte[][][] pixels = InverseDct.TransformAll(dequantized);

        // For solid black, Y pixels should be near 0 (or low values due to JPEG compression)
        foreach (byte pixel in pixels[0][0])
        {
            Assert.True(pixel <= 20, $"Solid black should have Y pixels near 0, got {pixel}");
        }
    }

    [Fact]
    public void TransformAll_SolidWhite_PixelsNear255()
    {
        string path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_white.jpg");
        byte[] data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        JpegFrame frame = reader.ReadFrame();

        var decoder = new EntropyDecoder(frame, data);
        short[][][] blocks = decoder.DecodeAllBlocks();

        var dequantizer = new Dequantizer(frame);
        int[][][] dequantized = dequantizer.DequantizeAll(blocks);

        byte[][][] pixels = InverseDct.TransformAll(dequantized);

        // For solid white, Y pixels should be near 255
        foreach (byte pixel in pixels[0][0])
        {
            Assert.True(pixel >= 235, $"Solid white should have Y pixels near 255, got {pixel}");
        }
    }

    #endregion

    #region All Files Test

    [Fact]
    public void TransformAllTestImages_NoExceptions()
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

                // Basic sanity checks
                if (pixels.Length != frame.ComponentCount)
                {
                    failures.Add($"{Path.GetFileName(file)}: Component count mismatch");
                    continue;
                }

                foreach (byte[][] compBlocks in pixels)
                {
                    foreach (byte[] block in compBlocks)
                    {
                        if (block.Length != 64)
                        {
                            failures.Add($"{Path.GetFileName(file)}: Block size != 64");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                failures.Add($"{Path.GetFileName(file)}: {ex.Message}");
            }
        }

        if (failures.Count > 0)
        {
            Assert.Fail($"Failed to transform {failures.Count} files:\n" + string.Join("\n", failures.Take(10)));
        }
    }

    #endregion

    #region Gradient Tests

    [Fact]
    public void TransformAll_Gradient_HasVariation()
    {
        string path = Path.Combine(GetTestImagesPath(), "level2_ac_coefficients/gray_gradient_diag.jpg");
        byte[] data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        JpegFrame frame = reader.ReadFrame();

        var decoder = new EntropyDecoder(frame, data);
        short[][][] blocks = decoder.DecodeAllBlocks();

        var dequantizer = new Dequantizer(frame);
        int[][][] dequantized = dequantizer.DequantizeAll(blocks);

        byte[][][] pixels = InverseDct.TransformAll(dequantized);

        // Gradient should have pixel variation within a block
        byte[] firstBlock = pixels[0][0];
        byte min = firstBlock.Min();
        byte max = firstBlock.Max();

        Assert.True(max - min > 10, $"Gradient block should have variation, range was {min}-{max}");
    }

    #endregion
}
