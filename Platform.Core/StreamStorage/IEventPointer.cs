using System;

namespace Platform.StreamStorage
{
    /// <summary>
    /// Atomic pointer to some event. It usually acts as commit record.
    /// Only one writer is allowed per pointer, but multiple readers are ok.
    /// </summary>
    public interface IEventPointer : IDisposable
    {
        /// <summary>
        /// Get current pointer
        /// </summary>
        /// <returns></returns>
        long Read();
        /// <summary>
        /// Write position
        /// </summary>
        /// <param name="position"></param>
        void Write(long position);
    }
}