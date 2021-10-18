using System;
using System.Diagnostics;

namespace UnityEngine.Rendering
{
    class StackTraceException : Exception
    {
        StackTrace m_StackTrace;

        internal StackTraceException(Exception exception)
            : base(exception.Message, exception)
        {
            m_StackTrace = new StackTrace(2, true);
        }

        public override string StackTrace => m_StackTrace.ToString();
    }

    static class ExceptionUtilities
    {
        /// <summary>
        /// Wraps an exception to provide the stacktrace of its instantiation instead of its thrown location
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public static Exception WithStackTrace(this Exception exception)
        {
            if (exception == null) return null;

            return new StackTraceException(exception);
        }
    }
}
