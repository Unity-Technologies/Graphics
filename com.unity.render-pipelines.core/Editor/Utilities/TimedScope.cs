using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Allows time measurements
    /// </summary>
    /// <example>
    /// double duration = 0;
    /// using (TimedScope.FromPtr(&duration))
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

        void IDisposable.Dispose()
        {
            s_StopWatch.Value.Stop();
            *m_DurationMsPtr = s_StopWatch.Value.Elapsed.TotalMilliseconds;
            s_StopWatch.Value.Reset();
        }

        /// <summary>
        /// Obtains a <see cref="TimedScope"/>
        /// </summary>
        /// <param name="durationMsPtr">The reference to the <see cref="double"/> to fill with the duration</param>
        /// <returns>A <see cref="TimedScope"/></returns>
        public static unsafe TimedScope FromPtr([DisallowNull] double* durationMsPtr)
        {
            return new TimedScope(durationMsPtr);
        }
    }
}
