using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    internal class SerializedHDReflectionProbe
    {
        internal ReflectionProbe target;
        internal HDAdditionalReflectionData targetData;

        internal SerializedObject so;
        internal SerializedObject addso;

        internal SerializedProperty mode;
        internal SerializedProperty renderDynamicObjects;
        internal SerializedProperty customBakedTexture;
        internal SerializedProperty refreshMode;
        internal SerializedProperty timeSlicingMode;
        internal SerializedProperty intensityMultiplier;
        internal SerializedProperty legacyBlendDistance;
        internal SerializedProperty boxSize;
        internal SerializedProperty boxOffset;
        internal SerializedProperty resolution;
        internal SerializedProperty shadowDistance;
        internal SerializedProperty cullingMask;
        internal SerializedProperty useOcclusionCulling;
        internal SerializedProperty nearClip;
        internal SerializedProperty farClip;
        internal SerializedProperty boxProjection;
        internal SerializedProperty weight;
        internal SerializedProperty multiplier;

        internal SerializedProperty proxyVolumeComponent;

        internal SerializedInfluenceVolume influenceVolume;

        public SerializedHDReflectionProbe(SerializedObject so, SerializedObject addso)
        {
            this.so = so;
            this.addso = addso;

            proxyVolumeComponent = addso.Find((HDAdditionalReflectionData d) => d.proxyVolume);
            influenceVolume = new SerializedInfluenceVolume(addso.Find((HDAdditionalReflectionData d) => d.influenceVolume));

            target = (ReflectionProbe)so.targetObject;
            targetData = target.GetComponent<HDAdditionalReflectionData>();

            mode = so.FindProperty("m_Mode");
            customBakedTexture = so.FindProperty("m_CustomBakedTexture");
            renderDynamicObjects = so.FindProperty("m_RenderDynamicObjects");
            refreshMode = so.FindProperty("m_RefreshMode");
            timeSlicingMode = so.FindProperty("m_TimeSlicingMode");
            intensityMultiplier = so.FindProperty("m_IntensityMultiplier");
            boxSize = so.FindProperty("m_BoxSize");
            boxOffset = so.FindProperty("m_BoxOffset");
            resolution = so.FindProperty("m_Resolution");
            shadowDistance = so.FindProperty("m_ShadowDistance");
            cullingMask = so.FindProperty("m_CullingMask");
            useOcclusionCulling = so.FindProperty("m_UseOcclusionCulling");
            nearClip = so.FindProperty("m_NearClip");
            farClip = so.FindProperty("m_FarClip");
            boxProjection = so.FindProperty("m_BoxProjection");
            legacyBlendDistance = so.FindProperty("m_BlendDistance");

            weight = addso.Find((HDAdditionalReflectionData d) => d.weight);
            multiplier = addso.Find((HDAdditionalReflectionData d) => d.multiplier);
        }

        public void Update()
        {
            so.Update();
            addso.Update();
            //InfluenceVolume does not have Update. Add it here if it have in the futur.

            // Set the legacy blend distance to 0 so the legacy culling system use the probe extent
            legacyBlendDistance.floatValue = 0;
        }

        public void Apply()
        {
            so.ApplyModifiedProperties();
            addso.ApplyModifiedProperties();
        }
    }
}
