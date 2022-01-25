using System;
using System.Collections.Generic;
using System.Linq;
using Codice.CM.SEIDInfo;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    class RenderingDebuggerState : ScriptableObject
    {
        public string selectedPanelName;

        [SerializeField]
        private List<RenderingDebuggerPanel> m_Panels = new ();

        public RenderingDebuggerPanel GetPanel(Type type)
        {
            if (TryGetPanel(type, out RenderingDebuggerPanel panel))
                return panel;

            panel = ScriptableObject.CreateInstance(type) as RenderingDebuggerPanel;
            m_Panels.Add(panel);

            return panel;
        }

        public RenderingDebuggerPanel GetPanel<T>() => GetPanel(typeof(T));

        public bool TryGetPanel(Type type, out RenderingDebuggerPanel renderingDebuggerPanel)
        {
            renderingDebuggerPanel = m_Panels.FirstOrDefault(p => p.GetType() == type);
            return renderingDebuggerPanel != null;
        }
        public bool TryGetPanel<T>(out RenderingDebuggerPanel renderingDebuggerPanel) => TryGetPanel(typeof(T), out renderingDebuggerPanel);

        void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
        }
    }
}
