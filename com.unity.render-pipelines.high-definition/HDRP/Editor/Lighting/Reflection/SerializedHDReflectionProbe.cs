using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    public class SerializedHDReflectionProbe
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

        internal SerializedProperty influenceShape;
        internal SerializedProperty influenceSphereRadius;
        internal SerializedProperty useSeparateProjectionVolume;
        internal SerializedProperty boxReprojectionVolumeSize;
        internal SerializedProperty boxReprojectionVolumeCenter;
        internal SerializedProperty sphereReprojectionVolumeRadius;
        internal SerializedProperty blendDistancePositive;
        internal SerializedProperty blendDistanceNegative;
        internal SerializedProperty blendNormalDistancePositive;
        internal SerializedProperty blendNormalDistanceNegative;
        internal SerializedProperty boxSideFadePositive;
        internal SerializedProperty boxSideFadeNegative;
        internal SerializedProperty weight;
        internal SerializedProperty multiplier;

        internal SerializedProperty editorAdvancedModeBlendDistancePositive;
        internal SerializedProperty editorAdvancedModeBlendDistanceNegative;
        internal SerializedProperty editorSimplifiedModeBlendDistance;
        internal SerializedProperty editorAdvancedModeBlendNormalDistancePositive;
        internal SerializedProperty editorAdvancedModeBlendNormalDistanceNegative;
        internal SerializedProperty editorSimplifiedModeBlendNormalDistance;
        internal SerializedProperty editorAdvancedModeEnabled;

        internal SerializedProperty proxyVolumeComponent;

        public SerializedHDReflectionProbe(SerializedObject so, SerializedObject addso)
        {
            this.so = so;
            this.addso = addso;

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

            influenceShape = addso.Find((HDAdditionalReflectionData d) => d.influenceShape);
            influenceSphereRadius = addso.Find((HDAdditionalReflectionData d) => d.influenceSphereRadius);
            useSeparateProjectionVolume = addso.Find((HDAdditionalReflectionData d) => d.useSeparateProjectionVolume);
            boxReprojectionVolumeSize = addso.Find((HDAdditionalReflectionData d) => d.boxReprojectionVolumeSize);
            boxReprojectionVolumeCenter = addso.Find((HDAdditionalReflectionData d) => d.boxReprojectionVolumeCenter);
            sphereReprojectionVolumeRadius = addso.Find((HDAdditionalReflectionData d) => d.sphereReprojectionVolumeRadius);
            weight = addso.Find((HDAdditionalReflectionData d) => d.weight);
            multiplier = addso.Find((HDAdditionalReflectionData d) => d.multiplier);
            blendDistancePositive = addso.Find((HDAdditionalReflectionData d) => d.blendDistancePositive);
            blendDistanceNegative = addso.Find((HDAdditionalReflectionData d) => d.blendDistanceNegative);
            blendNormalDistancePositive = addso.Find((HDAdditionalReflectionData d) => d.blendNormalDistancePositive);
            blendNormalDistanceNegative = addso.Find((HDAdditionalReflectionData d) => d.blendNormalDistanceNegative);
            boxSideFadePositive = addso.Find((HDAdditionalReflectionData d) => d.boxSideFadePositive);
            boxSideFadeNegative = addso.Find((HDAdditionalReflectionData d) => d.boxSideFadeNegative);

            editorAdvancedModeBlendDistancePositive = addso.FindProperty("editorAdvancedModeBlendDistancePositive");
            editorAdvancedModeBlendDistanceNegative = addso.FindProperty("editorAdvancedModeBlendDistanceNegative");
            editorSimplifiedModeBlendDistance = addso.FindProperty("editorSimplifiedModeBlendDistance");
            editorAdvancedModeBlendNormalDistancePositive = addso.FindProperty("editorAdvancedModeBlendNormalDistancePositive");
            editorAdvancedModeBlendNormalDistanceNegative = addso.FindProperty("editorAdvancedModeBlendNormalDistanceNegative");
            editorSimplifiedModeBlendNormalDistance = addso.FindProperty("editorSimplifiedModeBlendNormalDistance");
            editorAdvancedModeEnabled = addso.FindProperty("editorAdvancedModeEnabled");
            //handle data migration from before editor value were saved
            if(editorAdvancedModeBlendDistancePositive.vector3Value == Vector3.zero
                && editorAdvancedModeBlendDistanceNegative.vector3Value == Vector3.zero
                && editorSimplifiedModeBlendDistance.floatValue == 0f
                && editorAdvancedModeBlendNormalDistancePositive.vector3Value == Vector3.zero
                && editorAdvancedModeBlendNormalDistanceNegative.vector3Value == Vector3.zero
                && editorSimplifiedModeBlendNormalDistance.floatValue == 0f
                && (blendDistancePositive.vector3Value != Vector3.zero
                    || blendDistanceNegative.vector3Value != Vector3.zero
                    || blendNormalDistancePositive.vector3Value != Vector3.zero
                    || blendNormalDistanceNegative.vector3Value != Vector3.zero))
            {
                Vector3 positive = blendDistancePositive.vector3Value;
                Vector3 negative = blendDistanceNegative.vector3Value;
                //exact advanced
                editorAdvancedModeBlendDistancePositive.vector3Value = positive;
                editorAdvancedModeBlendDistanceNegative.vector3Value = negative;
                //aproximated simplified
                editorSimplifiedModeBlendDistance.floatValue = Mathf.Max(positive.x, positive.y, positive.z, negative.x, negative.y, negative.z);

                positive = blendNormalDistancePositive.vector3Value;
                negative = blendNormalDistanceNegative.vector3Value;
                //exact advanced
                editorAdvancedModeBlendNormalDistancePositive.vector3Value = positive;
                editorAdvancedModeBlendNormalDistanceNegative.vector3Value = negative;
                //aproximated simplified
                editorSimplifiedModeBlendNormalDistance.floatValue = Mathf.Max(positive.x, positive.y, positive.z, negative.x, negative.y, negative.z);

                //display old data
                editorAdvancedModeEnabled.boolValue = true;
                Apply();
            }

            proxyVolumeComponent = addso.Find((HDAdditionalReflectionData d) => d.proxyVolumeComponent);
        }

        public void Update()
        {
            so.Update();
            addso.Update();

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
