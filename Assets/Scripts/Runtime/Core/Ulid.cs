using System;
using System.Security.Cryptography;

namespace Kindrith.Core
{
    // 26-character Crockford base32, time-ordered, monotonic within the same millisecond.
    // 48-bit big-endian Unix-ms timestamp + 80-bit cryptographic randomness, with
    // increment-on-collision so adjacent calls in the same ms still sort.
    public static class Ulid
    {
        const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
        const int Length = 26;

        static readonly object _lock = new object();
        static readonly byte[] _lastRandom = new byte[10];
        static long _lastTimeMs;

        public static string New()
        {
            lock (_lock)
            {
                long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (nowMs == _lastTimeMs)
                {
                    // Same ms — increment random portion big-endian so order is preserved.
                    for (int i = 9; i >= 0; i--)
                    {
                        if (++_lastRandom[i] != 0) break;
                    }
                }
                else
                {
                    _lastTimeMs = nowMs;
                    using (var rng = RandomNumberGenerator.Create())
                    {
                        rng.GetBytes(_lastRandom);
                    }
                }
                return Encode(_lastTimeMs, _lastRandom);
            }
        }

        public static bool TryParse(string s, out DateTimeOffset timestamp)
        {
            timestamp = default;
            if (string.IsNullOrEmpty(s) || s.Length != Length) return false;

            var buf = new byte[16];
            int bitIdx = -2; // 2 leading padding bits (always zero) to make 130 = 26 * 5.
            for (int charIdx = 0; charIdx < Length; charIdx++)
            {
                int v = DecodeChar(s[charIdx]);
                if (v < 0) return false;
                for (int b = 4; b >= 0; b--)
                {
                    int bit = (v >> b) & 1;
                    if (bitIdx >= 0 && bitIdx < 128)
                    {
                        buf[bitIdx / 8] |= (byte)(bit << (7 - bitIdx % 8));
                    }
                    else if (bit != 0)
                    {
                        // Padding bits must be zero — anything else means a malformed ULID.
                        return false;
                    }
                    bitIdx++;
                }
            }

            long ms = ((long)buf[0] << 40) | ((long)buf[1] << 32) | ((long)buf[2] << 24)
                    | ((long)buf[3] << 16) | ((long)buf[4] << 8)  | buf[5];
            timestamp = DateTimeOffset.FromUnixTimeMilliseconds(ms);
            return true;
        }

        static string Encode(long timeMs, byte[] random)
        {
            var buf = new byte[16];
            buf[0] = (byte)((timeMs >> 40) & 0xFF);
            buf[1] = (byte)((timeMs >> 32) & 0xFF);
            buf[2] = (byte)((timeMs >> 24) & 0xFF);
            buf[3] = (byte)((timeMs >> 16) & 0xFF);
            buf[4] = (byte)((timeMs >> 8)  & 0xFF);
            buf[5] = (byte)(timeMs & 0xFF);
            Array.Copy(random, 0, buf, 6, 10);

            var result = new char[Length];
            int bitIdx = -2;
            for (int charIdx = 0; charIdx < Length; charIdx++)
            {
                int v = 0;
                for (int b = 0; b < 5; b++)
                {
                    v <<= 1;
                    if (bitIdx >= 0 && bitIdx < 128)
                    {
                        v |= (buf[bitIdx / 8] >> (7 - bitIdx % 8)) & 1;
                    }
                    bitIdx++;
                }
                result[charIdx] = Alphabet[v];
            }
            return new string(result);
        }

        static int DecodeChar(char c)
        {
            // Crockford base32: 0-9 + A-Z minus I, L, O, U.
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'A' && c <= 'H') return c - 'A' + 10;
            switch (c)
            {
                case 'J': return 18;
                case 'K': return 19;
                case 'M': return 20;
                case 'N': return 21;
                case 'P': return 22;
                case 'Q': return 23;
                case 'R': return 24;
                case 'S': return 25;
                case 'T': return 26;
                case 'V': return 27;
                case 'W': return 28;
                case 'X': return 29;
                case 'Y': return 30;
                case 'Z': return 31;
                default: return -1;
            }
        }
    }
}
