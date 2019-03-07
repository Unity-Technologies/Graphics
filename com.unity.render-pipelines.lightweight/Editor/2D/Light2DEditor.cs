using UnityEngine;
using UnityEngine.Experimental.Rendering.LWRP;
using UnityEngine.Rendering.LWRP;
using System.Linq;
using System.Collections.Generic;
using Unity.Path2D;

namespace UnityEditor.Experimental.Rendering.LWRP
{
    [CustomEditor(typeof(Light2D))]
    [CanEditMultipleObjects]
    internal class Light2DEditor : Editor
    {
        private class ShapeEditor : PolygonEditor
        {
            const string k_ShapePath = "m_ShapePath";

            protected override int GetPointCount(SerializedObject serializedObject)
            {
                return (serializedObject.targetObject as Light2D).shapePath.Length;
            }

            protected override Vector3 GetPoint(SerializedObject serializedObject, int index)
            {
                return (serializedObject.targetObject as Light2D).shapePath[index];
            }

            protected override void SetPoint(SerializedObject serializedObject, int index, Vector3 position)
            {
                serializedObject.Update();
                serializedObject.FindProperty(k_ShapePath).GetArrayElementAtIndex(index).vector3Value = position;
                serializedObject.ApplyModifiedProperties();
            }

            protected override void InsertPoint(SerializedObject serializedObject, int index, Vector3 position)
            {
                serializedObject.Update();
                var shapePath = serializedObject.FindProperty(k_ShapePath);
                shapePath.InsertArrayElementAtIndex(index);
                shapePath.GetArrayElementAtIndex(index).vector3Value = position;
                serializedObject.ApplyModifiedProperties();
            }

            protected override void RemovePoint(SerializedObject serializedObject, int index)
            {
                serializedObject.Update();
                serializedObject.FindProperty(k_ShapePath).DeleteArrayElementAtIndex(index);
                serializedObject.ApplyModifiedProperties();
            }
        }

        private static class Styles
        {
            const string k_TexturePath = "Textures/";

            public static Texture lightCapTopRight = Resources.Load<Texture>(k_TexturePath + "LightCapTopRight");
            public static Texture lightCapTopLeft = Resources.Load<Texture>(k_TexturePath + "LightCapTopLeft");
            public static Texture lightCapBottomLeft = Resources.Load<Texture>(k_TexturePath + "LightCapBottomLeft");
            public static Texture lightCapBottomRight = Resources.Load<Texture>(k_TexturePath + "LightCapBottomRight");
            public static Texture lightCapUp = Resources.Load<Texture>(k_TexturePath + "LightCapUp");
            public static Texture lightCapDown = Resources.Load<Texture>(k_TexturePath + "LightCapDown");

            public static GUIContent generalLightType = EditorGUIUtility.TrTextContent("Light Type", "Specify the light type");
            public static GUIContent generalFalloffSize = EditorGUIUtility.TrTextContent("Falloff", "Specify the falloff of the light");
            public static GUIContent generalFalloffIntensity = EditorGUIUtility.TrTextContent("Falloff Intensity", "Adjusts the falloff curve");
            public static GUIContent generalLightColor = EditorGUIUtility.TrTextContent("Color", "Specify the light color");
            public static GUIContent generalVolumeOpacity = EditorGUIUtility.TrTextContent("Volume Opacity", "Specify the light's volumetric light volume opacity");
            public static GUIContent generalLightOperation = EditorGUIUtility.TrTextContent("Light Operation", "Specify the light operation");

