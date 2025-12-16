# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-12-15

### Added

#### Unified Library Structure
- **Consolidated Architecture**: Single `ImageLibrary` package containing all codecs with namespace-based organization
  - `ImageLibrary.Bmp` - BMP codec
  - `ImageLibrary.Gif` - GIF codec
  - `ImageLibrary.Png` - PNG codec
  - `ImageLibrary.Tga` - TGA codec
  - `ImageLibrary.Jpeg` - JPEG codec
  - `ImageLibrary.Jbig2` - JBIG2 codec
  - `ImageLibrary.Jp2` - JPEG 2000 codec
- **Clean Public API**: Well-defined public surface with internal implementation details
- **Zero Dependencies**: Pure C# with no external packages or native libraries
- **Cross-Platform**: .NET Standard 2.1 compatible with all modern .NET runtimes

#### Unified Convenience Methods
- **Decoder Convenience Methods**: All decoders now support three input methods
  - `Decode(byte[] data)` or constructor for byte arrays
  - `Decode(Stream stream)` or constructor for streams
  - `Decode(string path)` or constructor for file paths
  - Static decoders: `BmpDecoder`, `GifDecoder`, `PngDecoder`, `TgaDecoder`
  - Instance decoders: `JpegDecoder`, `Jbig2Decoder`, `Jp2Decoder` (constructor overloads)
- **Encoder Convenience Methods**: All encoders now support three output methods
  - `Encode(...) → byte[]` - returns encoded data as byte array
  - `Encode(..., Stream stream)` - writes encoded data to stream
  - `Encode(..., string path)` - writes encoded data to file
  - All encoders: `BmpEncoder`, `GifEncoder`, `PngEncoder`, `TgaEncoder`

#### JPEG 2000 Decoder (ImageLibrary.Jp2)
- Complete JPEG 2000 Part 1 (ISO 15444-1) implementation
- **Entropy Decoding**:
  - MQ arithmetic decoder with full context modeling
  - EBCOT (Embedded Block Coding with Optimized Truncation)
  - Code-block contribution parsing
- **Wavelet Transform**:
  - Reversible 5/3 wavelet (lossless compression)
  - Irreversible 9/7 wavelet (lossy compression)
  - Multi-level decomposition with proper subband placement
- **Color Transforms**:
  - RCT (Reversible Color Transform) for lossless
  - ICT (Irreversible Color Transform) for lossy
- **Features**:
  - Scalar dequantization with configurable step sizes
  - Tiled image support
  - Multiple components (grayscale, RGB, YCbCr)
  - Chroma subsampling (4:4:4, 4:2:2, 4:2:0)
  - DC level shifting for unsigned samples
- **API**: `Jp2Decoder.Decode()`, intermediate data access for debugging

#### JBIG2 Decoder (ImageLibrary.Jbig2)
- Complete ITU T.88 JBIG2 bi-level image decoder
- **Entropy Decoding**:
  - QM-coder arithmetic decoder with 19 contexts
  - Standard Huffman table support
  - Custom Huffman table decoding
- **Region Types**:
  - Generic regions (templates 0-3 with adaptive pixels)
  - Symbol dictionary segments with refinement
  - Text region decoding with symbol placement
  - Halftone region decoding (pattern dictionary-based)
  - Refinement regions for progressive decoding
- **Compression**:
  - MMR (Modified Modified READ) compression
  - Arithmetic coding with context-based probability estimation
- **Security**:
  - `Jbig2DecoderOptions` with configurable resource limits
  - Default and Strict presets for untrusted input
  - DoS protection via iteration limits and size constraints
- **API**: `Jbig2Decoder.Decode()`, `Bitmap` bi-level image class

#### JPEG Decoder (ImageLibrary.Jpeg)
- Baseline DCT JPEG (JFIF) decoder
- Progressive DCT support
- **Features**:
  - Huffman entropy decoding
  - Fast inverse DCT
  - YCbCr to RGB color conversion
  - Chroma subsampling (4:4:4, 4:2:2, 4:2:0, 4:1:1)
  - Multiple scan decoding (spectral selection, successive approximation)
