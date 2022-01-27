using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace UnityEngine.Rendering.UIGen
{
    public static class Utilities
    {
        public static bool FailIfNullOrEmpty(
            [AllowNull]this string value,
            [DisallowNull] string name,
            [NotNullWhen(false)] out Exception error
        )
        {
            if (string.IsNullOrEmpty(value))
            {
                error = new ArgumentNullException(name);
                return false;
            }

            error = null;
            return true;
        }

        public static string AggregateStrings<TValues>(this TValues values)
            where TValues : IEnumerable<string>
        {
            return values.Aggregate(new StringBuilder(), (acc, s) =>
            {
                acc.Append(s);
                return acc;
            }).ToString();
        }

        public static string AggregateStrings<TValues>(this TValues values, string separator)
            where TValues : IEnumerable<string>
        {
            return values.Aggregate(new StringBuilder(), (acc, s) =>
            {
                if (acc.Length > 0)
                {
                    acc.Append(separator);
                }
                acc.Append(s);
                return acc;
            }).ToString();
        }

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
    }
}
