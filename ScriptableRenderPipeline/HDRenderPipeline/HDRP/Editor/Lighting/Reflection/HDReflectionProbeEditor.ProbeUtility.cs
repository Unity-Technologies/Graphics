using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering
{
    partial class HDReflectionProbeEditor
    {
        void ChangeVisibilityOfAllTargets(bool visibility)
        {
            for (var i = 0; i < targets.Length; ++i)
            {
                var p = (ReflectionProbe)targets[i];
                HDReflectionProbeEditorUtility.ChangeVisibility(p, visibility);
            }
        }

        void InitializeAllTargetProbes()
        {
            for (var i = 0; i < targets.Length; ++i)
            {
                var p = (ReflectionProbe)targets[i];
                var a = (HDAdditionalReflectionData)m_AdditionalDataSerializedObject.targetObjects[i];
                HDReflectionProbeEditorUtility.InitializeProbe(p, a);
            }

            // For an unknown reason, newly created probes sometype have the type "Quad" (value = 1)
            // This type of probe is not supported by Unity since 5.4
            // But we need to force it here so it does not bake into a 2D texture but a Cubemap
            serializedObject.Update();
            serializedObject.FindProperty("m_Type").intValue = 0;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
