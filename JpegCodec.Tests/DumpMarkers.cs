using Xunit;
using Xunit.Abstractions;

namespace JpegCodec.Tests;

/// <summary>
/// Dump all markers in the JPEG file.
/// </summary>
public class DumpMarkers
{
    private readonly ITestOutputHelper _output;

    public DumpMarkers(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void DumpAllMarkers()
    {
        var path = "/Users/michaeljordan/RiderProjects/ImageLibrary/TestImages/jpeg_test/backhoe-006.jpg";
        byte[] data = File.ReadAllBytes(path);

        _output.WriteLine($"File size: {data.Length} bytes");
        _output.WriteLine("");
        _output.WriteLine("Markers found:");

        var i = 0;
        while (i < data.Length - 1)
        {
            if (data[i] == 0xFF && data[i + 1] != 0x00 && data[i + 1] != 0xFF)
            {
                byte marker = data[i + 1];
                string name = GetMarkerName(marker);

                // Markers without length: D0-D9 (RST, SOI, EOI) and 01 (TEM)
                bool hasLength = !(marker >= 0xD0 && marker <= 0xD9) && marker != 0x01;

                if (hasLength && marker >= 0xC0 && marker <= 0xFE)
                {
                    // Marker with length
                    if (i + 3 < data.Length)
                    {
                        int len = (data[i + 2] << 8) | data[i + 3];
                        _output.WriteLine($"  [{i,5}] 0xFF{marker:X2} {name,-10} length={len}");

                        // Show extra info for certain markers
                        if (marker == 0xC0 || marker == 0xC2) // SOF
                        {
                            if (i + 9 < data.Length)
                            {
                                int height = (data[i + 5] << 8) | data[i + 6];
                                int width = (data[i + 7] << 8) | data[i + 8];
                                int nf = data[i + 9];
                                _output.WriteLine($"           SOF: {width}x{height}, {nf} components");

                                for (var c = 0; c < nf && i + 10 + c * 3 + 2 < data.Length; c++)
                                {
                                    int compId = data[i + 10 + c * 3];
                                    int sampling = data[i + 10 + c * 3 + 1];
                                    int hSamp = sampling >> 4;
                                    int vSamp = sampling & 0x0F;
                                    int qtId = data[i + 10 + c * 3 + 2];
                                    _output.WriteLine($"           Component {compId}: {hSamp}x{vSamp} sampling, QT={qtId}");
                                }
                            }
                        }
                        else if (marker == 0xDA) // SOS
                        {
                            if (i + 4 < data.Length)
                            {
                                int ns = data[i + 4];
                                _output.WriteLine($"           SOS: {ns} components");
                                for (var c = 0; c < ns && i + 5 + c * 2 + 1 < data.Length; c++)
                                {
                                    int compId = data[i + 5 + c * 2];
                                    int tables = data[i + 5 + c * 2 + 1];
                                    int dcId = tables >> 4;
                                    int acId = tables & 0x0F;
                                    _output.WriteLine($"           Component {compId}: DC={dcId}, AC={acId}");
                                }
                                int ss = data[i + 5 + ns * 2];
                                int se = data[i + 5 + ns * 2 + 1];
                                int ahAl = data[i + 5 + ns * 2 + 2];
                                _output.WriteLine($"           Ss={ss}, Se={se}, Ah={ahAl >> 4}, Al={ahAl & 0x0F}");
                            }
                        }
                        else if (marker == 0xEE) // APP14 Adobe
                        {
                            _output.WriteLine($"           Adobe APP14 marker");
                            if (i + 16 < data.Length)
                            {
                                byte colorTransform = data[i + 15];
                                _output.WriteLine($"           Color transform: {colorTransform}");
                            }
                        }
                        else if (marker == 0xDD) // DRI
                        {
                            if (i + 5 < data.Length)
                            {
                                int ri = (data[i + 4] << 8) | data[i + 5];
                                _output.WriteLine($"           Restart interval: {ri}");
                            }
                        }

                        i += 2 + len;
                    }
                    else
                    {
                        i += 2;
                    }
                }
                else if (marker >= 0xD0 && marker <= 0xD9) // RST, SOI, EOI
                {
                    _output.WriteLine($"  [{i,5}] 0xFF{marker:X2} {name}");
                    i += 2;
                }
                else
                {
                    _output.WriteLine($"  [{i,5}] 0xFF{marker:X2} {name}");
                    i += 2;
                }
            }
            else
            {
                i++;
            }
        }
    }

    private static string GetMarkerName(byte marker) => marker switch
    {
        0xD8 => "SOI",
        0xD9 => "EOI",
        0xC0 => "SOF0",
        0xC2 => "SOF2",
        0xC4 => "DHT",
        0xDB => "DQT",
        0xDA => "SOS",
        0xDD => "DRI",
        0xE0 => "APP0",
        0xE1 => "APP1",
        0xEE => "APP14",
        0xFE => "COM",
        _ when marker >= 0xD0 && marker <= 0xD7 => $"RST{marker - 0xD0}",
        _ when marker >= 0xE0 && marker <= 0xEF => $"APP{marker - 0xE0}",
        _ => $"UNK"
    };
}
