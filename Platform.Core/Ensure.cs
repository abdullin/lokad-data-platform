using System;
using System.Diagnostics.Contracts;

namespace Platform
{
    /// <summary>
    /// Set of helper methods that are slightly more readable than
    /// plain exception throws (but are equivalent to  them)
    /// </summary>
    public static class Ensure
    {
        public static void NotNull<T>(T argument, string argumentName) where T : class
        {
            Contract.Requires(argument != null);
            if (argument == null)
                throw new ArgumentNullException(argumentName);
        }

        public static void Nonnegative(long number, string argumentName)
        {
            Contract.Requires(number >= 0);
            if (number < 0)
                throw new ArgumentOutOfRangeException(argumentName, argumentName + " should be non negative.");
        }
    }

    public sealed class TopicName
    {

        public readonly string Name;
        public readonly string Folder;

        TopicName(string name)
        {
            Name = name;
            Folder = "topic-" + name;
        }

        public static TopicName FromName(string name)
        {
            ThrowIfInvalid(name);
            return new TopicName(name);
        }

        static bool IsAlphanumberic(char c)
        {
            if (c >= 'a' && c <= 'z')
                return true;
            if (char.IsDigit(c))
                return true;
            return false;
        }

        public enum Validity : byte
        {
            Valid,
            TooShort,
            TooLong,
            DoesNotStartWithNumberOrLowercaseLetter,
            DoesNotEndWithNumberOrLowercaseLetter,
            DoesNotContainDashesNumbersOrLowercaseChars,
            HasTwoConsequitiveDashes

        }

        public static string ToFolder(string name)
        {
            if (null == name)
                throw new ArgumentNullException("name");
            if (name.StartsWith("topic-"))
                throw new ArgumentException("Can't start with 'topic-'", "name");
            return "topic-" + name;
        }

        public static void ThrowIfInvalid(string name)
        {
            var result = IsValid(name);
            if (result != Validity.Valid)
                throw new ArgumentOutOfRangeException("name", name, "Topic name is invalid. Broken rule is: " + result);
        }

        public static Validity IsValid(string name)
        {
            if (null == name)
                throw new ArgumentNullException("name");
            var length = name.Length;
            if (length < 2)
                return Validity.TooShort;
            if (length > 48)
                return Validity.TooLong;

            int lastDash = -1;
            for (int i = 0; i < length; i++)
            {
                var c = name[i];
                if (i ==0)
                {
                    if (!IsAlphanumberic(c))
                        return Validity.DoesNotStartWithNumberOrLowercaseLetter;
                }
                else if (i == (length-1))
                {
                    if (!IsAlphanumberic(c))
                        return Validity.DoesNotEndWithNumberOrLowercaseLetter;
                }
                else
                {
                    if (c == '-')
                    {
                        if (i - 1 == lastDash)
                            return Validity.HasTwoConsequitiveDashes;
                        lastDash = i;
                    }
                    else
                    {
                        if (!IsAlphanumberic(c))
                            return Validity.DoesNotContainDashesNumbersOrLowercaseChars;
                    }
                }

            }
            return Validity.Valid;

        }
    }
}