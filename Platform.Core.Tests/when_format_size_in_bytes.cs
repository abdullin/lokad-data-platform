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

    public class when_topic_name_is_valid
    {
        [Test]
        public void given_valid_scenarios()
        {
            var lines = new string[]
                {
                    "valid",
                    "96dc098548ec40e0a1d5fa84cc44e16c"
                };

            foreach (var line in lines)
            {
                Assert.AreEqual(TopicName.Validity.Valid, TopicName.IsValid(line), line);
            }
        }

        [Test]
        public void has_two_consequitive_dashes()
        {
            Should(TopicName.Validity.HasTwoConsequitiveDashes, "asd--asd");
        }

        [Test]
        public void starts_with_dash()
        {
            Should(TopicName.Validity.DoesNotStartWithNumberOrLowercaseLetter, "-asd");
        }

        [Test]
        public void ends_with_dash()
        {
            Should(TopicName.Validity.DoesNotEndWithNumberOrLowercaseLetter,"asd-");
        }
        [Test]
        public void contains_illegal_char()
        {
            Should(TopicName.Validity.DoesNotContainDashesNumbersOrLowercaseChars, "aAa");
        }


        void Should(TopicName.Validity validity, string asdAsd)
        {
            Assert.AreEqual(validity, TopicName.IsValid(asdAsd), asdAsd);
        }
    }
}
