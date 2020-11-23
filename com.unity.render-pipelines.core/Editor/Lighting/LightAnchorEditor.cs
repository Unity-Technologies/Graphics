using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace UnityEditor
{
    /// <summary>
    /// LightAnchorEditor represent the inspector for the LightAnchor
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(LightAnchor))]
    public class LightAnchorEditor : Editor
    {
        Styles m_Styles;
        float m_Yaw;
        float m_Pitch;
        float m_Roll;
        float m_Distance;

        // used for cache invalidation
        Vector3 m_CamToLight;
        float m_CamLightForwardDot;

        bool m_EnableClickCatcher = false;

        VisualElement m_GameViewRootElement;
        VisualElement m_ClickCatcher;

        Styles styles
        {
            get
            {
                if (m_Styles == null)
                    m_Styles = new Styles();
                return m_Styles;
            }
        }

        LightAnchor firstManipulator
        {
            get { return target as LightAnchor; }
        }

        static GUISkin GetCurrentSkin()
        {
            return EditorGUIUtility.isProSkin ? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene) : EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
        }

        /// <summary>
        /// Calls the methods in its invocation list when show the Inspector
        /// </summary>
        public override void OnInspectorGUI()
        {
            var camera = Camera.main;

            foreach (var curTarget in targets)
            {
                LightAnchor manipulator = curTarget as LightAnchor;
                if (IsCacheInvalid(manipulator))
                {
                    manipulator.SynchronizeOnTransform(camera);
                    UpdateCache();
                }
            }

            // anchor is cached for it cannot be changed from the inspector,
            // we have a dedicated editor tool to move the anchor
            var anchor = firstManipulator.anchorPosition;

            bool yawChanged = false;
            bool pitchChanged = false;
            bool rollChanged = false;
            bool distanceChanged = false;
            bool frameChanged = false;
            bool upSpaceChanged = false;

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.Space();

                var widgetHeight = EditorGUIUtility.singleLineHeight * 7f;

                float oldValue;

                using (new EditorGUILayout.HorizontalScope())
                {
                    {
                        var localRect = EditorGUILayout.GetControlRect(false, widgetHeight);
                        oldValue = m_Yaw;
                        m_Yaw = AngleField(localRect, "Yaw", m_Yaw, 90);
                    }
                    yawChanged = oldValue != m_Yaw;
                    {
                        var localRect = EditorGUILayout.GetControlRect(false, widgetHeight);
                        oldValue = m_Pitch;
                        m_Pitch = AngleField(localRect, "Pitch", m_Pitch, 180);
                    }
                    pitchChanged = oldValue != m_Pitch;
                    {
                        var localRect = EditorGUILayout.GetControlRect(false, widgetHeight);
                        oldValue = m_Roll;
                        m_Roll = AngleField(localRect, "Roll", m_Roll, -90);
                    }
                    rollChanged = oldValue != m_Roll;
                }

                oldValue = firstManipulator.distance;
                m_Distance = EditorGUILayout.FloatField(styles.distanceProperty, firstManipulator.distance);
                distanceChanged = oldValue != m_Distance;

                var upIsWorldSpace = EditorGUILayout.Toggle(styles.upIsWorldSpaceProperty, firstManipulator.upIsWorldSpace);
                upSpaceChanged = firstManipulator.upIsWorldSpace != upIsWorldSpace;
                firstManipulator.upIsWorldSpace = upIsWorldSpace;

                if (upSpaceChanged)
                {
                    firstManipulator.SynchronizeOnTransform(camera);
                    UpdateCache();
                }
                frameChanged = yawChanged || pitchChanged || rollChanged || distanceChanged;

                EditorGUILayout.LabelField("Presets");
                Color cachedColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.440513f, 0.440513f, 0.440513f, 1.0f);
                var inspectorWidth = EditorGUIUtility.currentViewWidth - Styles.inspectorWidthPadding;
                var presetButtonWidth = GUILayout.Width(inspectorWidth / Styles.presetButtonCount);
                var presetButtonHeight = GUILayout.Height(inspectorWidth / Styles.presetButtonCount);

                using (new EditorGUILayout.HorizontalScope())
                {
                    bool rectFound = false;
                    Rect rect = new Rect();
                    const float eps = 1e-4f;
                    if (GUILayout.Button(styles.presetTextureRimLeft, presetButtonWidth, presetButtonHeight))
                    {
                        m_Yaw = 135;
                        m_Pitch = 0;
                        yawChanged = true;
                        pitchChanged = true;
                        frameChanged = true;
                    }
                    if (Mathf.Abs(m_Yaw - 135.0f) < eps && Mathf.Abs(m_Pitch - 0.0f) < eps)
                    {
                        rect = GUILayoutUtility.GetLastRect();
                        rectFound = true;
                    }
                    if (GUILayout.Button(styles.presetTextureKickLeft, presetButtonWidth, presetButtonHeight))
                    {
                        m_Yaw = 100;
                        m_Pitch = 10;
                        yawChanged = true;
                        pitchChanged = true;
                        frameChanged = true;
                    }
                    if (Mathf.Abs(m_Yaw - 100.0f) < eps && Mathf.Abs(m_Pitch - 10.0f) < eps)
                    {
                        rect = GUILayoutUtility.GetLastRect();
                        rectFound = true;
                    }
                    if (GUILayout.Button(styles.presetTextureBounceLeft, presetButtonWidth, presetButtonHeight))
                    {
                        m_Yaw = 30;
                        m_Pitch = -30;
                        yawChanged = true;
                        pitchChanged = true;
                        frameChanged = true;
                    }
                    if (Mathf.Abs(m_Yaw - 30.0f) < eps && Mathf.Abs(m_Pitch + 30.0f) < eps)
                    {
                        rect = GUILayoutUtility.GetLastRect();
                        rectFound = true;
                    }
                    if (GUILayout.Button(styles.presetTextureFillLeft, presetButtonWidth, presetButtonHeight))
                    {
                        m_Yaw = 35;
                        m_Pitch = 35;
                        yawChanged = true;
                        pitchChanged = true;
                        frameChanged = true;
                    }
                    if (Mathf.Abs(m_Yaw - 35.0f) < eps && Mathf.Abs(m_Pitch - 35.0f) < eps)
                    {
                        rect = GUILayoutUtility.GetLastRect();
                        rectFound = true;
                    }
                    if (GUILayout.Button(styles.presetTextureHair, presetButtonWidth, presetButtonHeight))
                    {
                        m_Yaw = 0;
                        m_Pitch = 110;
                        yawChanged = true;
                        pitchChanged = true;
                        frameChanged = true;
                    }
                    if (Mathf.Abs(m_Yaw - 0.0f) < eps && Mathf.Abs(m_Pitch - 110.0f) < eps)
                    {
                        rect = GUILayoutUtility.GetLastRect();
                        rectFound = true;
                    }
                    if (GUILayout.Button(styles.presetTextureFillRight, presetButtonWidth, presetButtonHeight))
                    {
                        m_Yaw = -35;
                        m_Pitch = 35;
                        yawChanged = true;
                        pitchChanged = true;
                        frameChanged = true;
                    }
                    if (Mathf.Abs(m_Yaw + 35.0f) < eps && Mathf.Abs(m_Pitch - 35.0f) < eps)
                    {
                        rect = GUILayoutUtility.GetLastRect();
                        rectFound = true;
                    }
                    if (GUILayout.Button(styles.presetTextureBounceRight, presetButtonWidth, presetButtonHeight))
                    {
                        m_Yaw = -30;
                        m_Pitch = -30;
                        yawChanged = true;
                        pitchChanged = true;
                        frameChanged = true;
                    }
                    if (Mathf.Abs(m_Yaw + 30.0f) < eps && Mathf.Abs(m_Pitch + 30.0f) < eps)
                    {
                        rect = GUILayoutUtility.GetLastRect();
                        rectFound = true;
                    }
                    if (GUILayout.Button(styles.presetTextureKickRight, presetButtonWidth, presetButtonHeight))
                    {
                        m_Yaw = -100;
                        m_Pitch = 10;
                        yawChanged = true;
                        pitchChanged = true;
                        frameChanged = true;
                    }
                    if (Mathf.Abs(m_Yaw + 100.0f) < eps && Mathf.Abs(m_Pitch - 10.0f) < eps)
                    {
                        rect = GUILayoutUtility.GetLastRect();
                        rectFound = true;
                    }
                    if (GUILayout.Button(styles.presetTextureRimRight, presetButtonWidth, presetButtonHeight))
                    {
                        m_Yaw = -135;
                        m_Pitch = 0;
                        yawChanged = true;
                        pitchChanged = true;
                        frameChanged = true;
                    }
                    if (Mathf.Abs(m_Yaw + 135.0f) < eps && Mathf.Abs(m_Pitch - 0.0f) < eps)
                    {
                        rect = GUILayoutUtility.GetLastRect();
                        rectFound = true;
                    }
                    if (rectFound)
                    {
                        GUISkin cur = GetCurrentSkin();
                        Handles.DrawSolidRectangleWithOutline(rect, new Color(0, 0, 0, 0), new Color(0.22745098039215686f, 0.4745098039215686f, 0.7333333333333333f, 1.0f));
                    }
                    GUILayout.FlexibleSpace();
                }
                GUI.backgroundColor = cachedColor;

                if (frameChanged)
                {
                    foreach (var curTarget in targets)
                    {
                        LightAnchor manipulator = curTarget as LightAnchor;

                        if (upSpaceChanged)
                        {
                            manipulator.SynchronizeOnTransform(camera);
                        }
                        manipulator.upIsWorldSpace = firstManipulator.upIsWorldSpace;

                        Undo.RecordObjects(new UnityEngine.Object[] { manipulator.transform }, "Reset Transform");
                        if (yawChanged)
                            manipulator.yaw = m_Yaw;
                        if (pitchChanged)
                            manipulator.pitch = m_Pitch;
                        if (rollChanged)
                            manipulator.roll = m_Roll;
                        if (distanceChanged)
                            manipulator.distance = m_Distance;
                        if (frameChanged)
                        {
                            if (targets.Length > 1)
                                manipulator.UpdateTransform(camera, manipulator.anchorPosition);
                            else
                                manipulator.UpdateTransform(camera, anchor);
                        }
                        EditorUtility.SetDirty(manipulator);
                        IsCacheInvalid(manipulator); // prevent feedback loop

                        EditorUtility.SetDirty(manipulator.transform);
                        EditorUtility.SetDirty(manipulator);

                        //Undo.RegisterCompleteObjectUndo(manipulator.transform, "Transform");
                        //Undo.RegisterCompleteObjectUndo(manipulator, "Inspector");
                    }
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
            m_ClickCatcher.RegisterCallback<MouseDownEvent>(OnMouseDown);

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

        void OnMouseDown(MouseDownEvent evt)
        {
            // On right mouse button we cast a ray and then move the light anchor to the hit point
            if (evt.button == 1)
            {
                // The mouse position reported by the event is not in the same space as Screen Space.  The Y axis is flipped
                // Also we need to account for the toolbar.  I couldn't find any way to get an accurate number so this is a best
                // guess for now
                var newMousePos = m_ClickCatcher.WorldToLocal(evt.mousePosition);
                var toolbarHeight = 21f;
                var mousePos = new Vector3(newMousePos.x, Screen.height - (newMousePos.y + toolbarHeight), 0f);
                //var ray = Camera.main.ScreenPointToRay(mousePos);

                // Useful to uncomment for debugging
                // Debug.DrawLine(ray.origin, ray.origin + ray.direction * 100f, Color.yellow, 10f);
            }
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
            m_Yaw = firstManipulator.yaw;
            m_Pitch = firstManipulator.pitch;
            m_Roll = firstManipulator.roll;
            m_Distance = firstManipulator.distance;
        }

        bool IsCacheInvalid(LightAnchor manipulator)
        {
            var camera = Camera.main;
            Assert.IsNotNull(camera, "Main Camera is NULL");
            var cameraTransform = camera.transform;
            var manipulatorTransform = manipulator.transform;
            var camToLight = manipulatorTransform.position - cameraTransform.position;
            var camLightForwardDot = Vector3.Dot(manipulatorTransform.forward, cameraTransform.forward);
            var dirty = camToLight != m_CamToLight || Math.Abs(camLightForwardDot - m_CamLightForwardDot) > float.Epsilon;
            m_CamToLight = camToLight;
            m_CamLightForwardDot = camLightForwardDot;
            return dirty;
        }

        AngleFieldState GetAngleFieldState(int id)
        {
            return (AngleFieldState)GUIUtility.GetStateObject(typeof(AngleFieldState), id);
        }

        Rect PrepareControlRect(float height = -1)
        {
            if (height < 0)
                height = EditorGUIUtility.singleLineHeight;
            var rect = GUILayoutUtility.GetRect(1f, 1f, height, height);
            rect.width -= 2f;
            rect.xMin += 2f;
            EditorGUIUtility.labelWidth = rect.width / 2f;
            return rect;
        }

        float AngleField(Rect r, string label, float angle, float offset)
        {
            var id = GUIUtility.GetControlID("AngleSlider".GetHashCode(), FocusType.Passive);
            var knobRect = SliceRectVertical(r, 0, 0.66f);
            var labelRect = SliceRectVertical(r, 0.75f, 1f);
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
                && r.Contains(Event.current.mousePosition);

            if (didReset)
            {
                newAngle = 0f;

                Event.current.Use();
                GUI.changed = true;
            }
            else
            {
                var srcPos = new Vector2(
                    Mathf.Cos((angle + offset) * Mathf.Deg2Rad),
                    Mathf.Sin((angle + offset) * Mathf.Deg2Rad)) * state.radius + state.position;

                var dstPos = Slider2D(id, srcPos, 5f, Handles.CircleHandleCap);
                dstPos -= state.position;
                dstPos.Normalize();

                newAngle = LightAnchor.NormalizeAngleDegree(Mathf.Atan2(dstPos.y, dstPos.x) * Mathf.Rad2Deg - offset);
            }

            if (Event.current.type == EventType.Repaint)
            {
                DrawAngleWidget(state.position, state.radius, newAngle, offset);
            }
            //labelRect.width *= 0.5f * 0.8f; // 0.5f half for text, 0.8f security padding
            //labelRect.height *= 0.5f;
            //EditorGUI.DrawRect(labelRect, new Color(0, 0, 0, 0));
            //labelRect.y += labelRect.height * 0.5f;
            //EditorGUI.LabelField(labelRect, label);
            //labelRect.x += labelRect.width;
            //newAngle = EditorGUI.FloatField(labelRect, newAngle); // TODO: FloatField with label with custom size of label & float

            //newAngle = Mathf.Round(newAngle*100.0f)/100.0f;

            return newAngle;
        }

        static void DrawAngleWidget(Vector2 center, float radius, float angleDegrees, float offset)
        {
            var handlePosition = center + new Vector2(
                Mathf.Cos((angleDegrees + offset) * Mathf.Deg2Rad),
                Mathf.Sin((angleDegrees + offset) * Mathf.Deg2Rad)) * radius;

            Handles.color = Color.grey * 0.66f;
            Handles.DrawSolidDisc(center, Vector3.forward, radius);
            Handles.color = Color.grey;
            Handles.DrawSolidArc(center, Vector3.forward, Quaternion.AngleAxis(offset, Vector3.forward) * Vector3.right, angleDegrees, radius);
            Handles.color = Color.white;
            Handles.DrawLine(center, handlePosition);
            Handles.DrawSolidDisc(handlePosition, Vector3.forward, 5f);
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

        static internal Vector2 Slider2D(int id, Vector2 position, float size, Handles.CapFunction drawCapFunction)
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

    class Styles
    {
        public const float inspectorWidthPadding = 60f;
        public const float presetButtonCount = 9f;
        public GUIStyle centeredLabel;
        public GUIContent presetTextureRimLeft;
        public GUIContent presetTextureKickLeft;
        public GUIContent presetTextureBounceLeft;
        public GUIContent presetTextureFillLeft;
        public GUIContent presetTextureHair;
        public GUIContent presetTextureRimRight;
        public GUIContent presetTextureKickRight;
        public GUIContent presetTextureBounceRight;
        public GUIContent presetTextureFillRight;
        public GUIContent distanceProperty;
        public GUIContent upIsWorldSpaceProperty;

        public Styles()
        {
            centeredLabel = GUI.skin.GetStyle("Label");
            centeredLabel.alignment = TextAnchor.UpperCenter;
            centeredLabel.wordWrap = true;
            presetTextureRimLeft = new GUIContent(Resources.Load<Texture2D>("PresetRim_Left"), "Rim Left");
            presetTextureKickLeft = new GUIContent(Resources.Load<Texture2D>("PresetKick_Left"), "Kick Left");
            presetTextureBounceLeft = new GUIContent(Resources.Load<Texture2D>("PresetBounce_Left"), "Bounce Left");
            presetTextureFillLeft = new GUIContent(Resources.Load<Texture2D>("PresetFill_Left"), "Fill Left");
            presetTextureHair = new GUIContent(Resources.Load<Texture2D>("PresetHair"), "Hair");
            presetTextureRimRight = new GUIContent(Resources.Load<Texture2D>("PresetRim_Right"), "Rim Right");
            presetTextureKickRight = new GUIContent(Resources.Load<Texture2D>("PresetKick_Right"), "Kick Right");
            presetTextureBounceRight = new GUIContent(Resources.Load<Texture2D>("PresetBounce_Right"), "Bounce Right");
            presetTextureFillRight = new GUIContent(Resources.Load<Texture2D>("PresetFill_Right"), "Fill Right");

            distanceProperty = new GUIContent("Distance", "How far 'back' in camera space is the light from its anchor");
            upIsWorldSpaceProperty = new GUIContent("Up is in World Space", "Should the light's Up vector be in World space (enabled) or Camera space (disabled)");
        }
    }
}
