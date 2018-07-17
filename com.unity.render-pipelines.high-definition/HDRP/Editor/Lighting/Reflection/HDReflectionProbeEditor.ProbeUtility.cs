using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering
{
    partial class HDReflectionProbeEditor
    {
        void InitializeAllTargetProbes()
        {
            // For an unknown reason, newly created probes sometype have the type "Quad" (value = 1)
            // This type of probe is not supported by Unity since 5.4
            // But we need to force it here so it does not bake into a 2D texture but a Cubemap
            serializedObject.Update();
            serializedObject.FindProperty("m_Type").intValue = 0;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
