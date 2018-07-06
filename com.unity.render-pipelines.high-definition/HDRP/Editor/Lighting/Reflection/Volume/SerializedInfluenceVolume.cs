using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class SerializedInfluenceVolume
    {
        public SerializedProperty root;

        public SerializedProperty shapeType;
        public SerializedProperty boxBaseSize;
        public SerializedProperty boxBaseOffset;
        public SerializedProperty boxInfluencePositiveFade;
        public SerializedProperty boxInfluenceNegativeFade;
        public SerializedProperty boxInfluenceNormalPositiveFade;
        public SerializedProperty boxInfluenceNormalNegativeFade;
        public SerializedProperty boxPositiveFaceFade;
        public SerializedProperty boxNegativeFaceFade;
        public SerializedProperty sphereBaseRadius;
        public SerializedProperty sphereBaseOffset;
        public SerializedProperty sphereInfluenceFade;
        public SerializedProperty sphereInfluenceNormalFade;

        internal SerializedProperty editorAdvancedModeBlendDistancePositive;
        internal SerializedProperty editorAdvancedModeBlendDistanceNegative;
        internal SerializedProperty editorSimplifiedModeBlendDistance;
        internal SerializedProperty editorAdvancedModeBlendNormalDistancePositive;
        internal SerializedProperty editorAdvancedModeBlendNormalDistanceNegative;
        internal SerializedProperty editorSimplifiedModeBlendNormalDistance;
        internal SerializedProperty editorAdvancedModeEnabled;

        public SerializedInfluenceVolume(SerializedProperty root)
        {
            this.root = root;

            shapeType = root.Find((InfluenceVolume i) => i.shapeType);
            boxBaseSize = root.Find((InfluenceVolume i) => i.boxBaseSize);
            boxBaseOffset = root.Find((InfluenceVolume i) => i.boxBaseOffset);
            boxInfluencePositiveFade = root.Find((InfluenceVolume i) => i.boxInfluencePositiveFade);
            boxInfluenceNegativeFade = root.Find((InfluenceVolume i) => i.boxInfluenceNegativeFade);
            boxInfluenceNormalPositiveFade = root.Find((InfluenceVolume i) => i.boxInfluenceNormalPositiveFade);
            boxInfluenceNormalNegativeFade = root.Find((InfluenceVolume i) => i.boxInfluenceNormalNegativeFade);
            boxPositiveFaceFade = root.Find((InfluenceVolume i) => i.boxPositiveFaceFade);
            boxNegativeFaceFade = root.Find((InfluenceVolume i) => i.boxNegativeFaceFade);
            sphereBaseRadius = root.Find((InfluenceVolume i) => i.sphereBaseRadius);
            sphereBaseOffset = root.Find((InfluenceVolume i) => i.sphereBaseOffset);
            sphereInfluenceFade = root.Find((InfluenceVolume i) => i.sphereInfluenceFade);
            sphereInfluenceNormalFade = root.Find((InfluenceVolume i) => i.sphereInfluenceNormalFade);

            editorAdvancedModeBlendDistancePositive = root.FindPropertyRelative("editorAdvancedModeBlendDistancePositive");
            editorAdvancedModeBlendDistanceNegative = root.FindPropertyRelative("editorAdvancedModeBlendDistanceNegative");
            editorSimplifiedModeBlendDistance = root.FindPropertyRelative("editorSimplifiedModeBlendDistance");
            editorAdvancedModeBlendNormalDistancePositive = root.FindPropertyRelative("editorAdvancedModeBlendNormalDistancePositive");
            editorAdvancedModeBlendNormalDistanceNegative = root.FindPropertyRelative("editorAdvancedModeBlendNormalDistanceNegative");
            editorSimplifiedModeBlendNormalDistance = root.FindPropertyRelative("editorSimplifiedModeBlendNormalDistance");
            editorAdvancedModeEnabled = root.FindPropertyRelative("editorAdvancedModeEnabled");
            //handle data migration from before editor value were saved
            if(editorAdvancedModeBlendDistancePositive.vector3Value == Vector3.zero
                && editorAdvancedModeBlendDistanceNegative.vector3Value == Vector3.zero
                && editorSimplifiedModeBlendDistance.floatValue == 0f
                && editorAdvancedModeBlendNormalDistancePositive.vector3Value == Vector3.zero
                && editorAdvancedModeBlendNormalDistanceNegative.vector3Value == Vector3.zero
                && editorSimplifiedModeBlendNormalDistance.floatValue == 0f
                && (boxInfluencePositiveFade.vector3Value != Vector3.zero
                    || boxInfluenceNegativeFade.vector3Value != Vector3.zero))
            {
                Vector3 positive = boxInfluencePositiveFade.vector3Value;
                Vector3 negative = boxInfluenceNegativeFade.vector3Value;
                //exact advanced
                editorAdvancedModeBlendDistancePositive.vector3Value = positive;
                editorAdvancedModeBlendDistanceNegative.vector3Value = negative;
                //aproximated simplified
                editorSimplifiedModeBlendDistance.floatValue = Mathf.Max(positive.x, positive.y, positive.z, negative.x, negative.y, negative.z);

                //no normal modification allowed anymore in PlanarReflectionProbe
                boxInfluenceNormalPositiveFade.vector3Value = Vector3.zero;
                boxInfluenceNormalNegativeFade.vector3Value = Vector3.zero;

                //display old data
                editorAdvancedModeEnabled.boolValue = true;
                Apply();
            }
        }

        public void Apply()
        {
            root.serializedObject.ApplyModifiedProperties();
        }
    }
}
