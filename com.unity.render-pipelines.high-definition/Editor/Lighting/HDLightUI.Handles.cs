using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Rendering;
using System;

namespace UnityEditor.Rendering.HighDefinition
{
    partial class HDLightUI
    {
        #region HDRPOnlyPreviouslyInCoreThatNeedRewrite
        // All this region was in CoreLightEditorUtilities
        // We must change the light gizmo to matches Universal ones
        // This was public API but we do not want it public as it need to be rewritten

        static Color GetLightHandleColor(Color wireframeColor)
        {
            Color color = wireframeColor;
            color.a = Mathf.Clamp01(color.a * 2);
            return (QualitySettings.activeColorSpace == ColorSpace.Linear) ? color.linear : color;
        }

        //copy of CoreLightEditorUtilities
        static Color GetLightBehindObjectWireframeColor(Color wireframeColor)
        {
            Color color = wireframeColor;
            color.a = 0.2f;
            return RemapLightColor(UnityEngine.Rendering.CoreUtils.ConvertLinearToActiveColorSpace(color.linear));
        }

        //copy of CoreLightEditorUtilities

        static Color RemapLightColor(Color src)
        {
            Color color = src;
            float max = Mathf.Max(Mathf.Max(color.r, color.g), color.b);
            if (max > 0f)
            {
                float mult = 1f / max;
                color.r *= mult;
                color.g *= mult;
                color.b *= mult;
            }
            else
            {
                color = Color.white;
            }

            return color;
        }

        //copy of CoreLightEditorUtilities
        static void DrawHandleLabel(Vector3 handlePosition, string labelText, float offsetFromHandle = 0.3f)
        {
            Vector3 labelPosition = Vector3.zero;

            var style = new GUIStyle { normal = { background = Texture2D.whiteTexture } };
            GUI.color = new Color(0.82f, 0.82f, 0.82f, 1);

            labelPosition = handlePosition + Handles.inverseMatrix.MultiplyVector(Vector3.up) * HandleUtility.GetHandleSize(handlePosition) * offsetFromHandle;
            Handles.Label(labelPosition, labelText, style);
        }

        //copy of CoreLightEditorUtilities
        static float SliderLineHandle(Vector3 position, Vector3 direction, float value)
        {
            return SliderLineHandle(GUIUtility.GetControlID(FocusType.Passive), position, direction, value, "");
        }

        //copy of CoreLightEditorUtilities
        static float SliderLineHandle(int id, Vector3 position, Vector3 direction, float value, string labelText = "")
        {
            Vector3 pos = position + direction * value;
            float sizeHandle = HandleUtility.GetHandleSize(pos);
            bool temp = GUI.changed;
            GUI.changed = false;
            pos = Handles.Slider(id, pos, direction, sizeHandle * 0.03f, Handles.DotHandleCap, 0f);
            if (GUI.changed)
            {
                value = Vector3.Dot(pos - position, direction);
            }
            GUI.changed |= temp;

            if (GUIUtility.hotControl == id && !String.IsNullOrEmpty(labelText))
            {
                labelText += FormattableString.Invariant($"{value:0.00}");
                DrawHandleLabel(pos, labelText);
            }

            return value;
        }

        //copy of CoreLightEditorUtilities
        static Vector2 SliderPlaneHandle(Vector3 origin, Vector3 axis1, Vector3 axis2, Vector2 position)
        {
            Vector3 pos = origin + position.x * axis1 + position.y * axis2;
            float sizeHandle = HandleUtility.GetHandleSize(pos);
            bool temp = GUI.changed;
            GUI.changed = false;
            pos = Handles.Slider2D(pos, Vector3.forward, axis1, axis2, sizeHandle * 0.03f, Handles.DotHandleCap, 0f);
            if (GUI.changed)
            {
                position = new Vector2(Vector3.Dot(pos, axis1), Vector3.Dot(pos, axis2));
            }
            GUI.changed |= temp;
            return position;
        }

