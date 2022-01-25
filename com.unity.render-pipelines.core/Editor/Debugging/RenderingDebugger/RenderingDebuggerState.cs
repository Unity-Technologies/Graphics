using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    class RenderingDebuggerState : ScriptableObject
    {
        private const string kSavePath = "Temp/RenderingDebuggerState.asset";

        public string selectedPanelName;

        [SerializeField]
        public List<RenderingDebuggerPanel> panels = new ();

        public RenderingDebuggerPanel GetPanel(Type type)
        {
            if (TryGetPanel(type, out RenderingDebuggerPanel panel))
                return panel;

            panel = ScriptableObject.CreateInstance(type) as RenderingDebuggerPanel;
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

        internal static RenderingDebuggerState Load()
        {
            RenderingDebuggerState state = AssetDatabase.LoadAssetAtPath<RenderingDebuggerState>(kSavePath);
            if (state == null)
            {
                state = ScriptableObject.CreateInstance<RenderingDebuggerState>();
            }
            return state;
        }

        internal static void Save(RenderingDebuggerState state)
        {
            var objects = new List<UnityEngine.Object>(){ state };
            objects.AddRange(state.panels);

            InternalEditorUtility.SaveToSerializedFileAndForget(objects.ToArray(), kSavePath, true);
            /*AssetDatabase.DeleteAsset(kSavePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.CreateAsset(state, kSavePath);
            foreach (var panel in state.panels)
            {
                AssetDatabase.AddObjectToAsset(panel, state);
            }
            AssetDatabase.SaveAssets();*/
        }
    }
}
