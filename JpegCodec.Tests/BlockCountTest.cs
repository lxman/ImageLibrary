using Xunit;
using Xunit.Abstractions;

namespace JpegCodec.Tests;

public class BlockCountTest
{
    private readonly ITestOutputHelper _output;

    public BlockCountTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void AnalyzeBackhoeBlockCount()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        var data = File.ReadAllBytes(path);

        var reader = new JpegReader(data);
        var frame = reader.ReadFrame();

        _output.WriteLine($"Image: {frame.Width}x{frame.Height}");
        _output.WriteLine($"Components: {frame.ComponentCount}");

        var comp = frame.Components[0];
        _output.WriteLine($"Component 0: hSamp={comp.HorizontalSamplingFactor}, vSamp={comp.VerticalSamplingFactor}");
        _output.WriteLine($"MaxHSamp={frame.MaxHorizontalSamplingFactor}, MaxVSamp={frame.MaxVerticalSamplingFactor}");
        _output.WriteLine($"MCUs: {frame.McuCountX}x{frame.McuCountY} = {frame.McuCountX * frame.McuCountY}");

        // Current (buggy) formula
        int hBlocks = (frame.Width * comp.HorizontalSamplingFactor + frame.MaxHorizontalSamplingFactor * 8 - 1)
                      / (frame.MaxHorizontalSamplingFactor * 8) * comp.HorizontalSamplingFactor;
        int vBlocks = (frame.Height * comp.VerticalSamplingFactor + frame.MaxVerticalSamplingFactor * 8 - 1)
                      / (frame.MaxVerticalSamplingFactor * 8) * comp.VerticalSamplingFactor;
        _output.WriteLine($"Current formula: {hBlocks}x{vBlocks} = {hBlocks * vBlocks} blocks");

        // Correct formula
        int hBlocksCorrect = (frame.Width + frame.MaxHorizontalSamplingFactor * 8 - 1)
                             / (frame.MaxHorizontalSamplingFactor * 8) * comp.HorizontalSamplingFactor;
        int vBlocksCorrect = (frame.Height + frame.MaxVerticalSamplingFactor * 8 - 1)
                             / (frame.MaxVerticalSamplingFactor * 8) * comp.VerticalSamplingFactor;
        _output.WriteLine($"Correct formula: {hBlocksCorrect}x{vBlocksCorrect} = {hBlocksCorrect * vBlocksCorrect} blocks");

        // Expected
        int expectedH = frame.McuCountX * comp.HorizontalSamplingFactor;
        int expectedV = frame.McuCountY * comp.VerticalSamplingFactor;
        _output.WriteLine($"Expected: {expectedH}x{expectedV} = {expectedH * expectedV} blocks");

        // Verify
        Assert.Equal(expectedH, hBlocksCorrect);
        Assert.Equal(expectedV, vBlocksCorrect);
    }
}
