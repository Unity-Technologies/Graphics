using System;
using System.Collections;
using UnityEngine.TestTools;

namespace UnityEditor.Rendering.Tests
{
    /// <summary>Test utility. Yields until the change check returns true or maxWaitTime has passed.</summary>
    /// <example>
    /// yield return new WaitUntil(element, condition, 100);
    /// </example>
    public sealed class WaitUntil : IEditModeTestYieldInstruction
    {
        readonly double m_StartTimeSeconds;
        readonly double m_MaxWaitTimeSeconds;
        readonly Func<bool> m_ChangeCheck;

        bool IEditModeTestYieldInstruction.ExpectDomainReload => false;
        bool IEditModeTestYieldInstruction.ExpectedPlaymodeState => false;

        public WaitUntil(Func<bool> changeCheck, double maxWaitTimeInSeconds = 1)
        {
            m_StartTimeSeconds = EditorApplication.timeSinceStartup;
            m_MaxWaitTimeSeconds = maxWaitTimeInSeconds;
            m_ChangeCheck = changeCheck;
        }

        IEnumerator IEditModeTestYieldInstruction.Perform()
        {
            double timeout = m_StartTimeSeconds + m_MaxWaitTimeSeconds + 0.01;

            while (!m_ChangeCheck.Invoke() && timeout > EditorApplication.timeSinceStartup)
                yield return null;
        }
    }
}
