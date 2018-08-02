using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    internal class SerializedHDReflectionProbe : SerializedHDProbe
    {
        internal SerializedObject serializedLegacyObject;

        internal SerializedProperty renderDynamicObjects;
        internal SerializedProperty customBakedTexture;
        internal SerializedProperty timeSlicingMode;
        internal SerializedProperty intensityMultiplier;
        internal SerializedProperty legacyBlendDistance;
        internal SerializedProperty boxSize;
        internal SerializedProperty boxOffset;

        internal new HDAdditionalReflectionData target { get { return serializedObject.targetObject as HDAdditionalReflectionData; } }
        internal ReflectionProbe targetLegacy { get { return serializedLegacyObject.targetObject as ReflectionProbe; } }

        public SerializedHDReflectionProbe(SerializedObject legacyProbe, SerializedObject additionalData) : base(additionalData)
        {
            serializedLegacyObject = legacyProbe;

            customBakedTexture = legacyProbe.FindProperty("m_CustomBakedTexture");
            renderDynamicObjects = legacyProbe.FindProperty("m_RenderDynamicObjects");
            timeSlicingMode = legacyProbe.FindProperty("m_TimeSlicingMode");
            intensityMultiplier = legacyProbe.FindProperty("m_IntensityMultiplier");
            boxSize = legacyProbe.FindProperty("m_BoxSize");
            boxOffset = legacyProbe.FindProperty("m_BoxOffset");
            resolution = legacyProbe.FindProperty("m_Resolution");
            shadowDistance = legacyProbe.FindProperty("m_ShadowDistance");
            cullingMask = legacyProbe.FindProperty("m_CullingMask");
            useOcclusionCulling = legacyProbe.FindProperty("m_UseOcclusionCulling");
            nearClip = legacyProbe.FindProperty("m_NearClip");
            farClip = legacyProbe.FindProperty("m_FarClip");
            legacyBlendDistance = legacyProbe.FindProperty("m_BlendDistance");
        }

        internal override void Update()
        {
            serializedLegacyObject.Update();
            base.Update();

            // Set the legacy blend distance to 0 so the legacy culling system use the probe extent
            legacyBlendDistance.floatValue = 0;
        }

        public new void Apply()
        {
            serializedLegacyObject.ApplyModifiedProperties();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
