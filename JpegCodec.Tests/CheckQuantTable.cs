using Xunit;
using Xunit.Abstractions;

namespace JpegCodec.Tests;

public class CheckQuantTable
{
    private readonly ITestOutputHelper _output;

    public CheckQuantTable(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ShowQuantizationTable()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        byte[] data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        JpegFrame frame = reader.ReadFrame();

        _output.WriteLine("Quantization table 0:");
        ushort[]? qt = frame.QuantizationTables[0];
        if (qt != null)
        {
            _output.WriteLine($"  DC (position 0): {qt[0]}");
            _output.WriteLine("");
            _output.WriteLine("  Full table:");
            for (var y = 0; y < 8; y++)
            {
                var row = "  ";
                for (var x = 0; x < 8; x++)
                {
                    row += $"{qt[y * 8 + x],4}";
                }
                _output.WriteLine(row);
            }
        }

        // Calculate what DC=73 means in terms of pixel values
        _output.WriteLine("");
        _output.WriteLine("DC coefficient interpretation:");
        var dcCoeff = 73;
        int dcQuantValue = qt![0];
        int dequantDc = dcCoeff * dcQuantValue;
        _output.WriteLine($"  Raw DC coefficient: {dcCoeff}");
        _output.WriteLine($"  DC quant value: {dcQuantValue}");
        _output.WriteLine($"  Dequantized DC: {dequantDc}");

        // The DC value after IDCT represents the average pixel value (scaled by 8)
        // Actually, it's average * 8 (because IDCT has 1/8 factor)
        // So pixel average = DC / 8
        // But we also need level shift (+128)

        double avgPixel = dequantDc / 8.0 + 128;
        _output.WriteLine($"  Average pixel value (approx): {avgPixel:F1}");
    }

    [Fact]
    public void TraceFullPipeline()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        byte[] data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        JpegFrame frame = reader.ReadFrame();

        var decoder = new EntropyDecoder(frame, data);
        short[][][] blocks = decoder.DecodeAllBlocks();

        _output.WriteLine("Block 0 (first block):");
        _output.WriteLine("  DCT coefficients:");
        for (var y = 0; y < 8; y++)
        {
            var row = "    ";
            for (var x = 0; x < 8; x++)
            {
                row += $"{blocks[0][0][y * 8 + x],5}";
            }
            _output.WriteLine(row);
        }

        // Dequantize
        var dequant = new Dequantizer(frame);
        int[][][] dequantBlocks = dequant.DequantizeAll(blocks);

        _output.WriteLine("");
        _output.WriteLine("  After dequantization:");
        for (var y = 0; y < 8; y++)
        {
            var row = "    ";
            for (var x = 0; x < 8; x++)
            {
                row += $"{dequantBlocks[0][0][y * 8 + x],5}";
            }
            _output.WriteLine(row);
        }

        // IDCT
        byte[] pixelBlock = InverseDct.Transform(dequantBlocks[0][0]);

        _output.WriteLine("");
        _output.WriteLine("  After IDCT (pixel values):");
        for (var y = 0; y < 8; y++)
        {
            var row = "    ";
            for (var x = 0; x < 8; x++)
            {
                row += $"{pixelBlock[y * 8 + x],5}";
            }
            _output.WriteLine(row);
        }
    }

    [Fact]
    public void TraceBlock40()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        byte[] data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        JpegFrame frame = reader.ReadFrame();

        var decoder = new EntropyDecoder(frame, data);
        short[][][] blocks = decoder.DecodeAllBlocks();

        _output.WriteLine("Block 40 (should be at pixel 16,8 for MCU 1 sub(0,1)):");
        _output.WriteLine("  DCT coefficients:");
        for (var y = 0; y < 8; y++)
        {
            var row = "    ";
            for (var x = 0; x < 8; x++)
            {
                row += $"{blocks[0][40][y * 8 + x],5}";
            }
            _output.WriteLine(row);
        }

        // Check if all coefficients are 0 except DC
        var allAcZero = true;
        for (var i = 1; i < 64; i++)
        {
            if (blocks[0][40][i] != 0)
            {
                allAcZero = false;
                break;
            }
        }
        _output.WriteLine($"  All AC = 0: {allAcZero}");
        _output.WriteLine($"  DC = {blocks[0][40][0]}");

        // Dequantize and IDCT
        var dequant = new Dequantizer(frame);
        int[][][] dequantBlocks = dequant.DequantizeAll(blocks);
        byte[] pixelBlock = InverseDct.Transform(dequantBlocks[0][40]);

        _output.WriteLine("");
        _output.WriteLine("  After IDCT (pixel values):");
        for (var y = 0; y < 8; y++)
        {
            var row = "    ";
            for (var x = 0; x < 8; x++)
            {
                row += $"{pixelBlock[y * 8 + x],5}";
            }
            _output.WriteLine(row);
        }
    }

    [Fact]
    public void TraceBlock20()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        byte[] data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        JpegFrame frame = reader.ReadFrame();

        var decoder = new EntropyDecoder(frame, data);
        short[][][] blocks = decoder.DecodeAllBlocks();

        _output.WriteLine("Block 20 (our first non-white, MCU 10 sub(0,0)):");
        _output.WriteLine("  DCT coefficients:");
        var hasNonZeroAc = false;
        for (var y = 0; y < 8; y++)
        {
            var row = "    ";
            for (var x = 0; x < 8; x++)
            {
                int idx = y * 8 + x;
                short val = blocks[0][20][idx];
                if (idx > 0 && val != 0) hasNonZeroAc = true;
                row += $"{val,5}";
            }
            _output.WriteLine(row);
        }
        _output.WriteLine($"  Has non-zero AC: {hasNonZeroAc}");
        _output.WriteLine($"  DC = {blocks[0][20][0]}");

        // Dequantize and IDCT
        var dequant = new Dequantizer(frame);
        int[][][] dequantBlocks = dequant.DequantizeAll(blocks);
        byte[] pixelBlock = InverseDct.Transform(dequantBlocks[0][20]);

        _output.WriteLine("");
        _output.WriteLine("  After IDCT (pixel values):");
        for (var y = 0; y < 8; y++)
        {
            var row = "    ";
            for (var x = 0; x < 8; x++)
            {
                row += $"{pixelBlock[y * 8 + x],5}";
            }
            _output.WriteLine(row);
        }
    }
}
