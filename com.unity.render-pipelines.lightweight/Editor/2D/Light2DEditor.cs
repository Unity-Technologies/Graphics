using UnityEngine;
using UnityEngine.Experimental.Rendering.LWRP;
using UnityEngine.Rendering.LWRP;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using Unity.Path2D;

namespace UnityEditor.Experimental.Rendering.LWRP
{
    [EditorTool("Edit Light Shape", typeof(Light2D))]
    public class ShapeLightTool : ShapeEditorTool<DefaultShapeEditor, Light2DEditor> { }

    [CustomEditor(typeof(Light2D))]
    [CanEditMultipleObjects]
    public class Light2DEditor : ShapeProviderEditor
    {
        static string k_TexturePath = "Textures/";

        string[] m_LayerNames;

        private class Styles
        {
            public Texture lt_tr;
            public Texture lt_tl;
            public Texture lt_bl;
            public Texture lt_br;
            public Texture lt_uc;
            public Texture lt_dr;
        }

        private static Styles s_Styles;
        private static Styles styles
        {
            get
            {
                if (s_Styles == null)
                    s_Styles = new Styles();

                return s_Styles;
            }
        }

        static float s_GlobalLightGizmoSize = 1.2f;
        static float s_AngleCapSize = 0.16f * s_GlobalLightGizmoSize;
        static float s_AngleCapOffset = 0.08f * s_GlobalLightGizmoSize;
        static float s_AngleCapOffsetSecondary = -0.05f;
        //static Handles.CapFunction s_AngleCapFunction = Handles.ConeHandleCap;

        //static float s_RotationCapSize = 0.05f * s_GlobalLightGizmoSize;
        //static Handles.CapFunction s_RotationCapFunction = Handles.CircleHandleCap;

        static float s_RangeCapSize = 0.025f * s_GlobalLightGizmoSize;
        static Handles.CapFunction s_RangeCapFunction = Handles.DotHandleCap;

        static float s_InnerRangeCapSize = 0.08f * s_GlobalLightGizmoSize;
        static Handles.CapFunction s_InnerRangeCapFunction = Handles.SphereHandleCap;

        SerializedProperty m_LightProjectionType;
        SerializedProperty m_LightColor;
        SerializedProperty m_ApplyToSortingLayers;
        SerializedProperty m_VolumetricAlpha;
        SerializedProperty m_LightOperation;

        bool m_ModifiedMesh = false;

        int[] m_LightOperationIndices;
        GUIContent[] m_LightOperationNames;
        bool m_AnyLightOperationEnabled = false;

        private Light2D lightObject { get { return target as Light2D; } }

        private Rect m_SortingLayerDropdownRect = new Rect();
        private SortingLayer[] m_AllSortingLayers;
        private GUIContent[] m_AllSortingLayerNames;
        private List<int> m_ApplyToSortingLayersList;

        #region Handle Utilities

        public static void TriCapTR(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (styles.lt_tr == null)
                styles.lt_tr = Resources.Load<Texture>(k_TexturePath + "lt_tr");
            Light2DEditorUtility.GUITextureCap(controlID, styles.lt_tr, position, rotation, size, eventType);
        }

        public static void TriCapTL(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (styles.lt_tl == null)
                styles.lt_tl = Resources.Load<Texture>(k_TexturePath + "lt_tl");
            Light2DEditorUtility.GUITextureCap(controlID, styles.lt_tl, position, rotation, size, eventType);
        }

        public static void TriCapBR(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (styles.lt_br == null)
                styles.lt_br = Resources.Load<Texture>(k_TexturePath + "lt_br");
            Light2DEditorUtility.GUITextureCap(controlID, styles.lt_br, position, rotation, size, eventType);
        }

        public static void TriCapBL(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (styles.lt_bl == null)
                styles.lt_bl = Resources.Load<Texture>(k_TexturePath + "lt_bl");
            Light2DEditorUtility.GUITextureCap(controlID, styles.lt_bl, position, rotation, size, eventType);
        }

        public static void SemiCircleCapUC(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (styles.lt_uc == null)
                styles.lt_uc = Resources.Load<Texture>(k_TexturePath + "lt_uc");
            Light2DEditorUtility.GUITextureCap(controlID, styles.lt_uc, position, rotation, size, eventType);
        }

