using System;
using NUnit.Framework;
using Platform.Storage;
// ReSharper disable InconsistentNaming

namespace Platform.Core.Tests
{
    public class when_writing_file_checkpoint : SpecificationWithFile
    {
        [Test]
        public void writeable_checkpoint_writes_expected_value()
        {
            var check = FileCheckpoint.OpenOrCreate(FileName, true);
            check.Write(long.MaxValue);
            Assert.AreEqual(check.Read(),long.MaxValue);}

        [Test, ExpectedException(typeof(NotSupportedException))]
        public void readonly_checkpoint_throws()
        {
            var check = FileCheckpoint.OpenOrCreate(FileName, false);
            check.Write(long.MaxValue);
        }
    }

    public class when_reading_file_checkpoint : SpecificationWithFile
    {
        [Test]
        public void fresh_writeable_checkpoint_returns_zero()
        {
            var check = FileCheckpoint.OpenOrCreate(FileName, true);
            Assert.AreEqual(0, check.Read());
        }

        [Test]
        public void fresh_readable_checkpoint_returns_zero()
        {
            var check = FileCheckpoint.OpenOrCreate(FileName, false);
            Assert.AreEqual(0, check.Read());
        }
    }

}