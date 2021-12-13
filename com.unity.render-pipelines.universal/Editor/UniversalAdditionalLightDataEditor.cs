using UnityEngine.Rendering.Universal;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.Rendering.Universal
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UniversalAdditionalLightData))]
    public class UniversalAdditionLightDataEditor : Editor
    {
        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
        }
    }
}
