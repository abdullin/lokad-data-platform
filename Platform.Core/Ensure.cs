using System;
using System.Diagnostics.Contracts;

namespace Platform
{
    public static class Ensure
    {
        public static void NotNull<T>(T argument, string argumentName) where T : class
        {
            Contract.Requires(argument != null);
            if (argument == null)
                throw new ArgumentNullException(argumentName);
        }

    }
}