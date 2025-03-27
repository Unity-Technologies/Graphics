using System;
using System.Text;
using System.Collections.Generic;

namespace UnityEngine.Rendering.RenderGraphModule
{
    struct RenderGraphLogIndent : IDisposable
    {
        int m_Indentation;
        RenderGraphLogger m_Logger;
        bool m_Disposed;

        public RenderGraphLogIndent(RenderGraphLogger logger, int indentation = 1)
        {
            m_Disposed = false;
            m_Indentation = indentation;
            m_Logger = logger;

            m_Logger.IncrementIndentation(m_Indentation);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
            Debug.Assert(m_Logger != null, "RenderGraphLogIndent: logger parameter should not be null.");

            if (m_Disposed)
                return;

            if (disposing && m_Logger != null)
            {
                m_Logger.DecrementIndentation(m_Indentation);
            }

            m_Disposed = true;
        }
    }

    class RenderGraphLogger
    {
        Dictionary<string, StringBuilder> m_LogMap = new Dictionary<string, StringBuilder>(); // Can log multiple instances before flush everything.
        StringBuilder m_CurrentBuilder;
        int m_CurrentIndentation;

        public void Initialize(string logName)
        {
            if (!m_LogMap.TryGetValue(logName, out var stringBuilder))
            {
                stringBuilder = new StringBuilder();
                m_LogMap.Add(logName, stringBuilder);
            }

            m_CurrentBuilder = stringBuilder;
            m_CurrentBuilder.Clear();
            m_CurrentIndentation = 0;
        }

        public void IncrementIndentation(int value)
        {
            m_CurrentIndentation += Math.Abs(value);
        }

        public void DecrementIndentation(int value)
        {
            m_CurrentIndentation = Math.Max(0, m_CurrentIndentation - Math.Abs(value));
        }

        public void LogLine(string format, params object[] args)
        {
            for (int i = 0; i < m_CurrentIndentation; ++i)
                m_CurrentBuilder.Append('\t');
            m_CurrentBuilder.AppendFormat(format, args);
            m_CurrentBuilder.AppendLine();
        }

        public void FlushLogs()
        {
            string result = "";
            foreach (var kvp in m_LogMap)
            {
                var builder = kvp.Value;
                builder.AppendLine();

                result += builder.ToString();
            }

            m_LogMap.Clear();

            Debug.Log(result);
        }
    }
}