        //copy of CoreLightEditorUtilities
        static float SizeSliderSpotAngle(Vector3 position, Vector3 forward, Vector3 axis, float range, float spotAngle, string controlName)
        {
            if (Mathf.Abs(spotAngle) <= 0.05f)
                return spotAngle;
            var angledForward = Quaternion.AngleAxis(Mathf.Max(spotAngle, 0.05f) * 0.5f, axis) * forward;
            var centerToLeftOnSphere = (angledForward * range + position) - (position + forward * range);
            bool temp = GUI.changed;
            GUI.changed = false;
            var handlePosition = position + forward * range;
            var id = GUIUtility.GetControlID(FocusType.Passive);
            var newMagnitude = Mathf.Max(0f, SliderLineHandle(id, handlePosition, centerToLeftOnSphere.normalized, centerToLeftOnSphere.magnitude));
            if (GUI.changed)
            {
                centerToLeftOnSphere = centerToLeftOnSphere.normalized * newMagnitude;
                angledForward = (centerToLeftOnSphere + (position + forward * range) - position).normalized;
                spotAngle = Mathf.Clamp(Mathf.Acos(Vector3.Dot(forward, angledForward)) * Mathf.Rad2Deg * 2, 0f, 179f);
                if (spotAngle <= 0.05f || float.IsNaN(spotAngle))
                    spotAngle = 0f;
            }
            GUI.changed |= temp;

            if (GUIUtility.hotControl == id)
            {
                var pos = handlePosition + centerToLeftOnSphere.normalized * newMagnitude;
                string labelText = FormattableString.Invariant($"{controlName} {spotAngle:0.00}");
                DrawHandleLabel(pos, labelText);
            }

            return spotAngle;
        }

        //TODO: decompose arguments (or tuples) + put back to CoreLightEditorUtilities
        static Vector3 DrawSpotlightHandle(Vector3 outerAngleInnerAngleRange)
        {
            float outerAngle = outerAngleInnerAngleRange.x;
            float innerAngle = outerAngleInnerAngleRange.y;
            float range = outerAngleInnerAngleRange.z;

            if (innerAngle > 0f)
            {
                innerAngle = SizeSliderSpotAngle(Vector3.zero, Vector3.forward, Vector3.right, range, innerAngle, String.Empty);
                innerAngle = SizeSliderSpotAngle(Vector3.zero, Vector3.forward, Vector3.left, range, innerAngle, String.Empty);
                innerAngle = SizeSliderSpotAngle(Vector3.zero, Vector3.forward, Vector3.up, range, innerAngle, String.Empty);
                innerAngle = SizeSliderSpotAngle(Vector3.zero, Vector3.forward, Vector3.down, range, innerAngle, String.Empty);
            }

            outerAngle = SizeSliderSpotAngle(Vector3.zero, Vector3.forward, Vector3.right, range, outerAngle, String.Empty);
            outerAngle = SizeSliderSpotAngle(Vector3.zero, Vector3.forward, Vector3.left, range, outerAngle, String.Empty);
            outerAngle = SizeSliderSpotAngle(Vector3.zero, Vector3.forward, Vector3.up, range, outerAngle, String.Empty);
            outerAngle = SizeSliderSpotAngle(Vector3.zero, Vector3.forward, Vector3.down, range, outerAngle, String.Empty);

            range = SliderLineHandle(Vector3.zero, Vector3.forward, range);

            return new Vector3(outerAngle, innerAngle, range);
        }

        //TODO: decompose arguments (or tuples) + put back to CoreLightEditorUtilities
        static void DrawSpotlightWireframe(Vector3 outerAngleInnerAngleRange, float shadowPlaneDistance = -1f)
        {
            float outerAngle = outerAngleInnerAngleRange.x;
            float innerAngle = outerAngleInnerAngleRange.y;
            float range = outerAngleInnerAngleRange.z;


            var outerDiscRadius = range * Mathf.Sin(outerAngle * Mathf.Deg2Rad * 0.5f);
            var outerDiscDistance = Mathf.Cos(Mathf.Deg2Rad * outerAngle * 0.5f) * range;
            var vectorLineUp = Vector3.Normalize(Vector3.forward * outerDiscDistance + Vector3.up * outerDiscRadius);
            var vectorLineLeft = Vector3.Normalize(Vector3.forward * outerDiscDistance + Vector3.left * outerDiscRadius);

            if (innerAngle > 0f)
            {
                var innerDiscRadius = range * Mathf.Sin(innerAngle * Mathf.Deg2Rad * 0.5f);
                var innerDiscDistance = Mathf.Cos(Mathf.Deg2Rad * innerAngle * 0.5f) * range;
                DrawConeWireframe(innerDiscRadius, innerDiscDistance);
            }
            DrawConeWireframe(outerDiscRadius, outerDiscDistance);
            Handles.DrawWireArc(Vector3.zero, Vector3.right, vectorLineUp, outerAngle, range);
            Handles.DrawWireArc(Vector3.zero, Vector3.up, vectorLineLeft, outerAngle, range);

            if (shadowPlaneDistance > 0)
            {
                var shadowDiscRadius = shadowPlaneDistance * Mathf.Sin(outerAngle * Mathf.Deg2Rad * 0.5f);
                var shadowDiscDistance = Mathf.Cos(Mathf.Deg2Rad * outerAngle / 2) * shadowPlaneDistance;
                Handles.DrawWireDisc(Vector3.forward * shadowDiscDistance, Vector3.forward, shadowDiscRadius);
            }
        }
        