            public static GUIContent pointLightQuality = EditorGUIUtility.TrTextContent("Quality", "Use accurate if there are noticable visual issues");
            public static GUIContent pointLightInnerAngle =  EditorGUIUtility.TrTextContent("Inner Angle", "Specify the inner angle of the light");
            public static GUIContent pointLightOuterAngle = EditorGUIUtility.TrTextContent("Outer Angle", "Specify the outer angle of the light");
            public static GUIContent pointLightInnerRadius = EditorGUIUtility.TrTextContent("Inner Radius", "Specify the inner radius of the light");
            public static GUIContent pointLightOuterRadius = EditorGUIUtility.TrTextContent("Outer Radius", "Specify the outer radius of the light");
            public static GUIContent pointLightZDistance = EditorGUIUtility.TrTextContent("Distance", "Specify the Z Distance of the light");
            public static GUIContent pointLightCookie = EditorGUIUtility.TrTextContent("Cookie", "Specify a sprite as the cookie for the light");

            public static GUIContent shapeLightNoLightDefined = EditorGUIUtility.TrTextContentWithIcon("No valid Shape Light type is defined.", MessageType.Error);
            public static GUIContent shapeLightSprite = EditorGUIUtility.TrTextContent("Sprite", "Specify the sprite");
            public static GUIContent shapeLightParametricRadius = EditorGUIUtility.TrTextContent("Radius", "Adjust the size of the object");
            public static GUIContent shapeLightParametricSides = EditorGUIUtility.TrTextContent("Sides", "Adjust the shapes number of sides");
            public static GUIContent shapeLightFalloffOffset = EditorGUIUtility.TrTextContent("Falloff Offset", "Specify the shape's falloff offset");
            public static GUIContent shapeLightAngleOffset = EditorGUIUtility.TrTextContent("Angle Offset", "Adjust the rotation of the object");
            public static GUIContent shapeLightOverlapMode = EditorGUIUtility.TrTextContent("Light Overlap Mode", "Specify what should happen when this light overlaps other lights");
            public static GUIContent shapeLightOrder = EditorGUIUtility.TrTextContent("Light Order", "Shape light order");

            public static GUIContent sortingLayerPrefixLabel = EditorGUIUtility.TrTextContent("Target Sorting Layers", "Apply this light to the specified sorting layers.");
            public static GUIContent sortingLayerAll = EditorGUIUtility.TrTextContent("All");
            public static GUIContent sortingLayerNone = EditorGUIUtility.TrTextContent("None");
            public static GUIContent sortingLayerMixed = EditorGUIUtility.TrTextContent("Mixed...");

            public static GUIContent renderPipelineUnassignedWarning = EditorGUIUtility.TrTextContentWithIcon("Lightweight scriptable renderpipeline asset must be assigned in graphics settings", MessageType.Warning);
            public static GUIContent asset2DUnassignedWarning = EditorGUIUtility.TrTextContentWithIcon("2D renderer data must be assigned to your lightweight render pipeline asset", MessageType.Warning);
        }

        const float     k_GlobalLightGizmoSize      = 1.2f;
        const float     k_AngleCapSize              = 0.16f * k_GlobalLightGizmoSize;
        const float     k_AngleCapOffset            = 0.08f * k_GlobalLightGizmoSize;
        const float     k_AngleCapOffsetSecondary   = -0.05f;
        const float     k_RangeCapSize              = 0.025f * k_GlobalLightGizmoSize;
        const float     k_InnerRangeCapSize         = 0.08f * k_GlobalLightGizmoSize;

        SerializedProperty m_LightType;
        SerializedProperty m_LightColor;
        SerializedProperty m_ApplyToSortingLayers;
        SerializedProperty m_VolumetricAlpha;
        SerializedProperty m_LightOperation;
        SerializedProperty m_FalloffCurve;

        // Point Light Properties
        SerializedProperty m_PointInnerAngle;
        SerializedProperty m_PointOuterAngle;
        SerializedProperty m_PointInnerRadius;
        SerializedProperty m_PointOuterRadius;
        SerializedProperty m_PointZDistance;
        SerializedProperty m_PointLightCookie;
        SerializedProperty m_PointLightQuality;

