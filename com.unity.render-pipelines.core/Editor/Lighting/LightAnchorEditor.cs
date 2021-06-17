using System;
using System.Linq;
using UnityEngine;
using UnityEditor.EditorTools;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace UnityEditor
{
    /// <summary>
    /// LightAnchorEditor represent the inspector for the LightAnchor
    /// </summary>
    [CustomEditor(typeof(LightAnchor))]
    public class LightAnchorEditor : Editor
    {
        float m_Yaw;
        float m_Pitch;
        float m_Roll;
        float m_Distance;
        LightAnchor.UpDirection m_FrameSpace;

        // used for cache invalidation
        Vector3 m_CamToLight;
        float m_CamLightForwardDot;
        float m_CamLightRightDot;

        bool m_EnableClickCatcher = false;
        bool m_FoldoutPreset = true;

        VisualElement m_GameViewRootElement;
        VisualElement m_ClickCatcher;

        LightAnchor manipulator
        {
            get { return target as LightAnchor; }
        }

        /// <summary>
        /// Calls the methods in its invocation list when show the Inspector
        /// </summary>
        public override void OnInspectorGUI()
        {
            var camera = Camera.main;

            if (camera == null)
            {
                Debug.LogError("Light Anchor: At least one camera must be tagged as MainCamera");
                return;
            }

            if (IsCacheInvalid(manipulator))
            {
                manipulator.SynchronizeOnTransform(camera);
                UpdateCache();
            }

            // anchor is cached for it cannot be changed from the inspector,
            // we have a dedicated editor tool to move the anchor
            var anchor = manipulator.anchorPosition;

            bool yawChanged = false;
            bool pitchChanged = false;
            bool rollChanged = false;
            bool distanceChanged = false;
            bool upChanged = false;

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.Space();

                float widgetHeight = EditorGUIUtility.singleLineHeight * 5f;

                using (new EditorGUILayout.HorizontalScope())
                {
                    Color usedColor;
                    EditorGUI.BeginChangeCheck();
                    {
                        var localRect = EditorGUILayout.GetControlRect(false, widgetHeight);
                        usedColor = Color.green;
                        usedColor.a = 0.2f;
                        m_Yaw = AngleField(localRect, "Yaw", m_Yaw, 90, usedColor, true);
                    }
                    yawChanged = EditorGUI.EndChangeCheck();
                    EditorGUI.BeginChangeCheck();
                    {
                        var localRect = EditorGUILayout.GetControlRect(false, widgetHeight);
                        usedColor = Color.blue;
                        usedColor.a = 0.2f;
                        m_Pitch = AngleField(localRect, "Pitch", m_Pitch, 180, usedColor, true);
                    }
                    pitchChanged = EditorGUI.EndChangeCheck();
                    EditorGUI.BeginChangeCheck();
                    {
                        var localRect = EditorGUILayout.GetControlRect(false, widgetHeight);
                        usedColor = Color.grey;
                        usedColor.a = 0.2f;
                        bool enabledKnob = true;
                        m_Roll = AngleField(localRect, "Roll", m_Roll, -90, usedColor, enabledKnob);
                    }
                    rollChanged = EditorGUI.EndChangeCheck();
                }
                EditorGUILayout.Space();
                Rect angleRect = EditorGUILayout.GetControlRect(true, EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector3, EditorGUIUtility.TrTextContent("")));
                float[] angles = new float[3] { m_Yaw, m_Pitch, m_Roll };
                EditorGUI.BeginChangeCheck();
                EditorGUI.MultiFloatField(angleRect, LightAnchorStyles.angleSubContent, angles);
                const float eps = 1e-4f;
                if (EditorGUI.EndChangeCheck())
                {
                    if (Mathf.Abs(angles[0] - m_Yaw) > eps)
                    {
                        m_Yaw = angles[0];
                        yawChanged = true;
                    }
                    if (Mathf.Abs(angles[1] - m_Pitch) > eps)
                    {
                        m_Pitch = angles[1];
                        pitchChanged = true;
                    }
                    if (Mathf.Abs(angles[2] - m_Roll) > eps)
                    {
                        m_Roll = angles[2];
                        rollChanged = true;
                    }
                }
                EditorGUILayout.Space();

                EditorGUI.BeginChangeCheck();
                m_Distance = EditorGUILayout.FloatField(LightAnchorStyles.distanceProperty, manipulator.distance);
                distanceChanged = EditorGUI.EndChangeCheck();

                EditorGUI.BeginChangeCheck();
                var dropRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                m_FrameSpace = (LightAnchor.UpDirection)EditorGUI.EnumPopup(dropRect, LightAnchorStyles.upDirectionProperty, manipulator.frameSpace);
                upChanged = EditorGUI.EndChangeCheck();


                if (m_FoldoutPreset = EditorGUILayout.Foldout(m_FoldoutPreset, "Common"))
                {
                    Color cachedColor = GUI.backgroundColor;
                    GUI.backgroundColor = LightAnchorStyles.BackgroundIconColor();
                    var inspectorWidth = EditorGUIUtility.currentViewWidth - LightAnchorStyles.inspectorWidthPadding;
                    var presetButtonWidth = GUILayout.Width(inspectorWidth / LightAnchorStyles.presetButtonCount);
                    var presetButtonHeight = GUILayout.Height(inspectorWidth / LightAnchorStyles.presetButtonCount);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        bool rectFound = false;
                        Rect rect = new Rect();
                        if (GUILayout.Button(LightAnchorStyles.presetTextureRimLeft, presetButtonWidth, presetButtonHeight))
                        {
                            m_Yaw = 135;
                            m_Pitch = 0;
                            yawChanged = true;
                            pitchChanged = true;
                        }
                        if (Mathf.Abs(m_Yaw - 135.0f) < eps && Mathf.Abs(m_Pitch - 0.0f) < eps)
                        {
                            rect = GUILayoutUtility.GetLastRect();
                            rectFound = true;
                        }
                        if (GUILayout.Button(LightAnchorStyles.presetTextureKickLeft, presetButtonWidth, presetButtonHeight))
                        {
                            m_Yaw = 100;
                            m_Pitch = 10;
                            yawChanged = true;
                            pitchChanged = true;
                        }
                        if (Mathf.Abs(m_Yaw - 100.0f) < eps && Mathf.Abs(m_Pitch - 10.0f) < eps)
                        {
                            rect = GUILayoutUtility.GetLastRect();
                            rectFound = true;
                        }
                        if (GUILayout.Button(LightAnchorStyles.presetTextureBounceLeft, presetButtonWidth, presetButtonHeight))
                        {
                            m_Yaw = 30;
                            m_Pitch = -30;
                            yawChanged = true;
                            pitchChanged = true;
                        }
                        if (Mathf.Abs(m_Yaw - 30.0f) < eps && Mathf.Abs(m_Pitch + 30.0f) < eps)
                        {
                            rect = GUILayoutUtility.GetLastRect();
                            rectFound = true;
                        }
                        if (GUILayout.Button(LightAnchorStyles.presetTextureFillLeft, presetButtonWidth, presetButtonHeight))
                        {
                            m_Yaw = 35;
                            m_Pitch = 35;
                            yawChanged = true;
                            pitchChanged = true;
                        }
                        if (Mathf.Abs(m_Yaw - 35.0f) < eps && Mathf.Abs(m_Pitch - 35.0f) < eps)
                        {
                            rect = GUILayoutUtility.GetLastRect();
                            rectFound = true;
                        }
                        if (GUILayout.Button(LightAnchorStyles.presetTextureHair, presetButtonWidth, presetButtonHeight))
                        {
                            m_Yaw = 0;
                            m_Pitch = 110;
                            yawChanged = true;
                            pitchChanged = true;
                        }
                        if (Mathf.Abs(m_Yaw - 0.0f) < eps && Mathf.Abs(m_Pitch - 110.0f) < eps)
                        {
                            rect = GUILayoutUtility.GetLastRect();
                            rectFound = true;
                        }
                        if (GUILayout.Button(LightAnchorStyles.presetTextureFillRight, presetButtonWidth, presetButtonHeight))
                        {
                            m_Yaw = -35;
                            m_Pitch = 35;
                            yawChanged = true;
                            pitchChanged = true;
                        }
                        if (Mathf.Abs(m_Yaw + 35.0f) < eps && Mathf.Abs(m_Pitch - 35.0f) < eps)
                        {
                            rect = GUILayoutUtility.GetLastRect();
                            rectFound = true;
                        }
                        if (GUILayout.Button(LightAnchorStyles.presetTextureBounceRight, presetButtonWidth, presetButtonHeight))
                        {
                            m_Yaw = -30;
                            m_Pitch = -30;
                            yawChanged = true;
                            pitchChanged = true;
                        }
                        if (Mathf.Abs(m_Yaw + 30.0f) < eps && Mathf.Abs(m_Pitch + 30.0f) < eps)
                        {
                            rect = GUILayoutUtility.GetLastRect();
                            rectFound = true;
                        }
                        if (GUILayout.Button(LightAnchorStyles.presetTextureKickRight, presetButtonWidth, presetButtonHeight))
                        {
                            m_Yaw = -100;
                            m_Pitch = 10;
                            yawChanged = true;
                            pitchChanged = true;
                        }
                        if (Mathf.Abs(m_Yaw + 100.0f) < eps && Mathf.Abs(m_Pitch - 10.0f) < eps)
                        {
                            rect = GUILayoutUtility.GetLastRect();
                            rectFound = true;
                        }
                        if (GUILayout.Button(LightAnchorStyles.presetTextureRimRight, presetButtonWidth, presetButtonHeight))
                        {
                            m_Yaw = -135;
                            m_Pitch = 0;
                            yawChanged = true;
                            pitchChanged = true;
                        }
                        if (Mathf.Abs(m_Yaw + 135.0f) < eps && Mathf.Abs(m_Pitch - 0.0f) < eps)
                        {
                            rect = GUILayoutUtility.GetLastRect();
                            rectFound = true;
                        }
                        if (rectFound)
                        {
                            Handles.DrawSolidRectangleWithOutline(rect, LightAnchorStyles.totalTransparentColor, LightAnchorStyles.hoverColor);
                        }
                        GUILayout.FlexibleSpace();
                    }
                    GUI.backgroundColor = cachedColor;
                }

                if (upChanged)
                {
                    Undo.RecordObjects(new UnityEngine.Object[] { target, manipulator.transform }, "Light Anchor Change");

                    manipulator.frameSpace = m_FrameSpace;
                    manipulator.SynchronizeOnTransform(camera);
                    UpdateCache();
                }
                if (yawChanged || pitchChanged || rollChanged || distanceChanged)
                {
                    Undo.RecordObjects(new UnityEngine.Object[] { target, manipulator.transform }, "Light Anchor Change");

                    if (yawChanged)
                        manipulator.yaw = m_Yaw;
                    if (pitchChanged)
                        manipulator.pitch = m_Pitch;
                    if (rollChanged)
                        manipulator.roll = m_Roll;
                    if (distanceChanged)
                        manipulator.distance = m_Distance;

                    manipulator.UpdateTransform(camera, anchor);
                    IsCacheInvalid(manipulator);
                }
            }
        }

        void OnEnable()
        {
            UpdateCache();

            // Setup event to keep track of EditorTool state
            ToolManager.activeToolChanged += EditorToolsOnactiveToolChanged;

            // Get a reference to the GameView
            // TODO: This currently only works with a single Game View
            var gameViews = Resources.FindObjectsOfTypeAll<EditorWindow>()
                .Where(w => w.GetType().FullName == "UnityEditor.GameView")
                .ToArray();
            var gameView = gameViews.First();

            // We create an invisible VisualElement that gets overlayed onto the Game View to intercept mouse clicks
            // when the editor tool is active
            m_GameViewRootElement = gameView.rootVisualElement;
            m_ClickCatcher = new VisualElement();
            m_ClickCatcher.style.flexGrow = 1;

            // Determine if the editor tool is already active (this helps keep our state when swapping between
            // different light objects directly, or after an assembly reload)
            var anchorTools = Resources.FindObjectsOfTypeAll<LightAnchorEditorTool>()
                .ToArray();
            if (anchorTools.Length > 0)
            {
                var anchorTool = anchorTools[0];
                if (anchorTool != null)
                {
                    EnableClickCatcher(m_EnableClickCatcher);
                }
            }
        }

        void EditorToolsOnactiveToolChanged()
        {
            // When the active editor tool changes we need to either enable or disable our click catcher
            EnableClickCatcher(ToolManager.activeToolType == typeof(LightAnchorEditorTool));
        }

        void OnDisable()
        {
            // cleanup our click catcher element
            m_ClickCatcher?.RemoveFromHierarchy();

            // unsubscribe from EditorTool event
            ToolManager.activeToolChanged -= EditorToolsOnactiveToolChanged;
        }

        void EnableClickCatcher(bool enable)
        {
            // adds or removes the click catcher element to the Game View as needed
            if (enable)
            {
                m_GameViewRootElement.Add(m_ClickCatcher);
            }
            else
            {
                if (m_ClickCatcher != null)
                {
                    m_ClickCatcher.RemoveFromHierarchy();
                }
            }
        }

        void UpdateCache()
        {
            if (manipulator != null)
            {
                m_Yaw = manipulator.yaw;
                m_Pitch = manipulator.pitch;
                m_Roll = manipulator.roll;
                m_Distance = manipulator.distance;
                m_FrameSpace = manipulator.frameSpace;
            }
        }

        bool IsCacheInvalid(LightAnchor manipulator)
        {
            var camera = Camera.main;
            Assert.IsNotNull(camera, "Light Anchor: Main Camera is NULL");
            var cameraTransform = camera.transform;
            var manipulatorTransform = manipulator.transform;
            var camToLight = manipulatorTransform.position - cameraTransform.position;
            var camLightForwardDot = Vector3.Dot(manipulatorTransform.forward, cameraTransform.forward);
            var camLightRightDot = Vector3.Dot(manipulatorTransform.right, cameraTransform.right);
            var dirty = camToLight != m_CamToLight || Math.Abs(camLightForwardDot - m_CamLightForwardDot) > float.Epsilon
                || Math.Abs(camLightRightDot - m_CamLightRightDot) > float.Epsilon || m_FrameSpace != manipulator.frameSpace;
            m_CamToLight = camToLight;
            m_CamLightForwardDot = camLightForwardDot;
            m_CamLightRightDot = camLightRightDot;

            return dirty;
        }

        AngleFieldState GetAngleFieldState(int id)
        {
            return (AngleFieldState)GUIUtility.GetStateObject(typeof(AngleFieldState), id);
        }

        float AngleField(Rect knobRect, string label, float angle, float offset, Color sectionColor, bool enabled)
        {
            var id = GUIUtility.GetControlID("AngleSlider".GetHashCode(), FocusType.Passive);
            var state = GetAngleFieldState(id);

            if (Event.current.type == EventType.Repaint)
            {
                state.radius = Mathf.Min(knobRect.width, knobRect.height) * 0.5f;
                state.position = knobRect.center;
            }

            // state object not populated yet, we'll wait for repaint, abort
            if (Math.Abs(state.radius) < Mathf.Epsilon)
                return angle;

            var newAngle = 0f;
            // reset on right click
            var didReset = GUIUtility.hotControl == 0
                && Event.current.type == EventType.MouseDown
                && Event.current.button == 1
                && knobRect.Contains(Event.current.mousePosition);

            if (didReset)
            {
                newAngle = 0f;

                Event.current.Use();
                GUI.changed = true;
            }
            else if (enabled)
            {
                var srcPos = new Vector2(
                    Mathf.Cos((angle + offset) * Mathf.Deg2Rad),
                    Mathf.Sin((angle + offset) * Mathf.Deg2Rad)) * state.radius + state.position;

                var dstPos = Slider2DCircular(id, srcPos, 5f, Handles.CircleHandleCap);
                dstPos -= state.position;
                dstPos.Normalize();

                newAngle = LightAnchor.NormalizeAngleDegree(Mathf.Atan2(dstPos.y, dstPos.x) * Mathf.Rad2Deg - offset);
                newAngle = Mathf.Round(newAngle * 100.0f) / 100.0f;
            }
            else
            {
                newAngle = 0;
            }

            if (Event.current.type == EventType.Repaint)
            {
                DrawAngleWidget(state.position, state.radius, newAngle, offset, sectionColor, enabled);
            }

            return newAngle;
        }

        static void DrawAngleWidget(Vector2 center, float radius, float angleDegrees, float offset, Color sectionColor, bool enabled)
        {
            Vector2 originPosition = center + new Vector2(
                Mathf.Cos(offset * Mathf.Deg2Rad),
                Mathf.Sin(offset * Mathf.Deg2Rad)) * radius;

            Vector2 toOrigin = originPosition - center;

            Vector2 handlePosition = center + new Vector2(
                Mathf.Cos((angleDegrees + offset) * Mathf.Deg2Rad),
                Mathf.Sin((angleDegrees + offset) * Mathf.Deg2Rad)) * radius;

            Color backupColor = Handles.color;
            Handles.color = LightAnchorStyles.DiskBackgroundAngleColor();
            Handles.DrawSolidDisc(center, Vector3.forward, radius);
            Handles.color = LightAnchorStyles.angleDiskBorderColor;
            Handles.DrawWireDisc(center, Vector3.forward, radius);
            Handles.color = sectionColor;
            Handles.DrawSolidArc(center, Vector3.forward, Quaternion.AngleAxis(offset, Vector3.forward) * Vector3.right, angleDegrees, radius);
            Handles.color = LightAnchorStyles.WireDiskAngleColor();
            Handles.DrawLine(center + toOrigin * 0.75f, center + toOrigin * 0.9f);
            Handles.DrawLine(center, handlePosition);
            Handles.DrawSolidDisc(handlePosition, Vector3.forward, 5f);
            Handles.color = backupColor;
        }

        static Rect SliceRectVertical(Rect r, float min, float max)
        {
            return Rect.MinMaxRect(
                r.xMin, Mathf.Lerp(r.yMin, r.yMax, min),
                r.xMax, Mathf.Lerp(r.yMin, r.yMax, max));
        }

        static Vector2 s_CurrentMousePosition;
        static Vector2 s_DragStartScreenPosition;
        static Vector2 s_DragScreenOffset;

        static internal Vector2 Slider2DCircular(int id, Vector2 position, float size, Handles.CapFunction drawCapFunction)
        {
            var type = Event.current.GetTypeForControl(id);

            switch (type)
            {
                case EventType.MouseDown:
                    if (Event.current.button == 0 && HandleUtility.nearestControl == id && !Event.current.alt)
                    {
                        GUIUtility.keyboardControl = id;
                        GUIUtility.hotControl = id;
                        s_CurrentMousePosition = Event.current.mousePosition;
                        s_DragStartScreenPosition = Event.current.mousePosition;
                        Vector2 b = HandleUtility.WorldToGUIPoint(position);
                        s_DragScreenOffset = s_CurrentMousePosition - b;
                        EditorGUIUtility.SetWantsMouseJumping(1);
                        Event.current.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && (Event.current.button == 0 || Event.current.button == 2))
                    {
                        GUIUtility.hotControl = 0;
                        Event.current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        s_CurrentMousePosition = Event.current.mousePosition;
                        Vector2 center = position;
                        position = Handles.inverseMatrix.MultiplyPoint(s_CurrentMousePosition - s_DragScreenOffset);
                        if (!Mathf.Approximately((center - position).magnitude, 0f))
                        {
                            GUI.changed = true;
                        }
                        Event.current.Use();
                    }
                    break;
                case EventType.KeyDown:
                    if (GUIUtility.hotControl == id && Event.current.keyCode == KeyCode.Escape)
                    {
                        position = Handles.inverseMatrix.MultiplyPoint(s_DragStartScreenPosition - s_DragScreenOffset);
                        GUIUtility.hotControl = 0;
                        GUI.changed = true;
                        Event.current.Use();
                    }
                    break;
            }

            if (drawCapFunction != null)
                drawCapFunction(id, position, Quaternion.identity, size, type);

            return position;
        }
    }
    class AngleFieldState
    {
        public float radius;
        public Vector2 position;
    }

    static class LightAnchorStyles
    {
        static public float inspectorWidthPadding = 60f;
        static public float presetButtonCount = 9f;
        static public GUIStyle centeredLabel = GUI.skin.GetStyle("Label");
        static public string k_IconFolder = @"Packages/com.unity.render-pipelines.core/Editor/Lighting/Icons/";
        static public GUIContent presetTextureRimLeft = EditorGUIUtility.TrTextContent("", "Rim Left", UnityEditor.Rendering.CoreEditorUtils.LoadIcon(LightAnchorStyles.k_IconFolder, "PresetRim_Left", ".png", false));
        static public GUIContent presetTextureKickLeft = EditorGUIUtility.TrTextContent("", "Kick Left", UnityEditor.Rendering.CoreEditorUtils.LoadIcon(LightAnchorStyles.k_IconFolder, "PresetKick_Left", ".png", false));
        static public GUIContent presetTextureBounceLeft = EditorGUIUtility.TrTextContent("", "Bounce Left", UnityEditor.Rendering.CoreEditorUtils.LoadIcon(LightAnchorStyles.k_IconFolder, "PresetBounce_Left", ".png", false));
        static public GUIContent presetTextureFillLeft = EditorGUIUtility.TrTextContent("", "Fill Left", UnityEditor.Rendering.CoreEditorUtils.LoadIcon(LightAnchorStyles.k_IconFolder, "PresetFill_Left", ".png", false));
        static public GUIContent presetTextureHair = EditorGUIUtility.TrTextContent("", "Hair", UnityEditor.Rendering.CoreEditorUtils.LoadIcon(LightAnchorStyles.k_IconFolder, "PresetHair", ".png", false));
        static public GUIContent presetTextureFillRight = EditorGUIUtility.TrTextContent("", "Fill Right", UnityEditor.Rendering.CoreEditorUtils.LoadIcon(LightAnchorStyles.k_IconFolder, "PresetFill_Right", ".png", false));
        static public GUIContent presetTextureBounceRight = EditorGUIUtility.TrTextContent("", "Bounce Right", UnityEditor.Rendering.CoreEditorUtils.LoadIcon(LightAnchorStyles.k_IconFolder, "PresetBounce_Right", ".png", false));
        static public GUIContent presetTextureKickRight = EditorGUIUtility.TrTextContent("", "Kick Right", UnityEditor.Rendering.CoreEditorUtils.LoadIcon(LightAnchorStyles.k_IconFolder, "PresetKick_Right", ".png", false));
        static public GUIContent presetTextureRimRight = EditorGUIUtility.TrTextContent("", "Rim Right", UnityEditor.Rendering.CoreEditorUtils.LoadIcon(LightAnchorStyles.k_IconFolder, "PresetRim_Right", ".png", false));
        static public GUIContent distanceProperty = EditorGUIUtility.TrTextContent("Distance", "Controls how far 'back', the light is placed from its anchor");
        static public GUIContent upDirectionProperty = EditorGUIUtility.TrTextContent("Up direction", "Specifies the space in which the up direction of the anchor is defined. Local is relative to the camera.");
        static public GUIContent[] angleSubContent = new[]
        {
            EditorGUIUtility.TrTextContent("Orbit"),
            EditorGUIUtility.TrTextContent("Elevation"),
            EditorGUIUtility.TrTextContent("Roll")
        };
        static public Color totalTransparentColor = new Color(0, 0, 0, 0);
        static public Color hoverColor = new Color(0.22745098039215686f, 0.4745098039215686f, 0.7333333333333333f, 1.0f);

        static public Color darkBackgroundIconColor = new Color(50f / 255f, 50f / 255f, 50f / 255f, 128f / 255f);
        static public Color lightBackgroundIconColor = new Color(1f, 1f, 1f);

        static public Color angleDiskBorderColor = new Color(12f / 255f, 12f / 255f, 12f / 255f);

        static public Color darkDiskBackgroundAngleColor = new Color(42f / 255f, 42f / 255f, 42f / 255f);
        static public Color lightDiskBackgroundAngleColor = new Color(229f / 255f, 229f / 255f, 229f / 255f);

        static public Color darkWireDiskAngleColor = new Color(196f / 255f, 196f / 255f, 196f / 255f);
        static public Color lightWireDiskAngleColor = new Color(97f / 255f, 97f / 255f, 97f / 255f);

        static public Color BackgroundIconColor()
        {
            if (EditorGUIUtility.isProSkin)
                return darkBackgroundIconColor;
            else
                return lightBackgroundIconColor;
        }

        static public Color DiskBackgroundAngleColor()
        {
            if (EditorGUIUtility.isProSkin)
                return darkDiskBackgroundAngleColor;
            else
                return lightDiskBackgroundAngleColor;
        }

        static public Color WireDiskAngleColor()
        {
            if (EditorGUIUtility.isProSkin)
                return darkWireDiskAngleColor;
            else
                return lightWireDiskAngleColor;
        }

        static LightAnchorStyles()
        {
            centeredLabel.alignment = TextAnchor.UpperCenter;
            centeredLabel.wordWrap = true;
        }
    }
}
