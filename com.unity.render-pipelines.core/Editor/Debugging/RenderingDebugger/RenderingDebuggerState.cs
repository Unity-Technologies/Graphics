using UnityEngine;

namespace UnityEditor.Rendering
{
    class RenderingDebuggerState : ScriptableObject
    {
        public string selectedPanelName;

        void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
        }
    }
}
