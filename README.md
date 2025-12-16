# ImageLibrary

A comprehensive, pure C# image processing library providing native encoders and decoders for various image formats with no external dependencies.

## Features

ImageLibrary is a single, unified library targeting .NET Standard 2.1 with complete implementations of multiple image formats:

- **BMP** - Windows Bitmap format (encode/decode)
- **GIF** - Graphics Interchange Format with animation support (encode/decode)
- **PNG** - Portable Network Graphics with all color types (encode/decode)
- **TGA** - Truevision TGA format (encode/decode)
- **JPEG** - JPEG/JFIF baseline and progressive (decode only)
- **JBIG2** - Bi-level compression for monochrome images (decode only)
- **JPEG 2000** - Advanced wavelet-based compression (decode only)

All codecs are pure C# with **zero native dependencies** and work cross-platform on any .NET Standard 2.1+ runtime.

## Installation

```bash
dotnet add package ImageLibrary
```

## Quick Start

### Decoding Images

All decoders support **three convenient ways** to load images:

```csharp
using ImageLibrary.Bmp;
using ImageLibrary.Gif;
using ImageLibrary.Png;
using ImageLibrary.Jpeg;

// Method 1: From byte array
byte[] data = GetImageData();
BmpImage image = BmpDecoder.Decode(data);

// Method 2: From Stream
using var stream = File.OpenRead("image.bmp");
BmpImage image = BmpDecoder.Decode(stream);

// Method 3: From file path (most convenient!)
BmpImage image = BmpDecoder.Decode("image.bmp");
GifFile gif = GifDecoder.Decode("animation.gif");
PngImage png = PngDecoder.Decode("image.png");
TgaImage tga = TgaDecoder.Decode("image.tga");

// JPEG (instance-based decoder)
var jpegDecoder = new JpegDecoder("photo.jpg");
DecodedImage jpeg = jpegDecoder.Decode();

// JBIG2 (monochrome images)
var jbig2Decoder = new Jbig2Decoder("scanned.jbig2");
Bitmap bitmap = jbig2Decoder.Decode();

// JPEG 2000
var jp2Decoder = new Jp2Decoder("image.jp2");
byte[] pixels = jp2Decoder.Decode();
```

### Encoding Images

All encoders support **three convenient ways** to save images:

```csharp
using ImageLibrary.Bmp;
using ImageLibrary.Gif;
using ImageLibrary.Png;
using ImageLibrary.Tga;

// Create an image (32-bit BGRA format)
var image = new BmpImage(width: 800, height: 600);
image.SetPixel(x, y, r: 255, g: 128, b: 64, a: 255);

// Method 1: To byte array
byte[] bmpData = BmpEncoder.Encode(image, bitsPerPixel: 24);
byte[] pngData = PngEncoder.Encode(new PngImage(image.PixelData, width, height));
byte[] tgaData = TgaEncoder.Encode(new TgaImage(width, height, image.PixelData));
byte[] gifData = GifEncoder.Encode(new GifImage(width, height, image.PixelData));

// Method 2: To Stream
using var stream = File.Create("output.bmp");
BmpEncoder.Encode(image, stream, bitsPerPixel: 24);

// Method 3: To file path (most convenient!)
BmpEncoder.Encode(image, "output.bmp", bitsPerPixel: 24);
PngEncoder.Encode(new PngImage(image.PixelData, width, height), "output.png");
TgaEncoder.Encode(new TgaImage(width, height, image.PixelData), "output.tga");
GifEncoder.Encode(new GifImage(width, height, image.PixelData), "output.gif");
```

## Format-Specific Examples

### JPEG 2000 (Advanced)

```csharp
using ImageLibrary.Jp2;

// Load from file, stream, or byte array
var decoder = new Jp2Decoder("image.jp2");
// var decoder = new Jp2Decoder(stream);
// var decoder = new Jp2Decoder(byteArray);

// Access codestream parameters before decoding
var frame = decoder.Codestream.Frame;
Console.WriteLine($"Size: {frame.Width}x{frame.Height}");
Console.WriteLine($"Components: {frame.Components.Length}");
Console.WriteLine($"Bit depth: {frame.Components[0].BitDepth}");

// Decode to interleaved RGB/YCbCr pixel data
byte[] pixels = decoder.Decode();

// Access intermediate decoding stages for debugging
var intermediate = decoder.GetIntermediateData();
var tier2Output = intermediate.Tier2Output;
var dwtCoefficients = intermediate.DwtCoefficients;
```