        public static void SemiCircleCapDC(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (styles.lt_dr == null)
                styles.lt_dr = Resources.Load<Texture>(k_TexturePath + "lt_dr");
            Light2DEditorUtility.GUITextureCap(controlID, styles.lt_dr, position, rotation, size, eventType);
        }

        #endregion

        private void OnEnable()
        {
            m_LightProjectionType = serializedObject.FindProperty("m_LightProjectionType");
            m_LightColor = serializedObject.FindProperty("m_LightColor");
            m_ApplyToSortingLayers = serializedObject.FindProperty("m_ApplyToSortingLayers");
            m_VolumetricAlpha = serializedObject.FindProperty("m_LightVolumeOpacity");

            m_LightOperation = serializedObject.FindProperty("m_LightOperation");

            var light = target as Light2D;

            m_AnyLightOperationEnabled = false;
            var lightOperationIndices = new List<int>();
            var lightOperationNames = new List<string>();

            var pipelineAsset = UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset as LightweightRenderPipelineAsset;
            var rendererData = pipelineAsset != null ? pipelineAsset.rendererData as Default2DRendererData : null;
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
            SerializedProperty pointInnerAngle = serializedObject.FindProperty("m_PointLightInnerAngle");
            SerializedProperty pointOuterAngle = serializedObject.FindProperty("m_PointLightOuterAngle");
            SerializedProperty pointInnerRadius = serializedObject.FindProperty("m_PointLightInnerRadius");
            SerializedProperty pointOuterRadius = serializedObject.FindProperty("m_PointLightOuterRadius");
            SerializedProperty pointZDistance = serializedObject.FindProperty("m_PointLightZDistance");
            SerializedProperty pointLightCookie = serializedObject.FindProperty("m_LightCookieSprite");
            SerializedProperty lightQuality = serializedObject.FindProperty("m_LightQuality");

            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(lightQuality, EditorGUIUtility.TrTextContent("Quality", "Use accurate if there are noticable visual issues"));

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Slider(pointInnerAngle, 0, 360, EditorGUIUtility.TrTextContent("Inner Angle", "Specify the inner angle of the light"));
            if (EditorGUI.EndChangeCheck())
                pointInnerAngle.floatValue = Mathf.Min(pointInnerAngle.floatValue, pointOuterAngle.floatValue);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Slider(pointOuterAngle, 0, 360, EditorGUIUtility.TrTextContent("Outer Angle", "Specify the outer angle of the light"));
            if (EditorGUI.EndChangeCheck())
                pointOuterAngle.floatValue = Mathf.Max(pointInnerAngle.floatValue, pointOuterAngle.floatValue);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(pointInnerRadius, EditorGUIUtility.TrTextContent("Inner Radius", "Specify the inner radius of the light"));
            if (EditorGUI.EndChangeCheck())
                pointInnerRadius.floatValue = Mathf.Min(pointInnerRadius.floatValue, pointOuterRadius.floatValue);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(pointOuterRadius, EditorGUIUtility.TrTextContent("Outer Radius", "Specify the outer radius of the light"));
            if (EditorGUI.EndChangeCheck())
                pointOuterRadius.floatValue = Mathf.Max(pointInnerRadius.floatValue, pointOuterRadius.floatValue);

            EditorGUILayout.PropertyField(pointZDistance, EditorGUIUtility.TrTextContent("Distance", "Specify the Z Distance of the light"));
            EditorGUILayout.PropertyField(pointLightCookie, EditorGUIUtility.TrTextContent("Cookie", "Specify a sprite as the cookie for the light"));
            if (pointInnerRadius.floatValue < 0) pointInnerRadius.floatValue = 0;
            if (pointOuterRadius.floatValue < 0) pointOuterRadius.floatValue = 0;
            if (pointZDistance.floatValue < 0) pointZDistance.floatValue = 0;

            EditorGUI.indentLevel--;
        }

