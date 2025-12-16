using Jp2Codec.Pipeline;
using Jp2Codec.Tier1;
using Jp2Codec.Tier2;
using Xunit.Abstractions;

namespace Jp2Codec.Tests;

/// <summary>
/// Tests for Tier-1 decoder (EBCOT arithmetic decoding).
/// </summary>
public class Tier1DecoderTests
{
    private readonly ITestOutputHelper _output;

    public Tier1DecoderTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string GetTestImagesPath()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "TestImages", "jp2_test")))
        {
            dir = dir.Parent;
        }

        return dir != null
            ? Path.Combine(dir.FullName, "TestImages", "jp2_test")
            : "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jp2_test";
    }

    [Fact]
    public void MqDecoder_DecodesBits()
    {
        // A minimal MQ-encoded stream (all zeros with uniform context)
        // This is more of a smoke test
        byte[] data = [0x00, 0x00, 0x00, 0x00];
        var mq = new MqDecoder(data);

        // Try to decode some bits - it should not crash
        // The actual values depend on the stream
        for (var i = 0; i < 10; i++)
        {
            try
            {
                int bit = mq.Decode(0); // Use uniform context
            }
            catch (Jp2Exception)
            {
                // Expected when data runs out
                break;
            }
        }

        _output.WriteLine("MQ decoder basic test passed");
    }

    [Fact]
    public void EbcotDecoder_DecodesEmptyBlock()
    {
        var ebcot = new EbcotDecoder();

        // Empty code-block
        var block = new Pipeline.CodeBlockBitstream
        {
            BlockX = 0,
            BlockY = 0,
            Width = 8,
            Height = 8,
            CodingPasses = 0,
            ZeroBitPlanes = 0,
            Data = [],
        };

        int[,] result = ebcot.Process(block);

        Assert.Equal(8, result.GetLength(0));
        Assert.Equal(8, result.GetLength(1));

        // Should be all zeros
        for (var y = 0; y < 8; y++)
        {
            for (var x = 0; x < 8; x++)
            {
                Assert.Equal(0, result[y, x]);
            }
        }
    }

    [Fact]
    public void Tier1Decoder_DecodesSimpleCodestream_8x8()
    {
        string path = Path.Combine(GetTestImagesPath(), "test_8x8.jp2");
        byte[] data = File.ReadAllBytes(path);

        // Parse codestream
        var fileReader = new Jp2FileReader(data);
        Jp2FileInfo fileInfo = fileReader.Read();
        var codestreamReader = new CodestreamReader(fileInfo.CodestreamData!);
        Jp2Codestream codestream = codestreamReader.ReadMainHeader();

        // Read tile-part
        Jp2TilePart? tilePart = codestreamReader.ReadTilePart();
        Assert.NotNull(tilePart);

        // Tier-2 decode
        var tier2 = new Tier2Decoder(codestream);
        Tier2Output tier2Output = tier2.Process(tilePart);

        _output.WriteLine($"Tier-2: {tier2Output.CodeBlocks.Sum(r => r.Sum(s => s.Length))} code-blocks");

        // Tier-1 decode
        var tier1 = new Tier1Decoder(codestream);
        QuantizedSubband[] subbands = tier1.DecodeToSubbands(tier2Output);

        _output.WriteLine($"\nTier-1 output: {subbands.Length} subbands");
        foreach (QuantizedSubband subband in subbands)
        {
            _output.WriteLine($"  {subband.Type}: {subband.Width}x{subband.Height} @ res {subband.ResolutionLevel}");

            // Show some coefficient values
            int[,] coefs = subband.Coefficients;
            var nonZero = 0;
            var maxAbs = 0;
            for (var y = 0; y < coefs.GetLength(0); y++)
            {
                for (var x = 0; x < coefs.GetLength(1); x++)
                {
                    if (coefs[y, x] != 0) nonZero++;
                    maxAbs = Math.Max(maxAbs, Math.Abs(coefs[y, x]));
                }
            }
            _output.WriteLine($"    Non-zero: {nonZero}, Max abs: {maxAbs}");
        }

        Assert.True(subbands.Length > 0, "Expected at least one subband");
    }

    [Fact]
    public void Tier1Decoder_ShowsCoefficients_8x8()
    {
        string path = Path.Combine(GetTestImagesPath(), "test_8x8.jp2");
        byte[] data = File.ReadAllBytes(path);

        var fileReader = new Jp2FileReader(data);
        Jp2FileInfo fileInfo = fileReader.Read();
        var codestreamReader = new CodestreamReader(fileInfo.CodestreamData!);
        Jp2Codestream codestream = codestreamReader.ReadMainHeader();

        Jp2TilePart? tilePart = codestreamReader.ReadTilePart();
        var tier2 = new Tier2Decoder(codestream);
        Tier2Output tier2Output = tier2.Process(tilePart);

        var tier1 = new Tier1Decoder(codestream);
        QuantizedSubband[] subbands = tier1.DecodeToSubbands(tier2Output);

        // For an 8x8 image with no decomposition, we should have just the LL subband
        Assert.True(subbands.Length >= 1);

        QuantizedSubband ll = subbands.First(s => s.Type == Pipeline.SubbandType.LL);
        _output.WriteLine($"LL subband ({ll.Width}x{ll.Height}):");

        int[,] coefs = ll.Coefficients;
        for (var y = 0; y < ll.Height; y++)
        {
            string row = string.Join(" ", Enumerable.Range(0, ll.Width)
                .Select(x => coefs[y, x].ToString().PadLeft(5)));
            _output.WriteLine(row);
        }

        _output.WriteLine($"\nQuantization: {ll.StepSize}");
    }

    [Fact]
    public void Tier1Decoder_DecodesMultiResolution_16x16()
    {
        string path = Path.Combine(GetTestImagesPath(), "test_16x16.jp2");
        byte[] data = File.ReadAllBytes(path);

        var fileReader = new Jp2FileReader(data);
        Jp2FileInfo fileInfo = fileReader.Read();
        var codestreamReader = new CodestreamReader(fileInfo.CodestreamData!);
        Jp2Codestream codestream = codestreamReader.ReadMainHeader();

        _output.WriteLine($"Image: {codestream.Frame.Width}x{codestream.Frame.Height}");
        _output.WriteLine($"Decomposition: {codestream.CodingParameters.DecompositionLevels} levels");

        Jp2TilePart? tilePart = codestreamReader.ReadTilePart();
        var tier2 = new Tier2Decoder(codestream);
        Tier2Output tier2Output = tier2.Process(tilePart);

        var tier1 = new Tier1Decoder(codestream);
        QuantizedSubband[] subbands = tier1.DecodeToSubbands(tier2Output);

        _output.WriteLine($"\nSubbands decoded:");
        foreach (QuantizedSubband subband in subbands)
        {
            int[,] coefs = subband.Coefficients;
            var nonZero = 0;
            for (var y = 0; y < coefs.GetLength(0); y++)
                for (var x = 0; x < coefs.GetLength(1); x++)
                    if (coefs[y, x] != 0) nonZero++;

            _output.WriteLine($"  Res {subband.ResolutionLevel} {subband.Type}: {subband.Width}x{subband.Height}, {nonZero} non-zero");
        }

        // Should have LL at resolution 0, and HL/LH/HH at resolution 1
        Assert.Contains(subbands, s => s.Type == Pipeline.SubbandType.LL);

        if (codestream.CodingParameters.DecompositionLevels > 0)
        {
            Assert.Contains(subbands, s => s.Type == Pipeline.SubbandType.HL);
            Assert.Contains(subbands, s => s.Type == Pipeline.SubbandType.LH);
            Assert.Contains(subbands, s => s.Type == Pipeline.SubbandType.HH);
        }
    }

    [Fact]
    public void DiagnoseCodeBlockData_8x8()
    {
        string path = Path.Combine(GetTestImagesPath(), "test_8x8.jp2");
        byte[] data = File.ReadAllBytes(path);

        var fileReader = new Jp2FileReader(data);
        Jp2FileInfo fileInfo = fileReader.Read();
        var codestreamReader = new CodestreamReader(fileInfo.CodestreamData!);
        Jp2Codestream codestream = codestreamReader.ReadMainHeader();

        _output.WriteLine($"Codestream info:");
        _output.WriteLine($"  Size: {codestream.Frame.Width}x{codestream.Frame.Height}");
        _output.WriteLine($"  Components: {codestream.Frame.ComponentCount}");
        _output.WriteLine($"  Decomposition: {codestream.CodingParameters.DecompositionLevels} levels");
        _output.WriteLine($"  Wavelet: {codestream.CodingParameters.WaveletType}");
        _output.WriteLine($"  CodeBlock: {codestream.CodingParameters.CodeBlockWidth}x{codestream.CodingParameters.CodeBlockHeight}");
        _output.WriteLine($"  Layers: {codestream.CodingParameters.LayerCount}");
        _output.WriteLine($"  Quantization: {codestream.QuantizationParameters.Style}");
        _output.WriteLine($"  Guard bits: {codestream.QuantizationParameters.GuardBits}");
        _output.WriteLine($"  Step sizes: {codestream.QuantizationParameters.StepSizes.Length}");

        Jp2TilePart? tilePart = codestreamReader.ReadTilePart();
        Assert.NotNull(tilePart);

        _output.WriteLine($"\nTile-part:");
        _output.WriteLine($"  TileIndex: {tilePart.TileIndex}");
        _output.WriteLine($"  Bitstream length: {tilePart.BitstreamData?.Length ?? 0}");

        if (tilePart.BitstreamData != null && tilePart.BitstreamData.Length > 0)
        {
            _output.WriteLine($"  First 32 bytes: {BitConverter.ToString(tilePart.BitstreamData.Take(32).ToArray())}");
        }

        // Tier-2 decode
        var tier2 = new Tier2Decoder(codestream);
        Tier2Output tier2Output = tier2.Process(tilePart);

        _output.WriteLine($"\nTier-2 output:");
        _output.WriteLine($"  Resolution levels: {tier2Output.ResolutionLevels}");

        for (var r = 0; r < tier2Output.CodeBlocks.Length; r++)
        {
            _output.WriteLine($"\n  Resolution {r}:");
            for (var s = 0; s < tier2Output.CodeBlocks[r].Length; s++)
            {
                CodeBlockBitstream[] subbandBlocks = tier2Output.CodeBlocks[r][s];
                _output.WriteLine($"    Subband {s}: {subbandBlocks.Length} code-blocks");

                foreach (CodeBlockBitstream cb in subbandBlocks)
                {
                    _output.WriteLine($"      Block ({cb.BlockX},{cb.BlockY}) {cb.Width}x{cb.Height}:");
                    _output.WriteLine($"        CodingPasses: {cb.CodingPasses}");
                    _output.WriteLine($"        ZeroBitPlanes: {cb.ZeroBitPlanes}");
                    _output.WriteLine($"        Data length: {cb.Data?.Length ?? 0}");

                    if (cb.Data != null && cb.Data.Length > 0)
                    {
                        byte[] preview = cb.Data.Take(16).ToArray();
                        _output.WriteLine($"        Data preview: {BitConverter.ToString(preview)}");
                    }
                }
            }
        }

        // Decode with EBCOT and show coefficients
        var tier1 = new Tier1Decoder(codestream);
        QuantizedSubband[] subbands = tier1.DecodeToSubbands(tier2Output);

        _output.WriteLine($"\nEBCOT decoded subbands:");
        foreach (QuantizedSubband subband in subbands)
        {
            _output.WriteLine($"  {subband.Type} ({subband.Width}x{subband.Height}):");

            int[,] coefs = subband.Coefficients;
            var nonZero = 0;
            var sum = 0;
            var max = int.MinValue;
            var min = int.MaxValue;

            for (var y = 0; y < coefs.GetLength(0); y++)
            {
                for (var x = 0; x < coefs.GetLength(1); x++)
                {
                    int v = coefs[y, x];
                    if (v != 0) nonZero++;
                    sum += v;
                    max = Math.Max(max, v);
                    min = Math.Min(min, v);
                }
            }

            _output.WriteLine($"    NonZero: {nonZero}, Sum: {sum}, Min: {min}, Max: {max}");
        }

        // Manually trace MQ decoding of first few bits
        byte[]? cbData = tier2Output.CodeBlocks[0][0][0].Data;
        if (cbData != null && cbData.Length > 0)
        {
            _output.WriteLine($"\nMQ Decoder trace (first 20 symbols with context 17 - run-length):");
            var mq = new MqDecoder(cbData);
            for (var i = 0; i < 20; i++)
            {
                try
                {
                    int bit = mq.Decode(17); // Run-length context
                    _output.WriteLine($"  Symbol {i}: {bit} (marker: {mq.MarkerFound})");
                    if (mq.MarkerFound) break;
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"  Symbol {i}: Exception - {ex.Message}");
                    break;
                }
            }
        }

        // Trace what the EBCOT decoder actually does
        _output.WriteLine($"\nEBCOT detailed trace:");
        CodeBlockBitstream codeBlock = tier2Output.CodeBlocks[0][0][0];
        _output.WriteLine($"  CodingPasses: {codeBlock.CodingPasses}");
        _output.WriteLine($"  ZeroBitPlanes: {codeBlock.ZeroBitPlanes}");
        _output.WriteLine($"  For 8-bit precision: {8 - codeBlock.ZeroBitPlanes} effective bit-planes");
        _output.WriteLine($"  Expected passes for full decode: {1 + (8 - codeBlock.ZeroBitPlanes - 1) * 3}");

        // Show raw EBCOT coefficients
        _output.WriteLine($"\nRaw EBCOT coefficients (before dequantization):");
        QuantizedSubband llSubband = subbands.First(s => s.Type == Pipeline.SubbandType.LL);
        for (var y = 0; y < 8; y++)
        {
            string row = string.Join(" ", Enumerable.Range(0, 8)
                .Select(x => llSubband.Coefficients[y, x].ToString().PadLeft(5)));
            _output.WriteLine($"  {row}");
        }
    }
}