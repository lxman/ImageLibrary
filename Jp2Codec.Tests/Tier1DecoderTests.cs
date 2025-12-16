using Jp2Codec;
using Jp2Codec.Tier1;
using Jp2Codec.Tier2;
using Xunit;
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
        for (int i = 0; i < 10; i++)
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

        var result = ebcot.Process(block);

        Assert.Equal(8, result.GetLength(0));
        Assert.Equal(8, result.GetLength(1));

        // Should be all zeros
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                Assert.Equal(0, result[y, x]);
            }
        }
    }

    [Fact]
    public void Tier1Decoder_DecodesSimpleCodestream_8x8()
    {
        var path = Path.Combine(GetTestImagesPath(), "test_8x8.jp2");
        var data = File.ReadAllBytes(path);

        // Parse codestream
        var fileReader = new Jp2FileReader(data);
        var fileInfo = fileReader.Read();
        var codestreamReader = new CodestreamReader(fileInfo.CodestreamData!);
        var codestream = codestreamReader.ReadMainHeader();

        // Read tile-part
        var tilePart = codestreamReader.ReadTilePart();
        Assert.NotNull(tilePart);

        // Tier-2 decode
        var tier2 = new Tier2Decoder(codestream);
        var tier2Output = tier2.Process(tilePart);

        _output.WriteLine($"Tier-2: {tier2Output.CodeBlocks.Sum(r => r.Sum(s => s.Length))} code-blocks");

        // Tier-1 decode
        var tier1 = new Tier1Decoder(codestream);
        var subbands = tier1.DecodeToSubbands(tier2Output);

        _output.WriteLine($"\nTier-1 output: {subbands.Length} subbands");
        foreach (var subband in subbands)
        {
            _output.WriteLine($"  {subband.Type}: {subband.Width}x{subband.Height} @ res {subband.ResolutionLevel}");

            // Show some coefficient values
            var coefs = subband.Coefficients;
            int nonZero = 0;
            int maxAbs = 0;
            for (int y = 0; y < coefs.GetLength(0); y++)
            {
                for (int x = 0; x < coefs.GetLength(1); x++)
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
        var path = Path.Combine(GetTestImagesPath(), "test_8x8.jp2");
        var data = File.ReadAllBytes(path);

        var fileReader = new Jp2FileReader(data);
        var fileInfo = fileReader.Read();
        var codestreamReader = new CodestreamReader(fileInfo.CodestreamData!);
        var codestream = codestreamReader.ReadMainHeader();

        var tilePart = codestreamReader.ReadTilePart();
        var tier2 = new Tier2Decoder(codestream);
        var tier2Output = tier2.Process(tilePart);

        var tier1 = new Tier1Decoder(codestream);
        var subbands = tier1.DecodeToSubbands(tier2Output);

        // For an 8x8 image with no decomposition, we should have just the LL subband
        Assert.True(subbands.Length >= 1);

        var ll = subbands.First(s => s.Type == Pipeline.SubbandType.LL);
        _output.WriteLine($"LL subband ({ll.Width}x{ll.Height}):");

        var coefs = ll.Coefficients;
        for (int y = 0; y < ll.Height; y++)
        {
            var row = string.Join(" ", Enumerable.Range(0, ll.Width)
                .Select(x => coefs[y, x].ToString().PadLeft(5)));
            _output.WriteLine(row);
        }

        _output.WriteLine($"\nQuantization: {ll.StepSize}");
    }

    [Fact]
    public void Tier1Decoder_DecodesMultiResolution_16x16()
    {
        var path = Path.Combine(GetTestImagesPath(), "test_16x16.jp2");
        var data = File.ReadAllBytes(path);

        var fileReader = new Jp2FileReader(data);
        var fileInfo = fileReader.Read();
        var codestreamReader = new CodestreamReader(fileInfo.CodestreamData!);
        var codestream = codestreamReader.ReadMainHeader();

        _output.WriteLine($"Image: {codestream.Frame.Width}x{codestream.Frame.Height}");
        _output.WriteLine($"Decomposition: {codestream.CodingParameters.DecompositionLevels} levels");

        var tilePart = codestreamReader.ReadTilePart();
        var tier2 = new Tier2Decoder(codestream);
        var tier2Output = tier2.Process(tilePart);

        var tier1 = new Tier1Decoder(codestream);
        var subbands = tier1.DecodeToSubbands(tier2Output);

        _output.WriteLine($"\nSubbands decoded:");
        foreach (var subband in subbands)
        {
            var coefs = subband.Coefficients;
            int nonZero = 0;
            for (int y = 0; y < coefs.GetLength(0); y++)
                for (int x = 0; x < coefs.GetLength(1); x++)
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
        var path = Path.Combine(GetTestImagesPath(), "test_8x8.jp2");
        var data = File.ReadAllBytes(path);

        var fileReader = new Jp2FileReader(data);
        var fileInfo = fileReader.Read();
        var codestreamReader = new CodestreamReader(fileInfo.CodestreamData!);
        var codestream = codestreamReader.ReadMainHeader();

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

        var tilePart = codestreamReader.ReadTilePart();
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
        var tier2Output = tier2.Process(tilePart);

        _output.WriteLine($"\nTier-2 output:");
        _output.WriteLine($"  Resolution levels: {tier2Output.ResolutionLevels}");

        for (int r = 0; r < tier2Output.CodeBlocks.Length; r++)
        {
            _output.WriteLine($"\n  Resolution {r}:");
            for (int s = 0; s < tier2Output.CodeBlocks[r].Length; s++)
            {
                var subbandBlocks = tier2Output.CodeBlocks[r][s];
                _output.WriteLine($"    Subband {s}: {subbandBlocks.Length} code-blocks");

                foreach (var cb in subbandBlocks)
                {
                    _output.WriteLine($"      Block ({cb.BlockX},{cb.BlockY}) {cb.Width}x{cb.Height}:");
                    _output.WriteLine($"        CodingPasses: {cb.CodingPasses}");
                    _output.WriteLine($"        ZeroBitPlanes: {cb.ZeroBitPlanes}");
                    _output.WriteLine($"        Data length: {cb.Data?.Length ?? 0}");

                    if (cb.Data != null && cb.Data.Length > 0)
                    {
                        var preview = cb.Data.Take(16).ToArray();
                        _output.WriteLine($"        Data preview: {BitConverter.ToString(preview)}");
                    }
                }
            }
        }

        // Decode with EBCOT and show coefficients
        var tier1 = new Tier1Decoder(codestream);
        var subbands = tier1.DecodeToSubbands(tier2Output);

        _output.WriteLine($"\nEBCOT decoded subbands:");
        foreach (var subband in subbands)
        {
            _output.WriteLine($"  {subband.Type} ({subband.Width}x{subband.Height}):");

            var coefs = subband.Coefficients;
            int nonZero = 0;
            int sum = 0;
            int max = int.MinValue;
            int min = int.MaxValue;

            for (int y = 0; y < coefs.GetLength(0); y++)
            {
                for (int x = 0; x < coefs.GetLength(1); x++)
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
        var cbData = tier2Output.CodeBlocks[0][0][0].Data;
        if (cbData != null && cbData.Length > 0)
        {
            _output.WriteLine($"\nMQ Decoder trace (first 20 symbols with context 17 - run-length):");
            var mq = new MqDecoder(cbData);
            for (int i = 0; i < 20; i++)
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
        var codeBlock = tier2Output.CodeBlocks[0][0][0];
        _output.WriteLine($"  CodingPasses: {codeBlock.CodingPasses}");
        _output.WriteLine($"  ZeroBitPlanes: {codeBlock.ZeroBitPlanes}");
        _output.WriteLine($"  For 8-bit precision: {8 - codeBlock.ZeroBitPlanes} effective bit-planes");
        _output.WriteLine($"  Expected passes for full decode: {1 + (8 - codeBlock.ZeroBitPlanes - 1) * 3}");

        // Show raw EBCOT coefficients
        _output.WriteLine($"\nRaw EBCOT coefficients (before dequantization):");
        var llSubband = subbands.First(s => s.Type == Pipeline.SubbandType.LL);
        for (int y = 0; y < 8; y++)
        {
            var row = string.Join(" ", Enumerable.Range(0, 8)
                .Select(x => llSubband.Coefficients[y, x].ToString().PadLeft(5)));
            _output.WriteLine($"  {row}");
        }
    }

    /// <summary>
    /// Compare MQ decoder outputs between our implementation and Melville's.
    /// </summary>
    [Fact]
    public void CompareMqDecoders_SameInput()
    {
        // Get the actual codeblock data from test_8x8
        var path = Path.Combine(GetTestImagesPath(), "test_8x8.jp2");
        var data = File.ReadAllBytes(path);

        var fileReader = new Jp2FileReader(data);
        var fileInfo = fileReader.Read();
        var codestreamReader = new CodestreamReader(fileInfo.CodestreamData!);
        var codestream = codestreamReader.ReadMainHeader();
        var tilePart = codestreamReader.ReadTilePart()!;
        var tier2 = new Tier2Decoder(codestream);
        var tier2Output = tier2.Process(tilePart);

        // Get the first code block's data
        var cb = tier2Output.CodeBlocks[0][0].First();
        var cbData = cb.Data!;
        
        _output.WriteLine($"Code block data ({cbData.Length} bytes): {BitConverter.ToString(cbData.Take(32).ToArray())}");

        // Create our MQ decoder
        var ourMq = new MqDecoder(cbData);

        // Create Melville's MQ decoder
        var melvilleByteInput = new CoreJ2K.j2k.entropy.decoder.ByteInputBuffer(cbData);
        var melvilleInitStates = new int[] { 46, 3, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        var melvilleMq = new CoreJ2K.j2k.entropy.decoder.MQDecoder(melvilleByteInput, 19, melvilleInitStates);

        // Decode first 100 symbols with both decoders and compare
        // Use a longer context sequence covering cleanup and sigprop passes
        _output.WriteLine("\nComparing MQ decoders (first 100 decodes):");
        int mismatches = 0;

        // Context sequence from our MQ trace - covering cleanup pass bp=29 through early sigprop bp=28
        var contexts = new[] {
            1, 0, 0, 11, 7, 2, 2, 5, 3, 2, 2, 1, 1, 1, 1, 0, 0, 11, 2, 2,  // MQ[0]-[19]
            3, 11, 9, 2, 3, 11, 9, 3, 1, 1, 0, 0, 11, 2, 2, 3, 11, 9, 2,  // MQ[20]-[38]
            3, 11, 9, 3, 4, 11, 9, 3, 2, 9, 3, 2, 2, 3, 2, 2, 2, 1,        // MQ[39]-[56] (end of cleanup)
            7, 12, 7, 12, 7, 5, 14, 9, 5, 3, 5, 14, 8, 5, 14, 8, 3, 5, 14, 8, 3, 9, 5, 14, 8, 10, 15, 9, 9, 9  // MQ[57]-[86] (sigprop)
        };

        for (int i = 0; i < contexts.Length; i++)
        {
            int ctx = contexts[i];
            int ourResult = ourMq.Decode(ctx);
            int melvilleResult = melvilleMq.decodeSymbol(ctx);

            string match = ourResult == melvilleResult ? "✓" : "✗";
            if (ourResult != melvilleResult || i < 20 || (i >= 57 && i < 70))
                _output.WriteLine($"  [{i}] ctx={ctx}: ours={ourResult}, melville={melvilleResult} {match}");

            if (ourResult != melvilleResult)
                mismatches++;
        }

        _output.WriteLine($"\nTotal mismatches: {mismatches}");
        Assert.Equal(0, mismatches);
    }
}