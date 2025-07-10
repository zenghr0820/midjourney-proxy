using System.Security.Cryptography;

namespace Midjourney.Base.Util
{
    /// <summary>
    /// 随机工具类
    /// </summary>
    public static class RandomUtils
    {
        private static readonly char[] Characters = "0123456789".ToCharArray();

        /// <summary>
        /// 生成指定长度的随机字符串
        /// </summary>
        /// <param name="length">字符串长度</param>
        /// <returns>随机字符串</returns>
        public static string RandomString(int length)
        {
            if (length < 1) throw new ArgumentException("Length must be greater than 0", nameof(length));

            var bytes = new byte[length];
            RandomNumberGenerator.Fill(bytes);

            return string.Create(length, bytes, (chars, state) =>
            {
                for (var i = 0; i < chars.Length; i++)
                {
                    chars[i] = Characters[state[i] % Characters.Length];
                }
            });
        }

        /// <summary>
        /// 生成指定长度的随机数字字符串
        /// </summary>
        /// <param name="length">数字字符串长度</param>
        /// <returns>随机数字字符串</returns>
        public static string RandomNumbers(int length)
        {
            if (length < 1) throw new ArgumentException("Length must be greater than 0", nameof(length));

            var bytes = new byte[length];
            RandomNumberGenerator.Fill(bytes);

            return string.Create(length, bytes, (chars, state) =>
            {
                for (var i = 0; i < chars.Length; i++)
                {
                    chars[i] = Characters[state[i] % 10]; // Only use '0' - '9'
                }
            });
        }
    }
}