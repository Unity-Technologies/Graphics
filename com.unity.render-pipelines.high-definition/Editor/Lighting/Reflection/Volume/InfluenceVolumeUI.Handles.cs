using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    partial class InfluenceVolumeUI
    {
        public static void DrawHandles_EditBase(SerializedInfluenceVolume serialized, Editor owner, Transform transform)
        {
            switch ((InfluenceShape)serialized.shape.intValue)
            {
                case InfluenceShape.Box:
                    DrawBoxHandle(serialized, owner, transform, s_BoxBaseHandle);
                    break;
                case InfluenceShape.Sphere:
                    using (new Handles.DrawingScope(Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one)))
                    {
                        s_SphereBaseHandle.center = Vector3.zero;
                        s_SphereBaseHandle.radius = serialized.sphereRadius.floatValue;

                        EditorGUI.BeginChangeCheck();
                        s_SphereBaseHandle.DrawHull(true);
                        s_SphereBaseHandle.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            Vector3 localSize = serialized.boxSize.vector3Value;
                            for (int i = 0; i < 3; ++i)
                            {
                                localSize[i] = Mathf.Max(Mathf.Epsilon, localSize[i]);
                            }
                            serialized.boxSize.vector3Value = localSize;
                            float radius = s_SphereBaseHandle.radius;
                            serialized.sphereRadius.floatValue = radius;
                            serialized.sphereBlendDistance.floatValue = Mathf.Clamp(serialized.sphereBlendDistance.floatValue, 0, radius);
                            serialized.sphereBlendNormalDistance.floatValue = Mathf.Clamp(serialized.sphereBlendNormalDistance.floatValue, 0, radius);
                        }
                        break;
                    }
            }
        }

        public static void DrawHandles_EditInfluence(SerializedInfluenceVolume serialized, Editor owner, Transform transform)
        {
            // Synchronize base handles
            s_BoxBaseHandle.center = Vector3.zero;
            s_BoxBaseHandle.size = serialized.boxSize.vector3Value;
            s_SphereBaseHandle.center = Vector3.zero;
            s_SphereBaseHandle.radius = serialized.sphereRadius.floatValue;

            switch ((InfluenceShape)serialized.shape.intValue)
            {
                case InfluenceShape.Box:
                    EditorGUI.BeginChangeCheck();
                    DrawBoxFadeHandle(serialized, owner, transform, s_BoxInfluenceHandle, serialized.boxBlendDistancePositive, serialized.boxBlendDistanceNegative);
                    if (EditorGUI.EndChangeCheck())
                    {
                        //save advanced/simplified saved data
                        if (serialized.editorAdvancedModeEnabled.boolValue)
                        {
                            serialized.editorAdvancedModeBlendDistancePositive.vector3Value = serialized.boxBlendDistancePositive.vector3Value;
                            serialized.editorAdvancedModeBlendDistanceNegative.vector3Value = serialized.boxBlendDistanceNegative.vector3Value;
                        }
                        else
                            serialized.editorSimplifiedModeBlendDistance.floatValue = serialized.boxBlendDistancePositive.vector3Value.x;
                        serialized.Apply();
                    }
                    break;
                case InfluenceShape.Sphere:
                    DrawSphereFadeHandle(serialized, owner, transform, s_SphereInfluenceHandle, serialized.sphereBlendDistance);
                    break;
            }
        }

        public static void DrawHandles_EditInfluenceNormal(SerializedInfluenceVolume serialized, Editor owner, Transform transform)
        {
            // Synchronize base handles
            s_BoxBaseHandle.center = Vector3.zero;
            s_BoxBaseHandle.size = serialized.boxSize.vector3Value;
            s_SphereBaseHandle.center = Vector3.zero;
            s_SphereBaseHandle.radius = serialized.sphereRadius.floatValue;

            switch ((InfluenceShape)serialized.shape.intValue)
            {
                case InfluenceShape.Box:
                    EditorGUI.BeginChangeCheck();
                    DrawBoxFadeHandle(serialized, owner, transform, s_BoxInfluenceNormalHandle, serialized.boxBlendNormalDistancePositive, serialized.boxBlendNormalDistanceNegative);
                    if (EditorGUI.EndChangeCheck())
                    {
                        //save advanced/simplified saved data
                        if (serialized.editorAdvancedModeEnabled.boolValue)
                        {
                            serialized.editorAdvancedModeBlendNormalDistancePositive.vector3Value = serialized.boxBlendNormalDistancePositive.vector3Value;
                            serialized.editorAdvancedModeBlendNormalDistanceNegative.vector3Value = serialized.boxBlendNormalDistanceNegative.vector3Value;
                        }
                        else
                            serialized.editorSimplifiedModeBlendNormalDistance.floatValue = serialized.boxBlendNormalDistancePositive.vector3Value.x;
                        serialized.Apply();
                    }
                    break;
                case InfluenceShape.Sphere:
                    DrawSphereFadeHandle(serialized, owner, transform, s_SphereInfluenceNormalHandle, serialized.sphereBlendNormalDistance);
                    break;
            }
        }

        static void DrawBoxHandle(SerializedInfluenceVolume serialized, Editor owner, Transform transform, HierarchicalBox box)
        {
            using (new Handles.DrawingScope(Matrix4x4.TRS(Vector3.zero, transform.rotation, Vector3.one)))
            {
                box.center = Quaternion.Inverse(transform.rotation) * transform.position;
                box.size = serialized.boxSize.vector3Value;

                EditorGUI.BeginChangeCheck();
                box.DrawHull(true);
                box.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    var newPosition = transform.rotation * box.center;
                    Undo.RecordObject(transform, "Moving Influence");
                    transform.position = newPosition;

                    // Clamp blend distances
                    var blendPositive = serialized.boxBlendDistancePositive.vector3Value;
                    var blendNegative = serialized.boxBlendDistanceNegative.vector3Value;
                    var blendNormalPositive = serialized.boxBlendNormalDistancePositive.vector3Value;
                    var blendNormalNegative = serialized.boxBlendNormalDistanceNegative.vector3Value;
                    var size = box.size;
                    serialized.boxSize.vector3Value = size;
                    var halfSize = size * .5f;
                    for (int i = 0; i < 3; ++i)
                    {
                        blendPositive[i] = Mathf.Clamp(blendPositive[i], 0f, halfSize[i]);
                        blendNegative[i] = Mathf.Clamp(blendNegative[i], 0f, halfSize[i]);
                        blendNormalPositive[i] = Mathf.Clamp(blendNormalPositive[i], 0f, halfSize[i]);
                        blendNormalNegative[i] = Mathf.Clamp(blendNormalNegative[i], 0f, halfSize[i]);
                    }
                    serialized.boxBlendDistancePositive.vector3Value = blendPositive;
                    serialized.boxBlendDistanceNegative.vector3Value = blendNegative;
                    serialized.boxBlendNormalDistancePositive.vector3Value = blendNormalPositive;
                    serialized.boxBlendNormalDistanceNegative.vector3Value = blendNormalNegative;

                    if (serialized.editorAdvancedModeEnabled.boolValue)
                    {
                        serialized.editorAdvancedModeBlendDistancePositive.vector3Value = serialized.boxBlendDistancePositive.vector3Value;
                        serialized.editorAdvancedModeBlendDistanceNegative.vector3Value = serialized.boxBlendDistanceNegative.vector3Value;
                        serialized.editorAdvancedModeBlendNormalDistancePositive.vector3Value = serialized.boxBlendNormalDistancePositive.vector3Value;
                        serialized.editorAdvancedModeBlendNormalDistanceNegative.vector3Value = serialized.boxBlendNormalDistanceNegative.vector3Value;
                    }
                    else
                    {
                        serialized.editorSimplifiedModeBlendDistance.floatValue = Mathf.Min(
                            serialized.boxBlendDistancePositive.vector3Value.x,
                            serialized.boxBlendDistancePositive.vector3Value.y,
                            serialized.boxBlendDistancePositive.vector3Value.z,
                            serialized.boxBlendDistanceNegative.vector3Value.x,
                            serialized.boxBlendDistanceNegative.vector3Value.y,
                            serialized.boxBlendDistanceNegative.vector3Value.z);
                        serialized.boxBlendDistancePositive.vector3Value = serialized.boxBlendDistanceNegative.vector3Value = Vector3.one * serialized.editorSimplifiedModeBlendDistance.floatValue;
                        serialized.editorSimplifiedModeBlendNormalDistance.floatValue = Mathf.Min(
                            serialized.boxBlendNormalDistancePositive.vector3Value.x,
                            serialized.boxBlendNormalDistancePositive.vector3Value.y,
                            serialized.boxBlendNormalDistancePositive.vector3Value.z,
                            serialized.boxBlendNormalDistanceNegative.vector3Value.x,
                            serialized.boxBlendNormalDistanceNegative.vector3Value.y,
                            serialized.boxBlendNormalDistanceNegative.vector3Value.z);
                        serialized.boxBlendNormalDistancePositive.vector3Value = serialized.boxBlendNormalDistanceNegative.vector3Value = Vector3.one * serialized.editorSimplifiedModeBlendNormalDistance.floatValue;
                    }
                }
            }
        }

        static void DrawBoxFadeHandle(SerializedInfluenceVolume serialized, Editor owner, Transform transform, HierarchicalBox box, SerializedProperty positive, SerializedProperty negative)
        {
            using (new Handles.DrawingScope(Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one)))
            {
                box.center = -(positive.vector3Value - negative.vector3Value) * 0.5f;
                box.size = serialized.boxSize.vector3Value - positive.vector3Value - negative.vector3Value;
                box.monoHandle = !serialized.editorAdvancedModeEnabled.boolValue;

                EditorGUI.BeginChangeCheck();
                box.DrawHandle();
                box.DrawHull(true);
                if (EditorGUI.EndChangeCheck())
                {
                    var influenceCenter = Vector3.zero;
                    var halfInfluenceSize = serialized.boxSize.vector3Value * .5f;

                    var centerDiff = box.center - influenceCenter;
                    var halfSizeDiff = halfInfluenceSize - box.size * .5f;
                    var positiveNew = halfSizeDiff - centerDiff;
                    var negativeNew = halfSizeDiff + centerDiff;
                    var blendDistancePositive = Vector3.Max(Vector3.zero, Vector3.Min(positiveNew, halfInfluenceSize));
                    var blendDistanceNegative = Vector3.Max(Vector3.zero, Vector3.Min(negativeNew, halfInfluenceSize));

                    positive.vector3Value = blendDistancePositive;
                    negative.vector3Value = blendDistanceNegative;
                }
            }
        }

        static void DrawSphereFadeHandle(SerializedInfluenceVolume serialized, Editor owner, Transform transform, HierarchicalSphere sphere, SerializedProperty radius)
        {
            //init parent sphere for clamping
            s_SphereBaseHandle.center = Vector3.zero;
            s_SphereBaseHandle.radius = serialized.sphereRadius.floatValue;

            using (new Handles.DrawingScope(Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one)))
            {
                sphere.center = Vector3.zero;
                sphere.radius = serialized.sphereRadius.floatValue - radius.floatValue;

                EditorGUI.BeginChangeCheck();
                sphere.DrawHull(true);
                sphere.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                    radius.floatValue = Mathf.Clamp(serialized.sphereRadius.floatValue - sphere.radius, 0, serialized.sphereRadius.floatValue);
            }
        }
    }
}
