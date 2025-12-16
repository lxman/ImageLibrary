using Xunit;
using Xunit.Abstractions;

namespace JpegCodec.Tests;

/// <summary>
/// Trace Huffman decoding for the first few blocks.
/// </summary>
public class TraceHuffman
{
    private readonly ITestOutputHelper _output;

    public TraceHuffman(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TraceFirstBits()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        _output.WriteLine($"Entropy data starts at offset {frame.EntropyDataOffset}");
        _output.WriteLine("");

        // Show first 40 bytes of entropy data in binary
        _output.WriteLine("First 20 bytes of entropy data:");
        for (int i = 0; i < 20; i++)
        {
            int offset = frame.EntropyDataOffset + i;
            byte b = data[offset];
            _output.WriteLine($"  [{offset}] 0x{b:X2} = {Convert.ToString(b, 2).PadLeft(8, '0')}");
        }

        _output.WriteLine("");

        // Show DC Huffman table
        var dcSpec = frame.DcHuffmanTables[0]!;
        _output.WriteLine("DC Huffman table 0 symbols by code length:");
        int symbolIdx = 0;
        for (int len = 1; len <= 16; len++)
        {
            if (dcSpec.CodeCounts[len - 1] > 0)
            {
                string symbols = "";
                for (int j = 0; j < dcSpec.CodeCounts[len - 1]; j++)
                {
                    symbols += $"{dcSpec.Symbols[symbolIdx++]:X2} ";
                }
                _output.WriteLine($"  {len} bits: {symbols}");
            }
        }
    }

    [Fact]
    public void ManualDcDecode()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        _output.WriteLine("Manual DC decoding trace:");
        _output.WriteLine("");

        // Build Huffman table manually to trace
        var dcSpec = frame.DcHuffmanTables[0]!;
        var table = new HuffmanTable(dcSpec);

        // Create bit reader
        var bitReader = new BitReader(data, frame.EntropyDataOffset, frame.EntropyDataLength);

        // Decode first few DC values
        int dcPredictor = 0;
        for (int block = 0; block < 10; block++)
        {
            // Read DC category
            int bitsRead = 0;
            int code = 0;
            byte dcCategory = 0;
            bool found = false;

            // Manually trace the code matching
            for (int len = 1; len <= 16 && !found; len++)
            {
                code = (code << 1) | bitReader.ReadBit();
                bitsRead++;

                // Check if this code matches
                // (We need to check the Huffman table internals)
            }

            // Use the actual table decode
            bitReader.Reset();

            // Skip bits for previous blocks
            // This is tricky because we need to know how many bits each block used
            // Let's just decode block by block properly

            _output.WriteLine($"Block {block}:");

            // Show first 16 bits
            string bits = "";
            for (int j = 0; j < 16; j++)
            {
                bits += bitReader.PeekBits(j + 1) % 2;
            }
            _output.WriteLine($"  Next 16 bits: {bits}");

            // Decode DC
            dcCategory = table.DecodeSymbol(bitReader);
            _output.WriteLine($"  DC category: {dcCategory}");

            if (dcCategory > 0)
            {
                int dcBits = bitReader.ReadBits(dcCategory);
                _output.WriteLine($"  DC extra bits: {dcBits} ({dcCategory} bits)");

                // Convert to signed
                int threshold = 1 << (dcCategory - 1);
                int dcDiff = dcBits < threshold ? dcBits - ((1 << dcCategory) - 1) : dcBits;
                _output.WriteLine($"  DC diff: {dcDiff}");

                dcPredictor += dcDiff;
            }
            _output.WriteLine($"  DC value: {dcPredictor}");
            _output.WriteLine("");

            // Now skip the rest of the block (AC coefficients)
            // This is the tricky part - we need to decode them to advance the bit position
            var acSpec = frame.AcHuffmanTables[0]!;
            var acTable = new HuffmanTable(acSpec);

            int k = 1;
            while (k < 64)
            {
                byte symbol = acTable.DecodeSymbol(bitReader);
                if (symbol == 0x00) break; // EOB
                if (symbol == 0xF0) { k += 16; continue; } // ZRL

                int run = symbol >> 4;
                int size = symbol & 0x0F;
                k += run + 1;

                if (size > 0 && k <= 64)
                {
                    bitReader.ReadBits(size); // Skip AC value bits
                }
            }
        }
    }

    [Fact]
    public void ShowDcHuffmanCodes()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        var dcSpec = frame.DcHuffmanTables[0]!;

        _output.WriteLine("DC Huffman codes:");

        // Generate the actual codes
        int code = 0;
        int symbolIdx = 0;
        for (int len = 1; len <= 16; len++)
        {
            for (int i = 0; i < dcSpec.CodeCounts[len - 1]; i++)
            {
                byte symbol = dcSpec.Symbols[symbolIdx++];
                _output.WriteLine($"  {Convert.ToString(code, 2).PadLeft(len, '0')} ({len} bits) -> category {symbol}");
                code++;
            }
            code <<= 1;
        }
    }
}
