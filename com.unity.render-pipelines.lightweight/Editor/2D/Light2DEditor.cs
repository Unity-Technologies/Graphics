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
        internal class ShapeEditor : PolygonEditor
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

        static string k_TexturePath = "Textures/";

        private class Styles
        {
            public static Texture lightTopRight;
            public static Texture lightTopLeft;
            public static Texture lightBottomLeft;
            public static Texture lightBottomRight;
            public static Texture lightUpCap;
            public static Texture lightDownCap;

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
            public static GUIContent shapeLightOffset = EditorGUIUtility.TrTextContent("Offset", "Specify the shape's offset");
            public static GUIContent shapeLightAngleOffset = EditorGUIUtility.TrTextContent("Angle Offset", "Adjust the rotation of the object");
            public static GUIContent shapeLightOverlapMode = EditorGUIUtility.TrTextContent("Light Overlap Mode", "Specify what should happen when this light overlaps other lights");
            public static GUIContent shapeLightOrder = EditorGUIUtility.TrTextContent("Light Order", "Shape light order");

            public static GUIContent sortingLayerPrefixLabel = EditorGUIUtility.TrTextContent("Target Sorting Layers", "Apply this light to the specified sorting layers.");
            public static GUIContent sortingLayerAll = EditorGUIUtility.TrTempContent("All");
            public static GUIContent sortingLayerNone = EditorGUIUtility.TrTempContent("None");
            public static GUIContent sortingLayerMixed = EditorGUIUtility.TrTempContent("Mixed...");

            public static GUIContent renderPipelineUnassignedWarning = EditorGUIUtility.TrTextContentWithIcon("Lightweight scriptable renderpipeline asset must be assigned in graphics settings", MessageType.Warning);
            public static GUIContent asset2DUnassignedWarning = EditorGUIUtility.TrTextContentWithIcon("2D renderer data must be assigned to your lightweight render pipeline asset", MessageType.Warning);
        }

        static float s_GlobalLightGizmoSize = 1.2f;
        static float s_AngleCapSize = 0.16f * s_GlobalLightGizmoSize;
        static float s_AngleCapOffset = 0.08f * s_GlobalLightGizmoSize;
        static float s_AngleCapOffsetSecondary = -0.05f;
        static float s_RangeCapSize = 0.025f * s_GlobalLightGizmoSize;
        static Handles.CapFunction s_RangeCapFunction = Handles.DotHandleCap;
        static float s_InnerRangeCapSize = 0.08f * s_GlobalLightGizmoSize;
        static Handles.CapFunction s_InnerRangeCapFunction = Handles.SphereHandleCap;

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

        // Shape Light Properies
        SerializedProperty m_ShapeLightRadius;
        SerializedProperty m_ShapeLightFalloffSize;
        SerializedProperty m_ShapeLightParametricSides;
        SerializedProperty m_ShapeLightParametricAngleOffset;
        SerializedProperty m_ShapeLightOffset;
        SerializedProperty m_ShapeLightSprite;
        SerializedProperty m_ShapeLightOrder;
        SerializedProperty m_ShapeLightOverlapMode;

        string[] m_LayerNames;
        bool m_ModifiedMesh = false;
        int[] m_LightOperationIndices;
        GUIContent[] m_LightOperationNames;
        bool m_AnyLightOperationEnabled = false;

        private Light2D lightObject { get { return target as Light2D; } }
        private Rect m_SortingLayerDropdownRect = new Rect();
        private SortingLayer[] m_AllSortingLayers;
        private GUIContent[] m_AllSortingLayerNames;
        private List<int> m_ApplyToSortingLayersList;

        ShapeEditor m_ShapeEditor = new ShapeEditor();

        #region Handle Utilities

        public static void TriangleCapTopRight(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (Styles.lightTopRight == null)
                Styles.lightTopRight = Resources.Load<Texture>(k_TexturePath + "lt_tr");
            Light2DEditorUtility.GUITextureCap(controlID, Styles.lightTopRight, position, rotation, size, eventType);
        }

        public static void TriangleCapTopLeft(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (Styles.lightTopLeft == null)
                Styles.lightTopLeft = Resources.Load<Texture>(k_TexturePath + "lt_tl");
            Light2DEditorUtility.GUITextureCap(controlID, Styles.lightTopLeft, position, rotation, size, eventType);
        }

        public static void TriangleCapBottomRight(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (Styles.lightBottomRight == null)
                Styles.lightBottomRight = Resources.Load<Texture>(k_TexturePath + "lt_br");
            Light2DEditorUtility.GUITextureCap(controlID, Styles.lightBottomRight, position, rotation, size, eventType);
        }

        public static void TriCapBL(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (Styles.lightBottomLeft == null)
                Styles.lightBottomLeft = Resources.Load<Texture>(k_TexturePath + "lt_bl");
            Light2DEditorUtility.GUITextureCap(controlID, Styles.lightBottomLeft, position, rotation, size, eventType);
        }

        public static void SemiCircleCapUp(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (Styles.lightUpCap == null)
                Styles.lightUpCap = Resources.Load<Texture>(k_TexturePath + "lt_uc");
            Light2DEditorUtility.GUITextureCap(controlID, Styles.lightUpCap, position, rotation, size, eventType);
        }

        public static void SemiCircleCapDown(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (Styles.lightDownCap == null)
                Styles.lightDownCap = Resources.Load<Texture>(k_TexturePath + "lt_dr");
            Light2DEditorUtility.GUITextureCap(controlID, Styles.lightDownCap, position, rotation, size, eventType);
        }
        #endregion

        private void OnEnable()
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
            m_ShapeLightOffset = serializedObject.FindProperty("m_ShapeLightOffset");
            m_ShapeLightSprite = serializedObject.FindProperty("m_LightCookieSprite");
            m_ShapeLightOrder = serializedObject.FindProperty("m_ShapeLightOrder");
            m_ShapeLightOverlapMode = serializedObject.FindProperty("m_ShapeLightOverlapMode");

            m_AnyLightOperationEnabled = false;
            var light = target as Light2D;
            var lightOperationIndices = new List<int>();
            var lightOperationNames = new List<string>();
            var pipelineAsset = UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset as LightweightRenderPipelineAsset;
            var rendererData = pipelineAsset != null ? pipelineAsset.rendererData as _2DRendererData : null;
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
                for (int i = 0; i < 3; ++i)
                {
                    lightOperationIndices.Add(i);
                    lightOperationNames.Add("Type" + i);
                }
            }

            m_LightOperationIndices = lightOperationIndices.ToArray();
            m_LightOperationNames = lightOperationNames.Select(x => EditorGUIUtility.TrTextContent(x)).ToArray();

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

        private void OnPointLight(SerializedObject serializedObject)
        {
            EditorGUI.indentLevel++;

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
                m_PointInnerRadius.floatValue = Mathf.Min(m_PointInnerRadius.floatValue, m_PointOuterRadius.floatValue);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_PointOuterRadius, Styles.pointLightOuterRadius);
            if (EditorGUI.EndChangeCheck())
                m_PointOuterRadius.floatValue = Mathf.Max(m_PointInnerRadius.floatValue, m_PointOuterRadius.floatValue);

            EditorGUILayout.PropertyField(m_PointZDistance, Styles.pointLightZDistance);
            EditorGUILayout.Slider(m_FalloffCurve, 0, 1, Styles.generalFalloffIntensity);
            EditorGUILayout.PropertyField(m_PointLightCookie, Styles.pointLightCookie);
            if (m_PointInnerRadius.floatValue < 0) m_PointInnerRadius.floatValue = 0;
            if (m_PointOuterRadius.floatValue < 0) m_PointOuterRadius.floatValue = 0;
            if (m_PointZDistance.floatValue < 0) m_PointZDistance.floatValue = 0;

            EditorGUI.indentLevel--;
        }

        private bool OnShapeLight(Light2D.LightType lightProjectionType, bool changedType, SerializedObject serializedObject)
        {
            if (!m_AnyLightOperationEnabled)
            {
                EditorGUILayout.HelpBox(Styles.shapeLightNoLightDefined);
                return false;
            }

            bool updateMesh = false;

            EditorGUI.indentLevel++;
            if (lightProjectionType == Light2D.LightType.Sprite)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_ShapeLightSprite, Styles.shapeLightSprite);
                updateMesh |= EditorGUI.EndChangeCheck();
            }
            else if (lightProjectionType == Light2D.LightType.Parametric || lightProjectionType == Light2D.LightType.Freeform)
            {
                if (m_ModifiedMesh)
                    updateMesh = true;

                if (changedType)
                {
                    int sides = m_ShapeLightParametricSides.intValue;
                    if (lightProjectionType == Light2D.LightType.Parametric) sides = 6;
                    else if (lightProjectionType == Light2D.LightType.Freeform) sides = 4; // This one should depend on if this has data at the moment
                    m_ShapeLightParametricSides.intValue = sides;
                }

                m_ModifiedMesh = false;

                if (lightProjectionType == Light2D.LightType.Parametric)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.Slider(m_ShapeLightRadius, 0, 20, Styles.shapeLightParametricRadius);
                    EditorGUILayout.IntSlider(m_ShapeLightParametricSides, 3, 24, Styles.shapeLightParametricSides);
                }

                EditorGUILayout.Slider(m_ShapeLightFalloffSize, 0, 5, Styles.generalFalloffSize);
                EditorGUILayout.Slider(m_FalloffCurve, 0, 1, Styles.generalFalloffIntensity);
                Vector2 lastOffset = m_ShapeLightOffset.vector2Value;

                EditorGUILayout.PropertyField(m_ShapeLightOffset, Styles.shapeLightOffset);
                if (lightProjectionType == Light2D.LightType.Parametric)
                    EditorGUILayout.Slider(m_ShapeLightParametricAngleOffset, 0, 359, Styles.shapeLightAngleOffset);
            }

            EditorGUILayout.PropertyField(m_ShapeLightOverlapMode, Styles.shapeLightOverlapMode);
            EditorGUILayout.PropertyField(m_ShapeLightOrder, Styles.shapeLightOrder);

            EditorGUI.indentLevel--;

            return updateMesh;
        }

        private void OnTargetSortingLayers()
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

        private Vector3 DrawAngleSlider2D(Transform transform, Quaternion rotation, float radius, float offset, Handles.CapFunction capFunc, float capSize, bool leftAngle, bool drawLine, ref float angle)
        {
            float angleBy2 = (angle / 2) * (leftAngle ? -1.0f : 1.0f);
            Vector3 trcwPos = Quaternion.AngleAxis(angleBy2, -transform.forward) * (transform.up);
            Vector3 cwPos = transform.position + trcwPos * (radius + offset);

            EditorGUI.BeginChangeCheck();
            Vector3 cwHandle = Handles.Slider2D(cwPos, Vector3.forward, rotation * Vector3.up, rotation * Vector3.right, capSize, capFunc, Vector3.zero);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 toCwHandle = (transform.position - cwHandle).normalized;
                angle = 360 - 2 * Quaternion.Angle(Quaternion.FromToRotation(transform.up, toCwHandle), Quaternion.identity);
                angle = Mathf.Round(angle * 100) / 100f;
            }

            if (drawLine)
                Handles.DrawLine(transform.position, cwHandle);

            return cwHandle;
        }

        private float DrawAngleHandle(Transform transform, float radius, float offset, Handles.CapFunction capLeft, Handles.CapFunction capRight, ref float angle)
        {
            float old = angle;
            float handleOffset = HandleUtility.GetHandleSize(transform.position) * offset;
            float handleSize = HandleUtility.GetHandleSize(transform.position) * s_AngleCapSize;

            Quaternion rotLt = Quaternion.AngleAxis(-angle / 2, -transform.forward) * transform.rotation;
            DrawAngleSlider2D(transform, rotLt, radius, handleOffset, capLeft, handleSize, true, true, ref angle);

            Quaternion rotRt = Quaternion.AngleAxis(angle / 2, -transform.forward) * transform.rotation;
            DrawAngleSlider2D(transform, rotRt, radius, handleOffset, capRight, handleSize, false, true, ref angle);

            return angle - old;
        }

        private void DrawRadiusArc(Transform transform, float radius, float angle, int steps, Handles.CapFunction capFunc, float capSize, bool even)
        {
            Handles.DrawWireArc(transform.position, transform.forward, Quaternion.AngleAxis(180 - angle / 2, transform.forward) * -transform.up, angle, radius);
        }

        private void DrawAngleHandles(Light2D lt)
        {
            var oldColor = Handles.color;
            Handles.color = Color.yellow;

            float outerAngle = lt.pointLightOuterAngle;
            float diff = DrawAngleHandle(lt.transform, lt.pointLightOuterRadius, s_AngleCapOffset, TriangleCapTopRight, TriangleCapBottomRight, ref outerAngle);
            lt.pointLightOuterAngle = outerAngle;

            if (diff != 0.0f)
                lt.pointLightInnerAngle = Mathf.Max(0.0f, lt.pointLightInnerAngle + diff);

            float innerAngle = lt.pointLightInnerAngle;
            diff = DrawAngleHandle(lt.transform, lt.pointLightOuterRadius, -s_AngleCapOffset, TriangleCapTopLeft, TriCapBL, ref innerAngle);
            lt.pointLightInnerAngle = innerAngle;

            if (diff != 0.0f)
                lt.pointLightInnerAngle = lt.pointLightInnerAngle < lt.pointLightOuterAngle ? lt.pointLightInnerAngle : lt.pointLightOuterAngle;

            Handles.color = oldColor;
        }

        private float DrawRadiusHandle(Transform transform, float radius, float angle, Handles.CapFunction capFunc, float capSize, ref Vector3 handlePos)
        {
            Vector3 dir = (Quaternion.AngleAxis(angle, -transform.forward) * transform.up).normalized;
            Vector3 handle = transform.position + dir * radius;
            handlePos = Handles.FreeMoveHandle(handle, Quaternion.identity, HandleUtility.GetHandleSize(transform.position) * capSize, Vector3.zero, capFunc);
            return (transform.position - handlePos).magnitude;
        }

        private void DrawRangeHandles(Light2D lt)
        {
            var handleColor = Handles.color;
            var dummy = 0.0f;
            bool radiusChanged = false;
            Vector3 handlePos = Vector3.zero;
            Quaternion rotLeft = Quaternion.AngleAxis(0, -lt.transform.forward) * lt.transform.rotation;
            float handleOffset = HandleUtility.GetHandleSize(lt.transform.position) * s_AngleCapOffsetSecondary;
            float handleSize = HandleUtility.GetHandleSize(lt.transform.position) * s_AngleCapSize;

            var oldColor = Handles.color;
            Handles.color = Color.yellow;

            float outerRadius = lt.pointLightOuterRadius;
            EditorGUI.BeginChangeCheck();
            Vector3 returnPos = DrawAngleSlider2D(lt.transform, rotLeft, outerRadius, -handleOffset, SemiCircleCapUp, handleSize, false, false, ref dummy);
            if (EditorGUI.EndChangeCheck())
            {
                var vec = (returnPos - lt.transform.position).normalized;
                lt.transform.up = new Vector3(vec.x, vec.y, 0);
                outerRadius = (returnPos - lt.transform.position).magnitude;
                outerRadius = outerRadius + handleOffset;
                radiusChanged = true;
            }
            DrawRadiusArc(lt.transform, lt.pointLightOuterRadius, lt.pointLightOuterAngle, 0, s_RangeCapFunction, s_RangeCapSize, false);

            Handles.color = Color.gray;
            float innerRadius = lt.pointLightInnerRadius;
            EditorGUI.BeginChangeCheck();
            returnPos = DrawAngleSlider2D(lt.transform, rotLeft, innerRadius, handleOffset, SemiCircleCapDown, handleSize, true, false, ref dummy);
            if (EditorGUI.EndChangeCheck())
            {
                innerRadius = (returnPos - lt.transform.position).magnitude;
                innerRadius = innerRadius - handleOffset;
                radiusChanged = true;
            }
            DrawRadiusArc(lt.transform, lt.pointLightInnerRadius, lt.pointLightOuterAngle, 0, s_InnerRangeCapFunction, s_InnerRangeCapSize, false);

            Handles.color = oldColor;

            if (radiusChanged)
            {
                lt.pointLightInnerRadius = (outerRadius < innerRadius) ? outerRadius : innerRadius;
                lt.pointLightOuterRadius = (innerRadius > outerRadius) ? innerRadius : outerRadius;
            }
            
            Handles.color = handleColor;
        }

        protected virtual void OnSceneGUI()
        {
            var lt = target as Light2D;
            if (lt == null)
                return;

            if (lt.lightType == Light2D.LightType.Point)
            {

                Undo.RecordObject(lt, "Edit Target Light");
                Undo.RecordObject(lt.transform, lt.transform.GetHashCode() + "_undo");

                DrawRangeHandles(lt);
                DrawAngleHandles(lt);

                if (GUI.changed)
                    EditorUtility.SetDirty(lt);
            }
            else
            {
                Transform t = lt.transform;
                Vector3 posOffset = lt.shapeLightOffset;

                if (lt.lightType == Light2D.LightType.Sprite)
                {
                    var cookieSprite = lt.lightCookieSprite;
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
                else if (lt.lightType == Light2D.LightType.Parametric)
                {
                    float radius = lt.shapeLightRadius;
                    float sides = lt.shapeLightParametricSides;
                    float angleOffset = Mathf.PI / 2.0f + Mathf.Deg2Rad * lt.shapeLightParametricAngleOffset;

                    if (sides < 3)
                        sides = 4;

                    if (sides == 4)
                        angleOffset = Mathf.PI / 4.0f + Mathf.Deg2Rad * lt.shapeLightParametricAngleOffset;
    
                    Vector3 startPoint = new Vector3(radius * Mathf.Cos(angleOffset), radius * Mathf.Sin(angleOffset), 0);
                    Vector3 featherStartPoint = (1 + lt.shapeLightFalloffSize * 2.0f) * startPoint;
                    float radiansPerSide = 2 * Mathf.PI / sides;
                    for (int i = 0; i < sides; i++)
                    {
                        float endAngle = (i + 1) * radiansPerSide;
                        Vector3 endPoint = new Vector3(radius * Mathf.Cos(endAngle + angleOffset), radius * Mathf.Sin(endAngle + angleOffset), 0);
                        Vector3 featherEndPoint = (1 + lt.shapeLightFalloffSize * 2.0f) * endPoint;


                        Handles.DrawLine(t.TransformPoint(startPoint + posOffset), t.TransformPoint(endPoint + posOffset));
                        Handles.DrawLine(t.TransformPoint(featherStartPoint + posOffset), t.TransformPoint(featherEndPoint + posOffset));

                        startPoint = endPoint;
                        featherStartPoint = featherEndPoint;
                    }
                }
                else  // Freeform light
                    m_ShapeEditor.OnGUI(target);
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
            _2DRendererData assetData = asset.rendererData as _2DRendererData; 
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
                        
                        updateMesh |= OnShapeLight((Light2D.LightType)m_LightType.intValue, updateMesh, serializedObject);
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
                    light.UpdateCookieSpriteMaterials();
                }
            }
        }
    }
}