        // Shape Light Properties
        SerializedProperty m_ShapeLightRadius;
        SerializedProperty m_ShapeLightFalloffSize;
        SerializedProperty m_ShapeLightParametricSides;
        SerializedProperty m_ShapeLightParametricAngleOffset;
        SerializedProperty m_ShapeLightFalloffOffset;
        SerializedProperty m_ShapeLightSprite;
        SerializedProperty m_ShapeLightOrder;
        SerializedProperty m_ShapeLightOverlapMode;

        int[]           m_LightOperationIndices;
        GUIContent[]    m_LightOperationNames;
        bool            m_AnyLightOperationEnabled  = false;
        Rect            m_SortingLayerDropdownRect  = new Rect();
        SortingLayer[]  m_AllSortingLayers;
        GUIContent[]    m_AllSortingLayerNames;
        List<int>       m_ApplyToSortingLayersList;
        ShapeEditor     m_ShapeEditor               = new ShapeEditor();

        Light2D lightObject => target as Light2D;

        void OnEnable()
        {
            m_LightType = serializedObject.FindProperty("m_LightType");
            m_LightColor = serializedObject.FindProperty("m_Color");
            m_ApplyToSortingLayers = serializedObject.FindProperty("m_ApplyToSortingLayers");
            m_VolumetricAlpha = serializedObject.FindProperty("m_LightVolumeOpacity");
            m_LightOperation = serializedObject.FindProperty("m_LightOperationIndex");
            m_FalloffCurve = serializedObject.FindProperty("m_FalloffCurve");

            // Point Light
            m_PointInnerAngle = serializedObject.FindProperty("m_PointLightInnerAngle");
            m_PointOuterAngle = serializedObject.FindProperty("m_PointLightOuterAngle");
            m_PointInnerRadius = serializedObject.FindProperty("m_PointLightInnerRadius");
            m_PointOuterRadius = serializedObject.FindProperty("m_PointLightOuterRadius");
            m_PointZDistance = serializedObject.FindProperty("m_PointLightDistance");
            m_PointLightCookie = serializedObject.FindProperty("m_LightCookieSprite");
            m_PointLightQuality = serializedObject.FindProperty("m_PointLightQuality");

            // Shape Light
            m_ShapeLightRadius = serializedObject.FindProperty("m_ShapeLightRadius");
            m_ShapeLightFalloffSize = serializedObject.FindProperty("m_ShapeLightFalloffSize");
            m_ShapeLightParametricSides = serializedObject.FindProperty("m_ShapeLightParametricSides");
            m_ShapeLightParametricAngleOffset = serializedObject.FindProperty("m_ShapeLightParametricAngleOffset");
            m_ShapeLightFalloffOffset = serializedObject.FindProperty("m_ShapeLightFalloffOffset");
            m_ShapeLightSprite = serializedObject.FindProperty("m_LightCookieSprite");
            m_ShapeLightOrder = serializedObject.FindProperty("m_ShapeLightOrder");
            m_ShapeLightOverlapMode = serializedObject.FindProperty("m_ShapeLightOverlapMode");

            m_AnyLightOperationEnabled = false;
            var lightOperationIndices = new List<int>();
            var lightOperationNames = new List<string>();
            var pipelineAsset = UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset as LightweightRenderPipelineAsset;
            var rendererData = pipelineAsset != null ? pipelineAsset.scriptableRendererData as _2DRendererData : null;
            if (rendererData != null)
            {
                for (int i = 0; i < rendererData.lightOperations.Length; ++i)
                {
                    var lightOperation = rendererData.lightOperations[i];
                    if (lightOperation.enabled)
                    {
                        lightOperationIndices.Add(i);
                        lightOperationNames.Add(lightOperation.name);
                    }
                }

                m_AnyLightOperationEnabled = lightOperationIndices.Count != 0;
            }
            else
            {
                for (int i = 0; i < 4; ++i)
                {
                    lightOperationIndices.Add(i);
                    lightOperationNames.Add("Operation" + i);
                }
            }

            m_LightOperationIndices = lightOperationIndices.ToArray();
            m_LightOperationNames = lightOperationNames.Select(x => new GUIContent(x)).ToArray();

            m_AllSortingLayers = SortingLayer.layers;
            m_AllSortingLayerNames = m_AllSortingLayers.Select(x => new GUIContent(x.name)).ToArray();

            int applyToSortingLayersSize = m_ApplyToSortingLayers.arraySize;
            m_ApplyToSortingLayersList = new List<int>(applyToSortingLayersSize);

            for (int i = 0; i < applyToSortingLayersSize; ++i)
            {
                int layerID = m_ApplyToSortingLayers.GetArrayElementAtIndex(i).intValue;
                if (SortingLayer.IsValid(layerID))
                    m_ApplyToSortingLayersList.Add(layerID);
            }
        }

