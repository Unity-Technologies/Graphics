using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
#pragma warning disable 618 // Obsolete warning
    [CanEditMultipleObjects]
    // Disable HDRP custom editor to display full shadow settings (only for dev purpose, reset for pr)
    [CustomEditor(typeof(AdditionalShadowData))]
    class AdditionalShadowDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
        }
    }
#pragma warning restore 618 // Obsolete warning
}
