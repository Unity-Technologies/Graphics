using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.U2D.Shape;
using UnityEditor;
using UnityEditor.U2D;
using UnityEditor.U2D.Shape;
using UnityEditorInternal;
using System.Reflection;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEditor.Experimental.U2D.Common;
using UnityEngine.Experimental.UIElements;


namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [CustomEditor(typeof(Light2D))]
    [CanEditMultipleObjects]
    public class Light2DEditor : Editor
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

        SplineEditor m_SplineEditor;
        SplineSceneEditor m_SplineSceneEditor;

        bool m_ModifiedMesh = false;

        private Light2D lightObject { get { return target as Light2D; } }

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
            var light = target as Light2D;
            m_SplineEditor = new SplineEditor(this);
            m_SplineSceneEditor = new SplineSceneEditor(light.spline, this, light);
            //m_SplineSceneEditor.SplineEditMode = SplineSceneEditor.SplineEditModes.Buttonless;
        }

        private void OnDestroy()
        {
            if (m_SplineEditor != null)
                m_SplineEditor.OnDisable();
            if (m_SplineSceneEditor != null)
                m_SplineSceneEditor.OnDisable();

            ShapeEditorCache.ClearSelection();
        }

        private void OnPointLight(SerializedObject serializedObject)
        {
            SerializedProperty pointInnerAngle = serializedObject.FindProperty("m_PointLightInnerAngle");
            SerializedProperty pointOuterAngle = serializedObject.FindProperty("m_PointLightOuterAngle");
            SerializedProperty pointInnerRadius = serializedObject.FindProperty("m_PointLightInnerRadius");
            SerializedProperty pointOuterRadius = serializedObject.FindProperty("m_PointLightOuterRadius");
            SerializedProperty castsShadows = serializedObject.FindProperty("m_CastsShadows");
            SerializedProperty shadowColor = serializedObject.FindProperty("m_ShadowColor");
            SerializedProperty pointLightCookie = serializedObject.FindProperty("m_LightCookieSprite");

            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(pointInnerAngle, EditorGUIUtility.TrTextContent("Inner Angle", "Specify the inner angle of the light"));
            EditorGUILayout.PropertyField(pointOuterAngle, EditorGUIUtility.TrTextContent("Outer Angle", "Specify the outer angle of the light"));
            EditorGUILayout.PropertyField(pointInnerRadius, EditorGUIUtility.TrTextContent("Inner Radius", "Specify the inner radius of the light"));
            EditorGUILayout.PropertyField(pointOuterRadius, EditorGUIUtility.TrTextContent("Outer Radius", "Specify the outer radius of the light"));
            EditorGUILayout.PropertyField(pointLightCookie, EditorGUIUtility.TrTextContent("Cookie", "Specify a sprite as the cookie for the light"));
            if (pointInnerRadius.floatValue < 0) pointInnerRadius.floatValue = 0;
            if (pointOuterRadius.floatValue < 0) pointOuterRadius.floatValue = 0;

            EditorGUILayout.PropertyField(castsShadows, EditorGUIUtility.TrTextContent("Casts Shadows", "Specify if this light should casts shadows"));


            if (castsShadows.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(shadowColor, EditorGUIUtility.TrTextContent("Shadow Color", "Specify the shadow color of the light"));
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
        }

        private bool OnShapeLight(SerializedObject serializedObject)
        {
            bool updateMesh = false;
            SerializedProperty shapeLightStyle = serializedObject.FindProperty("m_ShapeLightStyle");
            SerializedProperty shapeLightType = serializedObject.FindProperty("m_ShapeLightType");
            SerializedProperty shapeLightFeathering = serializedObject.FindProperty("m_ShapeLightFeathering");
            SerializedProperty shapeLightParametricShape = serializedObject.FindProperty("m_ParametricShape");
            SerializedProperty shapeLightParametricSides = serializedObject.FindProperty("m_ParametricSides");
            SerializedProperty shapeLightOffset = serializedObject.FindProperty("m_ShapeLightOffset");
            SerializedProperty shapeLightSprite = serializedObject.FindProperty("m_LightCookieSprite");

            int prevShapeLightStyle = shapeLightStyle.intValue;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(shapeLightType, EditorGUIUtility.TrTextContent("Type", "Specify the shape light type"));
            EditorGUILayout.PropertyField(shapeLightStyle, EditorGUIUtility.TrTextContent("Cookie Style", "Specify the cookie style"));
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
                {
                    updateMesh = true;
                }

                int lastShape = shapeLightParametricShape.enumValueIndex;
                int lastSides = shapeLightParametricSides.intValue;
                float lastFeathering = shapeLightFeathering.floatValue;

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
                {
                    EditorGUILayout.PropertyField(shapeLightParametricSides, EditorGUIUtility.TrTextContent("Sides", "Adjust the shapes number of sides"));
                    if (shapeLightParametricSides.intValue < 3)
                        shapeLightParametricSides.intValue = 3;
                }

                EditorGUILayout.Slider(shapeLightFeathering, 0, 1, EditorGUIUtility.TrTextContent("Feathering", "Specify the shapes number of sides"));


                Vector2 lastOffset = shapeLightOffset.vector2Value;
                EditorGUILayout.PropertyField(shapeLightOffset, EditorGUIUtility.TrTextContent("Offset", "Specify the shape's offset"));

                // update the light meshes if either the sides or feathering has changed;
                updateMesh |= (lastSides != shapeLightParametricSides.intValue || lastFeathering != shapeLightFeathering.floatValue || lastOffset.x != shapeLightOffset.vector2Value.x || lastOffset.y != shapeLightOffset.vector2Value.y || lastShape != shapeLightParametricShape.enumValueIndex);
                EditorGUI.indentLevel--;
            }

            if (prevShapeLightStyle != shapeLightStyle.intValue)
                updateMesh = true; ;

            EditorGUI.indentLevel--;
            return updateMesh;
        }

        private Vector3 DrawAngleSlider2D(Transform transform, Quaternion rotation, float radius, float offset, Handles.CapFunction capFunc, float capSize, bool leftAngle, bool drawLine, ref float angle)
        {
            float angleBy2 = (angle / 2) * (leftAngle ? -1.0f : 1.0f);
            Vector3 trcwPos = Quaternion.AngleAxis(angleBy2, -transform.forward) * (transform.up);
            Vector3 cwPos = transform.position + trcwPos * (radius + offset);
            Vector3 cwHandle = Handles.Slider2D(cwPos, Vector3.forward, rotation * Vector3.up, rotation * Vector3.right, capSize, capFunc, Vector3.zero);
            Vector3 toCwHandle = (transform.position - cwHandle).normalized;
            if (drawLine)
                Handles.DrawLine(transform.position, cwHandle);

            if (GUIUtility.hotControl == GetLastControlId())
                angle = 360 - 2 * Quaternion.Angle(Quaternion.FromToRotation(transform.up, toCwHandle), Quaternion.identity);
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

            angle = Mathf.Round(angle * 100) / 100f;
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
            float iang = lt.m_PointLightInnerAngle + diff;
            lt.m_PointLightInnerAngle = iang > 0 ? iang : 0;
            DrawAngleHandle(lt.transform, lt.m_PointLightOuterRadius, -s_AngleCapOffset, TriCapTL, TriCapBL, ref lt.m_PointLightInnerAngle);
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
            var angle = 0.0f;
            Vector3 handlePos = Vector3.zero;
            Quaternion rotLeft = Quaternion.AngleAxis(0, -lt.transform.forward) * lt.transform.rotation;
            float handleOffset = HandleUtility.GetHandleSize(lt.transform.position) * s_AngleCapOffsetSecondary;
            float handleSize = HandleUtility.GetHandleSize(lt.transform.position) * s_AngleCapSize;

            var oldColor = Handles.color;
            Handles.color = Color.yellow;

            float outerRadius = lt.m_PointLightOuterRadius;
            Vector3 returnPos = DrawAngleSlider2D(lt.transform, rotLeft, outerRadius, -handleOffset, SemiCircleCapUC, handleSize, false, false, ref angle);
            if (GUIUtility.hotControl == GetLastControlId())
            {
                var vec = (returnPos - lt.transform.position).normalized;
                lt.transform.up = new Vector3(vec.x, vec.y, 0);
                outerRadius = (returnPos - lt.transform.position).magnitude;
                outerRadius = outerRadius + handleOffset;
            }
            DrawRadiusArc(lt.transform, lt.m_PointLightOuterRadius, lt.m_PointLightOuterAngle, 0, s_RangeCapFunction, s_RangeCapSize, false);

            Handles.color = Color.gray;
            float innerRadius = lt.m_PointLightInnerRadius;
            returnPos = DrawAngleSlider2D(lt.transform, rotLeft, innerRadius, handleOffset, SemiCircleCapDC, handleSize, true, false, ref angle);
            if (GUIUtility.hotControl == GetLastControlId())
            {
                innerRadius = (returnPos - lt.transform.position).magnitude;
                innerRadius = innerRadius - handleOffset;
            }
            DrawRadiusArc(lt.transform, lt.m_PointLightInnerRadius, lt.m_PointLightOuterAngle, 0, s_InnerRangeCapFunction, s_InnerRangeCapSize, false);

            Handles.color = oldColor;

            lt.m_PointLightInnerRadius = (outerRadius < innerRadius) ? outerRadius : innerRadius;
            lt.m_PointLightOuterRadius = (innerRadius > outerRadius) ? innerRadius : outerRadius;
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

                    if (lt.m_ParametricShape == Light2D.ParametricShapes.Freeform)
                    {
                        m_SplineSceneEditor.CalculateBounds();

                        EditorGUI.BeginChangeCheck();

                        if (EditMode.IsOwner(this))
                        {
                            m_SplineSceneEditor.OnSceneGUI();
                            m_SplineEditor.HandleHotKeys();
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorUtility.SetDirty(lightObject);
                            m_ModifiedMesh = true;
                            //lightObject.UpdateShapeLightMesh();
                        }
                    }
                    else
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

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space();

            serializedObject.Update();

            SerializedProperty lightProjectionType = serializedObject.FindProperty("m_LightProjectionType");
            SerializedProperty lightColor = serializedObject.FindProperty("m_LightColor");
            SerializedProperty applyToLayers = serializedObject.FindProperty("m_ApplyToLayers");

            bool updateMesh = false;
            EditorGUILayout.PropertyField(lightProjectionType, EditorGUIUtility.TrTextContent("Light Type", "Specify the light type"));
            switch (lightProjectionType.intValue)
            {
                case (int)Light2D.LightProjectionTypes.Point:
                    {
                        OnPointLight(serializedObject);
                    }
                    break;
                case (int)Light2D.LightProjectionTypes.Shape:
                    {
                        updateMesh = OnShapeLight(serializedObject);
                    }
                    break;
            }

            Color previousColor = lightColor.colorValue;
            EditorGUILayout.PropertyField(lightColor, EditorGUIUtility.TrTextContent("Light Color", "Specify the light color"));
            updateMesh = updateMesh || (previousColor != lightColor.colorValue);

            InternalEditorBridge.SortingLayerField(EditorGUIUtility.TrTextContent("Target Sorting Layer", "Apply this light to the specifed layer"), applyToLayers, EditorStyles.popup, EditorStyles.label);

            if (lightObject.m_ParametricShape == Light2D.ParametricShapes.Freeform && lightObject.LightProjectionType == Light2D.LightProjectionTypes.Shape && lightObject.m_ShapeLightStyle != Light2D.CookieStyles.Sprite)
            {
                m_SplineSceneEditor.OnInspectorGUI();
                m_SplineEditor.OnInspectorGUI(lightObject.spline);
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.LabelField("");
            // EditorGUILayout.LabelField("Hold SHIFT to preview lighting");

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

        // Use Internal Bridge Later.
        public static FieldInfo LastControlIdField = typeof(EditorGUIUtility).GetField("s_LastControlID", BindingFlags.Static | BindingFlags.NonPublic);
        public static int GetLastControlId()
        {
            if (LastControlIdField == null)
            {
                Debug.LogError("Compatibility with Unity broke: can't find lastControlId field in EditorGUI");
                return 0;
            }
            return (int)LastControlIdField.GetValue(null);
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

    }
}
