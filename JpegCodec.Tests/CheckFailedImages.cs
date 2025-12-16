using Xunit;
using Xunit.Abstractions;

namespace JpegCodec.Tests;

public class CheckFailedImages
{
    private readonly ITestOutputHelper _output;

    public CheckFailedImages(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string GetTestImagesPath()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "TestImages", "jpeg_test")))
        {
            dir = dir.Parent;
        }

        return dir != null
            ? Path.Combine(dir.FullName, "TestImages", "jpeg_test")
            : "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test";
    }

    [Fact]
    public void CheckSamplingFactors()
    {
        string testPath = GetTestImagesPath();

        var failedImages = new[]
        {
            "level3_multiple_blocks/gray_16x16_gradient_h.jpg",
            "level3_multiple_blocks/gray_16x16_gradient_v.jpg",
            "level3_multiple_blocks/gray_32x32_block_checker.jpg",
            "level5_color_420/color420_solid_blue.jpg",
            "level6_non_aligned/gray_9x9.jpg",
            "backhoe-006.jpg"  // This one works
        };

        foreach (string relativePath in failedImages)
        {
            string path = Path.Combine(testPath, relativePath);
            if (!File.Exists(path))
            {
                _output.WriteLine($"{relativePath}: FILE NOT FOUND");
                continue;
            }

            byte[] data = File.ReadAllBytes(path);
            var reader = new JpegReader(data);
            JpegFrame frame = reader.ReadFrame();

            _output.WriteLine($"{relativePath}:");
            _output.WriteLine($"  Size: {frame.Width}x{frame.Height}");
            _output.WriteLine($"  Components: {frame.ComponentCount}");
            _output.WriteLine($"  Max sampling: {frame.MaxHorizontalSamplingFactor}x{frame.MaxVerticalSamplingFactor}");

            for (var i = 0; i < frame.ComponentCount; i++)
            {
                JpegComponent comp = frame.Components[i];
                _output.WriteLine($"  Component {i}: {comp.HorizontalSamplingFactor}x{comp.VerticalSamplingFactor}");
            }
            _output.WriteLine("");
        }
    }
}
