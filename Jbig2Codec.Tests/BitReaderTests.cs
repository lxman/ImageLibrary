using Xunit;

namespace Jbig2Codec.Tests;

public class BitReaderTests
{
    [Fact]
    public void ReadBit_ReadsCorrectBits()
    {
        // 0xA5 = 10100101
        var data = new byte[] { 0xA5 };
        var reader = new BitReader(data);

        Assert.Equal(1, reader.ReadBit());
        Assert.Equal(0, reader.ReadBit());
        Assert.Equal(1, reader.ReadBit());
        Assert.Equal(0, reader.ReadBit());
        Assert.Equal(0, reader.ReadBit());
        Assert.Equal(1, reader.ReadBit());
        Assert.Equal(0, reader.ReadBit());
        Assert.Equal(1, reader.ReadBit());
    }

    [Fact]
    public void ReadBits_ReadsCorrectValues()
    {
        // 0xAB = 10101011, 0xCD = 11001101
        var data = new byte[] { 0xAB, 0xCD };
        var reader = new BitReader(data);

        // Read 4 bits: 1010 = 10
        Assert.Equal(10u, reader.ReadBits(4));
        // Read 4 bits: 1011 = 11
        Assert.Equal(11u, reader.ReadBits(4));
        // Read 4 bits: 1100 = 12
        Assert.Equal(12u, reader.ReadBits(4));
        // Read 4 bits: 1101 = 13
        Assert.Equal(13u, reader.ReadBits(4));
    }

    [Fact]
    public void ReadBits_CrossesByteBoundary()
    {
        var data = new byte[] { 0xFF, 0x00 };
        var reader = new BitReader(data);

        // Read 4 bits: 1111 = 15
        Assert.Equal(15u, reader.ReadBits(4));
        // Read 8 bits crossing boundary: 11110000 = 240
        Assert.Equal(240u, reader.ReadBits(8));
    }

    [Fact]
    public void ReadByte_ReadsCorrectValue()
    {
        var data = new byte[] { 0x12, 0x34, 0x56 };
        var reader = new BitReader(data);

        Assert.Equal(0x12, reader.ReadByte());
        Assert.Equal(0x34, reader.ReadByte());
        Assert.Equal(0x56, reader.ReadByte());
    }

    [Fact]
    public void ReadByte_AlignsToByteFirst()
    {
        // When ReadByte is called mid-byte, it aligns to next byte boundary
        var data = new byte[] { 0xF0, 0xAB };
        var reader = new BitReader(data);

        // Read 4 bits first
        reader.ReadBits(4);
        // ReadByte should align and read next byte
        Assert.Equal(0xAB, reader.ReadByte());
    }

    [Fact]
    public void ReadUInt16BigEndian_ReadsCorrectValue()
    {
        var data = new byte[] { 0x12, 0x34 };
        var reader = new BitReader(data);

        Assert.Equal((ushort)0x1234, reader.ReadUInt16BigEndian());
    }

    [Fact]
    public void ReadUInt32BigEndian_ReadsCorrectValue()
    {
        var data = new byte[] { 0x12, 0x34, 0x56, 0x78 };
        var reader = new BitReader(data);

        Assert.Equal(0x12345678u, reader.ReadUInt32BigEndian());
    }

    [Fact]
    public void BytePosition_TracksCorrectly()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var reader = new BitReader(data);

        Assert.Equal(0, reader.BytePosition);
        reader.ReadByte();
        Assert.Equal(1, reader.BytePosition);
        reader.ReadByte();
        Assert.Equal(2, reader.BytePosition);
    }

    [Fact]
    public void BytePosition_AfterBitReads()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var reader = new BitReader(data);

        reader.ReadBits(4);
        Assert.Equal(0, reader.BytePosition); // Still in first byte
        reader.ReadBits(4);
        Assert.Equal(1, reader.BytePosition); // Now in second byte
    }

    [Fact]
    public void AlignToByte_AlignsToByteBoundary()
    {
        var data = new byte[] { 0xFF, 0xAA };
        var reader = new BitReader(data);

        reader.ReadBits(3);
        reader.AlignToByte();
        Assert.Equal(1, reader.BytePosition);
        Assert.Equal(0xAA, reader.ReadByte());
    }

    [Fact]
    public void AlignToByte_NoOpWhenAligned()
    {
        var data = new byte[] { 0xFF, 0xAA };
        var reader = new BitReader(data);

        reader.ReadByte();
        reader.AlignToByte();
        Assert.Equal(1, reader.BytePosition);
        Assert.Equal(0xAA, reader.ReadByte());
    }

    [Fact]
    public void IsAtEnd_ReturnsCorrectly()
    {
        var data = new byte[] { 0xFF };
        var reader = new BitReader(data);

        Assert.False(reader.IsAtEnd);
        reader.ReadByte();
        Assert.True(reader.IsAtEnd);
    }

    [Fact]
    public void ReadBits_ZeroBits_ReturnsZero()
    {
        var data = new byte[] { 0xFF };
        var reader = new BitReader(data);

        Assert.Equal(0u, reader.ReadBits(0));
        Assert.Equal(0, reader.BytePosition);
    }

    [Fact]
    public void RemainingBytes_TracksCorrectly()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var reader = new BitReader(data);

        Assert.Equal(4, reader.RemainingBytes);
        reader.ReadByte();
        Assert.Equal(3, reader.RemainingBytes);
        reader.ReadByte();
        reader.ReadByte();
        Assert.Equal(1, reader.RemainingBytes);
    }

    [Fact]
    public void Seek_MovesToPosition()
    {
        var data = new byte[] { 0x11, 0x22, 0x33, 0x44 };
        var reader = new BitReader(data);

        reader.Seek(2);
        Assert.Equal(0x33, reader.ReadByte());
    }

    [Fact]
    public void SkipBytes_AdvancesPosition()
    {
        var data = new byte[] { 0x11, 0x22, 0x33, 0x44 };
        var reader = new BitReader(data);

        reader.SkipBytes(2);
        Assert.Equal(0x33, reader.ReadByte());
    }

    [Fact]
    public void Constructor_WithOffset_StartsAtOffset()
    {
        var data = new byte[] { 0x11, 0x22, 0x33, 0x44 };
        var reader = new BitReader(data, 2);

        Assert.Equal(0x33, reader.ReadByte());
    }

    [Fact]
    public void PeekByte_DoesNotAdvance()
    {
        var data = new byte[] { 0xAB, 0xCD };
        var reader = new BitReader(data);

        Assert.Equal(0xAB, reader.PeekByte());
        Assert.Equal(0xAB, reader.PeekByte()); // Still same
        Assert.Equal(0, reader.BytePosition);
    }

    [Fact]
    public void ReadBytes_ReadsMultipleBytes()
    {
        var data = new byte[] { 0x11, 0x22, 0x33, 0x44 };
        var reader = new BitReader(data);

        byte[] result = reader.ReadBytes(3);
        Assert.Equal(new byte[] { 0x11, 0x22, 0x33 }, result);
        Assert.Equal(3, reader.BytePosition);
    }

    [Fact]
    public void ReadInt32BigEndian_ReadsSignedValue()
    {
        // -1 in big-endian = 0xFF FF FF FF
        var data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
        var reader = new BitReader(data);

        Assert.Equal(-1, reader.ReadInt32BigEndian());
    }
}
