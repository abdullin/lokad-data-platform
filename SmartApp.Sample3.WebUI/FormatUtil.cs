#region (c) 2012 Lokad Data Platform - New BSD License 
// Copyright (c) Lokad 2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence
#endregion

using System;

namespace SmartApp.Sample3.WebUI
{
    public static class FormatUtil
    {
        static readonly string[] ByteOrders = new[] { "EB", "PB", "TB", "GB", "MB", "KB", "Bytes" };
        static readonly long MaxScale;

        static FormatUtil()
        {
            MaxScale = (long)Math.Pow(1024, ByteOrders.Length - 1);
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