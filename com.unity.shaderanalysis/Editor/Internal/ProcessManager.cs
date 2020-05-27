using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace UnityEditor.ShaderAnalysis.Internal
{
    public class ProcessManager
    {
        public interface IProcess
        {
            ProcessStartInfo startInfo { get; }
            Process process { get; }
            bool hasStarted { get; }
        }

        class ProcessImpl : IProcess
        {
            Action<IProcess> m_PreStart;
            Action<IProcess> m_PostStart;

            public ProcessStartInfo startInfo { get; private set; }
            public Process process { get; private set; }
            public bool hasStarted { get { return process != null; } }

            public ProcessImpl(ProcessStartInfo startInfo, Action<IProcess> preStart, Action<IProcess> postStart)
            {
                this.startInfo = startInfo;
                m_PreStart = preStart;
                m_PostStart = postStart;
            }

            public void Start()
            {
                process = new Process();
                process.StartInfo = startInfo;
                process.EnableRaisingEvents = true;

                if (m_PreStart != null)
                    m_PreStart(this);

                process.Start();

                if (m_PostStart != null)
                    m_PostStart(this);
            }

            public void Cancel()
            {
                if (process != null && !process.HasExited)
                    process.Kill();
            }
        }

        const int k_MaxProcesses = 16;

        static ProcessManager s_Instance = new ProcessManager(k_MaxProcesses);

        int m_UpdateRefCount;
        int m_MaxProcesses;
        List<ProcessImpl> m_PendingProcesses = new List<ProcessImpl>();
        List<ProcessImpl> m_RunningProcesses = new List<ProcessImpl>();

        public static IProcess Enqueue(ProcessStartInfo startInfo, Action<IProcess> preStart, Action<IProcess> postStart)
        {
            return s_Instance.DoEnqueue(startInfo, preStart, postStart);
        }

        public static void Cancel(IProcess process)
        {
            s_Instance.DoCancel(process);
        }

        public ProcessManager(int maxProcesses)
        {
            m_MaxProcesses = maxProcesses;
        }

        IProcess DoEnqueue(ProcessStartInfo startInfo, Action<IProcess> preStart, Action<IProcess> postStart)
        {
            var impl = new ProcessImpl(startInfo, preStart, postStart);
            m_PendingProcesses.Add(impl);

            if (m_UpdateRefCount == 0)
                EditorUpdateManager.ToUpdate += Update;

            ++m_UpdateRefCount;

            return impl;
        }

        void DoCancel(IProcess process)
        {
            var processImpl = (ProcessImpl)process;
            var pendingIndex = m_PendingProcesses.IndexOf(processImpl);
            if (pendingIndex != -1)
            {
                m_PendingProcesses.RemoveAt(pendingIndex);
                return;
            }

            var runningIndex = m_RunningProcesses.IndexOf(processImpl);
            if (runningIndex != -1)
            {
                m_RunningProcesses.RemoveAt(runningIndex);
                processImpl.Cancel();
            }
        }

        void Update()
        {
            for (var i = m_RunningProcesses.Count - 1; i >= 0 ; --i)
            {
                var proc = m_RunningProcesses[i];
                if (proc.process.HasExited)
                {
                    m_RunningProcesses.RemoveAt(i);

                    --m_UpdateRefCount;
                    if (m_UpdateRefCount == 0)
                        EditorUpdateManager.ToUpdate -= Update;
                }
            }

            var processToRun = Mathf.Min(m_MaxProcesses, m_PendingProcesses.Count - m_RunningProcesses.Count);
            for (var i = 0; i < processToRun; i++)
            {
                var proc = m_PendingProcesses[0];
                m_PendingProcesses.RemoveAt(0);

                proc.Start();
                m_RunningProcesses.Add(proc);
            }
        }
    }

    public static class ProcessManagerExtensions
    {
        public static bool IsComplete(this ProcessManager.IProcess process)
        {
            return process != null && process.process != null && process.hasStarted && process.process.HasExited;
        }
    }
}
