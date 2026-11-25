using System;
using System.Collections;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
#endif

namespace Unity.Rendering.Universal.Tests
{
    /// <summary>
    /// Fails a test that logs one of URP's loose-uniform warnings, which URP and its tests must not cause. Editor only.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class FailOnConstantBufferPropertyWarningsAttribute : NUnitAttribute, IOuterUnityTestAction
    {
#if UNITY_EDITOR
        // Filled from any thread: MaterialPropertyBlock setters can run on worker threads.
        readonly List<string> m_Warnings = new List<string>();
#endif

        public IEnumerator BeforeTest(ITest test)
        {
#if UNITY_EDITOR
            // Each warning is logged once per property: mark every property again, and check the writes during the whole
            // test, not only while URP renders.
            ConstantBufferPropertyWarnings.Release();
            ConstantBufferPropertyWarnings.BeginRender();

            lock (m_Warnings)
                m_Warnings.Clear();
            Application.logMessageReceivedThreaded += OnLogMessage;
#endif
            yield break;
        }

        public IEnumerator AfterTest(ITest test)
        {
#if UNITY_EDITOR
            ConstantBufferPropertyWarnings.EndRender();
            Application.logMessageReceivedThreaded -= OnLogMessage;

            string[] warnings;
            lock (m_Warnings)
            {
                warnings = m_Warnings.ToArray();
                m_Warnings.Clear();
            }

            if (warnings.Length > 0)
                Assert.Fail("Loose-uniform writes to variables of URP's constant buffers:\n" + string.Join("\n", warnings));
#endif
            yield break;
        }

#if UNITY_EDITOR
        void OnLogMessage(string message, string stackTrace, LogType type)
        {
            if (type != LogType.Warning || !IsConstantBufferWarning(message))
                return;

            lock (m_Warnings)
                m_Warnings.Add(message);
        }

        static bool IsConstantBufferWarning(string message)
        {
            if (message == null)
                return false;

            if (Matches(message, ConstantBufferPropertyWarnings.k_GlobalWarningBound)
                || Matches(message, ConstantBufferPropertyWarnings.k_GlobalWarningBoundLater))
                return true;

            foreach (var constantBuffer in ConstantBufferPropertyWarnings.k_LightingConstantBuffers)
            {
                if (Matches(message, ConstantBufferPropertyWarnings.LightingConstantBufferWarning(constantBuffer.name)))
                    return true;
            }

            return message.StartsWith("Compute shader '", StringComparison.Ordinal)
                && message.IndexOf("' is bound as a whole", StringComparison.Ordinal) > 0;
        }

        static bool Matches(string message, string warning)
        {
            int name = warning.IndexOf("{0}", StringComparison.Ordinal);
            return message.StartsWith(warning.Substring(0, name), StringComparison.Ordinal)
                && message.EndsWith(warning.Substring(name + 3), StringComparison.Ordinal);
        }
#endif
    }
}
