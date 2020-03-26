using System.Collections.Generic;
using System.Linq;
using UnityEditor.EditorTools;
using UnityEditor.Experimental.Rendering.Universal.Path2D;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Experimental.Rendering.Universal
{
    [CustomEditor(typeof(Light2D))]
    [CanEditMultipleObjects]
    internal class Light2DEditor : PathComponentEditor<ScriptablePath>
    {
        [EditorTool("Edit Freeform Shape", typeof(Light2D))]
        class FreeformShapeTool : PathEditorTool<ScriptablePath>
        {
            const string k_ShapePath = "m_ShapePath";

            public override bool IsAvailable()
            {
                var light = target as Light2D;

                if (light == null)
                    return false;
                else
                    return base.IsAvailable() && light.lightType == Light2D.LightType.Freeform;
            }

            protected override IShape GetShape(Object target)
            {
                return (target as Light2D).shapePath.ToPolygon(false);
            }

            protected override void SetShape(ScriptablePath shapeEditor, SerializedObject serializedObject)
            {
                serializedObject.Update();

                var pointsProperty = serializedObject.FindProperty(k_ShapePath);
                pointsProperty.arraySize = shapeEditor.pointCount;

                for (var i = 0; i < shapeEditor.pointCount; ++i)
                    pointsProperty.GetArrayElementAtIndex(i).vector3Value = shapeEditor.GetPoint(i).position;

                // This is untracked right now...
                serializedObject.ApplyModifiedProperties();
            }
        }

        private static class Styles
        {
            public static Texture lightCapTopRight = Resources.Load<Texture>("LightCapTopRight");
            public static Texture lightCapTopLeft = Resources.Load<Texture>("LightCapTopLeft");
            public static Texture lightCapBottomLeft = Resources.Load<Texture>("LightCapBottomLeft");
            public static Texture lightCapBottomRight = Resources.Load<Texture>("LightCapBottomRight");
            public static Texture lightCapUp = Resources.Load<Texture>("LightCapUp");
            public static Texture lightCapDown = Resources.Load<Texture>("LightCapDown");

            public static GUIContent lightTypeParametric = new GUIContent("Parametric", Resources.Load("InspectorIcons/ParametricLight") as Texture);
            public static GUIContent lightTypeFreeform = new GUIContent("Freeform", Resources.Load("InspectorIcons/FreeformLight") as Texture);
            public static GUIContent lightTypeSprite = new GUIContent("Sprite", Resources.Load("InspectorIcons/SpriteLight") as Texture);
            public static GUIContent lightTypePoint = new GUIContent("Point", Resources.Load("InspectorIcons/PointLight") as Texture);
            public static GUIContent lightTypeGlobal = new GUIContent("Global", Resources.Load("InspectorIcons/GlobalLight") as Texture);
            public static GUIContent[] lightTypeOptions = new GUIContent[] { lightTypeParametric, lightTypeFreeform, lightTypeSprite, lightTypePoint, lightTypeGlobal };

            public static GUIContent generalLightType = EditorGUIUtility.TrTextContent("Light Type", "Specify the light type");
            public static GUIContent generalFalloffSize = EditorGUIUtility.TrTextContent("Falloff", "Specify the falloff of the light");
            public static GUIContent generalFalloffIntensity = EditorGUIUtility.TrTextContent("Falloff Intensity", "Adjusts the falloff curve");
            public static GUIContent generalLightColor = EditorGUIUtility.TrTextContent("Color", "Specify the light color");
            public static GUIContent generalLightIntensity = EditorGUIUtility.TrTextContent("Intensity", "Specify the light color's intensity");
            public static GUIContent generalUseNormalMap = EditorGUIUtility.TrTextContent("Use Normal Map", "Specify whether the light considers normal maps");
            public static GUIContent generalVolumeOpacity = EditorGUIUtility.TrTextContent("Volume Opacity", "Specify the light's volumetric light volume opacity");
            public static GUIContent generalBlendStyle = EditorGUIUtility.TrTextContent("Blend Style", "Specify the blend style");
            public static GUIContent generalLightOverlapMode = EditorGUIUtility.TrTextContent("Alpha Blend on Overlap", "Use alpha blending instead of additive blending when this light overlaps others");
            public static GUIContent generalLightOrder = EditorGUIUtility.TrTextContent("Light Order", "The relative order in which lights of the same blend style get rendered.");
            public static GUIContent generalShadowIntensity = EditorGUIUtility.TrTextContent("Shadow Intensity", "Controls the shadow's darkness.");
            public static GUIContent generalShadowVolumeIntensity = EditorGUIUtility.TrTextContent("Shadow Volume Intensity", "Controls the shadow volume's darkness.");
            public static GUIContent generalSortingLayerPrefixLabel = EditorGUIUtility.TrTextContent("Target Sorting Layers", "Apply this light to the specified sorting layers.");
            public static GUIContent generalLightNoLightEnabled = EditorGUIUtility.TrTextContentWithIcon("No valid blend styles are enabled.", MessageType.Error);

            public static GUIContent pointLightQuality = EditorGUIUtility.TrTextContent("Quality", "Use accurate if there are noticeable visual issues");
            public static GUIContent pointLightInnerAngle =  EditorGUIUtility.TrTextContent("Inner Angle", "Specify the inner angle of the light");
            public static GUIContent pointLightOuterAngle = EditorGUIUtility.TrTextContent("Outer Angle", "Specify the outer angle of the light");
            public static GUIContent pointLightInnerRadius = EditorGUIUtility.TrTextContent("Inner Radius", "Specify the inner radius of the light");
            public static GUIContent pointLightOuterRadius = EditorGUIUtility.TrTextContent("Outer Radius", "Specify the outer radius of the light");
            public static GUIContent pointLightZDistance = EditorGUIUtility.TrTextContent("Distance", "Specify the Z Distance of the light");
            public static GUIContent pointLightCookie = EditorGUIUtility.TrTextContent("Cookie", "Specify a sprite as the cookie for the light");


            public static GUIContent shapeLightSprite = EditorGUIUtility.TrTextContent("Sprite", "Specify the sprite");
            public static GUIContent shapeLightParametricRadius = EditorGUIUtility.TrTextContent("Radius", "Adjust the size of the object");
            public static GUIContent shapeLightParametricSides = EditorGUIUtility.TrTextContent("Sides", "Adjust the shapes number of sides");
            public static GUIContent shapeLightFalloffOffset = EditorGUIUtility.TrTextContent("Falloff Offset", "Specify the shape's falloff offset");
            public static GUIContent shapeLightAngleOffset = EditorGUIUtility.TrTextContent("Angle Offset", "Adjust the rotation of the object");

            public static GUIContent renderPipelineUnassignedWarning = EditorGUIUtility.TrTextContentWithIcon("Universal scriptable renderpipeline asset must be assigned in Graphics Settings or Quality Settings.", MessageType.Warning);
            public static GUIContent asset2DUnassignedWarning = EditorGUIUtility.TrTextContentWithIcon("2D renderer data must be assigned to your universal render pipeline asset or camera.", MessageType.Warning);
        }

        const float     k_GlobalLightGizmoSize      = 1.2f;
        const float     k_AngleCapSize              = 0.16f * k_GlobalLightGizmoSize;
        const float     k_AngleCapOffset            = 0.08f * k_GlobalLightGizmoSize;
        const float     k_AngleCapOffsetSecondary   = -0.05f;
        const float     k_RangeCapSize              = 0.025f * k_GlobalLightGizmoSize;
        const float     k_InnerRangeCapSize         = 0.08f * k_GlobalLightGizmoSize;

        SerializedProperty m_LightType;
        SerializedProperty m_LightColor;
        SerializedProperty m_LightIntensity;
        SerializedProperty m_UseNormalMap;
        SerializedProperty m_ShadowIntensity;
        SerializedProperty m_ShadowVolumeIntensity;
        SerializedProperty m_ApplyToSortingLayers;
        SerializedProperty m_VolumetricAlpha;
        SerializedProperty m_BlendStyleIndex;
        SerializedProperty m_FalloffIntensity;
        SerializedProperty m_PointZDistance;
        SerializedProperty m_LightOrder;
        SerializedProperty m_AlphaBlendOnOverlap;

        // Point Light Properties
        SerializedProperty m_PointInnerAngle;
        SerializedProperty m_PointOuterAngle;
        SerializedProperty m_PointInnerRadius;
        SerializedProperty m_PointOuterRadius;
        SerializedProperty m_PointLightCookie;
        SerializedProperty m_PointLightQuality;

        // Shape Light Properties
        SerializedProperty m_ShapeLightParametricRadius;
        SerializedProperty m_ShapeLightFalloffSize;
        SerializedProperty m_ShapeLightParametricSides;
        SerializedProperty m_ShapeLightParametricAngleOffset;
        SerializedProperty m_ShapeLightFalloffOffset;
        SerializedProperty m_ShapeLightSprite;

        int[]           m_BlendStyleIndices;
        GUIContent[]    m_BlendStyleNames;
        bool            m_AnyBlendStyleEnabled  = false;

        SortingLayerDropDown m_SortingLayerDropDown;

        Light2D lightObject => target as Light2D;

        Analytics.Renderer2DAnalytics m_Analytics;
        HashSet<Light2D> m_ModifiedLights;

        private void AnalyticsTrackChanges(SerializedObject serializedObject)
        {
            if (serializedObject.hasModifiedProperties)
            {
                foreach (Object targetObj in serializedObject.targetObjects)
                {
                    Light2D light2d = (Light2D)targetObj;
                    if(!m_ModifiedLights.Contains(light2d))
                        m_ModifiedLights.Add(light2d);
                }
            }
        }

        void OnEnable()
        {
            m_Analytics = Analytics.Renderer2DAnalytics.instance;
            m_ModifiedLights = new HashSet<Light2D>();
            m_SortingLayerDropDown = new SortingLayerDropDown();

            m_LightType = serializedObject.FindProperty("m_LightType");
            m_LightColor = serializedObject.FindProperty("m_Color");
            m_LightIntensity = serializedObject.FindProperty("m_Intensity");
            m_UseNormalMap = serializedObject.FindProperty("m_UseNormalMap");
            m_ShadowIntensity = serializedObject.FindProperty("m_ShadowIntensity");
            m_ShadowVolumeIntensity = serializedObject.FindProperty("m_ShadowVolumeIntensity");
            m_ApplyToSortingLayers = serializedObject.FindProperty("m_ApplyToSortingLayers");
            m_VolumetricAlpha = serializedObject.FindProperty("m_LightVolumeOpacity");
            m_BlendStyleIndex = serializedObject.FindProperty("m_BlendStyleIndex");
            m_FalloffIntensity = serializedObject.FindProperty("m_FalloffIntensity");
            m_PointZDistance = serializedObject.FindProperty("m_PointLightDistance");
            m_LightOrder = serializedObject.FindProperty("m_LightOrder");
            m_AlphaBlendOnOverlap = serializedObject.FindProperty("m_AlphaBlendOnOverlap");

            // Point Light
            m_PointInnerAngle = serializedObject.FindProperty("m_PointLightInnerAngle");
            m_PointOuterAngle = serializedObject.FindProperty("m_PointLightOuterAngle");
            m_PointInnerRadius = serializedObject.FindProperty("m_PointLightInnerRadius");
            m_PointOuterRadius = serializedObject.FindProperty("m_PointLightOuterRadius");
            m_PointLightCookie = serializedObject.FindProperty("m_LightCookieSprite");
            m_PointLightQuality = serializedObject.FindProperty("m_PointLightQuality");

            // Shape Light
            m_ShapeLightParametricRadius = serializedObject.FindProperty("m_ShapeLightParametricRadius");
            m_ShapeLightFalloffSize = serializedObject.FindProperty("m_ShapeLightFalloffSize");
            m_ShapeLightParametricSides = serializedObject.FindProperty("m_ShapeLightParametricSides");
            m_ShapeLightParametricAngleOffset = serializedObject.FindProperty("m_ShapeLightParametricAngleOffset");
            m_ShapeLightFalloffOffset = serializedObject.FindProperty("m_ShapeLightFalloffOffset");
            m_ShapeLightSprite = serializedObject.FindProperty("m_LightCookieSprite");

            m_AnyBlendStyleEnabled = false;
            var blendStyleIndices = new List<int>();
            var blendStyleNames = new List<string>();

            var rendererData = Light2DEditorUtility.GetRenderer2DData();
            if (rendererData != null)
            {
                for (int i = 0; i < rendererData.lightBlendStyles.Length; ++i)
                {
                    blendStyleIndices.Add(i);

                    ref var blendStyle = ref rendererData.lightBlendStyles[i];
                    blendStyleNames.Add(blendStyle.name);
                    m_AnyBlendStyleEnabled = true;
                }
            }
            else
            {
                for (int i = 0; i < 4; ++i)
                {
                    blendStyleIndices.Add(i);
                    blendStyleNames.Add("Operation" + i);
                }
            }

            m_BlendStyleIndices = blendStyleIndices.ToArray();
            m_BlendStyleNames = blendStyleNames.Select(x => new GUIContent(x)).ToArray();


            m_SortingLayerDropDown.OnEnable(serializedObject, "m_ApplyToSortingLayers");
        }

        internal void SendModifiedAnalytics(Analytics.Renderer2DAnalytics analytics, Light2D light)
        {
            Analytics.Light2DData lightData = new Analytics.Light2DData();
            lightData.was_create_event = false;
            lightData.instance_id = light.GetInstanceID();
            lightData.light_type = light.lightType;
            Analytics.Renderer2DAnalytics.instance.SendData(Analytics.AnalyticsDataTypes.k_LightDataString, lightData);
        }

        private void OnDestroy()
        {

            if(m_ModifiedLights.Count > 0)
            {
                foreach (Light2D light in m_ModifiedLights)
                {
                    SendModifiedAnalytics(m_Analytics, light);
                }
            }
        }

        void OnPointLight(SerializedObject serializedObject)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Slider(m_PointInnerAngle, 0, 360, Styles.pointLightInnerAngle);
            if (EditorGUI.EndChangeCheck())
                m_PointInnerAngle.floatValue = Mathf.Min(m_PointInnerAngle.floatValue, m_PointOuterAngle.floatValue);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Slider(m_PointOuterAngle, 0, 360, Styles.pointLightOuterAngle);
            if (EditorGUI.EndChangeCheck())
                m_PointOuterAngle.floatValue = Mathf.Max(m_PointInnerAngle.floatValue, m_PointOuterAngle.floatValue);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_PointInnerRadius, Styles.pointLightInnerRadius);
            if (EditorGUI.EndChangeCheck())
                m_PointInnerRadius.floatValue = Mathf.Max(0.0f, Mathf.Min(m_PointInnerRadius.floatValue, m_PointOuterRadius.floatValue));

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_PointOuterRadius, Styles.pointLightOuterRadius);
            if (EditorGUI.EndChangeCheck())
                m_PointOuterRadius.floatValue = Mathf.Max(m_PointInnerRadius.floatValue, m_PointOuterRadius.floatValue);

            EditorGUILayout.Slider(m_FalloffIntensity, 0, 1, Styles.generalFalloffIntensity);
            EditorGUILayout.PropertyField(m_PointLightCookie, Styles.pointLightCookie);
        }

        void OnShapeLight(Light2D.LightType lightType, SerializedObject serializedObject)
        {
            if (lightType == Light2D.LightType.Sprite)
            {
                EditorGUILayout.PropertyField(m_ShapeLightSprite, Styles.shapeLightSprite);
            }
            else if (lightType == Light2D.LightType.Parametric || lightType == Light2D.LightType.Freeform)
            {
                if (lightType == Light2D.LightType.Parametric)
                {
                    EditorGUILayout.PropertyField(m_ShapeLightParametricRadius, Styles.shapeLightParametricRadius);
                    if (m_ShapeLightParametricRadius.floatValue < 0)
                        m_ShapeLightParametricRadius.floatValue = 0;

                    EditorGUILayout.IntSlider(m_ShapeLightParametricSides, 3, 48, Styles.shapeLightParametricSides);
                    EditorGUILayout.Slider(m_ShapeLightParametricAngleOffset, 0, 359, Styles.shapeLightAngleOffset);
                }

                EditorGUILayout.PropertyField(m_ShapeLightFalloffSize, Styles.generalFalloffSize);
                if (m_ShapeLightFalloffSize.floatValue < 0)
                    m_ShapeLightFalloffSize.floatValue = 0;

                EditorGUILayout.Slider(m_FalloffIntensity, 0, 1, Styles.generalFalloffIntensity);

                if (lightType == Light2D.LightType.Parametric || lightType == Light2D.LightType.Freeform)
                {
                    bool oldWideMode = EditorGUIUtility.wideMode;
                    EditorGUIUtility.wideMode = true;
                    EditorGUILayout.PropertyField(m_ShapeLightFalloffOffset, Styles.shapeLightFalloffOffset);
                    EditorGUIUtility.wideMode = oldWideMode;
                }
            }
        }

        Vector3 DrawAngleSlider2D(Transform transform, Quaternion rotation, float radius, float offset, Handles.CapFunction capFunc, float capSize, bool leftAngle, bool drawLine, bool useCapOffset, ref float angle)
        {
            float oldAngle = angle;

            float angleBy2 = (angle / 2) * (leftAngle ? -1.0f : 1.0f);
            Vector3 trcwPos = Quaternion.AngleAxis(angleBy2, -transform.forward) * (transform.up);
            Vector3 cwPos = transform.position + trcwPos * (radius + offset);

            float direction = leftAngle ? 1 : -1;

            // Offset the handle
            float size = .25f * capSize;

            Vector3 handleOffset = useCapOffset ? rotation * new Vector3(direction * size, 0, 0) : Vector3.zero;

            EditorGUI.BeginChangeCheck();
            var id = GUIUtility.GetControlID("AngleSlider".GetHashCode(), FocusType.Passive);
            Vector3 cwHandle = Handles.Slider2D(id, cwPos, handleOffset, Vector3.forward, rotation * Vector3.up, rotation * Vector3.right, capSize, capFunc, Vector3.zero);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 toCwHandle = (transform.position - cwHandle).normalized;

                angle = 360 - 2 * Quaternion.Angle(Quaternion.FromToRotation(transform.up, toCwHandle), Quaternion.identity);
                angle = Mathf.Round(angle * 100) / 100f;

                float side = Vector3.Dot(direction * transform.right, toCwHandle);
                if (side < 0)
                {
                    if (oldAngle < 180)
                        angle = 0;
                    else
                        angle = 360;
                }
            }

            if (drawLine)
                Handles.DrawLine(transform.position, cwHandle);

            return cwHandle;
        }

        private float DrawAngleHandle(Transform transform, float radius, float offset, Handles.CapFunction capLeft, Handles.CapFunction capRight, ref float angle)
        {
            float old = angle;
            float handleOffset = HandleUtility.GetHandleSize(transform.position) * offset;
            float handleSize = HandleUtility.GetHandleSize(transform.position) * k_AngleCapSize;

            Quaternion rotLt = Quaternion.AngleAxis(-angle / 2, -transform.forward) * transform.rotation;
            DrawAngleSlider2D(transform, rotLt, radius, handleOffset, capLeft, handleSize, true, true, true, ref angle);

            Quaternion rotRt = Quaternion.AngleAxis(angle / 2, -transform.forward) * transform.rotation;
            DrawAngleSlider2D(transform, rotRt, radius, handleOffset, capRight, handleSize, false, true, true, ref angle);

            return angle - old;
        }

        private void DrawRadiusArc(Transform transform, float radius, float angle, int steps, Handles.CapFunction capFunc, float capSize, bool even)
        {
            Handles.DrawWireArc(transform.position, transform.forward, Quaternion.AngleAxis(180 - angle / 2, transform.forward) * -transform.up, angle, radius);
        }

        Handles.CapFunction GetCapFunc(Texture texture, bool isAngleHandle)
        {
            return (controlID, position, rotation, size, eventType) => Light2DEditorUtility.GUITextureCap(controlID, texture, position, rotation, size, eventType, isAngleHandle);
        }

        private void DrawAngleHandles(Light2D light)
        {
            var oldColor = Handles.color;
            Handles.color = Color.yellow;

            float outerAngle = light.pointLightOuterAngle;
            float diff = DrawAngleHandle(light.transform, light.pointLightOuterRadius, k_AngleCapOffset, GetCapFunc(Styles.lightCapTopRight, true), GetCapFunc(Styles.lightCapBottomRight, true), ref outerAngle);
            light.pointLightOuterAngle = outerAngle;

            if (diff != 0.0f)
                light.pointLightInnerAngle = Mathf.Max(0.0f, light.pointLightInnerAngle + diff);

            float innerAngle = light.pointLightInnerAngle;
            diff = DrawAngleHandle(light.transform, light.pointLightOuterRadius, -k_AngleCapOffset, GetCapFunc(Styles.lightCapTopLeft, true), GetCapFunc(Styles.lightCapBottomLeft, true), ref innerAngle);
            light.pointLightInnerAngle = innerAngle;

            if (diff != 0.0f)
                light.pointLightInnerAngle = light.pointLightInnerAngle < light.pointLightOuterAngle ? light.pointLightInnerAngle : light.pointLightOuterAngle;

            light.pointLightInnerAngle = Mathf.Min(light.pointLightInnerAngle, light.pointLightOuterAngle);

            Handles.color = oldColor;
        }

        private void DrawRangeHandles(Light2D light)
        {
            var dummy = 0.0f;
            bool radiusChanged = false;
            Vector3 handlePos = Vector3.zero;
            Quaternion rotLeft = Quaternion.AngleAxis(0, -light.transform.forward) * light.transform.rotation;
            float handleOffset = HandleUtility.GetHandleSize(light.transform.position) * k_AngleCapOffsetSecondary;
            float handleSize = HandleUtility.GetHandleSize(light.transform.position) * k_AngleCapSize;

            var oldColor = Handles.color;
            Handles.color = Color.yellow;

            float outerRadius = light.pointLightOuterRadius;
            EditorGUI.BeginChangeCheck();
            Vector3 returnPos = DrawAngleSlider2D(light.transform, rotLeft, outerRadius, -handleOffset, GetCapFunc(Styles.lightCapUp, false), handleSize, false, false, false, ref dummy);
            if (EditorGUI.EndChangeCheck())
            {
                var vec = (returnPos - light.transform.position).normalized;
                light.transform.up = new Vector3(vec.x, vec.y, 0);
                outerRadius = (returnPos - light.transform.position).magnitude;
                outerRadius = outerRadius + handleOffset;
                radiusChanged = true;
            }
            DrawRadiusArc(light.transform, light.pointLightOuterRadius, light.pointLightOuterAngle, 0, Handles.DotHandleCap, k_RangeCapSize, false);

            Handles.color = Color.gray;
            float innerRadius = light.pointLightInnerRadius;
            EditorGUI.BeginChangeCheck();
            returnPos = DrawAngleSlider2D(light.transform, rotLeft, innerRadius, handleOffset, GetCapFunc(Styles.lightCapDown, false), handleSize, true, false, false, ref dummy);
            if (EditorGUI.EndChangeCheck())
            {
                innerRadius = (returnPos - light.transform.position).magnitude;
                innerRadius = innerRadius - handleOffset;
                radiusChanged = true;
            }
            DrawRadiusArc(light.transform, light.pointLightInnerRadius, light.pointLightOuterAngle, 0, Handles.SphereHandleCap, k_InnerRangeCapSize, false);

            Handles.color = oldColor;

            if (radiusChanged)
            {
                light.pointLightInnerRadius = (outerRadius < innerRadius) ? outerRadius : innerRadius;
                light.pointLightOuterRadius = (innerRadius > outerRadius) ? innerRadius : outerRadius;
            }
        }

        void OnSceneGUI()
        {
            var light = target as Light2D;
            if (light == null)
                return;

            Transform t = light.transform;
            switch (light.lightType)
            {
                case Light2D.LightType.Point:
                    {
                        Undo.RecordObject(light.transform, "Edit Point Light Transform");
                        Undo.RecordObject(light, "Edit Point Light");

                        DrawRangeHandles(light);
                        DrawAngleHandles(light);

                        if (GUI.changed)
                            EditorUtility.SetDirty(light);
                    }
                    break;
                case Light2D.LightType.Sprite:
                    {
                        var cookieSprite = light.lightCookieSprite;
                        if (cookieSprite != null)
                        {
                            Vector3 min = cookieSprite.bounds.min;
                            Vector3 max = cookieSprite.bounds.max;

                            Vector3 v0 = t.TransformPoint(new Vector3(min.x, min.y));
                            Vector3 v1 = t.TransformPoint(new Vector3(max.x, min.y));
                            Vector3 v2 = t.TransformPoint(new Vector3(max.x, max.y));
                            Vector3 v3 = t.TransformPoint(new Vector3(min.x, max.y));

                            Handles.DrawLine(v0, v1);
                            Handles.DrawLine(v1, v2);
                            Handles.DrawLine(v2, v3);
                            Handles.DrawLine(v3, v0);
                        }
                    }
                    break;
                case Light2D.LightType.Parametric:
                    {
                        float radius = light.shapeLightParametricRadius;
                        float sides = light.shapeLightParametricSides;
                        float angleOffset = Mathf.PI / 2.0f + Mathf.Deg2Rad * light.shapeLightParametricAngleOffset;

                        if (sides < 3)
                            sides = 3;

                        if (sides == 4)
                            angleOffset = Mathf.PI / 4.0f + Mathf.Deg2Rad * light.shapeLightParametricAngleOffset;

                        Vector3 direction = new Vector3(Mathf.Cos(angleOffset), Mathf.Sin(angleOffset), 0);
                        Vector3 startPoint = radius * direction;
                        Vector3 featherStartPoint = startPoint + light.shapeLightFalloffSize * direction;
                        float radiansPerSide = 2 * Mathf.PI / sides;
                        Vector3 falloffOffset = light.shapeLightFalloffOffset;

                        for (int i = 0; i < sides; ++i)
                        {
                            float endAngle = (i + 1) * radiansPerSide;

                            direction = new Vector3(Mathf.Cos(endAngle + angleOffset), Mathf.Sin(endAngle + angleOffset), 0);
                            Vector3 endPoint = radius * direction;
                            Vector3 featherEndPoint = endPoint + light.shapeLightFalloffSize * direction;

                            Handles.DrawLine(t.TransformPoint(startPoint), t.TransformPoint(endPoint));
                            Handles.DrawLine(t.TransformPoint(featherStartPoint + falloffOffset), t.TransformPoint(featherEndPoint + falloffOffset));

                            startPoint = endPoint;
                            featherStartPoint = featherEndPoint;
                        }
                    }
                    break;
                case Light2D.LightType.Freeform:
                    {
                        // Draw the falloff shape's outline
                        List<Vector2> falloffShape = light.GetFalloffShape();
                        Handles.color = Color.white;


                        Vector3 falloffOffset = m_ShapeLightFalloffOffset.vector2Value;

                        for (int i = 0; i < falloffShape.Count - 1; ++i)
                        {
                            Handles.DrawLine(t.TransformPoint(falloffShape[i]) + falloffOffset, t.TransformPoint(falloffShape[i + 1]) + falloffOffset);
                            Handles.DrawLine(t.TransformPoint(light.shapePath[i]), t.TransformPoint(light.shapePath[i + 1]));
                        }

                        Handles.DrawLine(t.TransformPoint(falloffShape[falloffShape.Count - 1]) + falloffOffset, t.TransformPoint(falloffShape[0]) + falloffOffset);
                        Handles.DrawLine(t.TransformPoint(light.shapePath[falloffShape.Count - 1]), t.TransformPoint(light.shapePath[0]));
                    }
                    break;
            }
        }

        public override void OnInspectorGUI()
        {
            UniversalRenderPipelineAsset asset = UniversalRenderPipeline.asset;

            if (asset != null)
            {
                if (!Light2DEditorUtility.IsUsing2DRenderer())
                {
                    EditorGUILayout.HelpBox(Styles.asset2DUnassignedWarning);
                    return;
                }
            }
            else
            {
                EditorGUILayout.HelpBox(Styles.renderPipelineUnassignedWarning);
                return;
            }

            EditorGUILayout.Space();

            serializedObject.Update();

            Rect lightTypeRect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(lightTypeRect, GUIContent.none, m_LightType);
            EditorGUI.BeginChangeCheck();
            int newLightType = EditorGUI.Popup(lightTypeRect, Styles.generalLightType, m_LightType.intValue, Styles.lightTypeOptions);
            if (EditorGUI.EndChangeCheck())
                m_LightType.intValue = newLightType;
            EditorGUI.EndProperty();

            switch (m_LightType.intValue)
            {
                case (int)Light2D.LightType.Point:
                    {
                        OnPointLight(serializedObject);
                    }
                    break;
                case (int)Light2D.LightType.Parametric:
                case (int)Light2D.LightType.Freeform:
                case (int)Light2D.LightType.Sprite:
                    {
                        OnShapeLight((Light2D.LightType)m_LightType.intValue, serializedObject);
                    }
                    break;
            }

            if(m_LightType.intValue != (int)Light2D.LightType.Global)
                EditorGUILayout.PropertyField(m_AlphaBlendOnOverlap, Styles.generalLightOverlapMode);

            EditorGUILayout.PropertyField(m_LightOrder, Styles.generalLightOrder);

            if (!m_AnyBlendStyleEnabled)
                EditorGUILayout.HelpBox(Styles.generalLightNoLightEnabled);
            else
                EditorGUILayout.IntPopup(m_BlendStyleIndex, m_BlendStyleNames, m_BlendStyleIndices, Styles.generalBlendStyle);

            EditorGUILayout.PropertyField(m_LightColor, Styles.generalLightColor);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_LightIntensity, Styles.generalLightIntensity);
            if (EditorGUI.EndChangeCheck())
                m_LightIntensity.floatValue = Mathf.Max(m_LightIntensity.floatValue, 0);

            if (m_LightType.intValue != (int)Light2D.LightType.Global)
            {
                
                EditorGUILayout.PropertyField(m_UseNormalMap, Styles.generalUseNormalMap);

                if (m_UseNormalMap.boolValue)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_PointZDistance, Styles.pointLightZDistance);
                    if (EditorGUI.EndChangeCheck())
                        m_PointZDistance.floatValue = Mathf.Max(0.0f, m_PointZDistance.floatValue);
                    EditorGUILayout.PropertyField(m_PointLightQuality, Styles.pointLightQuality);
                }

                EditorGUILayout.Slider(m_VolumetricAlpha, 0, 1, Styles.generalVolumeOpacity);

                EditorGUILayout.Slider(m_ShadowIntensity, 0, 1, Styles.generalShadowIntensity);
                if(m_VolumetricAlpha.floatValue > 0)
                    EditorGUILayout.Slider(m_ShadowVolumeIntensity, 0, 1, Styles.generalShadowVolumeIntensity);
            }

            m_SortingLayerDropDown.OnTargetSortingLayers(serializedObject, targets, Styles.generalSortingLayerPrefixLabel, AnalyticsTrackChanges);

            if (m_LightType.intValue == (int)Light2D.LightType.Freeform)
            {
                DoEditButton<FreeformShapeTool>(PathEditorToolContents.icon, "Edit Shape");
                DoPathInspector<FreeformShapeTool>();
                DoSnappingInspector<FreeformShapeTool>();
            }

            AnalyticsTrackChanges(serializedObject);
            serializedObject.ApplyModifiedProperties();
        }

    }
}