### JBIG2 (Monochrome Compression)

```csharp
using ImageLibrary.Jbig2;

// Load from file, stream, or byte array
var decoder = new Jbig2Decoder("scanned_page.jbig2");
// var decoder = new Jbig2Decoder(stream, globalData: null, options: null);
// var decoder = new Jbig2Decoder(byteArray, globalData: null, options: null);

// Optional: Configure resource limits for untrusted input
var options = Jbig2DecoderOptions.Strict;  // Lower limits
var strictDecoder = new Jbig2Decoder("untrusted.jbig2", globalData: null, options);

var bitmap = decoder.Decode();

// Access bi-level bitmap data
int width = bitmap.Width;
int height = bitmap.Height;
bool isBlack = bitmap.GetPixel(x, y);  // true = black, false = white

// Blit (composite) bitmaps with combination operators
var target = new Bitmap(1000, 1000);
target.Blit(bitmap, x: 100, y: 100, CombinationOperator.Or);
```

### Working with Pixel Data

All decoded images provide access to pixel data:

```csharp
// BMP, GIF, PNG, TGA images use 32-bit BGRA format
var image = BmpDecoder.Decode(data);
Console.WriteLine($"Size: {image.Width}x{image.Height}");
Console.WriteLine($"Stride: {image.Stride} bytes per row");

// Get pixel color
var (r, g, b, a) = image.GetPixel(x, y);

// Set pixel color
image.SetPixel(x, y, r: 255, g: 0, b: 0, a: 255);

// Direct access to pixel data (BGRA format, top-down)
byte[] pixels = image.PixelData;
int offset = (y * image.Width + x) * 4;
byte blue = pixels[offset];
byte green = pixels[offset + 1];
byte red = pixels[offset + 2];
byte alpha = pixels[offset + 3];
```

## Supported Formats

| Format | Read | Write | Color Modes | Compression |
|--------|------|-------|-------------|-------------|
| BMP | ✓ | ✓ | 1/4/8/16/24/32-bpp | Uncompressed, RLE4, RLE8 |
| GIF | ✓ | ✓ | Indexed 256 colors | LZW |
| PNG | ✓ | ✓ | Grayscale, RGB, Indexed, +Alpha | DEFLATE (zlib) |
| TGA | ✓ | ✓ | 8/16/24/32-bpp | Uncompressed, RLE |
| JPEG | ✓ | ✗ | Grayscale, YCbCr | DCT (baseline, progressive) |
| JBIG2 | ✓ | ✗ | Bi-level (1-bpp) | Arithmetic, Huffman, MMR |
| JPEG 2000 | ✓ | ✗ | Grayscale, RGB, YCbCr | Wavelet (5/3, 9/7) |

## Implementation Details

### JPEG 2000 Decoder

Complete JPEG 2000 Part 1 (ISO 15444-1) implementation:

- **Tier-1 Decoding**: MQ arithmetic decoder with EBCOT (Embedded Block Coding with Optimized Truncation)
- **Tier-2 Decoding**: Packet header parsing, tag trees, code-block contributions
- **Wavelet Transform**:
  - Reversible 5/3 filter (lossless compression)
  - Irreversible 9/7 filter (lossy compression)
  - Multi-level decomposition with proper coefficient placement
- **Color Transforms**:
  - RCT (Reversible Color Transform) for lossless
  - ICT (Irreversible Color Transform) for lossy
- **Dequantization**: Scalar quantization with configurable step sizes
- **Features**: Tiling, multiple components, chroma subsampling, region of interest

### JBIG2 Decoder

Complete ITU T.88 JBIG2 implementation:

