using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
#if UNITY_EDITOR
    public class RenderingDebuggerState : ScriptableSingleton<RenderingDebuggerState>
    {
#else
    public class RenderingDebuggerState : ScriptableObject
    {
        private static RenderingDebuggerState s_Instance;

        /// <summary>
        /// The singleton instance that contains the current settings of Rendering Debugger.
        /// </summary>
        public static RenderingDebuggerState instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = CreateInstance<RenderingDebuggerState>();
                return s_Instance;
            }
        }
#endif

        public string selectedPanelName;

        private List<RenderingDebuggerPanel> panels = new ();

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

        public T GetPanel<T>()
            where T : RenderingDebuggerPanel => GetPanel(typeof(T)) as T;

        public bool TryGetPanel(Type type, out RenderingDebuggerPanel renderingDebuggerPanel)
        {
            renderingDebuggerPanel = panels.FirstOrDefault(p => p.GetType() == type);
            return panels.Any(p => p.GetType() == type);
        }
        public bool TryGetPanel<T>(out RenderingDebuggerPanel renderingDebuggerPanel) => TryGetPanel(typeof(T), out renderingDebuggerPanel);

        public List<RenderingDebuggerPanel> GetPanels() => panels;

        void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
        }
    }
}
