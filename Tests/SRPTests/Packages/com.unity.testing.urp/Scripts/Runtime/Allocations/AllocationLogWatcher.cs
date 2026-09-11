using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.Universal.Tests.Allocations
{
    /// <summary>
    /// Collects log messages produced while a measurement is running, so a failure can name the cause
    /// in text.
    ///
    /// A logged message allocates: the string, its arguments, and the log machinery. That makes logging
    /// one of the most common causes of a per-frame allocation, and when it is the cause the message
    /// itself is a better diagnosis than a callstack. It also works on every platform, which the
    /// profiler trace does not: on Switch2 <c>Profiler.logFile</c> writes nothing.
    ///
    /// The test framework fails a test that logs an error, with a message saying only that one was
    /// logged. That pre-empts the allocation report on some platforms but not others for the same
    /// underlying fault, so its reaction is suppressed for the length of the run and the errors are
    /// re-raised through <see cref="AllocationResult"/>, which reports them alongside the verdict.
    ///
    /// Costs nothing on a clean run, because a run with no logs never invokes the handler.
    /// </summary>
    sealed class AllocationLogWatcher : IDisposable
    {
        const int k_MaxDistinctMessages = 5;
        const int k_MaxDistinctPlainLogs = 3;

        readonly List<string> m_Messages = new();
        int m_PlainLogCount;
        readonly object m_Lock = new();
        bool m_Subscribed;
        bool m_PreviousIgnoreFailingMessages;
        bool m_HasErrors;

        /// <summary>Whether anything worse than a warning was logged.</summary>
        public bool hasErrors
        {
            get { lock (m_Lock) return m_HasErrors; }
        }

        public void Start()
        {
            if (m_Subscribed)
                return;

            m_PreviousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;

            // The threaded event, not logMessageReceived: that one only fires for logs raised on the
            // main thread (Application.cs, CallLogCallback), so under native graphics jobs the log that
            // explains the allocation is invisible to it. The threaded event fires for both.
            Application.logMessageReceivedThreaded += OnLogMessage;
            m_Subscribed = true;
        }

        public void Dispose()
        {
            if (!m_Subscribed)
                return;

            Application.logMessageReceivedThreaded -= OnLogMessage;
            LogAssert.ignoreFailingMessages = m_PreviousIgnoreFailingMessages;
            m_Subscribed = false;
        }

        void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            lock (m_Lock)
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                    m_HasErrors = true;

                if (m_Messages.Count >= k_MaxDistinctMessages || m_Messages.Contains(condition))
                    return;

                if (type == LogType.Log)
                {
                    if (m_PlainLogCount >= k_MaxDistinctPlainLogs)
                        return;

                    m_PlainLogCount++;
                }

                m_Messages.Add(condition);
            }
        }

        /// <summary>
        /// The distinct messages seen, or null if there were none.
        /// </summary>
        public string Describe()
        {
            lock (m_Lock)
            {
                if (m_Messages.Count == 0)
                    return null;

                var sb = new StringBuilder();
                sb.AppendLine($"  Logged:        {m_Messages.Count} distinct message(s) during the run. A "
                    + "logged message allocates, so if one repeats every frame it is very likely the cause:");
                foreach (var message in m_Messages)
                    sb.AppendLine($"                 {message}");

                return sb.ToString();
            }
        }
    }
}
