using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    sealed partial class HDReflectionProbeEditor
    {
        void InitializeTargetProbe()
        {
            // For an unknown reason, newly created probes sometype have the type "Quad" (value = 1)
            // This type of probe is not supported by Unity since 5.4
            // But we need to force it here so it does not bake into a 2D texture but a Cubemap
            serializedObject.Update();
            serializedObject.FindProperty("m_Type").intValue = 0;
            serializedObject.ApplyModifiedProperties();

            foreach (var target in serializedObject.targetObjects)
            {
                var probeTarget = (ReflectionProbe)target;
                var addTarget = probeTarget.GetComponent<HDAdditionalReflectionData>();
                // unhide previously hidden components if any
                addTarget.hideFlags = HideFlags.None;
            }
        }
    }
}
