# ImageLibrary

A comprehensive .NET image processing library providing native decoders for various image formats.

## Features

ImageLibrary provides pure C# implementations of image format decoders, with no native dependencies:

- **BmpCodec** - Windows Bitmap decoder
- **GifCodec** - Graphics Interchange Format decoder with animation support
- **Jbig2Codec** - JBIG2 bi-level image decoder (used in PDF documents)
- **Jp2Codec** - JPEG 2000 decoder with support for both reversible (5/3) and irreversible (9/7) wavelets
- **JpegCodec** - JPEG decoder
- **PngCodec** - Portable Network Graphics decoder
- **TgaCodec** - Truevision TGA decoder

## Installation

```bash
dotnet add package ImageLibrary
```

## Usage

### JPEG 2000 Decoding

```csharp
using Jp2Codec;

// Load and decode a JPEG 2000 file
byte[] jp2Data = File.ReadAllBytes("image.jp2");
var decoder = new Jp2Decoder(jp2Data);
byte[] pixels = decoder.Decode();

// Access image properties
int width = decoder.Codestream.Frame.Width;
int height = decoder.Codestream.Frame.Height;
int components = decoder.Codestream.Frame.Components.Length;
```

### JBIG2 Decoding

```csharp
using Jbig2Codec;

// Decode a JBIG2 image (commonly found in PDFs)
byte[] jbig2Data = File.ReadAllBytes("image.jbig2");
var decoder = new Jbig2Decoder(jbig2Data);
var bitmap = decoder.Decode();

// Access decoded bitmap
int width = bitmap.Width;
int height = bitmap.Height;
bool pixel = bitmap.GetPixel(x, y);
```

## Supported Formats

| Format | Read | Write | Notes |
|--------|------|-------|-------|
| BMP | Yes | No | All bit depths |
| GIF | Yes | No | Animated GIFs supported |
| JBIG2 | Yes | No | Full T.88 implementation |
| JPEG 2000 | Yes | No | JP2 and raw codestream |
| JPEG | Yes | No | Baseline and progressive |
| PNG | Yes | No | All color types and bit depths |
| TGA | Yes | No | Uncompressed and RLE |

## JPEG 2000 Implementation Details

The Jp2Codec provides a complete JPEG 2000 Part 1 decoder:

- **Entropy Decoding**: MQ arithmetic decoder with EBCOT (Embedded Block Coding with Optimized Truncation)
- **Wavelet Transform**: Both reversible 5/3 (lossless) and irreversible 9/7 (lossy) lifting implementations
- **Color Transform**: RCT (Reversible Color Transform) and ICT (Irreversible Color Transform)
- **Quantization**: Scalar quantization with configurable step sizes
- **Tiling**: Support for tiled images
- **Multiple Components**: Grayscale, RGB, and YCbCr with chroma subsampling

## JBIG2 Implementation Details

The Jbig2Codec provides a complete T.88 JBIG2 decoder:

- **Arithmetic Decoding**: Full QM-coder implementation
- **Huffman Decoding**: Standard and custom Huffman tables
- **Symbol Dictionaries**: Text region symbol aggregation and refinement
- **Generic Regions**: Templates 0-3 with adaptive pixels
- **Halftone Regions**: Pattern dictionary-based halftoning
- **MMR Compression**: Modified Modified READ encoding

## Requirements

- .NET 8.0 or later

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