        void OnPointLight(SerializedObject serializedObject)
        {
            EditorGUILayout.PropertyField(m_PointLightQuality, Styles.pointLightQuality);

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

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_PointZDistance, Styles.pointLightZDistance);
            if (EditorGUI.EndChangeCheck())
                m_PointZDistance.floatValue = Mathf.Max(0.0f, m_PointZDistance.floatValue);

            EditorGUILayout.Slider(m_FalloffCurve, 0, 1, Styles.generalFalloffIntensity);
            EditorGUILayout.PropertyField(m_PointLightCookie, Styles.pointLightCookie);
        }

        void OnShapeLight(Light2D.LightType lightType, SerializedObject serializedObject)
        {
            if (!m_AnyLightOperationEnabled)
                EditorGUILayout.HelpBox(Styles.shapeLightNoLightDefined);

            if (lightType == Light2D.LightType.Sprite)
            {
                EditorGUILayout.PropertyField(m_ShapeLightSprite, Styles.shapeLightSprite);
            }
            else if (lightType == Light2D.LightType.Parametric || lightType == Light2D.LightType.Freeform)
            {
                if (lightType == Light2D.LightType.Parametric)
                {
                    EditorGUILayout.Slider(m_ShapeLightRadius, 0, 20, Styles.shapeLightParametricRadius);
                    EditorGUILayout.IntSlider(m_ShapeLightParametricSides, 3, 24, Styles.shapeLightParametricSides);
                    EditorGUILayout.Slider(m_ShapeLightParametricAngleOffset, 0, 359, Styles.shapeLightAngleOffset);
                }

                EditorGUILayout.Slider(m_ShapeLightFalloffSize, 0, 5, Styles.generalFalloffSize);
                EditorGUILayout.Slider(m_FalloffCurve, 0, 1, Styles.generalFalloffIntensity);

                if (lightType == Light2D.LightType.Parametric)
                    EditorGUILayout.PropertyField(m_ShapeLightFalloffOffset, Styles.shapeLightFalloffOffset);
            }

            EditorGUILayout.PropertyField(m_ShapeLightOverlapMode, Styles.shapeLightOverlapMode);
            EditorGUILayout.PropertyField(m_ShapeLightOrder, Styles.shapeLightOrder);
        }

        void OnTargetSortingLayers()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(Styles.sortingLayerPrefixLabel);

            GUIContent selectedLayers;
            if (m_ApplyToSortingLayersList.Count == 1)
                selectedLayers = new GUIContent(SortingLayer.IDToName(m_ApplyToSortingLayersList[0]));
            else if (m_ApplyToSortingLayersList.Count == m_AllSortingLayers.Length)
                selectedLayers = Styles.sortingLayerAll;
            else if (m_ApplyToSortingLayersList.Count == 0)
                selectedLayers = Styles.sortingLayerNone;
            else
                selectedLayers = Styles.sortingLayerMixed;

            bool buttonDown = EditorGUILayout.DropdownButton(selectedLayers, FocusType.Keyboard, EditorStyles.popup);

            if (Event.current.type == EventType.Repaint)
                m_SortingLayerDropdownRect = GUILayoutUtility.GetLastRect();

