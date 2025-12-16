using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;
using Xunit.Abstractions;

namespace JpegCodec.Tests;

/// <summary>
/// Test if using decode-order storage matches ImageSharp.
/// </summary>
public class TestDecodeOrderStorage
{
    private readonly ITestOutputHelper _output;

    public TestDecodeOrderStorage(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void CompareWithDecodeOrderStorage()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        var decoder = new EntropyDecoder(frame, data);
        var blocks = decoder.DecodeAllBlocks();

        // Get the blocks in decode order by re-decoding sequentially
        decoder.Reset();
        var decodeOrderBlocks = new short[frame.McuCount * 4][];
        for (int i = 0; i < decodeOrderBlocks.Length; i++)
        {
            decodeOrderBlocks[i] = decoder.DecodeSingleBlock(0);
        }

        _output.WriteLine($"Total blocks: {decodeOrderBlocks.Length}");
        _output.WriteLine("");

        // Compare our block 20 with decode-order block 40
        _output.WriteLine("Comparing storage index 20 (our) with decode order 40:");
        _output.WriteLine($"  Our block 20 DC: {blocks[0][20][0]}");
        _output.WriteLine($"  Decode order 40 DC: {decodeOrderBlocks[40][0]}");

        bool match = true;
        for (int i = 0; i < 64; i++)
        {
            if (blocks[0][20][i] != decodeOrderBlocks[40][i])
            {
                match = false;
                break;
            }
        }
        _output.WriteLine($"  Full match: {match}");

        // Now let's create an image using decode-order storage and compare to ImageSharp
        _output.WriteLine("");
        _output.WriteLine("Testing if decode-order storage matches ImageSharp...");

        // Dequantize and IDCT the decode-order blocks
        var dequant = new Dequantizer(frame);
        var pixelBlocks = new byte[decodeOrderBlocks.Length][];

        // Create arrays for dequantizer
        var tempBlocks = new short[1][][];
        tempBlocks[0] = decodeOrderBlocks;
        var dequantBlocks = dequant.DequantizeAll(tempBlocks);

        for (int i = 0; i < decodeOrderBlocks.Length; i++)
        {
            pixelBlocks[i] = InverseDct.Transform(dequantBlocks[0][i]);
        }

        // Now create image using decode order mapping
        using var isImage = Image.Load<L8>(path);

        _output.WriteLine("Pixel comparison at (16,8) - MCU 1, sub(0,1):");
        // MCU 1, sub(0,1) = decode position 6 (MCU 1 is at position 1, sub(0,1) is 3rd sub-block = 4+2=6)
        int decodePos = 1 * 4 + 2; // MCU 1, sub-block index 2 (which is (0,1) in our iteration order)
        _output.WriteLine($"  Decode position: {decodePos}");
        _output.WriteLine($"  Our decode-order block {decodePos} first pixel: {pixelBlocks[decodePos][0]}");
        _output.WriteLine($"  ImageSharp pixel (16,8): {isImage[16, 8].PackedValue}");

        // Let's also check what decode position would give us the ImageSharp pixel value
        for (int dp = 0; dp < Math.Min(80, pixelBlocks.Length); dp++)
        {
            if (Math.Abs(pixelBlocks[dp][0] - isImage[16, 8].PackedValue) < 5)
            {
                _output.WriteLine($"  Found match at decode position {dp}: first pixel = {pixelBlocks[dp][0]}");
            }
        }

        // Check what decode position has the block that matches ImageSharp's (16,8) region
        _output.WriteLine("");
        _output.WriteLine("Finding which decode position matches ImageSharp (16,8)-(23,15) block:");
        byte[] isBlock = new byte[64];
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                isBlock[y * 8 + x] = isImage[16 + x, 8 + y].PackedValue;
            }
        }

        int bestMatch = -1;
        int bestScore = int.MaxValue;
        for (int dp = 0; dp < pixelBlocks.Length; dp++)
        {
            int score = 0;
            for (int i = 0; i < 64; i++)
            {
                score += Math.Abs(pixelBlocks[dp][i] - isBlock[i]);
            }
            if (score < bestScore)
            {
                bestScore = score;
                bestMatch = dp;
            }
        }
        _output.WriteLine($"  Best match: decode position {bestMatch} with score {bestScore}");

        // What MCU and sub-block is this?
        int mcuIdx = bestMatch / 4;
        int subIdx = bestMatch % 4;
        int mcuX = mcuIdx % frame.McuCountX;
        int mcuY = mcuIdx / frame.McuCountX;
        int subX = subIdx % 2;
        int subY = subIdx / 2;
        _output.WriteLine($"  MCU ({mcuX}, {mcuY}), sub-block ({subX}, {subY})");
    }

    [Fact]
    public void AnalyzeStorageMapping()
    {
        // Create a mapping from decode order to spatial storage index
        _output.WriteLine("Decode order to spatial storage index mapping for first 80 positions:");
        _output.WriteLine("DecodePos | MCU(x,y) | Sub(x,y) | SpatialIdx | PixelStart");
        _output.WriteLine("----------|----------|----------|------------|----------");

        int blocksPerRow = 38;
        int mcuCountX = 19;

        for (int decodePos = 0; decodePos < 80; decodePos++)
        {
            int mcuIdx = decodePos / 4;
            int subIdx = decodePos % 4;

            int mcuX = mcuIdx % mcuCountX;
            int mcuY = mcuIdx / mcuCountX;

            // Our sub-block iteration order is (0,0), (1,0), (0,1), (1,1)
            // subIdx 0 = (0,0), 1 = (1,0), 2 = (0,1), 3 = (1,1)
            int subX = subIdx % 2;
            int subY = subIdx / 2;

            int globalBlockX = mcuX * 2 + subX;
            int globalBlockY = mcuY * 2 + subY;
            int spatialIdx = globalBlockY * blocksPerRow + globalBlockX;

            int pixelX = globalBlockX * 8;
            int pixelY = globalBlockY * 8;

            if (decodePos < 20 || decodePos == 40 || decodePos == 41 || decodePos == 6)
            {
                _output.WriteLine($"    {decodePos,5} | ({mcuX,2},{mcuY,2}) | ({subX},{subY})    |       {spatialIdx,4} | ({pixelX,3},{pixelY,3})");
            }
        }
    }
}
