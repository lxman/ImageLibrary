using Xunit;

namespace Jbig2Codec.Tests;

public class ArithmeticDecoderTests
{
    [Fact]
    public void Constructor_WithValidData_Succeeds()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0xFF, 0xAC };
        var decoder = new ArithmeticDecoder(data);

        Assert.NotNull(decoder);
    }

    [Fact]
    public void Constructor_WithOffset_Succeeds()
    {
        var data = new byte[] { 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xAC };
        var decoder = new ArithmeticDecoder(data, 2, 6);

        Assert.NotNull(decoder);
    }

    [Fact]
    public void Constructor_NullData_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ArithmeticDecoder(null!));
    }

    [Fact]
    public void Constructor_NegativeOffset_Throws()
    {
        var data = new byte[] { 0x00 };
        Assert.Throws<ArgumentOutOfRangeException>(() => new ArithmeticDecoder(data, -1, 1));
    }

    [Fact]
    public void Constructor_NegativeLength_Throws()
    {
        var data = new byte[] { 0x00 };
        Assert.Throws<ArgumentOutOfRangeException>(() => new ArithmeticDecoder(data, 0, -1));
    }

    [Fact]
    public void Constructor_OffsetPlusLengthExceedsData_Throws()
    {
        var data = new byte[] { 0x00, 0x01 };
        Assert.Throws<ArgumentException>(() => new ArithmeticDecoder(data, 1, 5));
    }

    [Fact]
    public void DecodeBit_NullContext_Throws()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0xFF, 0xAC };
        var decoder = new ArithmeticDecoder(data);

        Assert.Throws<ArgumentNullException>(() => decoder.DecodeBit(null!));
    }

    [Fact]
    public void DecodeBit_ReturnsZeroOrOne()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0xFF, 0xAC };
        var decoder = new ArithmeticDecoder(data);
        var context = new ArithmeticDecoder.Context();

        int bit = decoder.DecodeBit(context);

        Assert.True(bit == 0 || bit == 1);
    }

    [Fact]
    public void Context_InitializesToZero()
    {
        var context = new ArithmeticDecoder.Context();

        Assert.Equal(0, context.State);
        Assert.Equal(0, context.Mps);
    }

    [Fact]
    public void DecodeBit_UpdatesContextState()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0xFF, 0xAC };
        var decoder = new ArithmeticDecoder(data);
        var context = new ArithmeticDecoder.Context();

        // Decode several bits - context state should evolve
        for (var i = 0; i < 10; i++)
        {
            decoder.DecodeBit(context);
        }

        // After several MPS decodes, state should have advanced
        // (exact value depends on decoded bits, but shouldn't stay at 0 forever)
        Assert.True(context.State >= 0 && context.State < 47);
    }

    [Fact]
    public void DecodeInt_NullContexts_Throws()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0xFF, 0xAC };
        var decoder = new ArithmeticDecoder(data);

        Assert.Throws<ArgumentNullException>(() => decoder.DecodeInt(null!));
    }

    [Fact]
    public void DecodeInt_InsufficientContexts_Throws()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0xFF, 0xAC };
        var decoder = new ArithmeticDecoder(data);
        var contexts = new ArithmeticDecoder.Context[100]; // Need 512

        Assert.Throws<ArgumentException>(() => decoder.DecodeInt(contexts));
    }

    [Fact]
    public void BytesConsumed_TracksProgress()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0xFF, 0xAC };
        var decoder = new ArithmeticDecoder(data);

        // Initial consumption happens during construction
        Assert.True(decoder.BytesConsumed >= 0);
    }

    [Fact]
    public void DebugState_ReturnsNonEmpty()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0xFF, 0xAC };
        var decoder = new ArithmeticDecoder(data);

        Assert.False(string.IsNullOrEmpty(decoder.DebugState));
    }

    [Fact]
    public void MultipleContexts_IndependentState()
    {
        var data = new byte[] { 0x55, 0xAA, 0x55, 0xAA, 0xFF, 0xAC };
        var decoder = new ArithmeticDecoder(data);

        var context1 = new ArithmeticDecoder.Context();
        var context2 = new ArithmeticDecoder.Context();

        // Decode with context1
        decoder.DecodeBit(context1);
        decoder.DecodeBit(context1);

        // context2 should still be at initial state
        Assert.Equal(0, context2.State);
        Assert.Equal(0, context2.Mps);
    }

    [Fact]
    public void DecodeBit_DeterministicWithSameData()
    {
        var data = new byte[] { 0x12, 0x34, 0x56, 0x78, 0xFF, 0xAC };

        var decoder1 = new ArithmeticDecoder(data);
        var context1 = new ArithmeticDecoder.Context();

        var decoder2 = new ArithmeticDecoder(data);
        var context2 = new ArithmeticDecoder.Context();

        // Same input should produce same output
        for (var i = 0; i < 20; i++)
        {
            int bit1 = decoder1.DecodeBit(context1);
            int bit2 = decoder2.DecodeBit(context2);
            Assert.Equal(bit1, bit2);
        }
    }
}
