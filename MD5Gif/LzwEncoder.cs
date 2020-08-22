using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace MD5Gif {
    unsafe public static class LzwEncoder {
        const int MaxMaxBits = 12;
        const int HashTableLength = 5003;

        private ref struct Writer {
            public Stream Stream;
            public byte* Buffer255;
            public int Buffer255Offset;
            public int Buffer;
            public int BufferBits;

            public void WriteInt(int value, int bits) {
                Buffer |= value << BufferBits;
                BufferBits += bits;
                for (; BufferBits >= 8; Buffer >>= 8, BufferBits -= 8) {
                    Buffer255[Buffer255Offset++] = unchecked((byte)Buffer);
                    if (Buffer255Offset == 255) {
                        Stream.WriteByte(255);
                        Stream.Write(new ReadOnlySpan<byte>(Buffer255, 255));
                        Buffer255Offset = 0;
                    }
                }
            }

            public void Flush() {
                if (BufferBits > 0) {
                    Buffer255[Buffer255Offset++] = (byte)Buffer;
                }
                if (Buffer255Offset > 0) {
                    Stream.WriteByte((byte)Buffer255Offset);
                    Stream.Write(new ReadOnlySpan<byte>(Buffer255, Buffer255Offset));
                }
                Stream.WriteByte(0);
            }
        }

        /// <summary>
		/// LZW编码
		/// </summary>
		/// <param name="stream">压缩后的数据将写入流。</param>
		/// <param name="data">要压缩的数据。</param>
		/// <param name="initBits">编码开始时，编号的bit数。</param>
		/// <param name="maxOverflowCount">
		/// 当编号等于最大值时，溢出次数大于 <paramref name="maxOverflowCount"/> 才会 CLEAR。
		/// <para>在图像简单并且图像很大的时候，该参数&gt;0能够产生更高的压缩率。该参数不能&gt;4095。</para>
		/// </param>
        [SkipLocalsInit]
        public static void Encode(Stream stream, ReadOnlySpan<byte> data, int initBits, int maxOverflowCount = 4) {
            if (stream is null) throw new ArgumentNullException(nameof(stream));
            if (data.IsEmpty) throw new ArgumentNullException(nameof(data));
            if (initBits < 2 || initBits > 8) throw new ArgumentOutOfRangeException(nameof(initBits));
            if (maxOverflowCount >= 1 << MaxMaxBits) throw new ArgumentOutOfRangeException(nameof(maxOverflowCount));

            var buffer255 = stackalloc byte[255];
            var writer = new Writer {
                Stream = stream,
                Buffer255 = buffer255,
                Buffer255Offset = 0,
                Buffer = 0,
                BufferBits = 0,
            };
            var hashTable = stackalloc int[HashTableLength * 2];
            var overflowTable = stackalloc int[HashTableLength];
            int clearToken = 1 << initBits;
            int eofToken = (1 << initBits) + 1;
            int currIndex = (1 << initBits) + 2;
            int currBits = initBits + 1;
            int overflow = 0;

            fixed (byte* dataPointer = data) {
                writer.WriteInt(clearToken, currBits);
                new Span<int>(hashTable, HashTableLength * 2).Fill(-1);
                new Span<int>(overflowTable, HashTableLength).Fill(-1);
                int prevIndex = dataPointer[0];

                for (int i = 1; i < data.Length; i++) {
                    int c = dataPointer[i];
                    int entry = (c << MaxMaxBits) | prevIndex;
                    int hashCode = (c << 4) ^ prevIndex;

                Retry:
                    if (hashTable[2 * hashCode] == entry) {
                        prevIndex = hashTable[2 * hashCode + 1];
                    } else if (hashTable[2 * hashCode] >= 0) {
                        if (hashCode != 0) {
                            hashCode *= 2;
                            if (hashCode >= HashTableLength) hashCode -= HashTableLength;
                        } else {
                            hashCode = HashTableLength - 1;
                        }
                        goto Retry;
                    } else {
                        writer.WriteInt(prevIndex, currBits);
                        prevIndex = dataPointer[i];

                        if (currIndex < (1 << MaxMaxBits)) {
                            if (currIndex >= (1 << currBits)) currBits++;
                            hashTable[2 * hashCode] = entry;
                            hashTable[2 * hashCode + 1] = currIndex++;
                        } else {
                            hashCode = (c << 4) ^ prevIndex;
                        OverflowRetry:
                            if (overflowTable[hashCode] == entry) continue;
                            if (overflowTable[hashCode] >= 0) {
                                hashCode = hashCode * 2 + 1;
                                if (hashCode >= HashTableLength) hashCode -= HashTableLength;
                                goto OverflowRetry;
                            } else if (overflow < maxOverflowCount) {
                                overflowTable[hashCode] = entry;
                                overflow++;
                            } else {
                                writer.WriteInt(clearToken, currBits);
                                new Span<int>(hashTable, HashTableLength * 2).Fill(-1);
                                new Span<int>(overflowTable, HashTableLength).Fill(-1);
                                currIndex = (1 << initBits) + 2;
                                currBits = initBits + 1;
                                overflow = 0;
                            }
                        }
                    }
                }

                writer.WriteInt(prevIndex, currBits);
                writer.WriteInt(eofToken, currBits);
                writer.Flush();
            }
        }
    }
}
