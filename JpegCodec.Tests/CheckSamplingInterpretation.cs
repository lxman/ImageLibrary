using Xunit;
using Xunit.Abstractions;

namespace JpegCodec.Tests;

/// <summary>
/// Check how sampling factors affect block interpretation.
/// </summary>
public class CheckSamplingInterpretation
{
    private readonly ITestOutputHelper _output;

    public CheckSamplingInterpretation(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void CompareBlockStorageAssumption()
    {
        // If ImageSharp treats grayscale 2x2 as 1x1 effective sampling,
        // then their block layout would be different.
        //
        // With TRUE 2x2: 38x40 blocks = 1520 blocks, MCU = 16x16 pixels
        // With 1x1: 38x39 blocks = 1482 blocks, MCU = 8x8 pixels (but header says 2x2...)
        //
        // Actually for grayscale, the "sampling factor" in JPEG determines MCU size
        // even without chroma. So 2x2 means 16x16 pixel MCUs.

        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        byte[] data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        JpegFrame frame = reader.ReadFrame();

        _output.WriteLine($"Header says: {frame.Components[0].HorizontalSamplingFactor}x{frame.Components[0].VerticalSamplingFactor}");
        _output.WriteLine($"Max sampling: {frame.MaxHorizontalSamplingFactor}x{frame.MaxVerticalSamplingFactor}");
        _output.WriteLine($"MCU size: {frame.MaxHorizontalSamplingFactor * 8}x{frame.MaxVerticalSamplingFactor * 8}");
        _output.WriteLine($"MCU count: {frame.McuCountX}x{frame.McuCountY}");
        _output.WriteLine("");

        // Now let's test a different hypothesis:
        // What if we should be using 1x1 layout for ColorConverter even though
        // EntropyDecoder uses 2x2 for MCU decoding?
        //
        // This would mean the blocks are stored in a grid of:
        // Width: ceil(304/8) = 38
        // Height: ceil(309/8) = 39
        // But only some blocks are actually populated...

        _output.WriteLine("If treated as 1x1 layout:");
        int blocksX_1x1 = (frame.Width + 7) / 8;  // 38
        int blocksY_1x1 = (frame.Height + 7) / 8; // 39
        _output.WriteLine($"  Blocks: {blocksX_1x1}x{blocksY_1x1}");
        _output.WriteLine($"  Total: {blocksX_1x1 * blocksY_1x1}");

        _output.WriteLine("");
        _output.WriteLine("If treated as 2x2 layout:");
        int blocksX_2x2 = frame.McuCountX * 2;  // 38
        int blocksY_2x2 = frame.McuCountY * 2;  // 40
        _output.WriteLine($"  Blocks: {blocksX_2x2}x{blocksY_2x2}");
        _output.WriteLine($"  Total: {blocksX_2x2 * blocksY_2x2}");
    }

    [Fact]
    public void TestAlternativeBlockLayout()
    {
        // Try reading with different block layout assumptions
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        byte[] data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        JpegFrame frame = reader.ReadFrame();

        var decoder = new EntropyDecoder(frame, data);
        short[][][] blocks = decoder.DecodeAllBlocks();

        _output.WriteLine($"Total blocks decoded: {blocks[0].Length}");
        _output.WriteLine("");

        // Find first non-white block assuming OUR layout (38 blocks per row)
        _output.WriteLine("Our layout (38 blocks/row):");
        for (var i = 0; i < blocks[0].Length; i++)
        {
            if (blocks[0][i][0] != 73)
            {
                int blockX = i % 38;
                int blockY = i / 38;
                _output.WriteLine($"  First non-white at index {i}: block ({blockX},{blockY}), pixel ({blockX * 8},{blockY * 8})");
                break;
            }
        }

        // Try assuming different blocks per row
        // The content shows up at index 20 in our image.
        // ImageSharp puts it at index 40 (block 2,1).
        // If we had 20 blocks per row instead of 38, index 20 would be (0,1) not (20,0)
        // That's 20 blocks per row...
        //
        // Actually, 19 MCUs per row, each contributing 2 blocks = 38 blocks per row.
        // But what if we're off by one?

        _output.WriteLine("");
        _output.WriteLine("Alternative: if 40 blocks per row:");
        var idx = 20;
        int altBlockX = idx % 40;
        int altBlockY = idx / 40;
        _output.WriteLine($"  Index 20 would be block ({altBlockX},{altBlockY}), pixel ({altBlockX * 8},{altBlockY * 8})");

        // What blocks per row would make index 20 = block (2,1)?
        // y * bpr + x = 20
        // 1 * bpr + 2 = 20
        // bpr = 18
        _output.WriteLine("");
        _output.WriteLine("If 18 blocks per row:");
        altBlockX = 20 % 18;
        altBlockY = 20 / 18;
        _output.WriteLine($"  Index 20 would be block ({altBlockX},{altBlockY}), pixel ({altBlockX * 8},{altBlockY * 8})");

        // Or maybe MCU iteration order is different?
        // What if MCUs are processed column-first instead of row-first?
    }

    [Fact]
    public void CheckMcuVsBlockRowDifference()
    {
        // The key insight: ImageSharp sees content at block (2,1) = index 40.
        // We see content at block (20,0) = index 20.
        //
        // Block (2,1) is in MCU (1,0) [mcuX=2/2=1, mcuY=1/2=0]
        // Block (20,0) is in MCU (10,0)
        //
        // So MCU 1 vs MCU 10. MCU 10 is 9 MCUs later.
        // 9 MCUs * 4 blocks = 36 blocks difference in decode sequence.
        //
        // But storage index difference is 40 - 20 = 20.
        //
        // If our blocks are stored correctly but ImageSharp stores differently...
        // What if ImageSharp stores blocks in decode order, not spatial order?

        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        byte[] data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        JpegFrame frame = reader.ReadFrame();

        _output.WriteLine("Analyzing the index mismatch:");
        _output.WriteLine("");

        // If blocks were stored in DECODE order (not spatial):
        // MCU 0: decode indices 0,1,2,3
        // MCU 1: decode indices 4,5,6,7
        // ...
        // The 7th decoded block (index 6) is MCU 1's sub-block (0,1).
        //
        // In ImageSharp, that would be at storage index 6, not 40.
        // But ImageSharp says it's at index 40...

        _output.WriteLine("Decode sequence analysis:");
        _output.WriteLine("MCU 1's sub-block (0,1) is the 7th decoded block (0-indexed: 6)");
        _output.WriteLine("MCU 10's sub-block (0,0) is the 41st decoded block (0-indexed: 40)");
        _output.WriteLine("");

        // Wait - our index 20 has MCU 10's sub(0,0).
        // MCU 10 decodes 4 blocks starting at decode position 10*4=40.
        // So decode position 40 goes to... storage index?
        //
        // For MCU 10 sub(0,0):
        // globalBlockX = 10*2 + 0 = 20
        // globalBlockY = 0*2 + 0 = 0
        // storage index = 0*38 + 20 = 20

        _output.WriteLine("Decode position 40 -> MCU 10 sub(0,0) -> storage index 20");
        _output.WriteLine("Decode position 6 -> MCU 1 sub(0,1) -> storage index 40");
        _output.WriteLine("");

        _output.WriteLine("So decode positions and storage indices are swapped!");
        _output.WriteLine("Decode pos 6 -> storage 40");
        _output.WriteLine("Decode pos 40 -> storage 20");
        _output.WriteLine("");

        _output.WriteLine("This suggests we're storing blocks correctly by spatial position,");
        _output.WriteLine("but maybe ImageSharp stores by decode order and READS differently?");
    }

    [Fact]
    public void VerifyActualDecodeOrder()
    {
        // Let's decode the file and track actual DC values by decode order
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        byte[] data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        JpegFrame frame = reader.ReadFrame();

        var decoder = new EntropyDecoder(frame, data);
        short[][][] blocks = decoder.DecodeAllBlocks();

        _output.WriteLine("Checking if decode order 40 has different DC than storage 40:");
        _output.WriteLine("");

        // Decode order 40 = MCU 10, sub(0,0) = storage 20
        _output.WriteLine($"Storage index 20 (decode order 40): DC = {blocks[0][20][0]}");
        // Decode order 6 = MCU 1, sub(0,1) = storage 40
        _output.WriteLine($"Storage index 40 (decode order 6): DC = {blocks[0][40][0]}");
        _output.WriteLine("");

        // Now let's manually track decode order by re-reading with instrumentation
        _output.WriteLine("First 50 decode positions and their DC values:");
        var decodePos = 0;
        for (var mcuY = 0; mcuY < frame.McuCountY && decodePos < 50; mcuY++)
        {
            for (var mcuX = 0; mcuX < frame.McuCountX && decodePos < 50; mcuX++)
            {
                for (var subY = 0; subY < 2 && decodePos < 50; subY++)
                {
                    for (var subX = 0; subX < 2 && decodePos < 50; subX++)
                    {
                        int gx = mcuX * 2 + subX;
                        int gy = mcuY * 2 + subY;
                        int storageIdx = gy * 38 + gx;
                        short dc = blocks[0][storageIdx][0];

                        if (dc != 73 || decodePos < 10 || decodePos == 40 || decodePos == 6)
                        {
                            _output.WriteLine($"  Decode #{decodePos}: MCU({mcuX},{mcuY}) sub({subX},{subY}) -> storage {storageIdx}: DC = {dc}");
                        }
                        decodePos++;
                    }
                }
            }
        }
    }
}
