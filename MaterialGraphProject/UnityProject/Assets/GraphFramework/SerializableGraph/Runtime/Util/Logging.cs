using System;

namespace UnityEngine.Graphing
{
    public class ConsoleLogHandler : ILogHandler
    {
        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            var formatted = string.Format(format, args);
            Console.WriteLine("{0}:{1} - {2}", logType, context, formatted);
        }

        public void LogException(Exception exception, Object context)
        {
            Console.WriteLine("{0} - {1}", context, exception);
        }
    }
}