        private bool OnShapeLight(SerializedObject serializedObject)
        {
            if (!m_AnyLightOperationEnabled)
            {
                EditorGUILayout.HelpBox("No valid Shape Light type is defined.", MessageType.Error);
                return false;
            }

            bool updateMesh = false;

            SerializedProperty shapeLightStyle = serializedObject.FindProperty("m_ShapeLightStyle");
            SerializedProperty shapeLightFeathering = serializedObject.FindProperty("m_ShapeLightFeathering");
            SerializedProperty shapeLightParametricShape = serializedObject.FindProperty("m_ParametricShape");
            SerializedProperty shapeLightParametricSides = serializedObject.FindProperty("m_ParametricSides");
            SerializedProperty shapeLightOffset = serializedObject.FindProperty("m_ShapeLightOffset");
            SerializedProperty shapeLightSprite = serializedObject.FindProperty("m_LightCookieSprite");
            SerializedProperty shapeLightOrder = serializedObject.FindProperty("m_ShapeLightOrder");
            SerializedProperty shapeLightBlending = serializedObject.FindProperty("m_ShapeLightBlending");

            int prevShapeLightStyle = shapeLightStyle.intValue;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(shapeLightStyle, EditorGUIUtility.TrTextContent("Cookie Style", "Specify the cookie style"));
            EditorGUILayout.PropertyField(shapeLightBlending, EditorGUIUtility.TrTextContent("Blending Mode", "Specify the lights blending mode"));

            if (shapeLightStyle.intValue == (int)Light2D.CookieStyles.Sprite)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(shapeLightSprite, EditorGUIUtility.TrTextContent("Sprite", "Specify the sprite"));
                updateMesh |= EditorGUI.EndChangeCheck();
                EditorGUI.indentLevel--;
            }
            else if (shapeLightStyle.intValue == (int)Light2D.CookieStyles.Parametric)
            {
                EditorGUI.indentLevel++;

                if (m_ModifiedMesh)
                    updateMesh = true;

                int lastShape = shapeLightParametricShape.enumValueIndex;
                EditorGUILayout.PropertyField(shapeLightParametricShape, EditorGUIUtility.TrTextContent("Shape", "Specify the shape"));
                int shape = shapeLightParametricShape.enumValueIndex;
                if (lastShape != shape)
                {
                    int sides = shapeLightParametricSides.intValue;
                    if (shape == (int)Light2D.ParametricShapes.Circle) sides = 128;
                    else if (shape == (int)Light2D.ParametricShapes.Freeform) sides = 4; // This one should depend on if this has data at the moment
                    shapeLightParametricSides.intValue = sides;
                }

                m_ModifiedMesh = false;

                if (shapeLightParametricShape.enumValueIndex == (int)Light2D.ParametricShapes.Circle)
                    EditorGUILayout.IntSlider(shapeLightParametricSides, 3, 128, EditorGUIUtility.TrTextContent("Sides", "Adjust the shapes number of sides"));

                EditorGUILayout.Slider(shapeLightFeathering, 0, 1, EditorGUIUtility.TrTextContent("Feathering", "Specify the shapes number of sides"));
                Vector2 lastOffset = shapeLightOffset.vector2Value;
                EditorGUILayout.PropertyField(shapeLightOffset, EditorGUIUtility.TrTextContent("Offset", "Specify the shape's offset"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(shapeLightOrder, EditorGUIUtility.TrTextContent("Light Order", "Shape light order"));

            EditorGUI.indentLevel--;

            return updateMesh;
        }

        private void OnTargetSortingLayers()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(EditorGUIUtility.TrTextContent("Target Sorting Layers", "Apply this light to the specified sorting layers."));

            GUIContent selectedLayers;
            if (m_ApplyToSortingLayersList.Count == 1)
                selectedLayers = new GUIContent(SortingLayer.IDToName(m_ApplyToSortingLayersList[0]));
            else if (m_ApplyToSortingLayersList.Count == m_AllSortingLayers.Length)
                selectedLayers = EditorGUIUtility.TrTempContent("All");
            else if (m_ApplyToSortingLayersList.Count == 0)
                selectedLayers = EditorGUIUtility.TrTempContent("None");
            else
                selectedLayers = EditorGUIUtility.TrTempContent("Mixed...");

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

            float diff = DrawAngleHandle(lt.transform, lt.m_PointLightOuterRadius, s_AngleCapOffset, TriCapTR, TriCapBR, ref lt.m_PointLightOuterAngle);

            if (diff != 0.0f)
                lt.m_PointLightInnerAngle = Mathf.Max(0.0f, lt.m_PointLightInnerAngle + diff);

            diff = DrawAngleHandle(lt.transform, lt.m_PointLightOuterRadius, -s_AngleCapOffset, TriCapTL, TriCapBL, ref lt.m_PointLightInnerAngle);

            if (diff != 0.0f)
                lt.m_PointLightInnerAngle = lt.m_PointLightInnerAngle < lt.m_PointLightOuterAngle ? lt.m_PointLightInnerAngle : lt.m_PointLightOuterAngle;

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

            float outerRadius = lt.m_PointLightOuterRadius;
            EditorGUI.BeginChangeCheck();
            Vector3 returnPos = DrawAngleSlider2D(lt.transform, rotLeft, outerRadius, -handleOffset, SemiCircleCapUC, handleSize, false, false, ref dummy);
            if (EditorGUI.EndChangeCheck())
            {
                var vec = (returnPos - lt.transform.position).normalized;
                lt.transform.up = new Vector3(vec.x, vec.y, 0);
                outerRadius = (returnPos - lt.transform.position).magnitude;
                outerRadius = outerRadius + handleOffset;
                radiusChanged = true;
            }
            DrawRadiusArc(lt.transform, lt.m_PointLightOuterRadius, lt.m_PointLightOuterAngle, 0, s_RangeCapFunction, s_RangeCapSize, false);

            Handles.color = Color.gray;
            float innerRadius = lt.m_PointLightInnerRadius;
            EditorGUI.BeginChangeCheck();
            returnPos = DrawAngleSlider2D(lt.transform, rotLeft, innerRadius, handleOffset, SemiCircleCapDC, handleSize, true, false, ref dummy);
            if (EditorGUI.EndChangeCheck())
            {
                innerRadius = (returnPos - lt.transform.position).magnitude;
                innerRadius = innerRadius - handleOffset;
                radiusChanged = true;
            }
            DrawRadiusArc(lt.transform, lt.m_PointLightInnerRadius, lt.m_PointLightOuterAngle, 0, s_InnerRangeCapFunction, s_InnerRangeCapSize, false);

            Handles.color = oldColor;

            if (radiusChanged)
            {
                lt.m_PointLightInnerRadius = (outerRadius < innerRadius) ? outerRadius : innerRadius;
                lt.m_PointLightOuterRadius = (innerRadius > outerRadius) ? innerRadius : outerRadius;
            }
            
            Handles.color = handleColor;
        }

        protected virtual void OnSceneGUI()
        {
            var lt = target as Light2D;

            if (lt.LightProjectionType == Light2D.LightProjectionTypes.Point)
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
                Vector3 posOffset = lt.m_ShapeLightOffset;

                if (lt.m_ShapeLightStyle == Light2D.CookieStyles.Sprite)
                {
                    Vector3 v0 = t.TransformPoint(new Vector3(-0.5f, -0.5f));
                    Vector3 v1 = t.TransformPoint(new Vector3(0.5f, -0.5f));
                    Vector3 v2 = t.TransformPoint(new Vector3(0.5f, 0.5f));
                    Vector3 v3 = t.TransformPoint(new Vector3(-0.5f, 0.5f));
                    Handles.DrawLine(v0, v1);
                    Handles.DrawLine(v1, v2);
                    Handles.DrawLine(v2, v3);
                    Handles.DrawLine(v3, v0);
                }
                else
                {
                    float radius = 0.5f;
                    float sides = lt.m_ParametricSides;
                    float angleOffset = Mathf.PI / 2.0f;

                    if (lt.m_ParametricShape == Light2D.ParametricShapes.Circle)
                    {
                        if (sides < 2)
                        {
                            sides = 4;
                            angleOffset = Mathf.PI / 4.0f;
                            radius = radius * 0.70710678118654752440084436210485f;

                        }

                        Vector3 startPoint = new Vector3(radius * Mathf.Cos(angleOffset), radius * Mathf.Sin(angleOffset), 0);
                        Vector3 featherStartPoint = (1 - lt.m_ShapeLightFeathering) * startPoint;
                        float radiansPerSide = 2 * Mathf.PI / sides;
                        for (int i = 0; i < sides; i++)
                        {
                            float endAngle = (i + 1) * radiansPerSide;
                            Vector3 endPoint = new Vector3(radius * Mathf.Cos(endAngle + angleOffset), radius * Mathf.Sin(endAngle + angleOffset), 0);
                            Vector3 featherEndPoint = (1 - lt.m_ShapeLightFeathering) * endPoint;


                            Handles.DrawLine(t.TransformPoint(startPoint + posOffset), t.TransformPoint(endPoint + posOffset));
                            Handles.DrawLine(t.TransformPoint(featherStartPoint + posOffset), t.TransformPoint(featherEndPoint + posOffset));

                            startPoint = endPoint;
                            featherStartPoint = featherEndPoint;
                        }
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            bool updateMesh = false;
            

            EditorGUILayout.Space();

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_LightProjectionType, EditorGUIUtility.TrTextContent("Light Type", "Specify the light type"));
            updateMesh |= EditorGUI.EndChangeCheck();

            switch (m_LightProjectionType.intValue)
            {
                case (int)Light2D.LightProjectionTypes.Point:
                    {
                        OnPointLight(serializedObject);
                    }
                    break;
                case (int)Light2D.LightProjectionTypes.Shape:
                    {
                        
                        updateMesh |= OnShapeLight(serializedObject);
                    }
                    break;
            }

            Color previousColor = m_LightColor.colorValue;
            EditorGUILayout.IntPopup(m_LightOperation, m_LightOperationNames, m_LightOperationIndices, EditorGUIUtility.TrTextContent("Light Operation", "Specify the shape light type"));
            EditorGUILayout.PropertyField(m_LightColor, EditorGUIUtility.TrTextContent("Light Color", "Specify the light color"));
            EditorGUILayout.Slider(m_VolumetricAlpha, 0, 1, EditorGUIUtility.TrTextContent("Light Volume Opacity", "Specify the light color"));

            OnTargetSortingLayers();

            if (lightObject.m_ParametricShape == Light2D.ParametricShapes.Freeform && lightObject.LightProjectionType == Light2D.LightProjectionTypes.Shape && lightObject.m_ShapeLightStyle != Light2D.CookieStyles.Sprite)
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
                    light.UpdateMaterial();
                }
            }
        }

