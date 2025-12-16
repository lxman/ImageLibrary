using Xunit;

namespace JpegCodec.Tests;

/// <summary>
/// Tests for Stage 4: Dequantization.
/// Verifies that DCT coefficients are properly multiplied by quantization table values.
/// </summary>
public class DequantizerTests
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

    #region Basic Dequantization Tests

    [Fact]
    public void DequantizeSimpleGrayscale_NoException()
    {
        var path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_128.jpg");
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        var decoder = new EntropyDecoder(frame, data);
        var blocks = decoder.DecodeAllBlocks();

        var dequantizer = new Dequantizer(frame);
        var dequantized = dequantizer.DequantizeAll(blocks);

        Assert.NotNull(dequantized);
        Assert.Equal(blocks.Length, dequantized.Length);
    }

    [Fact]
    public void DequantizeBlock_OutputIs64Elements()
    {
        var path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_128.jpg");
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        var decoder = new EntropyDecoder(frame, data);
        var blocks = decoder.DecodeAllBlocks();

        var dequantizer = new Dequantizer(frame);
        var dequantized = dequantizer.DequantizeAll(blocks);

        foreach (var compBlocks in dequantized)
        {
            foreach (var block in compBlocks)
            {
                Assert.Equal(64, block.Length);
            }
        }
    }

    [Fact]
    public void DequantizeBlock_DCCoefficientScaled()
    {
        var path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_128.jpg");
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        var decoder = new EntropyDecoder(frame, data);
        var blocks = decoder.DecodeAllBlocks();

        // Get quantization table value at DC position (index 0 in zig-zag order)
        var comp = frame.Components[0];
        var qt = frame.QuantizationTables[comp.QuantizationTableId];
        Assert.NotNull(qt);
        int dcQuantValue = qt[0]; // DC is at position 0 in zig-zag order

        var dequantizer = new Dequantizer(frame);
        var dequantized = dequantizer.DequantizeAll(blocks);

        // The dequantized DC should be the original DC * quantization value
        short originalDC = blocks[0][0][0];
        int dequantizedDC = dequantized[0][0][0];

        Assert.Equal(originalDC * dcQuantValue, dequantizedDC);
    }

    #endregion

    #region Quantization Table Tests

    [Fact]
    public void QuantizationTable_HasNonZeroValues()
    {
        var path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_128.jpg");
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        var qt = frame.QuantizationTables[0];
        Assert.NotNull(qt);
        Assert.Equal(64, qt.Length);

        // All quantization values should be > 0
        foreach (var val in qt)
        {
            Assert.True(val > 0, "Quantization values must be positive");
        }
    }

    [Fact]
    public void QuantizationTable_DCValueTypicallySmall()
    {
        // The DC quantization value (position 0) is typically small (8-16 for standard tables)
        var path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_128.jpg");
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        var qt = frame.QuantizationTables[0];
        Assert.NotNull(qt);

        // DC value should be relatively small (typically 16 for standard luminance table)
        Assert.True(qt[0] <= 32, $"DC quantization value {qt[0]} seems unusually high");
    }

    [Fact]
    public void QuantizationTable_HighFrequencyValuesLarger()
    {
        // High frequency positions (bottom-right of 8x8) should have larger quantization values
        var path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_128.jpg");
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        var qt = frame.QuantizationTables[0];
        Assert.NotNull(qt);

        // The last position (63 in zig-zag order) typically has a large value
        // compared to the first position
        Assert.True(qt[63] >= qt[0],
            $"High frequency quantization ({qt[63]}) should be >= DC quantization ({qt[0]})");
    }

    #endregion

    #region Color Image Tests

    [Fact]
    public void DequantizeColor444_ThreeComponents()
    {
        var path = Path.Combine(GetTestImagesPath(), "level4_color_444/color_solid_red.jpg");
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        var decoder = new EntropyDecoder(frame, data);
        var blocks = decoder.DecodeAllBlocks();

        var dequantizer = new Dequantizer(frame);
        var dequantized = dequantizer.DequantizeAll(blocks);

        Assert.Equal(3, dequantized.Length);
    }

    [Fact]
    public void DequantizeColor_DifferentQuantTablesForChroma()
    {
        var path = Path.Combine(GetTestImagesPath(), "level4_color_444/color_solid_red.jpg");
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        // Y component uses table 0, Cb/Cr typically use table 1
        var yComp = frame.Components[0];
        var cbComp = frame.Components[1];

        // They might use the same table, but if different, verify both exist
        var qtY = frame.QuantizationTables[yComp.QuantizationTableId];
        var qtCb = frame.QuantizationTables[cbComp.QuantizationTableId];

        Assert.NotNull(qtY);
        Assert.NotNull(qtCb);
    }

    #endregion

    #region Gradient Tests (AC Coefficients)

    [Fact]
    public void DequantizeGradient_ACCoefficientsScaled()
    {
        var path = Path.Combine(GetTestImagesPath(), "level2_ac_coefficients/gray_gradient_diag.jpg");
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        var decoder = new EntropyDecoder(frame, data);
        var blocks = decoder.DecodeAllBlocks();

        var dequantizer = new Dequantizer(frame);
        var dequantized = dequantizer.DequantizeAll(blocks);

        // Verify that non-zero AC coefficients are properly scaled
        var qt = frame.QuantizationTables[0];
        Assert.NotNull(qt);

        for (int i = 1; i < 64; i++)
        {
            short original = blocks[0][0][i];
            if (original != 0)
            {
                // Find the zig-zag index for this natural position
                int zigzagIndex = GetZigZagIndex(i);
                int expected = original * qt[zigzagIndex];
                Assert.Equal(expected, dequantized[0][0][i]);
                break; // Just verify one non-zero AC coefficient
            }
        }
    }

    // Helper method to get zig-zag index from natural index
    private static int GetZigZagIndex(int naturalIndex)
    {
        // Build the inverse mapping
        for (int i = 0; i < 64; i++)
        {
            if (EntropyDecoder.ZigZagOrder[i] == naturalIndex)
            {
                return i;
            }
        }
        return -1;
    }

    #endregion

    #region All Files Test

    [Fact]
    public void DequantizeAllTestImages_NoExceptions()
    {
        var basePath = GetTestImagesPath();
        var jpegFiles = Directory.GetFiles(basePath, "*.jpg", SearchOption.AllDirectories);

        var failures = new List<string>();

        foreach (var file in jpegFiles)
        {
            try
            {
                var data = File.ReadAllBytes(file);
                var reader = new JpegReader(data);
                var frame = reader.ReadFrame();

                var decoder = new EntropyDecoder(frame, data);
                var blocks = decoder.DecodeAllBlocks();

                var dequantizer = new Dequantizer(frame);
                var dequantized = dequantizer.DequantizeAll(blocks);

                // Basic sanity checks
                if (dequantized.Length != frame.ComponentCount)
                {
                    failures.Add($"{Path.GetFileName(file)}: Component count mismatch");
                    continue;
                }

                foreach (var compBlocks in dequantized)
                {
                    foreach (var block in compBlocks)
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
            Assert.Fail($"Failed to dequantize {failures.Count} files:\n" + string.Join("\n", failures.Take(10)));
        }
    }

    #endregion

    #region Static Block Dequantization Tests

    [Fact]
    public void DequantizeBlock_AllZerosRemainZero()
    {
        var block = new short[64]; // All zeros
        var qt = new ushort[64];
        for (int i = 0; i < 64; i++)
        {
            qt[i] = (ushort)(i + 1); // Non-zero quantization values
        }

        var result = Dequantizer.DequantizeBlock(block, qt);

        foreach (var val in result)
        {
            Assert.Equal(0, val);
        }
    }

    [Fact]
    public void DequantizeBlock_SingleCoefficient()
    {
        var block = new short[64];
        block[0] = 10; // DC coefficient only

        var qt = new ushort[64];
        qt[0] = 16; // DC quantization value
        for (int i = 1; i < 64; i++)
        {
            qt[i] = 1;
        }

        var result = Dequantizer.DequantizeBlock(block, qt);

        Assert.Equal(160, result[0]); // 10 * 16
    }

    #endregion
}
