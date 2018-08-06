using System;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using Object = UnityEngine.Object;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class InfluenceVolumeUI
    {
        public static void DrawHandles_EditBase(InfluenceVolumeUI s, InfluenceVolume d, Editor o, Matrix4x4 matrix, Object sourceAsset)
        {
            var mat = Handles.matrix;
            var c = Handles.color;
            Handles.matrix = matrix;
            Handles.color = k_GizmoThemeColorBase;
            switch (d.shape)
            {
                case InfluenceShape.Box:
                    {
                        var center = d.offset;
                        var size = d.boxSize;
                        DrawBoxHandle(
                            s, d, o, sourceAsset,
                            s1 => s1.boxBaseHandle,
                            ref center,
                            ref size);
                        d.offset = center;
                        d.boxSize = size;
                        break;
                    }
                case InfluenceShape.Sphere:
                    {
                        var center = d.offset;
                        var radius = d.sphereRadius;
                        DrawSphereHandle(
                            s, d, o, sourceAsset,
                            s1 => s1.sphereBaseHandle,
                            ref center,
                            ref radius);
                        d.offset = center;
                        d.sphereRadius = radius;
                        break;
                    }
            }
            Handles.matrix = mat;
            Handles.color = c;
        }

        public static void DrawHandles_EditInfluence(InfluenceVolumeUI s, InfluenceVolume d, Editor o, Matrix4x4 matrix, Object sourceAsset)
        {
            using (new Handles.DrawingScope(k_GizmoThemeColorInfluence, matrix))
            {
                switch (d.shape)
                {
                    case InfluenceShape.Box:
                        {
                            var positive = d.boxBlendDistancePositive;
                            var negative = d.boxBlendDistanceNegative;
                            DrawBoxFadeHandle(
                                s, d, o, sourceAsset,
                                s1 => s1.boxInfluenceHandle,
                                d.offset, d.boxSize,
                                ref positive,
                                ref negative);
                            s.data.boxBlendDistancePositive.vector3Value = positive;
                            s.data.boxBlendDistanceNegative.vector3Value = negative;

                            //save advanced/simplified saved data
                            if (s.data.editorAdvancedModeEnabled.boolValue)
                            {
                                s.data.editorAdvancedModeBlendDistancePositive.vector3Value = positive;
                                s.data.editorAdvancedModeBlendDistanceNegative.vector3Value = negative;
                            }
                            else
                            {
                                s.data.editorSimplifiedModeBlendDistance.floatValue = positive.x;
                            }
                            s.data.Apply();
                            break;
                        }
                    case InfluenceShape.Sphere:
                        {
                            var fade = d.sphereBlendDistance;
                            DrawSphereFadeHandle(
                                s, d, o, sourceAsset,
                                s1 => s1.sphereInfluenceHandle,
                                d.offset, d.sphereRadius,
                                ref fade);
                            d.sphereBlendDistance = fade;
                            break;
                        }
                }
            }
        }

        public static void DrawHandles_EditInfluenceNormal(InfluenceVolumeUI s, InfluenceVolume d, Editor o, Matrix4x4 matrix, Object sourceAsset)
        {
            var mat = Handles.matrix;
            var c = Handles.color;
            Handles.matrix = matrix;
            Handles.color = k_GizmoThemeColorInfluenceNormal;
            switch (d.shape)
            {
                case InfluenceShape.Box:
                    {

                        Vector3 positive = d.boxBlendNormalDistancePositive;
                        Vector3 negative = d.boxBlendNormalDistanceNegative;
                        DrawBoxFadeHandle(
                            s, d, o, sourceAsset,
                            s1 => s1.boxInfluenceNormalHandle,
                            d.offset, d.boxSize,
                            ref positive,
                            ref negative);
                        s.data.boxBlendNormalDistancePositive.vector3Value = positive;
                        s.data.boxBlendNormalDistanceNegative.vector3Value = negative;

                        //save advanced/simplified saved data
                        if (s.data.editorAdvancedModeEnabled.boolValue)
                        {
                            s.data.editorAdvancedModeBlendNormalDistancePositive.vector3Value = positive;
                            s.data.editorAdvancedModeBlendNormalDistanceNegative.vector3Value = negative;
                        }
                        else
                        {
                            s.data.editorSimplifiedModeBlendNormalDistance.floatValue = positive.x;
                        }
                        s.data.Apply();
                        break;
                    }
                case InfluenceShape.Sphere:
                    {
                        var fade = d.sphereBlendNormalDistance;
                        DrawSphereFadeHandle(
                            s, d, o, sourceAsset,
                            s1 => s1.sphereInfluenceNormalHandle,
                            d.offset, d.sphereRadius,
                            ref fade);
                        d.sphereBlendNormalDistance = fade;
                        break;
                    }
            }
            Handles.matrix = mat;
            Handles.color = c;
        }

        static void DrawBoxHandle(
            InfluenceVolumeUI s, InfluenceVolume d, Editor o, Object sourceAsset,
            Func<InfluenceVolumeUI, Gizmo6FacesBox> boundsGetter,
            ref Vector3 center, ref Vector3 size)
        {
            var b = boundsGetter(s);
            b.center = center;
            b.size = size;

            EditorGUI.BeginChangeCheck();
            b.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sourceAsset, "Modified Base Volume AABB");

                center = b.center;
                size = b.size;


                Vector3 blendPositive = s.data.boxBlendDistancePositive.vector3Value;
                Vector3 blendNegative = s.data.boxBlendDistanceNegative.vector3Value;
                Vector3 blendNormalPositive = s.data.boxBlendNormalDistancePositive.vector3Value;
                Vector3 blendNormalNegative = s.data.boxBlendNormalDistanceNegative.vector3Value;
                for (int i = 0; i < 3; ++i)
                {
                    size[i] = Mathf.Max(0f, size[i]);
                }
                s.data.boxSize.vector3Value = size;
                Vector3 halfSize = size * .5f;
                for (int i = 0; i < 3; ++i)
                {
                    blendPositive[i] = Mathf.Clamp(blendPositive[i], 0f, halfSize[i]);
                    blendNegative[i] = Mathf.Clamp(blendNegative[i], 0f, halfSize[i]);
                    blendNormalPositive[i] = Mathf.Clamp(blendNormalPositive[i], 0f, halfSize[i]);
                    blendNormalNegative[i] = Mathf.Clamp(blendNormalNegative[i], 0f, halfSize[i]);
                }
                s.data.boxBlendDistancePositive.vector3Value = blendPositive;
                s.data.boxBlendDistanceNegative.vector3Value = blendNegative;
                s.data.boxBlendNormalDistancePositive.vector3Value = blendNormalPositive;
                s.data.boxBlendNormalDistanceNegative.vector3Value = blendNormalNegative;

                if (s.data.editorAdvancedModeEnabled.boolValue)
                {
                    s.data.editorAdvancedModeBlendDistancePositive.vector3Value = s.data.boxBlendDistancePositive.vector3Value;
                    s.data.editorAdvancedModeBlendDistanceNegative.vector3Value = s.data.boxBlendDistanceNegative.vector3Value;
                    s.data.editorAdvancedModeBlendNormalDistancePositive.vector3Value = s.data.boxBlendNormalDistancePositive.vector3Value;
                    s.data.editorAdvancedModeBlendNormalDistanceNegative.vector3Value = s.data.boxBlendNormalDistanceNegative.vector3Value;
                }
                else
                {
                    s.data.editorSimplifiedModeBlendDistance.floatValue = Mathf.Min(
                        s.data.boxBlendDistancePositive.vector3Value.x,
                        s.data.boxBlendDistancePositive.vector3Value.y,
                        s.data.boxBlendDistancePositive.vector3Value.z,
                        s.data.boxBlendDistanceNegative.vector3Value.x,
                        s.data.boxBlendDistanceNegative.vector3Value.y,
                        s.data.boxBlendDistanceNegative.vector3Value.z);
                    s.data.boxBlendDistancePositive.vector3Value = s.data.boxBlendDistanceNegative.vector3Value = Vector3.one * s.data.editorSimplifiedModeBlendDistance.floatValue;
                    s.data.editorSimplifiedModeBlendNormalDistance.floatValue = Mathf.Min(
                        s.data.boxBlendNormalDistancePositive.vector3Value.x,
                        s.data.boxBlendNormalDistancePositive.vector3Value.y,
                        s.data.boxBlendNormalDistancePositive.vector3Value.z,
                        s.data.boxBlendNormalDistanceNegative.vector3Value.x,
                        s.data.boxBlendNormalDistanceNegative.vector3Value.y,
                        s.data.boxBlendNormalDistanceNegative.vector3Value.z);
                    s.data.boxBlendNormalDistancePositive.vector3Value = s.data.boxBlendNormalDistanceNegative.vector3Value = Vector3.one * s.data.editorSimplifiedModeBlendNormalDistance.floatValue;
                }

                s.data.Apply();

                EditorUtility.SetDirty(sourceAsset);
            }
        }

        static void DrawBoxFadeHandle(
            InfluenceVolumeUI s, InfluenceVolume d, Editor o, Object sourceAsset,
            Func<InfluenceVolumeUI, Gizmo6FacesBox> boundsGetter,
            Vector3 baseOffset, Vector3 baseSize,
            ref Vector3 positive, ref Vector3 negative)
        {
            var b = boundsGetter(s);

            b.center = baseOffset - (positive - negative) * 0.5f;
            b.size = baseSize - positive - negative;
            b.allHandleControledByOne = !s.data.editorAdvancedModeEnabled.boolValue;

            EditorGUI.BeginChangeCheck();
            b.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sourceAsset, "Modified Influence Volume");

                var influenceCenter = baseOffset;
                var halfInfluenceSize = baseSize * .5f;

                var centerDiff = b.center - influenceCenter;
                var halfSizeDiff = halfInfluenceSize - b.size * .5f;
                var positiveNew = halfSizeDiff - centerDiff;
                var negativeNew = halfSizeDiff + centerDiff;
                var blendDistancePositive = Vector3.Max(Vector3.zero, Vector3.Min(positiveNew, halfInfluenceSize));
                var blendDistanceNegative = Vector3.Max(Vector3.zero, Vector3.Min(negativeNew, halfInfluenceSize));

                positive = blendDistancePositive;
                negative = blendDistanceNegative;

                EditorUtility.SetDirty(sourceAsset);
            }
        }

        static void DrawSphereHandle(
            InfluenceVolumeUI s, InfluenceVolume d, Editor o, Object sourceAsset,
            Func<InfluenceVolumeUI, SphereBoundsHandle> boundsGetter,
            ref Vector3 center, ref float radius)
        {
            var b = boundsGetter(s);
            b.center = center;
            b.radius = radius;

            EditorGUI.BeginChangeCheck();
            b.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sourceAsset, "Modified Base Volume AABB");

                radius = b.radius;
                s.data.sphereBlendDistance.floatValue = Mathf.Clamp(s.data.sphereBlendDistance.floatValue, 0, radius);
                s.data.sphereBlendNormalDistance.floatValue = Mathf.Clamp(s.data.sphereBlendNormalDistance.floatValue, 0, radius);
                s.data.Apply();

                EditorUtility.SetDirty(sourceAsset);
            }
        }

        static void DrawSphereFadeHandle(
            InfluenceVolumeUI s, InfluenceVolume d, Editor o, Object sourceAsset,
            Func<InfluenceVolumeUI, SphereBoundsHandle> boundsGetter,
            Vector3 baseOffset, float baseRadius,
            ref float radius)
        {
            var b = boundsGetter(s);
            b.center = baseOffset;
            b.radius = Mathf.Clamp(baseRadius - radius, 0, baseRadius);

            EditorGUI.BeginChangeCheck();
            b.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sourceAsset, "Modified Influence volume");

                radius = Mathf.Clamp(baseRadius - b.radius, 0, baseRadius);

                EditorUtility.SetDirty(sourceAsset);
            }
        }
    }
}