        [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.Selected | GizmoType.Pickable)]
        static void RenderSpline(Light2D light, GizmoType gizmoType)
        {
            if (light.m_ParametricShape == Light2D.ParametricShapes.Freeform && light.LightProjectionType == Light2D.LightProjectionTypes.Shape && light.m_ShapeLightStyle != Light2D.CookieStyles.Sprite)
            {
                UnityEngine.U2D.Shape.Spline m_Spline = light.spline;
                Matrix4x4 oldMatrix = Handles.matrix;
                Handles.matrix = light.transform.localToWorldMatrix;
                int points = m_Spline.GetPointCount();
                for (int i = 0; i < (m_Spline.isOpenEnded ? points - 1 : points); i++)
                {
                    Vector3 p1 = m_Spline.GetPosition(i);
                    Vector3 p2 = m_Spline.GetPosition((i + 1) % points);
                    var t1 = p1 + m_Spline.GetRightTangent(i);
                    var t2 = p2 + m_Spline.GetLeftTangent((i + 1) % points);
                    Handles.DrawBezier(p1, p2, t1, t2, Color.gray, null, 2f);
                }
                Handles.matrix = oldMatrix;
            }
        }

        protected override IShape GetShape(Object target)
        {
            var component = target as Light2D;
            return new Polygon() { controlPoints = component.spline.m_ControlPoints.Select(x => x.position).ToArray() };
        }

        protected override void SetShape(IShapeEditor shapeEditor, SerializedObject serializedObject)
        {
            serializedObject.Update();

            var pointsProperty = serializedObject.FindProperty("m_Spline.m_ControlPoints");
            pointsProperty.arraySize = shapeEditor.pointCount;

            for (var i = 0; i < shapeEditor.pointCount; ++i)
                pointsProperty.GetArrayElementAtIndex(i).FindPropertyRelative("position").vector3Value = shapeEditor.GetPoint(i).position;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