- **Arithmetic Coding**: QM-coder with 19 contexts for EBCOT-style decoding
- **Huffman Coding**: Standard tables and custom Huffman table decoding
- **Region Types**:
  - Generic regions (templates 0-3 with adaptive pixels)
  - Symbol dictionaries with refinement
  - Text regions (symbol placement and composition)
  - Halftone regions (pattern dictionary-based)
  - Refinement regions (progressive decoding)
- **Compression**: MMR (Modified Modified READ) for fax-like compression
- **Security**: Configurable resource limits to prevent DoS from malicious streams

### JPEG Decoder

Baseline and progressive JPEG (JFIF) decoder:

- **DCT**: Fast inverse discrete cosine transform
- **Entropy Decoding**: Huffman decoding with DC/AC coefficient extraction
- **Color Conversion**: YCbCr to RGB, grayscale passthrough
- **Chroma Subsampling**: 4:4:4, 4:2:2, 4:2:0, 4:1:1 support
- **Progressive**: Multiple scans with spectral selection and successive approximation

### PNG Decoder/Encoder

Full PNG specification support:

- **Color Types**: Grayscale, RGB, Indexed (palette), Grayscale+Alpha, RGBA
- **Bit Depths**: 1, 2, 4, 8, 16 bits per channel
- **Filtering**: Sub, Up, Average, Paeth predictors for optimal compression
- **Interlacing**: Adam7 interlacing for progressive rendering
- **Compression**: DEFLATE (zlib) compression/decompression
- **Chunks**: Critical chunks (IHDR, PLTE, IDAT, IEND) and ancillary chunks

## Public API

The library exposes a clean, focused API surface:

### Public Types

**Decoders**: `BmpDecoder`, `GifDecoder`, `PngDecoder`, `TgaDecoder`, `JpegDecoder`, `Jbig2Decoder`, `Jp2Decoder`
**Encoders**: `BmpEncoder`, `GifEncoder`, `PngEncoder`, `TgaEncoder`
**Images**: `BmpImage`, `GifImage`, `PngImage`, `TgaImage`, `DecodedImage` (JPEG), `Bitmap` (JBIG2)
**Exceptions**: `BmpException`, `GifException`, `PngException`, `TgaException`, `JpegException`, `Jbig2Exception`, `Jp2Exception`
**Configuration**: `Jbig2DecoderOptions`, `PngColorType`, `CombinationOperator`

All format-specific structures, helper classes, and internal decoders are marked `internal` for a clean API surface.

## Requirements

- **.NET Standard 2.1** or later
  - .NET Core 3.0+
  - .NET 5.0+
  - .NET 6.0+
  - .NET 7.0+
  - .NET 8.0+
  - .NET 9.0+
  - .NET 10.0+
  - Mono 6.4+
  - Unity 2021.2+

## Performance

- **Zero allocations** in critical decode loops using `Span<T>` and `stackalloc`
- **SIMD optimizations** where applicable (platform-specific)
- **Lazy evaluation** for metadata-only queries
- **Streaming support** for large images
- **Memory efficient** with configurable resource limits

## Security

- **Resource limits** configurable via options (JBIG2)
- **Input validation** at all parsing stages
- **Bounds checking** for buffer operations
- **DoS protection** via iteration limits and size constraints
- **No unsafe code** (except for optimized SIMD paths)

## Thread Safety

All decoders and encoders are **thread-safe for concurrent use on different instances**. Do not share a single decoder/encoder instance across threads.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Development Setup

```bash
# Clone the repository
git clone https://github.com/yourusername/ImageLibrary.git
cd ImageLibrary

# Build the library
dotnet build ImageLibrary/ImageLibrary.csproj

# Run tests
dotnet test
```

## Acknowledgments

- JPEG 2000 implementation based on ISO 15444-1 specification
- JBIG2 implementation based on ITU T.88 specification
- PNG implementation based on ISO/IEC 15948 specification
- JPEG implementation based on ITU T.81 specification

## Roadmap

- [ ] JPEG encoding support
- [ ] TIFF decoder
- [ ] WebP decoder/encoder
- [ ] AVIF decoder
- [ ] HEIF decoder
- [ ] Multi-page/animated format support (TIFF, WebP, AVIF)
