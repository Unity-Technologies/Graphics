using NUnit.Framework;
using System;
using UnityEngine;

namespace Diagnostics
{
    public static class DiagnosticSwitches
    {
        public const string ExportPreviewPNGs = "ExportPreviewPNGs";
    }

    public class DiagnosticSwitchGuard : IDisposable
    {
        DiagnosticSwitch m_Switch;
        object m_OriginalValue;

        public DiagnosticSwitchGuard(string name, object value)
        {
            m_Switch = FindSwitch(name);
            Assert.IsNotNull(m_Switch);

            m_OriginalValue = m_Switch.value;
            m_Switch.value = value;
        }

        public void Dispose()
        {
            m_Switch.value = m_OriginalValue;
        }

        private DiagnosticSwitch FindSwitch(string name)
        {
            foreach (var diagnosticSwitch in Debug.diagnosticSwitches)
            {
                if (diagnosticSwitch != null && diagnosticSwitch.name == name)
                {
                    return diagnosticSwitch;
                }
            }

            return null;
        }
    }
}
