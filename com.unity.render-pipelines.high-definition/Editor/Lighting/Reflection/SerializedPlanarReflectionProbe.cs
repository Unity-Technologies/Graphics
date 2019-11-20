using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    internal class SerializedPlanarReflectionProbe : SerializedHDProbe
    {
        internal SerializedProperty localReferencePosition;

        internal SerializedProperty bakedRenderData;
        internal SerializedProperty customRenderData;

        internal new PlanarReflectionProbe target { get { return serializedObject.targetObject as PlanarReflectionProbe; } }

        internal SerializedPlanarReflectionProbe(SerializedObject serializedObject)
            : base(serializedObject)
        {
            localReferencePosition = serializedObject.Find((PlanarReflectionProbe p) => p.localReferencePosition);
            bakedRenderData = serializedObject.Find((PlanarReflectionProbe p) => p.bakedRenderData);
            customRenderData = serializedObject.Find((PlanarReflectionProbe p) => p.customRenderData);

            probeSettings.influence.editorSimplifiedModeBlendNormalDistance.floatValue = 0;

            if (!HDUtils.IsQuaternionValid(probeSettings.proxyMirrorRotationProxySpace.quaternionValue))
                probeSettings.proxyMirrorRotationProxySpace.quaternionValue = Quaternion.LookRotation(Vector3.forward);
            if (!HDUtils.IsQuaternionValid(probeSettings.proxyCaptureRotationProxySpace.quaternionValue))
                probeSettings.proxyCaptureRotationProxySpace.quaternionValue = Quaternion.LookRotation(Vector3.forward);

#if !ENABLE_BAKED_PLANAR
            // Switch to realtime mode as other modes are not supported.
            probeSettings.mode.SetEnumValue(ProbeSettings.Mode.Realtime);
#endif

            serializedObject.ApplyModifiedProperties();
        }
    }
}