        static void DrawConeWireframe(float radius, float height)
        {
            var rangeCenter = Vector3.forward * height;
            var rangeUp = rangeCenter + Vector3.up * radius;
            var rangeDown = rangeCenter - Vector3.up * radius;
            var rangeRight = rangeCenter + Vector3.right * radius;
            var rangeLeft = rangeCenter - Vector3.right * radius;

            //Draw Lines
            Handles.DrawLine(Vector3.zero, rangeUp);
            Handles.DrawLine(Vector3.zero, rangeDown);
            Handles.DrawLine(Vector3.zero, rangeRight);
            Handles.DrawLine(Vector3.zero, rangeLeft);

            Handles.DrawWireDisc(Vector3.forward * height, Vector3.forward, radius);
        }

        //TODO: decompose arguments (or tuples) + put back to CoreLightEditorUtilities
        static void DrawAreaLightWireframe(Vector2 rectangleSize)
        {
            Handles.DrawWireCube(Vector3.zero, rectangleSize);
        }

        //TODO: decompose arguments (or tuples) + put back to CoreLightEditorUtilities
        static Vector2 DrawAreaLightHandle(Vector2 rectangleSize, bool withYAxis)
        {
            float halfWidth = rectangleSize.x * 0.5f;
            float halfHeight = rectangleSize.y * 0.5f;

            EditorGUI.BeginChangeCheck();
            halfWidth = SliderLineHandle(Vector3.zero, Vector3.right, halfWidth);
            halfWidth = SliderLineHandle(Vector3.zero, Vector3.left, halfWidth);
            if (EditorGUI.EndChangeCheck())
            {
                halfWidth = Mathf.Max(0f, halfWidth);
            }

            if (withYAxis)
            {
                EditorGUI.BeginChangeCheck();
                halfHeight = SliderLineHandle(Vector3.zero, Vector3.up, halfHeight);
                halfHeight = SliderLineHandle(Vector3.zero, Vector3.down, halfHeight);
                if (EditorGUI.EndChangeCheck())
                {
                    halfHeight = Mathf.Max(0f, halfHeight);
                }
            }

            return new Vector2(halfWidth * 2f, halfHeight * 2f);
        }
        
        //copy of CoreLightEditorUtilities
        static Vector3[] GetFrustrumProjectedRectAngles(float distance, float aspect, float tanFOV)
        {
            Vector3 sizeX;
            Vector3 sizeY;
            float minXYTruncSize = distance * tanFOV;
            if (aspect >= 1.0f)
            {
                sizeX = new Vector3(minXYTruncSize * aspect, 0, 0);
                sizeY = new Vector3(0, minXYTruncSize, 0);
            }
            else
            {
                sizeX = new Vector3(minXYTruncSize, 0, 0);
                sizeY = new Vector3(0, minXYTruncSize / aspect, 0);
            }
            Vector3 center = new Vector3(0, 0, distance);
            Vector3[] angles =
            {
                center + sizeX + sizeY,
                center - sizeX + sizeY,
                center - sizeX - sizeY,
                center + sizeX - sizeY
            };

            return angles;
        }

        //TODO: decompose arguments (or tuples) + put back to CoreLightEditorUtilities
        // Same as Gizmo.DrawFrustum except that when aspect is below one, fov represent fovX instead of fovY
        // Use to match our light frustum pyramid behavior
        static void DrawSpherePortionWireframe(Vector4 aspectFovMaxRangeMinRange, float distanceTruncPlane = 0f)
        {
            float aspect = aspectFovMaxRangeMinRange.x;
            float fov = aspectFovMaxRangeMinRange.y;
            float maxRange = aspectFovMaxRangeMinRange.z;
            float minRange = aspectFovMaxRangeMinRange.w;
            float tanfov = Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f);

