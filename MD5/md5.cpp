#include <cinttypes>

typedef uint32_t u32;

inline static u32 F(u32 x, u32 y, u32 z) { return (x & (y ^ z)) ^ z; }
inline static u32 G(u32 x, u32 y, u32 z) { return (z & (x ^ y)) ^ y; }
inline static u32 H(u32 x, u32 y, u32 z) { return x ^ y ^ z; }
inline static u32 I(u32 x, u32 y, u32 z) { return y ^ (x | ~z); }
inline static u32 RL(u32 x, int n) { return (x << n) | (x >> (32 - n)); }

#define R(f, a, b, c, d, m, k, s) \
    a += f(b, c, d) + m + k; \
    a = RL(a, s) + b;

extern "C" __declspec(dllexport)
void transform_block(u32 IV[4], const u32 M[16]) {
    u32 a = IV[0];
    u32 b = IV[1];
    u32 c = IV[2];
    u32 d = IV[3];

    R(F, a, b, c, d, M[0], 0xd76aa478, 7);
    R(F, d, a, b, c, M[1], 0xe8c7b756, 12);
    R(F, c, d, a, b, M[2], 0x242070db, 17);
    R(F, b, c, d, a, M[3], 0xc1bdceee, 22);
    R(F, a, b, c, d, M[4], 0xf57c0faf, 7);
    R(F, d, a, b, c, M[5], 0x4787c62a, 12);
    R(F, c, d, a, b, M[6], 0xa8304613, 17);
    R(F, b, c, d, a, M[7], 0xfd469501, 22);
    R(F, a, b, c, d, M[8], 0x698098d8, 7);
    R(F, d, a, b, c, M[9], 0x8b44f7af, 12);
    R(F, c, d, a, b, M[10], 0xffff5bb1, 17);
    R(F, b, c, d, a, M[11], 0x895cd7be, 22);
    R(F, a, b, c, d, M[12], 0x6b901122, 7);
    R(F, d, a, b, c, M[13], 0xfd987193, 12);
    R(F, c, d, a, b, M[14], 0xa679438e, 17);
    R(F, b, c, d, a, M[15], 0x49b40821, 22);

    R(G, a, b, c, d, M[1], 0xf61e2562, 5);
    R(G, d, a, b, c, M[6], 0xc040b340, 9);
    R(G, c, d, a, b, M[11], 0x265e5a51, 14);
    R(G, b, c, d, a, M[0], 0xe9b6c7aa, 20);
    R(G, a, b, c, d, M[5], 0xd62f105d, 5);
    R(G, d, a, b, c, M[10], 0x02441453, 9);
    R(G, c, d, a, b, M[15], 0xd8a1e681, 14);
    R(G, b, c, d, a, M[4], 0xe7d3fbc8, 20);
    R(G, a, b, c, d, M[9], 0x21e1cde6, 5);
    R(G, d, a, b, c, M[14], 0xc33707d6, 9);
    R(G, c, d, a, b, M[3], 0xf4d50d87, 14);
    R(G, b, c, d, a, M[8], 0x455a14ed, 20);
    R(G, a, b, c, d, M[13], 0xa9e3e905, 5);
    R(G, d, a, b, c, M[2], 0xfcefa3f8, 9);
    R(G, c, d, a, b, M[7], 0x676f02d9, 14);
    R(G, b, c, d, a, M[12], 0x8d2a4c8a, 20);

    R(H, a, b, c, d, M[5], 0xfffa3942, 4);
    R(H, d, a, b, c, M[8], 0x8771f681, 11);
    R(H, c, d, a, b, M[11], 0x6d9d6122, 16);
    R(H, b, c, d, a, M[14], 0xfde5380c, 23);
    R(H, a, b, c, d, M[1], 0xa4beea44, 4);
    R(H, d, a, b, c, M[4], 0x4bdecfa9, 11);
    R(H, c, d, a, b, M[7], 0xf6bb4b60, 16);
    R(H, b, c, d, a, M[10], 0xbebfbc70, 23);
    R(H, a, b, c, d, M[13], 0x289b7ec6, 4);
    R(H, d, a, b, c, M[0], 0xeaa127fa, 11);
    R(H, c, d, a, b, M[3], 0xd4ef3085, 16);
    R(H, b, c, d, a, M[6], 0x04881d05, 23);
    R(H, a, b, c, d, M[9], 0xd9d4d039, 4);
    R(H, d, a, b, c, M[12], 0xe6db99e5, 11);
    R(H, c, d, a, b, M[15], 0x1fa27cf8, 16);
    R(H, b, c, d, a, M[2], 0xc4ac5665, 23);

    R(I, a, b, c, d, M[0], 0xf4292244, 6);
    R(I, d, a, b, c, M[7], 0x432aff97, 10);
    R(I, c, d, a, b, M[14], 0xab9423a7, 15);
    R(I, b, c, d, a, M[5], 0xfc93a039, 21);
    R(I, a, b, c, d, M[12], 0x655b59c3, 6);
    R(I, d, a, b, c, M[3], 0x8f0ccc92, 10);
    R(I, c, d, a, b, M[10], 0xffeff47d, 15);
    R(I, b, c, d, a, M[1], 0x85845dd1, 21);
    R(I, a, b, c, d, M[8], 0x6fa87e4f, 6);
    R(I, d, a, b, c, M[15], 0xfe2ce6e0, 10);
    R(I, c, d, a, b, M[6], 0xa3014314, 15);
    R(I, b, c, d, a, M[13], 0x4e0811a1, 21);
    R(I, a, b, c, d, M[4], 0xf7537e82, 6);
    R(I, d, a, b, c, M[11], 0xbd3af235, 10);
    R(I, c, d, a, b, M[2], 0x2ad7d2bb, 15);
    R(I, b, c, d, a, M[9], 0xeb86d391, 21);

    IV[0] += a;
    IV[1] += b;
    IV[2] += c;
    IV[3] += d;
}
