using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedGlobalProbeVolumeSettings
    {
        internal SerializedProperty root;

        internal SerializedGlobalProbeVolumeSettings(SerializedProperty root)
        {
            this.root = root;
        }
    }
}
