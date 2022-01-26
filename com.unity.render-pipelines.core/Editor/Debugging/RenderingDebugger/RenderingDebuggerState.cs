using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    class RenderingDebuggerState : ScriptableObject
    {
        public string selectedPanelName;

        public List<RenderingDebuggerPanel> panels = new ();

        public RenderingDebuggerPanel GetPanel(Type type)
        {
            if (TryGetPanel(type, out RenderingDebuggerPanel panel))
                return panel;

            panel = ScriptableObject.CreateInstance(type) as RenderingDebuggerPanel;

            // IMPORTANT: Since this class has HideAndDontSave, all ScriptableObjects inside of it must also have it.
            // Otherwise you will end up with null objects in your List<ScriptableObject> when returning from play mode.
            // Also, if you use UITK Binding on an object with "Hide" in hideFlags, it will disable UI. Hence we only use
            // HideFlags.DontSave here.
            panel.hideFlags = HideFlags.DontSave;

            panels.Add(panel);

            return panel;
        }

        public RenderingDebuggerPanel GetPanel<T>() => GetPanel(typeof(T));

        public bool TryGetPanel(Type type, out RenderingDebuggerPanel renderingDebuggerPanel)
        {
            renderingDebuggerPanel = panels.FirstOrDefault(p => p.GetType() == type);
            return panels.Any(p => p.GetType() == type);
        }
        public bool TryGetPanel<T>(out RenderingDebuggerPanel renderingDebuggerPanel) => TryGetPanel(typeof(T), out renderingDebuggerPanel);

        void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
        }
    }
}