            var startAngles = new Vector3[4];
            if (minRange > 0f)
            {
                startAngles = GetFrustrumProjectedRectAngles(minRange, aspect, tanfov);
                Handles.DrawLine(startAngles[0], startAngles[1]);
                Handles.DrawLine(startAngles[1], startAngles[2]);
                Handles.DrawLine(startAngles[2], startAngles[3]);
                Handles.DrawLine(startAngles[3], startAngles[0]);
            }

            if (distanceTruncPlane > 0f)
            {
                var truncAngles = GetFrustrumProjectedRectAngles(distanceTruncPlane, aspect, tanfov);
                Handles.DrawLine(truncAngles[0], truncAngles[1]);
                Handles.DrawLine(truncAngles[1], truncAngles[2]);
                Handles.DrawLine(truncAngles[2], truncAngles[3]);
                Handles.DrawLine(truncAngles[3], truncAngles[0]);
            }

            var endAngles = GetSphericalProjectedRectAngles(maxRange, aspect, tanfov);
            var planProjectedCrossNormal0 = new Vector3(endAngles[0].y, -endAngles[0].x, 0).normalized;
            var planProjectedCrossNormal1 = new Vector3(endAngles[1].y, -endAngles[1].x, 0).normalized;
            Vector3[] faceNormals = new[] {
                Vector3.right - Vector3.Dot((endAngles[3] + endAngles[0]).normalized, Vector3.right) * (endAngles[3] + endAngles[0]).normalized,
                Vector3.up    - Vector3.Dot((endAngles[0] + endAngles[1]).normalized, Vector3.up)    * (endAngles[0] + endAngles[1]).normalized,
                Vector3.left  - Vector3.Dot((endAngles[1] + endAngles[2]).normalized, Vector3.left)  * (endAngles[1] + endAngles[2]).normalized,
                Vector3.down  - Vector3.Dot((endAngles[2] + endAngles[3]).normalized, Vector3.down)  * (endAngles[2] + endAngles[3]).normalized,
                //cross
                planProjectedCrossNormal0 - Vector3.Dot((endAngles[1] + endAngles[3]).normalized, planProjectedCrossNormal0)  * (endAngles[1] + endAngles[3]).normalized,
                planProjectedCrossNormal1 - Vector3.Dot((endAngles[0] + endAngles[2]).normalized, planProjectedCrossNormal1)  * (endAngles[0] + endAngles[2]).normalized,
            };

            float[] faceAngles = new[] {
                Vector3.Angle(endAngles[3], endAngles[0]),
                Vector3.Angle(endAngles[0], endAngles[1]),
                Vector3.Angle(endAngles[1], endAngles[2]),
                Vector3.Angle(endAngles[2], endAngles[3]),
                Vector3.Angle(endAngles[1], endAngles[3]),
                Vector3.Angle(endAngles[0], endAngles[2]),
            };

            Handles.DrawWireArc(Vector3.zero, faceNormals[0], endAngles[0], faceAngles[0], maxRange);
            Handles.DrawWireArc(Vector3.zero, faceNormals[1], endAngles[1], faceAngles[1], maxRange);
            Handles.DrawWireArc(Vector3.zero, faceNormals[2], endAngles[2], faceAngles[2], maxRange);
            Handles.DrawWireArc(Vector3.zero, faceNormals[3], endAngles[3], faceAngles[3], maxRange);
            Handles.DrawWireArc(Vector3.zero, faceNormals[4], endAngles[0], faceAngles[4], maxRange);
            Handles.DrawWireArc(Vector3.zero, faceNormals[5], endAngles[1], faceAngles[5], maxRange);

            Handles.DrawLine(startAngles[0], endAngles[0]);
            Handles.DrawLine(startAngles[1], endAngles[1]);
            Handles.DrawLine(startAngles[2], endAngles[2]);
            Handles.DrawLine(startAngles[3], endAngles[3]);
        }
        
        static Vector3[] GetSphericalProjectedRectAngles(float distance, float aspect, float tanFOV)
        {
            var angles = GetFrustrumProjectedRectAngles(distance, aspect, tanFOV);
            for (int index = 0; index < 4; ++index)
                angles[index] = angles[index].normalized * distance;
            return angles;
        }

