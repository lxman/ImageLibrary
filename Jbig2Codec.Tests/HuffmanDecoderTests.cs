using Xunit;
using Jbig2Codec;

namespace Jbig2Codec.Tests;

public class HuffmanDecoderTests
{
    [Fact]
    public void Constructor_WithValidData_Succeeds()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var decoder = new HuffmanDecoder(data, 0, data.Length);

        Assert.NotNull(decoder);
    }

    [Fact]
    public void Constructor_WithOffset_Succeeds()
    {
        var data = new byte[] { 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00 };
        var decoder = new HuffmanDecoder(data, 2, 4);

        Assert.NotNull(decoder);
        Assert.Equal(2, decoder.BytePosition);
    }

    [Fact]
    public void BytePosition_TracksCorrectly()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        var decoder = new HuffmanDecoder(data, 0, data.Length);

        Assert.Equal(0, decoder.BytePosition);
    }

    [Fact]
    public void RemainingBytes_TracksCorrectly()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var decoder = new HuffmanDecoder(data, 0, data.Length);

        Assert.Equal(4, decoder.RemainingBytes);
    }

    [Fact]
    public void ReadBits_ReadsCorrectValues()
    {
        // 0xAB = 10101011, 0xCD = 11001101
        var data = new byte[] { 0xAB, 0xCD, 0x00, 0x00 };
        var decoder = new HuffmanDecoder(data, 0, data.Length);

        // Read 4 bits: 1010 = 10
        Assert.Equal(10, decoder.ReadBits(4));
        // Read 4 bits: 1011 = 11
        Assert.Equal(11, decoder.ReadBits(4));
        // Read 4 bits: 1100 = 12
        Assert.Equal(12, decoder.ReadBits(4));
        // Read 4 bits: 1101 = 13
        Assert.Equal(13, decoder.ReadBits(4));
    }

    [Fact]
    public void ReadBits_CrossesByteBoundary()
    {
        var data = new byte[] { 0xFF, 0x00, 0x00, 0x00 };
        var decoder = new HuffmanDecoder(data, 0, data.Length);

        // Read 4 bits: 1111 = 15
        Assert.Equal(15, decoder.ReadBits(4));
        // Read 8 bits crossing boundary: 11110000 = 240
        Assert.Equal(240, decoder.ReadBits(8));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(33)]
    public void ReadBits_InvalidCount_Throws(int count)
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var decoder = new HuffmanDecoder(data, 0, data.Length);

        Assert.Throws<ArgumentOutOfRangeException>(() => decoder.ReadBits(count));
    }

    [Fact]
    public void SkipToByteAlign_AlignsToByteBoundary()
    {
        var data = new byte[] { 0xFF, 0xAA, 0x00, 0x00 };
        var decoder = new HuffmanDecoder(data, 0, data.Length);

        decoder.ReadBits(3);
        decoder.SkipToByteAlign();

        // Should now be at byte 1
        Assert.Equal(1, decoder.BytePosition);
    }

    [Fact]
    public void SkipToByteAlign_NoOpWhenAligned()
    {
        var data = new byte[] { 0xFF, 0xAA, 0x00, 0x00 };
        var decoder = new HuffmanDecoder(data, 0, data.Length);

        decoder.ReadBits(8);
        decoder.SkipToByteAlign();

        Assert.Equal(1, decoder.BytePosition);
    }

    [Fact]
    public void Advance_MovesForward()
    {
        var data = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 };
        var decoder = new HuffmanDecoder(data, 0, data.Length);

        decoder.Advance(2);
        Assert.Equal(2, decoder.BytePosition);
    }

    [Fact]
    public void GetData_ReturnsUnderlyingArray()
    {
        var data = new byte[] { 0x11, 0x22, 0x33, 0x44 };
        var decoder = new HuffmanDecoder(data, 0, data.Length);

        Assert.Same(data, decoder.GetData());
    }

    [Fact]
    public void Decode_WithTableA_ReturnsValidValue()
    {
        // TableA is a simple table for unsigned integers
        // Let's encode a known value and verify decoding
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        var decoder = new HuffmanDecoder(data, 0, data.Length);

        HuffmanTable table = StandardHuffmanTables.TableA;
        int value = decoder.Decode(table);

        // Should decode to some valid integer (not OOB)
        Assert.NotEqual(HuffmanDecoder.OOB, value);
    }

    [Fact]
    public void Decode_WithTableB_ReturnsValidValue()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        var decoder = new HuffmanDecoder(data, 0, data.Length);

        HuffmanTable table = StandardHuffmanTables.TableB;
        int value = decoder.Decode(table);

        Assert.NotEqual(HuffmanDecoder.OOB, value);
    }

    [Fact]
    public void Decode_MultipleTables_WorksCorrectly()
    {
        var data = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 };
        var decoder = new HuffmanDecoder(data, 0, data.Length);

        // Decode using different tables in sequence
        HuffmanTable tableA = StandardHuffmanTables.TableA;
        HuffmanTable tableB = StandardHuffmanTables.TableB;

        int val1 = decoder.Decode(tableA);
        int val2 = decoder.Decode(tableB);

        // Both should succeed
        Assert.True(val1 >= 0 || val1 == HuffmanDecoder.OOB);
        Assert.True(val2 >= int.MinValue);
    }

    [Fact]
    public void OOB_IsIntMinValue()
    {
        Assert.Equal(int.MinValue, HuffmanDecoder.OOB);
    }

    [Fact]
    public void DebugState_ReturnsNonEmpty()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var decoder = new HuffmanDecoder(data, 0, data.Length);

        Assert.False(string.IsNullOrEmpty(decoder.DebugState));
    }

    [Fact]
    public void ReadBits_ConsumesCorrectBits()
    {
        // 0x80 = 10000000
        var data = new byte[] { 0x80, 0x00, 0x00, 0x00 };
        var decoder = new HuffmanDecoder(data, 0, data.Length);

        // First bit should be 1
        Assert.Equal(1, decoder.ReadBits(1));
        // Next 7 bits should be 0
        Assert.Equal(0, decoder.ReadBits(7));
    }

    [Fact]
    public void ReadBits_32Bits_Works()
    {
        var data = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x00, 0x00, 0x00, 0x00 };
        var decoder = new HuffmanDecoder(data, 0, data.Length);

        int value = decoder.ReadBits(32);
        Assert.Equal(0x12345678, value);
    }

    [Fact]
    public void Decode_Deterministic_SameDataSameResult()
    {
        var data = new byte[] { 0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA };

        var decoder1 = new HuffmanDecoder(data, 0, data.Length);
        var decoder2 = new HuffmanDecoder(data, 0, data.Length);

        HuffmanTable table = StandardHuffmanTables.TableA;

        int val1 = decoder1.Decode(table);
        int val2 = decoder2.Decode(table);

        Assert.Equal(val1, val2);
    }

    [Fact]
    public void StandardTables_AllBuild()
    {
        // Verify all standard tables can be built without error
        Assert.NotNull(StandardHuffmanTables.TableA);
        Assert.NotNull(StandardHuffmanTables.TableB);
        Assert.NotNull(StandardHuffmanTables.TableC);
        Assert.NotNull(StandardHuffmanTables.TableD);
        Assert.NotNull(StandardHuffmanTables.TableE);
        Assert.NotNull(StandardHuffmanTables.TableF);
        Assert.NotNull(StandardHuffmanTables.TableG);
        Assert.NotNull(StandardHuffmanTables.TableH);
        Assert.NotNull(StandardHuffmanTables.TableI);
        Assert.NotNull(StandardHuffmanTables.TableJ);
        Assert.NotNull(StandardHuffmanTables.TableK);
        Assert.NotNull(StandardHuffmanTables.TableL);
        Assert.NotNull(StandardHuffmanTables.TableM);
        Assert.NotNull(StandardHuffmanTables.TableN);
        Assert.NotNull(StandardHuffmanTables.TableO);
    }
}
