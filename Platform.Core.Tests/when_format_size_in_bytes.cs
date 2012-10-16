using NUnit.Framework;
// ReSharper disable InconsistentNaming
namespace Platform.Core.Tests
{
    public class when_format_size_in_bytes
    {
        [Test]
        public void given_various_sizes()
        {
            Assert.AreEqual("10 Bytes",FormatEvil.SizeInBytes(10));
            Assert.AreEqual("10 KB", FormatEvil.SizeInBytes(10*1024));
            Assert.AreEqual("72 MB", FormatEvil.SizeInBytes(72 * 1024* 1024));
            Assert.AreEqual("72 GB", FormatEvil.SizeInBytes(72L *1024* 1024 *1024));
        }
    }
}
