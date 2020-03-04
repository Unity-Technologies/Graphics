using System;

namespace UnityEditor.ShaderAnalysis
{
    /// <summary>
    /// A helper class to handle progression in specific ranges.
    /// </summary>
    public class ProgressWrapper
    {
        float m_Start;
        float m_End;
        BuildReportJobAsync m_Job;

        public ProgressWrapper(BuildReportJobAsync owner)
            => m_Job = owner ?? throw new ArgumentNullException(nameof(owner));

        /// <summary>
        /// Set current range for normalized progress
        /// Use this in front of any asynchronous operation
        /// So within your method, you don't have to care about the progress range, but only [0..1]
        /// </summary>
        /// <param name="start">Start value of the range, must be in [0..1].</param>
        /// <param name="end">End value of the range, must be in [0..1].</param>
        /// <exception cref="ArgumentException">when <c><paramref name="start"/> &lt; 0</c></exception>
        /// <exception cref="ArgumentException">when <c><paramref name="end"/> &gt; A</c></exception>
        /// <exception cref="ArgumentException">when <c><paramref name="start"/> &gt; <paramref name="end"/></c></exception>
        public void SetProgressRange(float start, float end)
        {
            if (start < 0)
                throw new ArgumentException($"'{nameof(start)}' cannot be less than 0 (got {start})");
            if (end > 1)
                throw new ArgumentException($"'{nameof(end)}' cannot be grater than 1 (got {end})");
            if (start > end)
                throw new ArgumentException($"'{start}' must be less or equal to '{end}' (got {start} < {end})");

            m_Start = start;
            m_End = end;
        }

        /// <summary>
        /// Send the progress to the job in the provided range.
        /// 
        /// Example:
        /// if the range is <c>[0.5..0.75]</c>,
        /// then setting the progress to <c>0.5</c> will actually set in the job the progress value of <c>0.625</c>.
        /// </summary>
        /// <param name="progress">The progress to set, it will be clamped in [0..1]</param>
        /// <param name="message">A descriptive message about the current step, or a format of the message.</param>
        /// <param name="args">Arguments for the formatting of the message.</param>
        public void SetNormalizedProgress(float progress, string message, params object[] args)
        {
            if (args != null && args.Length > 0)
                message = string.Format(message, args);

            progress = Math.Max(0, Math.Min(1, progress));
            m_Job.SetProgress(progress * (m_End - m_Start) + m_Start, message);
        }
    }
}
