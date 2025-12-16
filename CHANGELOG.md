# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-12-15

### Added

- **Jp2Codec**: JPEG 2000 Part 1 decoder
  - MQ arithmetic decoder with EBCOT entropy decoding
  - Reversible 5/3 wavelet transform (lossless)
  - Irreversible 9/7 wavelet transform (lossy)
  - RCT and ICT color transforms
  - Scalar dequantization
  - Support for tiled images
  - Grayscale, RGB, and YCbCr color spaces
  - Chroma subsampling support

- **Jbig2Codec**: JBIG2 bi-level image decoder (T.88)
  - QM-coder arithmetic decoding
  - Standard and custom Huffman table decoding
  - Symbol dictionary segments with refinement
  - Text region decoding
  - Generic region decoding (templates 0-3)
  - Halftone region decoding
  - Pattern dictionary support
  - MMR compression support
  - Configurable resource limits for security

- **BmpCodec**: Windows Bitmap decoder
  - All standard bit depths (1, 4, 8, 16, 24, 32 bpp)
  - RLE4 and RLE8 compression

- **GifCodec**: GIF decoder
  - LZW decompression
  - Animated GIF support
  - Interlaced images

- **JpegCodec**: JPEG decoder
  - Baseline DCT
  - Progressive DCT
  - Huffman decoding

- **PngCodec**: PNG decoder
  - All color types (grayscale, RGB, indexed, with/without alpha)
  - All bit depths
  - Interlaced images
  - zlib decompression

- **TgaCodec**: Truevision TGA decoder
  - Uncompressed images
  - RLE compression
  - All color depths

[1.0.0]: https://github.com/yourusername/ImageLibrary/releases/tag/v1.0.0
