using System;

namespace UnityEngine.Rendering
{
    // A locally-serialized scene object reference paired with an explicit override flag, stored on a Volume's
    // GameObject rather than in the shared VolumeProfile. Because it lives on the Volume (not the profile asset) it
    // can hold a scene reference and differ per Volume. The override flag gives three states while blending:
    // "do not override" (flag off, let a lower-priority Volume's value pass through), "override with this object"
    // (flag on, value set), and "override with nothing" (flag on, value null). A Volume holds one
    // (Volume.sceneObjectReference); the component type it applies to is resolved at blend time, and the
    // overriding one is surfaced via VolumeStack.GetSceneObjectReference<T>.
    [Serializable]
    internal class VolumeSceneObjectReference
    {
        [SerializeField]
        bool m_OverrideState;

        [SerializeField]
        GameObject m_Value;

        // Whether this Volume overrides the reference. When false, Volumes of lower priority pass through.
        internal bool overrideState
        {
            get => m_OverrideState;
            set => m_OverrideState = value;
        }

        // The referenced object. May be null while overrideState is true, meaning "override with nothing".
        internal GameObject value
        {
            get => m_Value;
            set => m_Value = value;
        }
    }
}
