using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace UnityEditor.ShaderAnalysis.PSSLInternal
{
    class ProcessOperationBase
    {
        List<string> m_Errors = new List<string> { string.Empty };
        StringBuilder m_ErrorBuilder = new StringBuilder();

        List<string> m_Lines = new List<string>();
        StringBuilder m_OutputBuilder = new StringBuilder();

        public List<string> errors
        {
            get
            {
                Flush();
                return m_Errors;
            }
        }

        public List<string> lines
        {
            get
            {
                Flush();
                return m_Lines;
            }
        }

        public string output
        {
            get
            {
                Flush();
                return m_OutputBuilder.ToString();
            }
        }

        Process m_Process;

        public ProcessOperationBase(Process process)
        {
            m_Process = process;
        }

        public bool isComplete
        {
            get
            {
                return m_Process.HasExited;
            }
        }

        public void Cancel()
        {
            if (!m_Process.HasExited)
                m_Process.Kill();
        }

        void Flush()
        {
            while (m_Process.StandardError.Peek() > -1)
            {
                var line = m_Process.StandardError.ReadLine();
                m_ErrorBuilder.AppendLine(line);
            }
            m_Errors[0] = m_ErrorBuilder.ToString();

            while (m_Process.StandardOutput.Peek() > -1)
            {
                var line = m_Process.StandardOutput.ReadLine();
                m_Lines.Add(line);
                m_OutputBuilder.AppendLine(line);
            }
        }
    }
}