        //TODO: decompose arguments (or tuples) + put back to CoreLightEditorUtilities
        static Vector4 DrawSpherePortionHandle(Vector4 aspectFovMaxRangeMinRange, bool useNearPlane, float minAspect = 0.05f, float maxAspect = 20f, float minFov = 1f)
        {
            float aspect = aspectFovMaxRangeMinRange.x;
            float fov = aspectFovMaxRangeMinRange.y;
            float maxRange = aspectFovMaxRangeMinRange.z;
            float minRange = aspectFovMaxRangeMinRange.w;
            float tanfov = Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f);

            var endAngles = GetSphericalProjectedRectAngles(maxRange, aspect, tanfov);

            if (useNearPlane)
            {
                minRange = SliderLineHandle(Vector3.zero, Vector3.forward, minRange);
            }

            maxRange = SliderLineHandle(Vector3.zero, Vector3.forward, maxRange);

            float distanceRight = HandleUtility.DistanceToLine(endAngles[0], endAngles[3]);
            float distanceLeft = HandleUtility.DistanceToLine(endAngles[1], endAngles[2]);
            float distanceUp = HandleUtility.DistanceToLine(endAngles[0], endAngles[1]);
            float distanceDown = HandleUtility.DistanceToLine(endAngles[2], endAngles[3]);

            int pointIndex = 0;
            if (distanceRight < distanceLeft)
            {
                if (distanceUp < distanceDown)
                    pointIndex = 0;
                else
                    pointIndex = 3;
            }
            else
            {
                if (distanceUp < distanceDown)
                    pointIndex = 1;
                else
                    pointIndex = 2;
            }

            Vector2 send = endAngles[pointIndex];
            Vector3 farEnd = new Vector3(0, 0, endAngles[0].z);
            EditorGUI.BeginChangeCheck();
            Vector2 received = SliderPlaneHandle(farEnd, Vector3.right, Vector3.up, send);
            if (EditorGUI.EndChangeCheck())
            {
                bool fixedFov = Event.current.control && !Event.current.shift;
                bool fixedAspect = Event.current.shift && !Event.current.control;

                //work on positive quadrant
                int xSign = send.x < 0f ? -1 : 1;
                int ySign = send.y < 0f ? -1 : 1;
                Vector2 corrected = new Vector2(received.x * xSign, received.y * ySign);

                //fixed aspect correction
                if (fixedAspect)
                {
                    corrected.x = corrected.y * aspect;
                }

                //remove aspect deadzone
                if (corrected.x > maxAspect * corrected.y)
                {
                    corrected.y = corrected.x * minAspect;
                }
                if (corrected.x < minAspect * corrected.y)
                {
                    corrected.x = corrected.y / maxAspect;
                }

                //remove fov deadzone
                float deadThresholdFoV = Mathf.Tan(Mathf.Deg2Rad * minFov * 0.5f) * maxRange;
                corrected.x = Mathf.Max(corrected.x, deadThresholdFoV);
                corrected.y = Mathf.Max(corrected.y, deadThresholdFoV, Mathf.Epsilon * 100); //prevent any division by zero

                if (!fixedAspect)
                {
                    aspect = corrected.x / corrected.y;
                }
                float min = Mathf.Min(corrected.x, corrected.y);
                if (!fixedFov && maxRange > Mathf.Epsilon * 100)
                {
                    fov = Mathf.Atan(min / maxRange) * 2f * Mathf.Rad2Deg;
                }
            }

