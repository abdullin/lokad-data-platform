using System;
using NUnit.Framework;
using Platform.StreamStorage.File;

// ReSharper disable InconsistentNaming

namespace Platform.Core.Tests
{
    public class when_writing_file_checkpoint : SpecificationWithFile
    {
        [Test]
        public void writeable_checkpoint_writes_expected_value()
        {
            using (var check = FileCheckpoint.OpenOrCreateForWriting(FileName))
            {
                check.Write(long.MaxValue);
                Assert.AreEqual(check.Read(), long.MaxValue);
            }
        }

        [Test, ExpectedException(typeof(NotSupportedException))]
        public void readonly_checkpoint_throws()
        {
            using (var check = FileCheckpoint.OpenOrCreateForReading(FileName))
            {
                check.Write(long.MaxValue);
            }
        }
    }

    public class when_reading_file_checkpoint : SpecificationWithFile
    {
        [Test]
        public void fresh_writeable_checkpoint_returns_zero()
        {
            using (var check = FileCheckpoint.OpenOrCreateForWriting(FileName))
            {
                Assert.AreEqual(0, check.Read());
            }
        }

        [Test]
        public void fresh_readable_checkpoint_returns_zero()
        {
            using (var check = FileCheckpoint.OpenOrCreateForReading(FileName)) 
            {
                Assert.AreEqual(0, check.Read());
            }
        }
    }
}