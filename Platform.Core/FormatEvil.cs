using System;

namespace Platform
{
    public static class FormatEvil
    {
        static readonly string[] ByteOrders = new[] { "EB", "PB", "TB", "GB", "MB", "KB", "Bytes" };
        static readonly long MaxScale;

        static FormatEvil()
        {
            MaxScale = (long)Math.Pow(1024, ByteOrders.Length - 1);
        }



        public static string SpeedInBytes(double bytesPerSecond)
        {
            if (bytesPerSecond > long.MaxValue)
                return "N/A";
            return SizeInBytes(Convert.ToInt64(bytesPerSecond)) + "/s";
        }

        /// <summary>
        /// Formats the size in bytes to a pretty string.
        /// </summary>
        /// <param name="sizeInBytes">The size in bytes.</param>
        /// <returns></returns>
        public static string SizeInBytes(long sizeInBytes)
        {
            var max = MaxScale;

            foreach (var order in ByteOrders)
            {
                if (sizeInBytes > max)
                {
                    var divide = Decimal.Divide(sizeInBytes, max);
                    if (divide >= 100)
                    {
                        return String.Format("{0:##} {1}", divide, order);
                    }
                    if (divide >= 10)
                    {
                        return String.Format("{0:##.#} {1}", divide, order);
                    }
                    return String.Format("{0:##.##} {1}", divide, order);

                }

                max /= 1024;
            }
            return "0 Bytes";
        }
    }
}