            return new Vector4(aspect, fov, maxRange, minRange);
        }

        //TODO: decompose arguments (or tuples) + put back to CoreLightEditorUtilities
        static void DrawOrthoFrustumWireframe(Vector4 widthHeightMaxRangeMinRange, float distanceTruncPlane = 0f)
        {
            float halfWidth = widthHeightMaxRangeMinRange.x * 0.5f;
            float halfHeight = widthHeightMaxRangeMinRange.y * 0.5f;
            float maxRange = widthHeightMaxRangeMinRange.z;
            float minRange = widthHeightMaxRangeMinRange.w;

            Vector3 sizeX = new Vector3(halfWidth, 0, 0);
            Vector3 sizeY = new Vector3(0, halfHeight, 0);
            Vector3 nearEnd = new Vector3(0, 0, minRange);
            Vector3 farEnd = new Vector3(0, 0, maxRange);

            Vector3 s1 = nearEnd + sizeX + sizeY;
            Vector3 s2 = nearEnd - sizeX + sizeY;
            Vector3 s3 = nearEnd - sizeX - sizeY;
            Vector3 s4 = nearEnd + sizeX - sizeY;

            Vector3 e1 = farEnd + sizeX + sizeY;
            Vector3 e2 = farEnd - sizeX + sizeY;
            Vector3 e3 = farEnd - sizeX - sizeY;
            Vector3 e4 = farEnd + sizeX - sizeY;

            Handles.DrawLine(s1, s2);
            Handles.DrawLine(s2, s3);
            Handles.DrawLine(s3, s4);
            Handles.DrawLine(s4, s1);

            Handles.DrawLine(e1, e2);
            Handles.DrawLine(e2, e3);
            Handles.DrawLine(e3, e4);
            Handles.DrawLine(e4, e1);

            Handles.DrawLine(s1, e1);
            Handles.DrawLine(s2, e2);
            Handles.DrawLine(s3, e3);
            Handles.DrawLine(s4, e4);

            if (distanceTruncPlane > 0f)
            {
                Vector3 truncPoint = new Vector3(0, 0, distanceTruncPlane);
                Vector3 t1 = truncPoint + sizeX + sizeY;
                Vector3 t2 = truncPoint - sizeX + sizeY;
                Vector3 t3 = truncPoint - sizeX - sizeY;
                Vector3 t4 = truncPoint + sizeX - sizeY;

                Handles.DrawLine(t1, t2);
                Handles.DrawLine(t2, t3);
                Handles.DrawLine(t3, t4);
                Handles.DrawLine(t4, t1);
            }
        }

        //TODO: decompose arguments (or tuples) + put back to CoreLightEditorUtilities
        static Vector4 DrawOrthoFrustumHandle(Vector4 widthHeightMaxRangeMinRange, bool useNearHandle)
        {
            float halfWidth = widthHeightMaxRangeMinRange.x * 0.5f;
            float halfHeight = widthHeightMaxRangeMinRange.y * 0.5f;
            float maxRange = widthHeightMaxRangeMinRange.z;
            float minRange = widthHeightMaxRangeMinRange.w;
            Vector3 farEnd = new Vector3(0, 0, maxRange);

            if (useNearHandle)
            {
                minRange = SliderLineHandle(Vector3.zero, Vector3.forward, minRange);
            }

            maxRange = SliderLineHandle(Vector3.zero, Vector3.forward, maxRange);

            EditorGUI.BeginChangeCheck();
            halfWidth = SliderLineHandle(farEnd, Vector3.right, halfWidth);
            halfWidth = SliderLineHandle(farEnd, Vector3.left, halfWidth);
            if (EditorGUI.EndChangeCheck())
            {
                halfWidth = Mathf.Max(0f, halfWidth);
            }

            EditorGUI.BeginChangeCheck();
            halfHeight = SliderLineHandle(farEnd, Vector3.up, halfHeight);
            halfHeight = SliderLineHandle(farEnd, Vector3.down, halfHeight);
            if (EditorGUI.EndChangeCheck())
            {
                halfHeight = Mathf.Max(0f, halfHeight);
            }

            return new Vector4(halfWidth * 2f, halfHeight * 2f, maxRange, minRange);
        }

        #endregion

        public static void DrawHandles(HDAdditionalLightData additionalData, Editor owner)
        {
            Light light = additionalData.legacyLight;

            Color wireframeColorAbove = (owner as HDLightEditor).legacyLightColor;
            Color handleColorAbove = GetLightHandleColor(wireframeColorAbove);
            Color wireframeColorBehind = GetLightBehindObjectWireframeColor(wireframeColorAbove);
            Color handleColorBehind = GetLightHandleColor(wireframeColorBehind);

            switch (additionalData.type)
            {
                case HDLightType.Directional:
                case HDLightType.Point:
                    //use legacy handles for those cases:
                    //See HDLightEditor
                    break;
                case HDLightType.Spot:
                    switch (additionalData.spotLightShape)
                    {
                        case SpotLightShape.Cone:
                            using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, light.transform.rotation, Vector3.one)))
                            {
                                Vector3 outterAngleInnerAngleRange = new Vector3(light.spotAngle, light.spotAngle * additionalData.innerSpotPercent01, light.range);
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                                Handles.color = wireframeColorBehind;
                                DrawSpotlightWireframe(outterAngleInnerAngleRange, additionalData.shadowNearPlane);
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                                Handles.color = wireframeColorAbove;
                                DrawSpotlightWireframe(outterAngleInnerAngleRange, additionalData.shadowNearPlane);
                                EditorGUI.BeginChangeCheck();
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                                Handles.color = handleColorBehind;
                                outterAngleInnerAngleRange = DrawSpotlightHandle(outterAngleInnerAngleRange);
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                                Handles.color = handleColorAbove;
                                outterAngleInnerAngleRange = DrawSpotlightHandle(outterAngleInnerAngleRange);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObjects(new UnityEngine.Object[] { light, additionalData }, "Adjust Cone Spot Light");
                                    additionalData.innerSpotPercent = 100f * outterAngleInnerAngleRange.y / Mathf.Max(0.1f, outterAngleInnerAngleRange.x);
                                    light.spotAngle = outterAngleInnerAngleRange.x;
                                    light.range = outterAngleInnerAngleRange.z;
                                }

                                // Handles.color reseted at end of scope
                            }
                            break;
                        case SpotLightShape.Pyramid:
                            using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, light.transform.rotation, Vector3.one)))
                            {
                                Vector4 aspectFovMaxRangeMinRange = new Vector4(additionalData.aspectRatio, light.spotAngle, light.range);
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                                Handles.color = wireframeColorBehind;
                                DrawSpherePortionWireframe(aspectFovMaxRangeMinRange, additionalData.shadowNearPlane);
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                                Handles.color = wireframeColorAbove;
                                DrawSpherePortionWireframe(aspectFovMaxRangeMinRange, additionalData.shadowNearPlane);
                                EditorGUI.BeginChangeCheck();
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                                Handles.color = handleColorBehind;
                                aspectFovMaxRangeMinRange = DrawSpherePortionHandle(aspectFovMaxRangeMinRange, false);
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                                Handles.color = handleColorAbove;
                                aspectFovMaxRangeMinRange = DrawSpherePortionHandle(aspectFovMaxRangeMinRange, false);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObjects(new UnityEngine.Object[] { light, additionalData }, "Adjust Pyramid Spot Light");
                                    additionalData.aspectRatio = aspectFovMaxRangeMinRange.x;
                                    light.spotAngle = aspectFovMaxRangeMinRange.y;
                                    light.range = aspectFovMaxRangeMinRange.z;
                                }

                                // Handles.color reseted at end of scope
                            }
                            break;
                        case SpotLightShape.Box:
                            using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, light.transform.rotation, Vector3.one)))
                            {
                                Vector4 widthHeightMaxRangeMinRange = new Vector4(additionalData.shapeWidth, additionalData.shapeHeight, light.range);
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                                Handles.color = wireframeColorBehind;
                                DrawOrthoFrustumWireframe(widthHeightMaxRangeMinRange, additionalData.shadowNearPlane);
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                                Handles.color = wireframeColorAbove;
                                DrawOrthoFrustumWireframe(widthHeightMaxRangeMinRange, additionalData.shadowNearPlane);
                                EditorGUI.BeginChangeCheck();
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                                Handles.color = handleColorBehind;
                                widthHeightMaxRangeMinRange = DrawOrthoFrustumHandle(widthHeightMaxRangeMinRange, false);
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                                Handles.color = handleColorAbove;
                                widthHeightMaxRangeMinRange = DrawOrthoFrustumHandle(widthHeightMaxRangeMinRange, false);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObjects(new UnityEngine.Object[] { light, additionalData }, "Adjust Box Spot Light");
                                    additionalData.shapeWidth = widthHeightMaxRangeMinRange.x;
                                    additionalData.shapeHeight = widthHeightMaxRangeMinRange.y;
                                    light.range = widthHeightMaxRangeMinRange.z;
                                }

                                // Handles.color reseted at end of scope
                            }
                            break;
                    }
                    break;
                case HDLightType.Area:
                    switch (additionalData.areaLightShape)
                    {
                        case AreaLightShape.Rectangle:
                        case AreaLightShape.Tube:
                            bool withYAxis = additionalData.areaLightShape == AreaLightShape.Rectangle;
                            using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, light.transform.rotation, Vector3.one)))
                            {
                                Vector2 widthHeight = new Vector4(additionalData.shapeWidth, withYAxis ? additionalData.shapeHeight : 0f);
                                float range = light.range;
                                EditorGUI.BeginChangeCheck();
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                                Handles.color = wireframeColorBehind;
                                DrawAreaLightWireframe(widthHeight);
                                range = Handles.RadiusHandle(Quaternion.identity, Vector3.zero, range); //also draw handles
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                                Handles.color = wireframeColorAbove;
                                DrawAreaLightWireframe(widthHeight);
                                range = Handles.RadiusHandle(Quaternion.identity, Vector3.zero, range); //also draw handles
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                                Handles.color = handleColorBehind;
                                widthHeight = DrawAreaLightHandle(widthHeight, withYAxis);
                                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                                Handles.color = handleColorAbove;
                                widthHeight = DrawAreaLightHandle(widthHeight, withYAxis);
                                widthHeight = Vector2.Max(Vector2.one * k_MinLightSize, widthHeight);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObjects(new UnityEngine.Object[] { light, additionalData }, withYAxis ? "Adjust Area Rectangle Light" : "Adjust Area Tube Light");
                                    additionalData.shapeWidth = widthHeight.x;
                                    if (withYAxis)
                                    {
                                        additionalData.shapeHeight = widthHeight.y;
                                    }
                                    light.range = range;
                                }

                                // Handles.color reseted at end of scope
                            }
                            break;
                        case AreaLightShape.Disc:
                            //use legacy handles for this case
                            break;
                    }
                    break;
            }
        }

        [DrawGizmo(GizmoType.Selected)]
        static void DrawGizmoForHDAdditionalLightData(HDAdditionalLightData src, GizmoType gizmoType)
        {
            if (!(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset))
                return;

            if (src.type != HDLightType.Directional)
            {
                // Trace a ray down to better locate the light location
                Ray ray = new Ray(src.gameObject.transform.position, Vector3.down);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                    using (new Handles.DrawingScope(Color.green))
                    {
                        Handles.DrawLine(src.gameObject.transform.position, hit.point);
                        Handles.DrawWireDisc(hit.point, hit.normal, 0.5f);
                    }

                    Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                    using (new Handles.DrawingScope(Color.red))
                    {
                        Handles.DrawLine(src.gameObject.transform.position, hit.point);
                        Handles.DrawWireDisc(hit.point, hit.normal, 0.5f);
                    }
                }

                if ((ShaderConfig.s_BarnDoor == 1) && (src.type == HDLightType.Area && src.barnDoorAngle < 89.0f))
                {
                    // Convert the angle to randians
                    float angle = src.barnDoorAngle * Mathf.PI / 180.0f;

                    // Compute the depth of the pyramid
                    float depth = src.barnDoorLength * Mathf.Cos(angle);

                    // Evaluate the half dimensions of the rectangular area light
                    float halfWidth = src.shapeWidth * 0.5f;
                    float halfHeight = src.shapeHeight * 0.5f;
                    
                    // Evaluate the dimensions of the extended area light
                    float extendedWidth = Mathf.Tan(angle) * depth + halfWidth;
                    float extendedHeight = Mathf.Tan(angle) * depth + halfHeight;

                    // Compute all the points of the pyramid
                    Vector3 pos00 = src.transform.position + halfWidth * src.transform.right + halfHeight * src.transform.up;
                    Vector3 pos10 = src.transform.position - halfWidth * src.transform.right + halfHeight * src.transform.up;
                    Vector3 pos20 = src.transform.position - halfWidth * src.transform.right - halfHeight * src.transform.up;
                    Vector3 pos30 = src.transform.position + halfWidth * src.transform.right - halfHeight * src.transform.up;
                    Vector3 pos01 = src.transform.position + src.transform.forward * depth + src.transform.right * extendedWidth + src.transform.up * extendedHeight;
                    Vector3 pos11 = src.transform.position + src.transform.forward * depth - src.transform.right * extendedWidth + src.transform.up * extendedHeight;
                    Vector3 pos21 = src.transform.position + src.transform.forward * depth - src.transform.right * extendedWidth - src.transform.up * extendedHeight;
                    Vector3 pos31 = src.transform.position + src.transform.forward * depth + src.transform.right * extendedWidth - src.transform.up * extendedHeight;

                    // Draw the pyramid
                    Debug.DrawLine(pos00, pos01, Color.yellow);
                    Debug.DrawLine(pos10, pos11, Color.yellow);
                    Debug.DrawLine(pos20, pos21, Color.yellow);
                    Debug.DrawLine(pos30, pos31, Color.yellow);
                    Debug.DrawLine(pos01, pos11, Color.yellow);
                    Debug.DrawLine(pos11, pos21, Color.yellow);
                    Debug.DrawLine(pos21, pos31, Color.yellow);
                    Debug.DrawLine(pos31, pos01, Color.yellow);
                }
            }
        }
    }
}