            if (buttonDown)
            {
                GenericMenu menu = new GenericMenu();
                menu.allowDuplicateNames = true;

                GenericMenu.MenuFunction2 menuFunction = (layerIDObject) =>
                {
                    int layerID = (int)layerIDObject;

                    if (m_ApplyToSortingLayersList.Contains(layerID))
                        m_ApplyToSortingLayersList.RemoveAll(id => id == layerID);
                    else
                        m_ApplyToSortingLayersList.Add(layerID);

                    m_ApplyToSortingLayers.ClearArray();
                    for (int i = 0; i < m_ApplyToSortingLayersList.Count; ++i)
                    {
                        m_ApplyToSortingLayers.InsertArrayElementAtIndex(i);
                        m_ApplyToSortingLayers.GetArrayElementAtIndex(i).intValue = m_ApplyToSortingLayersList[i];
                    }

                    serializedObject.ApplyModifiedProperties();
                };

                for (int i = 0; i < m_AllSortingLayers.Length; ++i)
                {
                    var sortingLayer = m_AllSortingLayers[i];
                    menu.AddItem(m_AllSortingLayerNames[i], m_ApplyToSortingLayersList.Contains(sortingLayer.id), menuFunction, sortingLayer.id);
                }

                menu.DropDown(m_SortingLayerDropdownRect);
            }

            EditorGUILayout.EndHorizontal();
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

        Handles.CapFunction GetCapFunc(Texture texture)
        {
            return (controlID, position, rotation, size, eventType) => Light2DEditorUtility.GUITextureCap(controlID, texture, position, rotation, size, eventType);
        }

        private void DrawAngleHandles(Light2D light)
        {
            var oldColor = Handles.color;
            Handles.color = Color.yellow;

            float outerAngle = light.pointLightOuterAngle;
            float diff = DrawAngleHandle(light.transform, light.pointLightOuterRadius, k_AngleCapOffset, GetCapFunc(Styles.lightCapTopRight), GetCapFunc(Styles.lightCapBottomRight), ref outerAngle);
            light.pointLightOuterAngle = outerAngle;

            if (diff != 0.0f)
                light.pointLightInnerAngle = Mathf.Max(0.0f, light.pointLightInnerAngle + diff);

            float innerAngle = light.pointLightInnerAngle;
            diff = DrawAngleHandle(light.transform, light.pointLightOuterRadius, -k_AngleCapOffset, GetCapFunc(Styles.lightCapTopLeft), GetCapFunc(Styles.lightCapBottomLeft), ref innerAngle);
            light.pointLightInnerAngle = innerAngle;

            if (diff != 0.0f)
                light.pointLightInnerAngle = light.pointLightInnerAngle < light.pointLightOuterAngle ? light.pointLightInnerAngle : light.pointLightOuterAngle;

            light.pointLightInnerAngle = Mathf.Min(light.pointLightInnerAngle, light.pointLightOuterAngle);

            Handles.color = oldColor;
        }

        private void DrawRangeHandles(Light2D light)
        {
            var handleColor = Handles.color;
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
            Vector3 returnPos = DrawAngleSlider2D(light.transform, rotLeft, outerRadius, -handleOffset, GetCapFunc(Styles.lightCapUp), handleSize, false, false, false, ref dummy);
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
            returnPos = DrawAngleSlider2D(light.transform, rotLeft, innerRadius, handleOffset, GetCapFunc(Styles.lightCapDown), handleSize, true, false, false, ref dummy);
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
            
            Handles.color = handleColor;
        }

