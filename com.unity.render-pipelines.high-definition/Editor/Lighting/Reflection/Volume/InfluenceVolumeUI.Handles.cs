using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class InfluenceVolumeUI
    {
        public static void DrawHandles_EditBase(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor o, Transform transform)
        {
            switch ((InfluenceShape)d.shape.intValue)
            {
                case InfluenceShape.Box:
                    DrawBoxHandle(s, d, o, transform, s.boxBaseHandle);
                    break;
                case InfluenceShape.Sphere:
                    using (new Handles.DrawingScope(Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one)))
                    {
                        s.sphereBaseHandle.center = Vector3.zero;
                        s.sphereBaseHandle.radius = d.sphereRadius.floatValue;

                        EditorGUI.BeginChangeCheck();
                        s.sphereBaseHandle.DrawHull(true);
                        s.sphereBaseHandle.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            float radius = s.sphereBaseHandle.radius;
                            d.sphereRadius.floatValue = radius;
                            d.sphereBlendDistance.floatValue = Mathf.Clamp(d.sphereBlendDistance.floatValue, 0, radius);
                            d.sphereBlendNormalDistance.floatValue = Mathf.Clamp(d.sphereBlendNormalDistance.floatValue, 0, radius);
                        }
                        break;
                    }
            }
        }

        public static void DrawHandles_EditInfluence(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor o, Transform transform)
        {
            // Synchronize base handles
            s.boxBaseHandle.center = Vector3.zero;
            s.boxBaseHandle.size = d.boxSize.vector3Value;
            s.sphereBaseHandle.center = Vector3.zero;
            s.sphereBaseHandle.radius = d.sphereRadius.floatValue;

            switch ((InfluenceShape)d.shape.intValue)
            {
                case InfluenceShape.Box:
                    EditorGUI.BeginChangeCheck();
                    DrawBoxFadeHandle(s, d, o, transform, s.boxInfluenceHandle, d.boxBlendDistancePositive, d.boxBlendDistanceNegative);
                    if (EditorGUI.EndChangeCheck())
                    {
                        //save advanced/simplified saved data
                        if (d.editorAdvancedModeEnabled.boolValue)
                        {
                            d.editorAdvancedModeBlendDistancePositive.vector3Value = d.boxBlendDistancePositive.vector3Value;
                            d.editorAdvancedModeBlendDistanceNegative.vector3Value = d.boxBlendDistanceNegative.vector3Value;
                        }
                        else
                            d.editorSimplifiedModeBlendDistance.floatValue = d.boxBlendDistancePositive.vector3Value.x;
                        d.Apply();
                    }
                    break;
                case InfluenceShape.Sphere:
                    DrawSphereFadeHandle(s, d, o, transform, s.sphereInfluenceHandle, d.sphereBlendDistance);
                    break;
            }
        }

        public static void DrawHandles_EditInfluenceNormal(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor o, Transform transform)
        {
            // Synchronize base handles
            s.boxBaseHandle.center = Vector3.zero;
            s.boxBaseHandle.size = d.boxSize.vector3Value;
            s.sphereBaseHandle.center = Vector3.zero;
            s.sphereBaseHandle.radius = d.sphereRadius.floatValue;

            switch ((InfluenceShape)d.shape.intValue)
            {
                case InfluenceShape.Box:
                    EditorGUI.BeginChangeCheck();
                    DrawBoxFadeHandle(s, d, o, transform, s.boxInfluenceNormalHandle, d.boxBlendNormalDistancePositive, d.boxBlendNormalDistanceNegative);
                    if (EditorGUI.EndChangeCheck())
                    {
                        //save advanced/simplified saved data
                        if (d.editorAdvancedModeEnabled.boolValue)
                        {
                            d.editorAdvancedModeBlendNormalDistancePositive.vector3Value = d.boxBlendNormalDistancePositive.vector3Value;
                            d.editorAdvancedModeBlendNormalDistanceNegative.vector3Value = d.boxBlendNormalDistanceNegative.vector3Value;
                        }
                        else
                            d.editorSimplifiedModeBlendNormalDistance.floatValue = d.boxBlendNormalDistancePositive.vector3Value.x;
                        d.Apply();
                    }
                    break;
                case InfluenceShape.Sphere:
                    DrawSphereFadeHandle(s, d, o, transform, s.sphereInfluenceNormalHandle, d.sphereBlendNormalDistance);
                    break;
            }
        }

        static void DrawBoxHandle(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor o, Transform transform, HierarchicalBox box)
        {
            using (new Handles.DrawingScope(Matrix4x4.TRS(Vector3.zero, transform.rotation, Vector3.one)))
            {
                box.center = Quaternion.Inverse(transform.rotation) * transform.position;
                box.size = d.boxSize.vector3Value;

                EditorGUI.BeginChangeCheck();
                box.DrawHull(true);
                box.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    var newPosition = transform.rotation * box.center;
                    Undo.RecordObject(transform, "Moving Influence");
                    transform.position = newPosition;

                    // Clamp blend distances
                    var blendPositive = d.boxBlendDistancePositive.vector3Value;
                    var blendNegative = d.boxBlendDistanceNegative.vector3Value;
                    var blendNormalPositive = d.boxBlendNormalDistancePositive.vector3Value;
                    var blendNormalNegative = d.boxBlendNormalDistanceNegative.vector3Value;
                    var size = box.size;
                    d.boxSize.vector3Value = size;
                    var halfSize = size * .5f;
                    for (int i = 0; i < 3; ++i)
                    {
                        blendPositive[i] = Mathf.Clamp(blendPositive[i], 0f, halfSize[i]);
                        blendNegative[i] = Mathf.Clamp(blendNegative[i], 0f, halfSize[i]);
                        blendNormalPositive[i] = Mathf.Clamp(blendNormalPositive[i], 0f, halfSize[i]);
                        blendNormalNegative[i] = Mathf.Clamp(blendNormalNegative[i], 0f, halfSize[i]);
                    }
                    d.boxBlendDistancePositive.vector3Value = blendPositive;
                    d.boxBlendDistanceNegative.vector3Value = blendNegative;
                    d.boxBlendNormalDistancePositive.vector3Value = blendNormalPositive;
                    d.boxBlendNormalDistanceNegative.vector3Value = blendNormalNegative;

                    if (d.editorAdvancedModeEnabled.boolValue)
                    {
                        d.editorAdvancedModeBlendDistancePositive.vector3Value = d.boxBlendDistancePositive.vector3Value;
                        d.editorAdvancedModeBlendDistanceNegative.vector3Value = d.boxBlendDistanceNegative.vector3Value;
                        d.editorAdvancedModeBlendNormalDistancePositive.vector3Value = d.boxBlendNormalDistancePositive.vector3Value;
                        d.editorAdvancedModeBlendNormalDistanceNegative.vector3Value = d.boxBlendNormalDistanceNegative.vector3Value;
                    }
                    else
                    {
                        d.editorSimplifiedModeBlendDistance.floatValue = Mathf.Min(
                            d.boxBlendDistancePositive.vector3Value.x,
                            d.boxBlendDistancePositive.vector3Value.y,
                            d.boxBlendDistancePositive.vector3Value.z,
                            d.boxBlendDistanceNegative.vector3Value.x,
                            d.boxBlendDistanceNegative.vector3Value.y,
                            d.boxBlendDistanceNegative.vector3Value.z);
                        d.boxBlendDistancePositive.vector3Value = d.boxBlendDistanceNegative.vector3Value = Vector3.one * d.editorSimplifiedModeBlendDistance.floatValue;
                        d.editorSimplifiedModeBlendNormalDistance.floatValue = Mathf.Min(
                            d.boxBlendNormalDistancePositive.vector3Value.x,
                            d.boxBlendNormalDistancePositive.vector3Value.y,
                            d.boxBlendNormalDistancePositive.vector3Value.z,
                            d.boxBlendNormalDistanceNegative.vector3Value.x,
                            d.boxBlendNormalDistanceNegative.vector3Value.y,
                            d.boxBlendNormalDistanceNegative.vector3Value.z);
                        d.boxBlendNormalDistancePositive.vector3Value = d.boxBlendNormalDistanceNegative.vector3Value = Vector3.one * d.editorSimplifiedModeBlendNormalDistance.floatValue;
                    }
                }
            }
        }

        static void DrawBoxFadeHandle(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor o, Transform transform, HierarchicalBox box, SerializedProperty positive, SerializedProperty negative)
        {
            using (new Handles.DrawingScope(Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one)))
            {
                box.center = -(positive.vector3Value - negative.vector3Value) * 0.5f;
                box.size = d.boxSize.vector3Value - positive.vector3Value - negative.vector3Value;
                box.monoHandle = !d.editorAdvancedModeEnabled.boolValue;

                EditorGUI.BeginChangeCheck();
                box.DrawHandle();
                box.DrawHull(true);
                if (EditorGUI.EndChangeCheck())
                {
                    var influenceCenter = Vector3.zero;
                    var halfInfluenceSize = d.boxSize.vector3Value * .5f;

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

        static void DrawSphereFadeHandle(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor o, Transform transform, HierarchicalSphere sphere, SerializedProperty radius)
        {
            //init parent sphere for clamping
            s.sphereBaseHandle.center = Vector3.zero;
            s.sphereBaseHandle.radius = d.sphereRadius.floatValue;

            using (new Handles.DrawingScope(Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one)))
            {
                sphere.center = Vector3.zero;
                sphere.radius = d.sphereRadius.floatValue - radius.floatValue;

                EditorGUI.BeginChangeCheck();
                sphere.DrawHull(true);
                sphere.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                    radius.floatValue = Mathf.Clamp(d.sphereRadius.floatValue - sphere.radius, 0, d.sphereRadius.floatValue);
            }
        }
    }
}
