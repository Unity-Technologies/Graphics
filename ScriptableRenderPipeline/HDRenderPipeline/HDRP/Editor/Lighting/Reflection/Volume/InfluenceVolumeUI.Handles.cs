using System;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using Object = UnityEngine.Object;

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
            switch (d.shapeType)
            {
                case ShapeType.Box:
                {
                    var center = d.boxBaseOffset;
                    var size = d.boxBaseSize;
                    DrawBoxHandle(
                        s, d, o, sourceAsset,
                        s1 => s1.boxBaseHandle,
                        ref center,
                        ref size);
                    d.boxBaseOffset = center;
                    d.boxBaseSize = size;
                        break;
                }
                case ShapeType.Sphere:
                {
                    var center = d.sphereBaseOffset;
                    var radius = d.sphereBaseRadius;
                    DrawSphereHandle(
                        s, d, o, sourceAsset,
                        s1 => s1.sphereBaseHandle,
                        ref center,
                        ref radius);
                    d.sphereBaseOffset = center;
                    d.sphereBaseRadius = radius;
                        break;
                }
            }
            Handles.matrix = mat;
            Handles.color = c;
        }

        public static void DrawHandles_EditInfluence(InfluenceVolumeUI s, InfluenceVolume d, Editor o, Matrix4x4 matrix, Object sourceAsset)
        {
            var mat = Handles.matrix;
            var c = Handles.color;
            Handles.matrix = matrix;
            Handles.color = k_GizmoThemeColorInfluence;
            switch (d.shapeType)
            {
                case ShapeType.Box:
                {
                    var positive = d.boxInfluencePositiveFade;
                    var negative = d.boxInfluenceNegativeFade;
                    DrawBoxFadeHandle(
                        s, d, o, sourceAsset,
                        s1 => s1.boxInfluenceHandle,
                        d.boxBaseOffset, d.boxBaseSize,
                        ref positive,
                        ref negative);
                    d.boxInfluencePositiveFade = positive;
                    d.boxInfluenceNegativeFade = negative;
                    break;
                }
                case ShapeType.Sphere:
                {
                    var fade = d.sphereInfluenceFade;
                    DrawSphereFadeHandle(
                        s, d, o, sourceAsset,
                        s1 => s1.sphereInfluenceHandle,
                        d.sphereBaseOffset, d.sphereBaseRadius,
                        ref fade);
                    d.sphereInfluenceFade = fade;
                    break;
                }
            }
            Handles.matrix = mat;
            Handles.color = c;
        }

        public static void DrawHandles_EditInfluenceNormal(InfluenceVolumeUI s, InfluenceVolume d, Editor o, Matrix4x4 matrix, Object sourceAsset)
        {
            var mat = Handles.matrix;
            var c = Handles.color;
            Handles.matrix = matrix;
            Handles.color = k_GizmoThemeColorInfluenceNormal;
            switch (d.shapeType)
            {
                case ShapeType.Box:
                {
                    var positive = d.boxInfluenceNormalPositiveFade;
                    var negative = d.boxInfluenceNormalNegativeFade;
                    DrawBoxFadeHandle(
                        s, d, o, sourceAsset,
                        s1 => s1.boxInfluenceNormalHandle,
                        d.boxBaseOffset, d.boxBaseSize,
                        ref positive,
                        ref negative);
                    d.boxInfluenceNormalPositiveFade = positive;
                    d.boxInfluenceNormalNegativeFade = negative;
                    break;
                }
                case ShapeType.Sphere:
                {
                    var fade = d.sphereInfluenceNormalFade;
                    DrawSphereFadeHandle(
                        s, d, o, sourceAsset,
                        s1 => s1.sphereInfluenceNormalHandle,
                        d.sphereBaseOffset, d.sphereBaseRadius,
                        ref fade);
                    d.sphereInfluenceNormalFade = fade;
                    break;
                }
            }
            Handles.matrix = mat;
            Handles.color = c;
        }

        static void DrawBoxHandle(
            InfluenceVolumeUI s, InfluenceVolume d, Editor o, Object sourceAsset,
            Func<InfluenceVolumeUI, BoxBoundsHandle> boundsGetter,
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

                EditorUtility.SetDirty(sourceAsset);
            }
        }

        static void DrawBoxFadeHandle(
            InfluenceVolumeUI s, InfluenceVolume d, Editor o, Object sourceAsset,
            Func<InfluenceVolumeUI, BoxBoundsHandle> boundsGetter,
            Vector3 baseOffset, Vector3 baseSize,
            ref Vector3 positive, ref Vector3 negative)
        {
            var b = boundsGetter(s);

            b.center = baseOffset - (positive - negative) * 0.5f;
            b.size = baseSize - positive - negative;

            EditorGUI.BeginChangeCheck();
            b.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sourceAsset, "Modified Influence Volume");

                var center = baseOffset;
                var influenceSize = baseSize;

                var diff = 2 * (b.center - center);
                var sum = influenceSize - b.size;
                var positiveNew = (sum - diff) * 0.5f;
                var negativeNew = (sum + diff) * 0.5f;
                var blendDistancePositive = Vector3.Max(Vector3.zero, Vector3.Min(positiveNew, influenceSize));
                var blendDistanceNegative = Vector3.Max(Vector3.zero, Vector3.Min(negativeNew, influenceSize));

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