        void OnSceneGUI()
        {
            var light = target as Light2D;
            if (light == null)
                return;

            if (light.lightType == Light2D.LightType.Point)
            {

                Undo.RecordObject(light, "Edit Target Light");
                Undo.RecordObject(light.transform, light.transform.GetHashCode() + "_undo");

                DrawRangeHandles(light);
                DrawAngleHandles(light);

                if (GUI.changed)
                    EditorUtility.SetDirty(light);
            }
            else
            {
                Transform t = light.transform;
                Vector3 falloffOffset = light.shapeLightFalloffOffset;

                if (light.lightType == Light2D.LightType.Sprite)
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
                else if (light.lightType == Light2D.LightType.Parametric)
                {
                    float radius = light.shapeLightRadius;
                    float sides = light.shapeLightParametricSides;
                    float angleOffset = Mathf.PI / 2.0f + Mathf.Deg2Rad * light.shapeLightParametricAngleOffset;

                    if (sides < 3)
                        sides = 4;

                    if (sides == 4)
                        angleOffset = Mathf.PI / 4.0f + Mathf.Deg2Rad * light.shapeLightParametricAngleOffset;

                    Vector3 startPoint = new Vector3(radius * Mathf.Cos(angleOffset), radius * Mathf.Sin(angleOffset), 0);
                    Vector3 featherStartPoint = startPoint + light.shapeLightFalloffSize * Vector3.Normalize(startPoint);
                    float radiansPerSide = 2 * Mathf.PI / sides;
                    for (int i = 0; i < sides; i++)
                    {
                        float endAngle = (i + 1) * radiansPerSide;
                        Vector3 endPoint = new Vector3(radius * Mathf.Cos(endAngle + angleOffset), radius * Mathf.Sin(endAngle + angleOffset), 0);
                        Vector3 featherEndPoint = endPoint + light.shapeLightFalloffSize * Vector3.Normalize(endPoint);

                        Handles.DrawLine(t.TransformPoint(startPoint), t.TransformPoint(endPoint));
                        Handles.DrawLine(t.TransformPoint(featherStartPoint + falloffOffset), t.TransformPoint(featherEndPoint + falloffOffset));

                        startPoint = endPoint;
                        featherStartPoint = featherEndPoint;
                    }
                }
                else  // Freeform light
                {
                    m_ShapeEditor.OnGUI(target);

                    // Draw the falloff shape's outline
                    List<Vector2> falloffShape = light.GetFalloffShape();
                    Handles.color = Color.white;
                    for (int i = 0; i < falloffShape.Count-1; i++)
                    {
                        Handles.DrawLine(t.TransformPoint(falloffShape[i]), t.TransformPoint(falloffShape[i + 1]));
                    }
                    Handles.DrawLine(t.TransformPoint(falloffShape[falloffShape.Count - 1]), t.TransformPoint(falloffShape[0]));
                }
            }
        }

        public override void OnInspectorGUI()
        {
            LightweightRenderPipeline pipeline = UnityEngine.Rendering.RenderPipelineManager.currentPipeline as LightweightRenderPipeline;
            if (pipeline == null)
            {
                EditorGUILayout.HelpBox(Styles.renderPipelineUnassignedWarning);
                return;
            }

            LightweightRenderPipelineAsset asset = LightweightRenderPipeline.asset;
            _2DRendererData assetData = asset.scriptableRendererData as _2DRendererData; 
            if(assetData == null)
            {
                EditorGUILayout.HelpBox(Styles.asset2DUnassignedWarning);
                return;
            }


            bool updateMesh = false;
            

            EditorGUILayout.Space();

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_LightType, Styles.generalLightType);
            updateMesh |= EditorGUI.EndChangeCheck();

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

            Color previousColor = m_LightColor.colorValue;
            EditorGUILayout.IntPopup(m_LightOperation, m_LightOperationNames, m_LightOperationIndices, Styles.generalLightOperation);
            EditorGUILayout.PropertyField(m_LightColor, Styles.generalLightColor);
            EditorGUILayout.Slider(m_VolumetricAlpha, 0, 1, Styles.generalVolumeOpacity);

            OnTargetSortingLayers();

            if (lightObject.lightType == Light2D.LightType.Freeform )
            {
                // Draw the edit shape tool button here.
            }

            serializedObject.ApplyModifiedProperties();

            if (updateMesh)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    Light2D light = (Light2D)targets[i];
                    light.UpdateMesh();
                }
            }
        }
    }
}
