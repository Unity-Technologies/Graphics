using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Allows time measurements
    /// </summary>
    /// <example>
    /// double duration = 0;
    /// using (TimedScope.FromPtr(&amp;duration))
    /// {
    ///     // something to get the time
    /// }
    /// Debug.Log($"Duration: {duration}")
    /// </example>
    public unsafe struct TimedScope : IDisposable
    {
        static readonly ThreadLocal<Stopwatch> s_StopWatch = new ThreadLocal<Stopwatch>(() => new Stopwatch());

        double* m_DurationMsPtr;

        TimedScope(double* durationMsPtr)
        {
            m_DurationMsPtr = durationMsPtr;
            s_StopWatch.Value.Start();
        }

        /// <summary>
        /// Dispose method to retrieve the time
        /// </summary>
        void IDisposable.Dispose()
        {
            s_StopWatch.Value.Stop();
            *m_DurationMsPtr = s_StopWatch.Value.Elapsed.TotalMilliseconds;
            s_StopWatch.Value.Reset();
        }

        /// <summary>
        /// Obtains a <see cref="TimedScope"/>.
        ///
        /// Safety: <paramref name="durationMsPtr"/> must be a non-null pointer to a valid memory location for a double.
        /// </summary>
        /// <param name="durationMsPtr">The location to write the duration in milliseconds to.</param>
        /// <returns>A <see cref="TimedScope"/></returns>
        public static unsafe TimedScope FromPtr([DisallowNull] double* durationMsPtr)
        {
            return new TimedScope(durationMsPtr);
        }

        /// <summary>
        /// Obtains a <see cref="TimedScope"/>
        /// </summary>
        /// <param name="durationMs">The location to write the duration in milliseconds to.</param>
        /// <returns>A <see cref="TimedScope"/></returns>
        public static TimedScope FromRef(ref double durationMs)
        {
            return new TimedScope((double*)UnsafeUtility.AddressOf(ref durationMs));
        }
    }
}
