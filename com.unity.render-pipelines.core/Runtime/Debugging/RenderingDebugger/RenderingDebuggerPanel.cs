using System;

namespace UnityEngine.Rendering
{
    public abstract class RenderingDebuggerPanel : ScriptableObject
    {
        public abstract string panelName { get; }
        public abstract string uiDocument { get; }
    }
}
