using System;

namespace Platform
{
    /// <summary>
    /// Id of an event store within Data Platform. It has to follow strict semantics.
    /// </summary>
    public sealed class EventStoreId
    {
        public readonly string Name;

        EventStoreId(string name)
        {
            Name = name;
        }

        public static EventStoreId Parse(string name)
        {
            ThrowIfInvalid(name);
            return new EventStoreId(name);
        }

        static bool IsAlphanumberic(char c)
        {
            if (c >= 'a' && c <= 'z')
                return true;
            if (char.IsDigit(c))
                return true;
            return false;
        }

        public static void ThrowIfInvalid(string name)
        {
            var result = IsValid(name);
            if (result != Rule.Valid)
                throw new ArgumentOutOfRangeException("name", name, "Topic name is invalid. Broken rule is: " + result);
        }

        public static Rule IsValid(string name)
        {
            if (null == name)
                throw new ArgumentNullException("name");
            var length = name.Length;
            if (length < 3)
                return Rule.ShouldHave3CharsOrMore;
            if (length > 48)
                return Rule.ShouldHave48CharsOrLess;

            if (!IsAlphanumberic(name[0]))
                return Rule.ShouldStartWithLowercaseLetterOrNumber;

            if (!IsAlphanumberic(name[length - 1]))
                return Rule.ShouldEndWithLowercaseLetterOrNumber;
            

            var lastDash = -1;
            for (var i = 1; i < (length-1); i++)
            {
                var c = name[i];
                if (c == '-')
                {
                    if (i - 1 == lastDash)
                        return Rule.ShouldNotHaveHasTwoConsequitiveDashes;
                    lastDash = i;
                }
                else
                {
                    if (!IsAlphanumberic(c))
                        return Rule.ShouldContainOnlyLowercaseLetterNumberOrDash;
                }

            }
            return Rule.Valid;

        }

        public enum Rule : byte
        {
            Valid,
            ShouldHave3CharsOrMore,
            ShouldHave48CharsOrLess,
            ShouldStartWithLowercaseLetterOrNumber,
            ShouldEndWithLowercaseLetterOrNumber,
            ShouldContainOnlyLowercaseLetterNumberOrDash,
            ShouldNotHaveHasTwoConsequitiveDashes

        }
    }
}