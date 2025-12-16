using Xunit;

namespace JpegCodec.Tests;

/// <summary>
/// Tests for Stage 2: Huffman table building.
/// </summary>
public class HuffmanTableTests
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

    #region Table Building Tests

    [Fact]
    public void BuildHuffmanTable_FromSimpleJpeg_NoException()
    {
        var path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_128.jpg");
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        // Build DC table
        var dcSpec = frame.DcHuffmanTables[0];
        Assert.NotNull(dcSpec);
        var dcTable = new HuffmanTable(dcSpec);
        Assert.NotNull(dcTable);

        // Build AC table
        var acSpec = frame.AcHuffmanTables[0];
        Assert.NotNull(acSpec);
        var acTable = new HuffmanTable(acSpec);
        Assert.NotNull(acTable);
    }

    [Fact]
    public void BuildHuffmanTable_LookupTablePopulated()
    {
        var path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_128.jpg");
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        var dcTable = new HuffmanTable(frame.DcHuffmanTables[0]!);

        // The lookup table should have some non-zero entries
        int nonZeroCount = dcTable.LookupTable.Count(x => x != 0);
        Assert.True(nonZeroCount > 0, "Lookup table should have some entries");
    }

    [Fact]
    public void BuildHuffmanTable_SymbolsPreserved()
    {
        var path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_128.jpg");
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        var dcSpec = frame.DcHuffmanTables[0]!;
        var dcTable = new HuffmanTable(dcSpec);

        // Symbols should be preserved
        Assert.Equal(dcSpec.Symbols.Length, dcTable.Symbols.Length);
        for (int i = 0; i < dcSpec.Symbols.Length; i++)
        {
            Assert.Equal(dcSpec.Symbols[i], dcTable.Symbols[i]);
        }
    }

    #endregion

    #region Standard JPEG Huffman Tables Tests

    [Fact]
    public void BuildDcLuminanceTable_CorrectSymbolCount()
    {
        // Standard JPEG DC luminance table has 12 symbols (0-11)
        var path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_128.jpg");
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        var dcSpec = frame.DcHuffmanTables[0]!;
        Assert.Equal(12, dcSpec.TotalCodes);
    }

    [Fact]
    public void BuildAcLuminanceTable_CorrectSymbolCount()
    {
        // Standard JPEG AC luminance table has 162 symbols
        var path = Path.Combine(GetTestImagesPath(), "level1_simple/gray_solid_128.jpg");
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        var acSpec = frame.AcHuffmanTables[0]!;
        Assert.Equal(162, acSpec.TotalCodes);
    }

    #endregion

    #region Build All Test Images

    [Fact]
    public void BuildHuffmanTables_AllTestImages_NoExceptions()
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

                // Build all DC tables
                for (int i = 0; i < 4; i++)
                {
                    if (frame.DcHuffmanTables[i] != null)
                    {
                        var table = new HuffmanTable(frame.DcHuffmanTables[i]!);
                        Assert.NotNull(table);
                    }
                }

                // Build all AC tables
                for (int i = 0; i < 4; i++)
                {
                    if (frame.AcHuffmanTables[i] != null)
                    {
                        var table = new HuffmanTable(frame.AcHuffmanTables[i]!);
                        Assert.NotNull(table);
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
            Assert.Fail($"Failed to build tables for {failures.Count} files:\n" + string.Join("\n", failures));
        }
    }

    #endregion

    #region BitReader Tests

    [Fact]
    public void BitReader_ReadBits_CorrectValues()
    {
        // Test data: 0xAB = 10101011, 0xCD = 11001101
        var data = new byte[] { 0xAB, 0xCD };
        var reader = new BitReader(data, 0, 2);

        // Read 4 bits at a time
        Assert.Equal(0xA, reader.ReadBits(4)); // 1010
        Assert.Equal(0xB, reader.ReadBits(4)); // 1011
        Assert.Equal(0xC, reader.ReadBits(4)); // 1100
        Assert.Equal(0xD, reader.ReadBits(4)); // 1101
    }

    [Fact]
    public void BitReader_ReadBit_CorrectSequence()
    {
        // 0x80 = 10000000
        var data = new byte[] { 0x80 };
        var reader = new BitReader(data, 0, 1);

        Assert.Equal(1, reader.ReadBit());
        Assert.Equal(0, reader.ReadBit());
        Assert.Equal(0, reader.ReadBit());
        Assert.Equal(0, reader.ReadBit());
    }

    [Fact]
    public void BitReader_PeekBits_DoesNotConsume()
    {
        var data = new byte[] { 0xFF };
        var reader = new BitReader(data, 0, 1);

        Assert.Equal(0xFF, reader.PeekBits(8));
        Assert.Equal(0xFF, reader.PeekBits(8)); // Should be same
        Assert.Equal(0xFF, reader.ReadBits(8)); // Now consume
    }

    [Fact]
    public void BitReader_ByteStuffing_HandledCorrectly()
    {
        // 0xFF 0x00 should be read as just 0xFF (stuffed byte)
        var data = new byte[] { 0xFF, 0x00, 0xAB };
        var reader = new BitReader(data, 0, 3);

        Assert.Equal(0xFF, reader.ReadBits(8));
        Assert.Equal(0xAB, reader.ReadBits(8));
    }

    [Fact]
    public void BitReader_SignedValue_PositiveCorrect()
    {
        // For 4 bits: values 8-15 are positive (8-15)
        // 0x90 = 1001 0000, first 4 bits = 9
        var data = new byte[] { 0x90 };
        var reader = new BitReader(data, 0, 1);

        Assert.Equal(9, reader.ReadSignedValue(4));
    }

    [Fact]
    public void BitReader_SignedValue_NegativeCorrect()
    {
        // For 4 bits: values 0-7 map to -15 to -8
        // 0x30 = 0011 0000, first 4 bits = 3 -> -12
        var data = new byte[] { 0x30 };
        var reader = new BitReader(data, 0, 1);

        // Value 3 with 4 bits: 3 < 8 (threshold), so result = 3 - 15 = -12
        Assert.Equal(-12, reader.ReadSignedValue(4));
    }

    [Fact]
    public void BitReader_SignedValue_ZeroBits()
    {
        var data = new byte[] { 0xFF };
        var reader = new BitReader(data, 0, 1);

        Assert.Equal(0, reader.ReadSignedValue(0));
    }

    #endregion
}
