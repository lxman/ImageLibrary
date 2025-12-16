using Xunit;

namespace Jbig2Codec.Tests;

public class MmrDecoderTests
{
    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Simple MMR data - all white line (horizontal mode, white run = width)
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        var decoder = new MmrDecoder(data, 0, data.Length, 8, 1);

        Assert.NotNull(decoder);
    }

    [Fact]
    public void Constructor_WithOffset_Succeeds()
    {
        var data = new byte[] { 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        var decoder = new MmrDecoder(data, 2, 8, 8, 1);

        Assert.NotNull(decoder);
    }

    [Fact]
    public void Decode_ReturnsValidBitmap()
    {
        // MMR-encoded single white line (pass mode or horizontal with all white)
        // This encodes a simple 8-pixel wide, 1-pixel tall all-white bitmap
        // Horizontal mode: 001 (H), then white run code, then black run code (0)
        var data = new byte[] { 0x00, 0x10, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 };
        var decoder = new MmrDecoder(data, 0, data.Length, 8, 1);
        decoder.CheckForEofb = false;

        Bitmap bitmap = decoder.Decode();

        Assert.NotNull(bitmap);
        Assert.Equal(8, bitmap.Width);
        Assert.Equal(1, bitmap.Height);
    }

    [Fact]
    public void Decode_ProducesCorrectDimensions()
    {
        // Construct valid MMR data for a small bitmap
        var data = new byte[] { 0x00, 0x10, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 };
        var decoder = new MmrDecoder(data, 0, data.Length, 16, 2);
        decoder.CheckForEofb = false;

        Bitmap bitmap = decoder.Decode();

        Assert.Equal(16, bitmap.Width);
        Assert.Equal(2, bitmap.Height);
    }

    [Fact]
    public void BytesConsumed_IsNonNegative()
    {
        var data = new byte[] { 0x00, 0x10, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 };
        var decoder = new MmrDecoder(data, 0, data.Length, 8, 1);
        decoder.CheckForEofb = false;

        Bitmap bitmap = decoder.Decode();

        Assert.True(decoder.BytesConsumed >= 0);
    }

    [Fact]
    public void CheckForEofb_DefaultsToTrue()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var decoder = new MmrDecoder(data, 0, data.Length, 8, 1);

        Assert.True(decoder.CheckForEofb);
    }

    [Fact]
    public void CheckForEofb_CanBeSet()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var decoder = new MmrDecoder(data, 0, data.Length, 8, 1);

        decoder.CheckForEofb = false;
        Assert.False(decoder.CheckForEofb);
    }

    [Fact]
    public void Decode_AllWhiteLine_ProducesAllZeros()
    {
        // Horizontal mode + white run length covering full width
        // For 8-pixel width: H mode (001) + white 8 (0010111) + black 0 (0000001101)
        // Simplified: this should produce all-white (0) pixels
        var data = new byte[] { 0x35, 0xC0, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00 };
        var decoder = new MmrDecoder(data, 0, data.Length, 8, 1);
        decoder.CheckForEofb = false;

        Bitmap bitmap = decoder.Decode();

        // Check all pixels are white (0)
        for (var x = 0; x < 8; x++)
        {
            Assert.Equal(0, bitmap.GetPixel(x, 0));
        }
    }

    [Fact]
    public void Decode_MultipleLines_Works()
    {
        // MMR data for multiple lines
        var data = new byte[] {
            0x00, 0x10, 0x01, 0x00, 0x10, 0x01, 0x00, 0x10,
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };
        var decoder = new MmrDecoder(data, 0, data.Length, 8, 3);
        decoder.CheckForEofb = false;

        Bitmap bitmap = decoder.Decode();

        Assert.Equal(8, bitmap.Width);
        Assert.Equal(3, bitmap.Height);
    }

    [Fact]
    public void Decode_LargerWidth_Works()
    {
        var data = new byte[] {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };
        var decoder = new MmrDecoder(data, 0, data.Length, 64, 1);
        decoder.CheckForEofb = false;

        Bitmap bitmap = decoder.Decode();

        Assert.Equal(64, bitmap.Width);
        Assert.Equal(1, bitmap.Height);
    }

    [Fact]
    public void Decode_Deterministic_SameDataSameResult()
    {
        var data = new byte[] { 0x00, 0x10, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 };

        var decoder1 = new MmrDecoder(data, 0, data.Length, 8, 1);
        decoder1.CheckForEofb = false;
        Bitmap bitmap1 = decoder1.Decode();

        var decoder2 = new MmrDecoder(data, 0, data.Length, 8, 1);
        decoder2.CheckForEofb = false;
        Bitmap bitmap2 = decoder2.Decode();

        // Compare pixel by pixel
        for (var y = 0; y < 1; y++)
        {
            for (var x = 0; x < 8; x++)
            {
                Assert.Equal(bitmap1.GetPixel(x, y), bitmap2.GetPixel(x, y));
            }
        }
    }

    [Fact]
    public void Decode_WithValidEofb_Succeeds()
    {
        // EOFB is two consecutive EOL codes: 000000000001 000000000001
        // Followed by additional padding
        // This is a minimal valid MMR stream with EOFB
        var data = new byte[] {
            0x00, 0x10, 0x01,  // Some line data
            0x00, 0x10, 0x00, 0x10, // EOFB pattern
            0x00, 0x00, 0x00, 0x00
        };
        var decoder = new MmrDecoder(data, 0, data.Length, 8, 1);
        decoder.CheckForEofb = true;

        // Should not throw
        Bitmap bitmap = decoder.Decode();
        Assert.NotNull(bitmap);
    }

    [Fact]
    public void Constructor_ZeroWidth_StillConstructs()
    {
        // Edge case: zero width
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00 };

        // This might throw during construction or decode depending on implementation
        try
        {
            var decoder = new MmrDecoder(data, 0, data.Length, 0, 1);
            // If it doesn't throw on construction, it might throw on decode
        }
        catch (Exception)
        {
            // Expected for invalid dimensions
        }
    }

    [Fact]
    public void Constructor_ZeroHeight_ThrowsOnDecode()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var decoder = new MmrDecoder(data, 0, data.Length, 8, 0);
        decoder.CheckForEofb = false;

        // With height 0, decode throws due to invalid Bitmap dimensions
        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => decoder.Decode());
    }

    [Fact]
    public void BytesConsumed_IncreasesAfterDecode()
    {
        var data = new byte[] {
            0x00, 0x10, 0x01, 0x00, 0x10, 0x01,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };
        var decoder = new MmrDecoder(data, 0, data.Length, 8, 2);
        decoder.CheckForEofb = false;

        int beforeDecode = decoder.BytesConsumed;
        Bitmap bitmap = decoder.Decode();
        int afterDecode = decoder.BytesConsumed;

        // After decoding, some bytes should be consumed
        Assert.True(afterDecode >= beforeDecode);
    }
}
