using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Platform.Core.Tests
{
    public class FormatUtilTests
    {
        [Test]
        public void given_size_in_bytes()
        {
            Assert.AreEqual("10 Bytes",FormatEvil.SizeInBytes(10));
            Assert.AreEqual("10 KB", FormatEvil.SizeInBytes(10*1024));
            Assert.AreEqual("72 MB", FormatEvil.SizeInBytes(72 * 1024* 1024));
            Assert.AreEqual("72 GB", FormatEvil.SizeInBytes(72L *1024* 1024 *1024));
        }
    }
}
