using System;

namespace UnityEngine.Rendering.HighDefinition
{
    public struct CustomPassRenderEventContext
    {
        public enum EventType
        {
            OnExecute
        }

        public EventType eventType;
        public HDCamera hdCamera;
    }
}
