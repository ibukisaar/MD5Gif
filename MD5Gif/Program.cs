using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MD5Gif {
    class Program {
        /// <summary>
        /// fastcoll的可执行文件路径
        /// </summary>
        const string FastcollExe = @"Z:\fastcoll_v1.0.0.5.exe";
        /// <summary>
        /// 用于存放fastcoll碰撞结果的临时目录
        /// </summary>
        const string WorkspaceDir = @"Z:\"; // 建议用内存盘
        /// <summary>
        /// 最多同时运行的fastcoll进程个数
        /// </summary>
        const int FindCollisionConcurrency = 8;
        /// <summary>
        /// 制造开头为23333333的MD5值，通常这需要很长时间（除非欧皇
        /// </summary>
        const bool IsMake23333333MD5 = true;

        static string HashToString(byte[] hash) {
            return string.Concat(Array.ConvertAll(hash, h => h.ToString("x2")));
        }

        /// <summary>
        /// 返回true表示结果可用，否则返回false将继续碰撞。
        /// </summary>
        /// <param name="msg1"></param>
        /// <param name="msg2"></param>
        /// <returns></returns>
        delegate bool FindCollisionHandler(byte[] msg1, byte[] msg2);

        static void FindCollision(byte[] IV, FindCollisionHandler handler) {
            var processes = new Process[FindCollisionConcurrency];
            var tasks = new Task[FindCollisionConcurrency];
            var cancellationTokenSource = new CancellationTokenSource();

            while (true) {
                int taskId = 0;

                lock (processes) {
                    if (cancellationTokenSource.IsCancellationRequested) break;
                    taskId = Array.IndexOf(processes, null);
                    if (taskId < 0) {
                        try {
                            Task.Delay(50, cancellationTokenSource.Token).Wait();
                        } catch {
                            break;
                        }
                        continue;
                    }
                }

                tasks[taskId] = Task.Run(delegate {
                    string msg1File = Path.Combine(WorkspaceDir, $"{taskId}-1.bin");
                    string msg2File = Path.Combine(WorkspaceDir, $"{taskId}-2.bin");

                    Process process = new Process() {
                        StartInfo = new ProcessStartInfo(FastcollExe, $"-i {HashToString(IV)} -o \"{msg1File}\" \"{msg2File}\"") {
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                        },
                    };

                    lock (processes) {
                        if (cancellationTokenSource.IsCancellationRequested) return;
                        processes[taskId] = process;
                    }

                    process.Start();
                    process.WaitForExit();

                    lock (processes) {
                        if (cancellationTokenSource.IsCancellationRequested) return;
                        processes[taskId] = null;

                        byte[] msg1 = File.ReadAllBytes(msg1File);
                        byte[] msg2 = File.ReadAllBytes(msg2File);
                        bool result = handler(msg1, msg2);
                        if (result) {
                            cancellationTokenSource.Cancel();

                            foreach (var p in processes) {
                                if (p is null) continue;
                                try { p.Kill(); } catch { }
                            }
                            return;
                        }
                    }
                });

                try {
                    // fastcoll用time函数作为随机种子，因此至少要等待1s再启动新进程
                    Task.Delay(TimeSpan.FromSeconds(1), cancellationTokenSource.Token).Wait();
                } catch {
                    break;
                }
            }

            Task.WaitAll(tasks.Where(t => t is not null).ToArray());
        }


        [SuppressUnmanagedCodeSecurity]
        [DllImport("MD5", EntryPoint = "transform_block")]
        unsafe extern static void MD5TransformBlock(uint* IV, byte* msgBlock);


        const int StrokeThickness = 3;
        const int StrokeLength = 17;
        const int StrokeLongWidth = StrokeThickness * 2 + StrokeLength;
        const int StrokeShortWidth = StrokeThickness * 2 + 1;
        const int NumberSmallSpacing = 7;
        const int NumberBigSpacing = 23;
        const int NumberWidth = StrokeShortWidth * 2 + StrokeLongWidth;
        const int NumberHeight = StrokeShortWidth * 3 + StrokeLongWidth * 2;
        const int ImagePadding = 23;
        const int ImageWidth = NumberWidth * 16 + NumberSmallSpacing * 8 + NumberBigSpacing * 7 + ImagePadding * 2;
        const int ImageHeight = NumberHeight * 2 + NumberBigSpacing + ImagePadding * 2;


        unsafe static void Main(string[] args) {
            /*
               ===0===
             ||       ||
              3        4
             ||       ||
               ===1===
             ||       ||
              5        6
             ||       ||
               ===2===
             */

            //               0  1  2  3  4  5  6
            byte[] num_0 = { 1, 0, 1, 1, 1, 1, 1 };
            byte[] num_1 = { 0, 0, 0, 0, 1, 0, 1 };
            byte[] num_2 = { 1, 1, 1, 0, 1, 1, 0 };
            byte[] num_3 = { 1, 1, 1, 0, 1, 0, 1 };
            byte[] num_4 = { 0, 1, 0, 1, 1, 0, 1 };
            byte[] num_5 = { 1, 1, 1, 1, 0, 0, 1 };
            byte[] num_6 = { 1, 1, 1, 1, 0, 1, 1 };
            byte[] num_7 = { 1, 0, 0, 0, 1, 0, 1 };
            byte[] num_8 = { 1, 1, 1, 1, 1, 1, 1 };
            byte[] num_9 = { 1, 1, 1, 1, 1, 0, 1 };
            byte[] num_a = { 1, 1, 0, 1, 1, 1, 1 };
            byte[] num_b = { 0, 1, 1, 1, 0, 1, 1 };
            byte[] num_c = { 1, 0, 1, 1, 0, 1, 0 };
            byte[] num_d = { 0, 1, 1, 0, 1, 1, 1 };
            byte[] num_e = { 1, 1, 1, 1, 0, 1, 0 };
            byte[] num_f = { 1, 1, 0, 1, 0, 1, 0 };

            byte[][] numStrokes = { num_0, num_1, num_2, num_3, num_4, num_5, num_6, num_7, num_8, num_9, num_a, num_b, num_c, num_d, num_e, num_f };

            byte[,] horizontalStrokeTemplate = new byte[StrokeLongWidth, StrokeShortWidth]; // 横向笔画图像模板
            for (int x = 0; x < StrokeLongWidth; x++) {
                for (int y = 0; y < StrokeShortWidth; y++) {
                    int dx = Math.Min(x, StrokeLongWidth - 1 - x);
                    int dy = Math.Min(y, StrokeShortWidth - 1 - y);
                    if (dx + dy < StrokeThickness) {
                        horizontalStrokeTemplate[x, y] = 0; // 去掉四角
                    } else {
                        horizontalStrokeTemplate[x, y] = 1;
                    }
                }
            }

            byte[,] verticalStrokeTemplate = new byte[StrokeShortWidth, StrokeLongWidth]; // 竖向笔画图像模板
            for (int x = 0; x < StrokeLongWidth; x++) {
                for (int y = 0; y < StrokeShortWidth; y++) {
                    verticalStrokeTemplate[y, x] = horizontalStrokeTemplate[x, y];
                }
            }

            // 所有数字预先显示'8'的背景图像
            byte[] backImage = new byte[ImageWidth * ImageHeight];

            // 单个数字各笔画的图像模板和偏移量信息
            (byte[,] Template, int OffsetX, int OffsetY)[] strokeInfos = {
                (horizontalStrokeTemplate, StrokeShortWidth, 0),
                (horizontalStrokeTemplate, StrokeShortWidth, StrokeShortWidth + StrokeLongWidth),
                (horizontalStrokeTemplate, StrokeShortWidth, StrokeShortWidth * 2 + StrokeLongWidth * 2),
                (  verticalStrokeTemplate, 0, StrokeShortWidth),
                (  verticalStrokeTemplate, StrokeShortWidth + StrokeLongWidth, StrokeShortWidth),
                (  verticalStrokeTemplate, 0, StrokeShortWidth * 2 + StrokeLongWidth),
                (  verticalStrokeTemplate, StrokeShortWidth + StrokeLongWidth, StrokeShortWidth * 2 + StrokeLongWidth),
            };

            for (int i = 0; i < 32; i++) DrawNumber8(i);

            // 单个数字各笔画对应的覆盖图像的偏移量信息
            (int OffsetX, int OffsetY)[] overlayInfos = {
                (StrokeShortWidth, 0),
                (StrokeShortWidth, StrokeShortWidth + StrokeLongWidth),
                (StrokeShortWidth, StrokeShortWidth * 2 + StrokeLongWidth * 2),
                (0, StrokeShortWidth),
                (StrokeShortWidth * 2, StrokeShortWidth),
                (0, StrokeShortWidth * 2 + StrokeLongWidth),
                (StrokeShortWidth * 2, StrokeShortWidth * 2 + StrokeLongWidth),
            };

            byte[] overlayImage = new byte[StrokeLongWidth * StrokeLongWidth]; // 用于覆盖笔画的图像
            using MemoryStream overlayImageStream = new MemoryStream();
            using BinaryWriter overlayImageWriter = new BinaryWriter(overlayImageStream);
            WriteImage(overlayImageWriter, overlayImage, 0, 0, StrokeLongWidth, StrokeLongWidth);
            byte[] overlayImageTemplate = overlayImageStream.ToArray(); // 用于覆盖笔画的图像的GIF数据模板，可直接写入流


            using MemoryStream gifStream = new MemoryStream();
            using BinaryWriter gifWriter = new BinaryWriter(gifStream, Encoding.ASCII, leaveOpen: true);

            // Header
            gifWriter.Write(Encoding.ASCII.GetBytes("GIF89a"));

            // Logical Screen Descriptor
            gifWriter.Write((ushort)ImageWidth); // width
            gifWriter.Write((ushort)ImageHeight); // height
            gifWriter.Write((byte)0b_1_111_0_000); // 全局调色板标志(1)，颜色位数(8)，排序标志(0)，调色板大小(4)
            gifWriter.Write((byte)0);  // 背景色索引
            gifWriter.Write((byte)0);

            // Global Color Table
            gifWriter.Write((ReadOnlySpan<byte>)new byte[] { 0x22, 0x22, 0x22 }); // 背景色
            gifWriter.Write((ReadOnlySpan<byte>)new byte[] { 0x20, 0x90, 0xe0 }); // 前景色

            WriteImage(gifWriter, backImage, 0, 0, ImageWidth, ImageHeight);


            #region ============== 制造MD5碰撞部分

            byte[] mySignTemplate = Encoding.ASCII.GetBytes("github.com/ibukisaar ");
            uint* IV = stackalloc uint[] { 0x67452301u, 0xefcdab89u, 0x98badcfeu, 0x10325476u };
            int lastAlignOffset = 0; // 之前最后一次的64字节对齐偏移
            var collisionCache = new List<(int FileOffset, byte[] Hide, byte[] Show)>();
            int count = 0;

            void UpdateMD5IV() {
                using var tempStream = new MemoryStream();
                gifStream.Seek(lastAlignOffset, SeekOrigin.Begin);
                gifStream.CopyTo(tempStream);
                byte[] prevGifData = tempStream.ToArray();
                fixed (byte* p = prevGifData) {
                    for (int offs = 0; offs + 64 <= prevGifData.Length; offs += 64) {
                        MD5TransformBlock(IV, p + offs);
                    }
                }
                lastAlignOffset = (int)gifStream.Position & ~63;
            }

            Stopwatch timer = Stopwatch.StartNew();

            for (int numIndex = 0; numIndex < 32; numIndex++) {
                //if (count > 6) break;
                for (int strokeIndex = 0; strokeIndex < 7; strokeIndex++) {
                    Console.WriteLine($"{++count} of {7 * 32}");
                    //if (count > 6) break;

                    int commentSpace = 64 - (int)gifStream.Length % 64;
                    if (commentSpace < 3) commentSpace += 64;
                    gifWriter.Write((ReadOnlySpan<byte>)new byte[] { 0x21, 0xfe }); // Comment Extension
                    commentSpace -= 3;
                    gifWriter.Write((byte)(commentSpace + 19));
                    WriteMySign(commentSpace);

                    UpdateMD5IV();

                    FindCollision(new ReadOnlySpan<byte>(IV, 16).ToArray(), (msgBlock1, msgBlock2) => {
                        Console.Write('.');

                        int pathOffset1 = 19, pathOffset2 = 19; // msgBlock1和msgBlock2第19个(从0开始数)字节一定是不同的，将作为Comment长度，从这里开始尝试搜索两条不同的路径。pathOffset是msgBlock起始处的偏移量
                        while (pathOffset1 < 128) {
                            int commentLength = msgBlock1[pathOffset1];
                            if (commentLength == 0) return false; // 如果在msgBlock内搜到长度为0的Comment，意味着Comment Extension的结束，需要重新碰撞再搜索
                            pathOffset1 += commentLength + 1;
                        }
                        while (pathOffset2 < 128) {
                            int commentLength = msgBlock2[pathOffset2];
                            if (commentLength == 0) return false;
                            pathOffset2 += commentLength + 1;
                        }
                        if (pathOffset1 > 253 || pathOffset2 > 253) return false; // 把每个步骤产生的数据控制在256字节，至少预留3字节用于填充Comment Extension

                        int offsetDiff = Math.Abs(pathOffset1 - pathOffset2) - overlayImageTemplate.Length;
                        if (offsetDiff is (< 0) or 1 or 2 or 4) { // 如果是这些情况处理起来很麻烦，为了处理方便直接重新碰撞
                            return false;
                        }

                        Console.WriteLine("ok");

                        // 经过上面的过滤后，pathOffset可能有以下两种情况，最后都可以重新让两个pathOffset回到同一位置。（以下@表示pathOffset位置）

                        // hide path: [comment sub block...] @ 00 [image...] 21 fe [1 byte comment length] [comment data...]   00
                        // show path: [comment sub block...] [comment sub block...]        ...        [comment sub block...] @ 00

                        // or

                        // hide path: [comment sub block...] @ 00 [image... 00]
                        // show path: [comment sub block...             ] @ 00

                        int fileOffset = (int)gifStream.Position;
                        gifStream.Write(msgBlock1);

                        Span<byte> overlayData = overlayImageTemplate.Clone() as byte[];
                        (int imageX, int imageY) = GetOverlayImageLocation(numIndex, strokeIndex);
                        BinaryPrimitives.WriteUInt16LittleEndian(overlayData.Slice(9, 2), (ushort)imageX); // left
                        BinaryPrimitives.WriteUInt16LittleEndian(overlayData.Slice(11, 2), (ushort)imageY); // top

                        if (pathOffset1 < pathOffset2) { // 偏移量较小的是hide path
                            WriteCommonData(overlayData, fileOffset, pathOffset1, pathOffset2, msgBlock1, msgBlock2);
                        } else {
                            WriteCommonData(overlayData, fileOffset, pathOffset2, pathOffset1, msgBlock2, msgBlock1);
                        }

                        return true;
                    });
                }
            }

            timer.Stop();
            Console.WriteLine($"共耗时：{timer.Elapsed}");

            #endregion


            #region =============== 处理GIF文件尾 

            if (IsMake23333333MD5) {
                Console.WriteLine("开始23333333 Hash碰撞...");

                int nonceOffset;
                int blockOffset = (int)gifStream.Position % 64;
                int tailSpace = blockOffset != 0 ? 64 - blockOffset : 0;

                // 至少需要22字节空间：2字节Comment头 + 1字节Comment长度 + 8字节nonce + 1字节Comment结束标志 + 1字节Trailer + 1字节MD5 Padding + 8字节数据长度
                if (tailSpace < 22) { // 如果少于22字节则产生新的Block
                    gifWriter.Write((ReadOnlySpan<byte>)new byte[] { 0x21, 0xfe });
                    gifWriter.Write((byte)34); // 至少34字节可以保证在新Block的[8..16]处一定是Comment，此处可以当成nonce
                    gifWriter.Write(new byte[34]);
                    gifWriter.Write((byte)0);
                    nonceOffset = 8;
                } else {
                    gifWriter.Write((ReadOnlySpan<byte>)new byte[] { 0x21, 0xfe });
                    gifWriter.Write((byte)8);
                    gifWriter.Write(0L);
                    gifWriter.Write((byte)0);
                    nonceOffset = blockOffset + 3;
                }
                Console.WriteLine($"nonce offset: {nonceOffset:X}");

                // Trailer
                gifWriter.Write((byte)0x3b);

                UpdateMD5IV();

                byte* lastBlock = stackalloc byte[64];
                uint* finalIV = stackalloc uint[4];

                gifStream.Seek(lastAlignOffset, SeekOrigin.Begin);
                int lastLength = gifStream.Read(new Span<byte>(lastBlock, 64));
                int paddingBytes = 64 - 9 - lastLength;
                ulong gifBitCount = (ulong)gifStream.Length * 8;

                lastBlock[lastLength] = 0x80;
                new Span<byte>(lastBlock + lastLength + 1, paddingBytes).Clear();
                *(ulong*)(lastBlock + 56) = gifBitCount;

                for (ulong nonce = 0; ; nonce++) {
                    if (nonce % 131072 == 0) Console.Write('.');

                    ((ulong*)finalIV)[0] = ((ulong*)IV)[0];
                    ((ulong*)finalIV)[1] = ((ulong*)IV)[1];
                    *(ulong*)(lastBlock + nonceOffset) = nonce;
                    MD5TransformBlock(finalIV, lastBlock);
                    byte* finalHash = (byte*)finalIV;

                    if (finalIV[0] != 0x33333323u) continue;

                    string finalHashStr = string.Concat(Array.ConvertAll(new ReadOnlySpan<byte>(finalIV, 16).ToArray(), b => b.ToString("X2")));
                    
                    gifStream.Seek(lastAlignOffset + nonceOffset, SeekOrigin.Begin);
                    gifWriter.Write(nonce);
                    gifStream.Seek(0, SeekOrigin.End);
                    Console.WriteLine();
                    Console.WriteLine($"GIF Hash: {finalHashStr}");
                    break;
                }
            } else {
                // Trailer
                gifWriter.Write((byte)0x3b);
            }

            #endregion


            #region =============== 构造GIF部分

            byte[] gifFileData = gifStream.ToArray();

            byte[] gifFileHash = MD5.Create().ComputeHash(gifFileData);
            string gifFileHashStr = string.Concat(Array.ConvertAll(gifFileHash, b => b.ToString("X2")));

            for (int numIndex = 0, i = 0; numIndex < gifFileHashStr.Length; numIndex++) {
                int num = gifFileHashStr[numIndex] switch
                {
                    char c and >= '0' and <= '9' => c - '0',
                    char c => c - 'A' + 10,
                };
                for (int strokeIndex = 0; strokeIndex < 7; strokeIndex++, i++) {
                    var (offset, hide, show) = collisionCache[i];
                    if (numStrokes[num][strokeIndex] == 0) {
                        hide.CopyTo(gifFileData.AsSpan(offset));
                    } else {
                        show.CopyTo(gifFileData.AsSpan(offset));
                    }
                }
            }

            File.WriteAllBytes(Path.Combine(WorkspaceDir, "md5.gif"), gifFileData);

            foreach (var (offset, _, show) in collisionCache) {
                show.CopyTo(gifFileData.AsSpan(offset, 128));
            }
            File.WriteAllBytes(Path.Combine(WorkspaceDir, "debug-show.gif"), gifFileData);

            foreach (var (offset, hide, _) in collisionCache) {
                hide.CopyTo(gifFileData.AsSpan(offset, 128));
            }
            File.WriteAllBytes(Path.Combine(WorkspaceDir, "debug-hide.gif"), gifFileData);


            #endregion


            #region =============== 局部函数定义

            (int X, int Y) GetOverlayImageLocation(int numIndex, int strokeIndex) {
                int imageX = ImagePadding + ((numIndex / 2) & 7) * (NumberWidth * 2 + NumberSmallSpacing + NumberBigSpacing);
                int imageY = ImagePadding + (numIndex / 16) * (NumberHeight + NumberBigSpacing);
                if (numIndex % 2 != 0) imageX += NumberWidth + NumberSmallSpacing;
                (int offsetX, int offsetY) = overlayInfos[strokeIndex];
                return (imageX + offsetX, imageY + offsetY);
            }

            void DrawNumber8(int index) {
                int imageX = ImagePadding + ((index / 2) & 7) * (NumberWidth * 2 + NumberSmallSpacing + NumberBigSpacing);
                int imageY = ImagePadding + (index / 16) * (NumberHeight + NumberBigSpacing);
                if (index % 2 != 0) imageX += NumberWidth + NumberSmallSpacing;

                byte[] strokes = numStrokes[8];
                for (int i = 0; i < strokes.Length; i++) {
                    (byte[,] template, int offsetX, int offsetY) = strokeInfos[i];
                    int w = template.GetLength(0), h = template.GetLength(1);
                    for (int x = 0; x < w; x++) {
                        for (int y = 0; y < h; y++) {
                            backImage[(imageY + offsetY + y) * ImageWidth + (imageX + offsetX + x)] = template[x, y];
                        }
                    }
                }
            }

            void WriteMySign(int length) {
                for (int i = 0; i < length; i++) {
                    gifStream.WriteByte(mySignTemplate[i % mySignTemplate.Length]);
                }
            }

            void WriteCommonData(ReadOnlySpan<byte> overlayData, int fileOffset, int hidePathOffset, int showPathOffset, byte[] hideMsgBlock, byte[] showMsgBlock) {
                WriteMySign(hidePathOffset - 128);
                gifWriter.Write((byte)0);

                // Image Descriptor & Image Data
                gifWriter.Write(overlayData);
                hidePathOffset += overlayData.Length;

                if (hidePathOffset != showPathOffset) {
                    // 用一个Comment Extension将两条路径重新汇聚在一起

                    gifWriter.Write((ReadOnlySpan<byte>)new byte[] { 0x21, 0xfe });
                    if (showPathOffset - hidePathOffset > 4) {
                        gifWriter.Write((byte)(showPathOffset - hidePathOffset - 4));
                        WriteMySign(showPathOffset - hidePathOffset - 4);
                    }
                    gifWriter.Write((byte)0);
                }

                collisionCache.Add((fileOffset, hideMsgBlock, showMsgBlock));
            }

            #endregion
        }

        static void WriteImage(BinaryWriter writer, ReadOnlySpan<byte> pixels, int left, int top, int width, int height) {
            // Graphic Control Extension
            writer.Write((byte)0x21);
            writer.Write((byte)0xf9);
            writer.Write((byte)4);
            writer.Write((byte)0b_000_000_0_0);
            writer.Write((ushort)2); // Delay Time
            writer.Write((byte)0);
            writer.Write((byte)0); // Block Terminator


            // Image Descriptor
            writer.Write((byte)0x2c);
            writer.Write((ushort)left);
            writer.Write((ushort)top);
            writer.Write((ushort)width);
            writer.Write((ushort)height);
            writer.Write((byte)0b_0_0_0_00_000);

            // Image Data
            int lzwMinCodeSize = 2;
            writer.Write((byte)lzwMinCodeSize);
            LzwEncoder.Encode(writer.BaseStream, pixels, lzwMinCodeSize);
        }
    }
}
