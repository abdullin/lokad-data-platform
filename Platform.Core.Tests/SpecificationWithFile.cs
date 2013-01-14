using System.IO;
using NUnit.Framework;

namespace Platform.Core.Tests
{
    public abstract class SpecificationWithFile
    {
        public string FileName { get; set; }

        [SetUp]
        public void Setup()
        {
            FileName = Path.GetTempFileName();
        }
        [TearDown]
        public void TearDown()
        {
            File.Delete(FileName);
        }
    }
}