- **API**: `JpegDecoder.Decode()`, `DecodedImage` result type

#### BMP Codec (ImageLibrary.Bmp)
- **Decoder**: All standard bit depths (1, 4, 8, 16, 24, 32 bpp)
- **Encoder**: 24-bit and 32-bit output
- **Compression**: RLE4, RLE8, uncompressed
- **Features**: Top-down and bottom-up DIB support
- **API**: `BmpDecoder.Decode()`, `BmpEncoder.Encode()`, `BmpImage` class

#### GIF Codec (ImageLibrary.Gif)
- **Decoder**: Full GIF87a and GIF89a support
- **Encoder**: Single-frame GIF encoding
- **Features**:
  - LZW compression/decompression (internal)
  - 256-color indexed palette
  - Animated GIF support (decoder)
  - Interlaced images
  - Transparency support
- **API**: `GifDecoder.Decode()`, `GifEncoder.Encode()`, `GifImage` class

#### PNG Codec (ImageLibrary.Png)
- **Decoder**: Full PNG specification (ISO/IEC 15948)
- **Encoder**: All color types and bit depths
- **Color Types**: Grayscale, RGB, Indexed, Grayscale+Alpha, RGBA
- **Bit Depths**: 1, 2, 4, 8, 16 bits per channel
- **Features**:
  - DEFLATE (zlib) compression/decompression
  - PNG filtering (Sub, Up, Average, Paeth)
  - Adam7 interlacing
  - Critical chunks (IHDR, PLTE, IDAT, IEND)
  - Ancillary chunks support
  - CRC-32 validation (internal)
- **API**: `PngDecoder.Decode()`, `PngEncoder.Encode()`, `PngImage` class, `PngColorType` enum

#### TGA Codec (ImageLibrary.Tga)
- **Decoder**: Truevision TGA format support
- **Encoder**: Uncompressed and RLE compression
- **Features**:
  - 8, 16, 24, 32-bit color depths
  - RLE compression
  - Top-down and bottom-up orientation
  - Alpha channel support
- **API**: `TgaDecoder.Decode()`, `TgaEncoder.Encode()`, `TgaImage` class

### Public API

**Decoders**: `BmpDecoder`, `GifDecoder`, `PngDecoder`, `TgaDecoder`, `JpegDecoder`, `Jbig2Decoder`, `Jp2Decoder`

**Encoders**: `BmpEncoder`, `GifEncoder`, `PngEncoder`, `TgaEncoder`

**Image Types**: `BmpImage`, `GifImage`, `PngImage`, `TgaImage`, `DecodedImage`, `Bitmap`

**Exceptions**: `BmpException`, `GifException`, `PngException`, `TgaException`, `JpegException`, `Jbig2Exception`, `Jp2Exception`

**Configuration**: `Jbig2DecoderOptions`, `PngColorType`, `CombinationOperator`

**JPEG 2000 Types**: `Jp2Codestream`, `Jp2Frame`, `Jp2Component`, `CodingParameters`, `QuantizationParameters`, and related enums/structs

### Internal Implementation

All format-specific structures, helper classes, and sub-decoders are marked `internal` for a clean API surface:
- Format structures (BitmapFileHeader, GifHeader, etc.)
- Helper classes (BitReader, Crc32, LzwDecoder, ColorConverter, etc.)
- Sub-decoders (MqDecoder, EbcotDecoder, ArithmeticDecoder, HuffmanDecoder, etc.)
- Internal data structures (TagTree, MqState, SegmentHeader, etc.)

### Technical Details

- **Target Framework**: .NET Standard 2.1
- **Language Features**: C# 7.3 compatible (collection expressions converted to array initializers)
- **Performance**: Zero-allocation hot paths with `Span<T>` and `stackalloc`
- **Security**: Input validation, bounds checking, resource limits
- **Thread Safety**: Thread-safe for concurrent use on different instances

[1.0.0]: https://github.com/yourusername/ImageLibrary/releases/tag/v1.0.0
