using NUnit.Framework;

// ReSharper disable InconsistentNaming
namespace Platform.Core.Tests
{
    public class when_container_name_is_valid
    {
        [Test]
        public void given_valid_scenarios()
        {
            var lines = new string[]
                {
                    "valid",
                    "96dc098548ec40e0a1d5fa84cc44e16c",
                    "e97c55e5-9c08-4730-992c-bc2029f6486e",
                    "s2-stream",
                    "123"
                };

            foreach (var line in lines)
            {
                Should(EventStoreId.Rule.Valid, line);
            }
        }

        [Test]
        public void given_two_consequitive_dashes()
        {
            Should(EventStoreId.Rule.ShouldNotHaveHasTwoConsequitiveDashes, "asd--asd");
        }

        [Test]
        public void given_starting_dash()
        {
            Should(EventStoreId.Rule.ShouldStartWithLowercaseLetterOrNumber, "-asd");
        }

        [Test]
        public void given_string_with_last_dash()
        {
            Should(EventStoreId.Rule.ShouldEndWithLowercaseLetterOrNumber, "asd-");
        }
        [Test]
        public void given_illegal_char_in_middle()
        {
            Should(EventStoreId.Rule.ShouldContainOnlyLowercaseLetterNumberOrDash, "aAa");
        }

        [Test]
        public void given_really_long_string()
        {
            Should(EventStoreId.Rule.ShouldHave48CharsOrLess, new string('a',49));
        }

        [Test]
        public void given_really_short_string()
        {
            Should(EventStoreId.Rule.ShouldHave3CharsOrMore, "um");
        }

        void Should(EventStoreId.Rule validity, string asdAsd)
        {
            Assert.AreEqual(validity, EventStoreId.IsValid(asdAsd), asdAsd);
        }
    }
}