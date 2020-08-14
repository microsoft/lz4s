# Introduction

LZ4s is a compression algorithm supporting random access within the compressed content. It was designed to support efficient loading of portions of large compressed JSON documents. LZ4s provides very fast decompression speeds (over 1 GB/s per core) and supports easy parallel decompression for even higher speeds.

LZ4s is a tweak of the excellent LZ4 compression algorithm. LZ4 consists of a series of "tokens". Each token is a set of new (literal) bytes, followed by a set of bytes copied from within the last 64 KB in the decompressed content. LZ4s changes this by copying bytes from the **compressed**, rather than decompressed, content, and reduces the window from 64 KB to 8 KB. This means that decompressing from any particular position can be done simply by loading from the 8 KB before the desired point and then using those previous bytes as the context for decompression.

In order to map positions in the original document to positions in the compressed document, LZ4s also includes an index after the compressed data which identify where every 512th byte in the original document appears in the compressed form. This index is 8 bytes per 512 bytes, or 1.56% of the original file size.



# Why?

We were working with large JSON files. They compress very well (ZIP to 5-10% of original minified size), so we wanted to store them compressed. We typically want to load only a small part of the overall data. We designed a map, JsonMap, to quickly identify parts of the document we want to load or want to exclude. We then needed a way to efficiently load only the desired portion of the file without having to decompress all of it.

After surveying existing compression algorithms (DEFLATE/ZIP, LZ4, ZStandard, Snappy, Brotli), we found that many offered good compression levels and speeds, but none were designed for random access. They compress content using context from the decompressed data, which means that one must decompress from the beginning of the document (or the beginning of a "block"). Further, while block boundaries allow decompression from at least some points within the content, each block is compressed separately to allow this, so duplication across these boundaries can't be used.

We decided to try implementing this slight modification of the LZ4 algorithm as it is simple, fast, and would provide us with efficient random access. The choice to reduce the "copy from" window to 8 KB is to make random access on NVMe drives as close to the cost to read a single byte as possible. 



# Specification

An LZ4s document consists of:

* A marker: "LZ4s", in UTF-8.
* A version: "1" in UTF-8.
* 0xFF, to distinguish the document from UTF-8 text.
* A sequence of tokens. For each:
  * One byte with the number of literal bytes in the high four bits and the number of copied bytes in the low four bits.
  * If the number of literal bytes is 15, the next byte is the additional number of literal bytes.
  * The literal bytes.
  * If the number of copied byes is 15, the next byte is the additional number of copied bytes.
  * Two bytes with the position to copy the bytes from, as an offset relative to the first byte of this token. (So, 100 would mean to copy from 100 bytes before the first byte in this token)
  * Each token may represent a maximum of 255 uncompressed bytes (so the literal length plus copied length must be 255 or less)
* A 0x00 byte (indicating a token with zero literal and zero copied bytes)
* A sequence of index entries, one per 512 bytes of uncompressed file size. For each:
  * Seven bytes indicating the absolute byte position of the token containing uncompressed[512 * i].
  * One byte indicating how many bytes the indicated token encodes before uncompressed[512 * i].

**TODO:** 

* Add a container model so that an LZ4s stream can contain multiple files with identified relative URIs.
* Define how to specify an external Dictionary, which acts as the 8 KB of content immediately before the beginning of the compressed content (providing bytes which can be copied from during the first 8 KB of the compressed content).

