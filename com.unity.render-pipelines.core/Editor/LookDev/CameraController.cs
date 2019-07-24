using System;
using UnityEngine;
using UnityEditor.ShortcutManagement;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.LookDev
{
    class CameraController : Manipulator
    {
        float m_StartZoom = 0.0f;
        float m_ZoomSpeed = 0.0f;

        float m_TotalMotion = 0.0f;
        Vector3 m_MotionDirection = new Vector3();

        float m_FlySpeedNormalized = .5f;
        float m_FlySpeed = 1f;
        float m_FlySpeedAccelerated = 0f;
        const float m_FlySpeedMin = .01f;
        const float m_FlySpeedMax = 2f;
        //[TODO: check if necessary to add hability to deactivate acceleration]
        const float k_FlyAcceleration = 1.1f;
        bool m_ShiftBoostedFly = false;
        bool m_InFlyMotion;

        bool m_IsDragging;
        ViewTool m_BehaviorState;
        static TimeHelper s_Timer = new TimeHelper();

        protected CameraState m_CameraState;
        DisplayWindow m_Window;
        protected Action m_Focused;

        Rect screen => target.contentRect;

        bool inFlyMotion
        {
            get => m_InFlyMotion;
            set
            {
                if (value ^ m_InFlyMotion)
                {
                    if (value)
                    {
                        s_Timer.Begin();
                        EditorApplication.update += UpdateMotion;
                    }
                    else
                    {
                        m_FlySpeedAccelerated = 0f;
                        m_MotionDirection = Vector3.zero;
                        m_ShiftBoostedFly = false;
                        EditorApplication.update -= UpdateMotion;
                    }
                    m_InFlyMotion = value;
                }
            }
        }

        float flySpeedNormalized
        {
            get => m_FlySpeedNormalized;
            set
            {
                m_FlySpeedNormalized = Mathf.Clamp01(value);
                float speed = Mathf.Lerp(m_FlySpeedMin, m_FlySpeedMax, m_FlySpeedNormalized);
                // Round to nearest decimal: 2 decimal points when between [0.01, 0.1]; 1 decimal point when between [0.1, 10]; integral between [10, 99]
                speed = (float)(System.Math.Round((double)speed, speed < 0.1f ? 2 : speed < 10f ? 1 : 0));
                m_FlySpeed = Mathf.Clamp(speed, m_FlySpeedMin, m_FlySpeedMax);
            }
        }
        float flySpeed
        {
            get => m_FlySpeed;
            set => flySpeedNormalized = Mathf.InverseLerp(m_FlySpeedMin, m_FlySpeedMax, value);
        }

        virtual protected bool isDragging
        {
            get => m_IsDragging;
            set
            {
                //As in scene view, stop dragging as first button is release in case of multiple button down
                if (value ^ m_IsDragging)
                {
                    if (value)
                    {
                        target.RegisterCallback<MouseMoveEvent>(OnMouseDrag);
                        target.CaptureMouse();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    else
                    {
                        EditorGUIUtility.SetWantsMouseJumping(0);
                        target.ReleaseMouse();
                        target.UnregisterCallback<MouseMoveEvent>(OnMouseDrag);
                    }
                    m_IsDragging = value;
                }
            }
        }

        public CameraController(CameraState cameraState, DisplayWindow window, Action focused)
        {
            m_CameraState = cameraState;
            m_Window = window;
            m_Focused = focused;
        }

        private void ResetCameraControl()
        {
            isDragging = false;
            inFlyMotion = false;
            m_BehaviorState = ViewTool.None;
        }

        protected virtual void OnScrollWheel(WheelEvent evt)
        {
            // See UnityEditor.SceneViewMotion.HandleScrollWheel
            switch (m_BehaviorState)
            {
                case ViewTool.FPS: OnChangeFPSCameraSpeed(evt); break;
                default: OnZoom(evt); break;
            }
        }

        void OnMouseDrag(MouseMoveEvent evt)
        {
            switch (m_BehaviorState)
            {
                case ViewTool.Orbit: OnMouseDragOrbit(evt); break;
                case ViewTool.FPS: OnMouseDragFPS(evt); break;
                case ViewTool.Pan: OnMouseDragPan(evt); break;
                case ViewTool.Zoom: OnMouseDragZoom(evt); break;
                default: break;
            }
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            OnKeyDownFPS(evt);
            OnKeyDownReset(evt);
        }

        void OnChangeFPSCameraSpeed(WheelEvent evt)
        {
            float scrollWheelDelta = evt.delta.y;
            flySpeedNormalized -= scrollWheelDelta * .01f;
            string cameraSpeedDisplayValue = flySpeed.ToString(flySpeed < 0.1f ? "F2" : flySpeed < 10f ? "F1" : "F0");
            if (flySpeed < 0.1f)
                cameraSpeedDisplayValue = cameraSpeedDisplayValue.TrimStart(new Char[] { '0' });
            GUIContent cameraSpeedContent = EditorGUIUtility.TrTempContent(
                $"{cameraSpeedDisplayValue}x");
            m_Window.ShowNotification(cameraSpeedContent, .5f);
            evt.StopPropagation();
        }

        void OnZoom(WheelEvent evt)
        {
            float scrollWheelDelta = evt.delta.y;
            float relativeDelta = m_CameraState.viewSize * scrollWheelDelta * .015f;
            const float deltaCutoff = .3f;
            if (relativeDelta > 0 && relativeDelta < deltaCutoff)
                relativeDelta = deltaCutoff;
            else if (relativeDelta < 0 && relativeDelta > -deltaCutoff)
                relativeDelta = -deltaCutoff;
            m_CameraState.viewSize += relativeDelta;
            evt.StopPropagation();
        }

        void OnMouseDragOrbit(MouseMoveEvent evt)
        {
            Quaternion rotation = m_CameraState.rotation;
            rotation = Quaternion.AngleAxis(evt.mouseDelta.y * .003f * Mathf.Rad2Deg, rotation * Vector3.right) * rotation;
            rotation = Quaternion.AngleAxis(evt.mouseDelta.x * .003f * Mathf.Rad2Deg, Vector3.up) * rotation;
            m_CameraState.rotation = rotation;
            evt.StopPropagation();
        }

        void OnMouseDragFPS(MouseMoveEvent evt)
        {
            Vector3 camPos = m_CameraState.pivot - m_CameraState.rotation * Vector3.forward * m_CameraState.distanceFromPivot;
            Quaternion rotation = m_CameraState.rotation;
            rotation = Quaternion.AngleAxis(evt.mouseDelta.y * .003f * Mathf.Rad2Deg, rotation * Vector3.right) * rotation;
            rotation = Quaternion.AngleAxis(evt.mouseDelta.x * .003f * Mathf.Rad2Deg, Vector3.up) * rotation;
            m_CameraState.rotation = rotation;
            m_CameraState.pivot = camPos + rotation * Vector3.forward * m_CameraState.distanceFromPivot;
            evt.StopPropagation();
        }

        void OnMouseDragPan(MouseMoveEvent evt)
        {
            //[TODO: fix WorldToScreenPoint and ScreenToWorldPoint
            var screenPos = m_CameraState.QuickProjectPivotInScreen(screen);
            screenPos += new Vector3(evt.mouseDelta.x, -evt.mouseDelta.y, 0);
            //Vector3 newWorldPos = m_CameraState.ScreenToWorldPoint(screen, screenPos);
            Vector3 newWorldPos = m_CameraState.QuickReprojectionWithFixedFOVOnPivotPlane(screen, screenPos);
            Vector3 worldDelta = newWorldPos - m_CameraState.pivot;
            worldDelta *= EditorGUIUtility.pixelsPerPoint;
            if (evt.shiftKey)
                worldDelta *= 4;
            m_CameraState.pivot += worldDelta;
            evt.StopPropagation();
        }

        void OnMouseDragZoom(MouseMoveEvent evt)
        {
            float zoomDelta = HandleUtility.niceMouseDeltaZoom * (evt.shiftKey ? 9 : 3);
            m_TotalMotion += zoomDelta;
            if (m_TotalMotion < 0)
                m_CameraState.viewSize = m_StartZoom * (1 + m_TotalMotion * .001f);
            else
                m_CameraState.viewSize = m_CameraState.viewSize + zoomDelta * m_ZoomSpeed * .003f;
            evt.StopPropagation();
        }

        void OnKeyDownReset(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
                ResetCameraControl();
            evt.StopPropagation();
        }

        void OnKeyDownFPS(KeyDownEvent evt)
        {
            if (m_BehaviorState != ViewTool.FPS)
                return;

            //Note: Keydown is called in loop but between first occurence of the
            // loop and laters, there is a small pause. To deal with this, we
            // need to register the UpdateMovement function to the Editor update
            KeyCombination combination;
            if (GetKeyCombinationByID("3D Viewport/Fly Mode Forward", out combination) && combination.Match(evt))
                RegisterMotionChange(Vector3.forward, evt);
            if (GetKeyCombinationByID("3D Viewport/Fly Mode Backward", out combination) && combination.Match(evt))
                RegisterMotionChange(Vector3.back, evt);
            if (GetKeyCombinationByID("3D Viewport/Fly Mode Left", out combination) && combination.Match(evt))
                RegisterMotionChange(Vector3.left, evt);
            if (GetKeyCombinationByID("3D Viewport/Fly Mode Right", out combination) && combination.Match(evt))
                RegisterMotionChange(Vector3.right, evt);
            if (GetKeyCombinationByID("3D Viewport/Fly Mode Up", out combination) && combination.Match(evt))
                RegisterMotionChange(Vector3.up, evt);
            if (GetKeyCombinationByID("3D Viewport/Fly Mode Down", out combination) && combination.Match(evt))
                RegisterMotionChange(Vector3.down, evt);
        }

        void OnKeyUpFPS(KeyUpEvent evt)
        {
            if (m_BehaviorState != ViewTool.FPS)
                return;

            KeyCombination combination;
            if (GetKeyCombinationByID("3D Viewport/Fly Mode Forward", out combination) && combination.Match(evt))
                RegisterMotionChange(Vector3.back, evt);
            if (GetKeyCombinationByID("3D Viewport/Fly Mode Backward", out combination) && combination.Match(evt))
                RegisterMotionChange(Vector3.forward, evt);
            if (GetKeyCombinationByID("3D Viewport/Fly Mode Left", out combination) && combination.Match(evt))
                RegisterMotionChange(Vector3.right, evt);
            if (GetKeyCombinationByID("3D Viewport/Fly Mode Right", out combination) && combination.Match(evt))
                RegisterMotionChange(Vector3.left, evt);
            if (GetKeyCombinationByID("3D Viewport/Fly Mode Up", out combination) && combination.Match(evt))
                RegisterMotionChange(Vector3.down, evt);
            if (GetKeyCombinationByID("3D Viewport/Fly Mode Down", out combination) && combination.Match(evt))
                RegisterMotionChange(Vector3.up, evt);
        }

        void RegisterMotionChange<T>(Vector3 direction, KeyboardEventBase<T> evt)
            where T : KeyboardEventBase<T>, new()
        {
            m_ShiftBoostedFly = evt.shiftKey;
            m_MotionDirection = new Vector3(
                Mathf.Clamp(m_MotionDirection.x + direction.x, -1, 1),
                Mathf.Clamp(m_MotionDirection.y + direction.y, -1, 1),
                Mathf.Clamp(m_MotionDirection.z + direction.z, -1, 1));
            inFlyMotion = m_MotionDirection.sqrMagnitude != 0f;
            evt.StopPropagation();
        }

        Vector3 GetMotionDirection()
        {
            var deltaTime = s_Timer.Update();
            Vector3 result;
            float speed = (m_ShiftBoostedFly ? 5 * flySpeed : flySpeed);
            if (m_FlySpeedAccelerated == 0)
                m_FlySpeedAccelerated = 9;
            else
                m_FlySpeedAccelerated *= Mathf.Pow(k_FlyAcceleration, deltaTime);
            result = m_MotionDirection.normalized * m_FlySpeedAccelerated * speed * deltaTime;
            return result;
        }

        void UpdateMotion()
        {
            if (Mathf.Approximately(m_MotionDirection.sqrMagnitude, 0f))
                inFlyMotion = false;
            else
                m_CameraState.pivot += m_CameraState.rotation * GetMotionDirection();
        }

        bool GetKeyCombinationByID(string ID, out KeyCombination combination)
        {
            var sequence = ShortcutManager.instance.GetShortcutBinding(ID).keyCombinationSequence.GetEnumerator();
            if (sequence.MoveNext()) //have a first entry
            {
                combination = new KeyCombination(sequence.Current);
                return true;
            }
            else
            {
                combination = default;
                return false;
            }
        }


        void OnMouseUp(MouseUpEvent evt)
        {
            ResetCameraControl();
            evt.StopPropagation();
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            bool onMac = Application.platform == RuntimePlatform.OSXEditor;

            if (evt.button == 2)
                m_BehaviorState = ViewTool.Pan;
            else if (evt.button == 0 && evt.ctrlKey && onMac || evt.button == 1 && evt.altKey)
            {
                m_BehaviorState = ViewTool.Zoom;
                m_StartZoom = m_CameraState.viewSize;
                m_ZoomSpeed = Mathf.Max(Mathf.Abs(m_StartZoom), .3f);
                m_TotalMotion = 0;
            }
            else if (evt.button == 0)
                m_BehaviorState = ViewTool.Orbit;
            else if (evt.button == 1 && !evt.altKey)
                m_BehaviorState = ViewTool.FPS;

            // see also SceneView.HandleClickAndDragToFocus()
            if (evt.button == 1 && onMac)
                m_Window.Focus();

            target.Focus(); //required for keyboard event
            isDragging = true;
            evt.StopPropagation();

            m_Focused?.Invoke();
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.focusable = true; //prerequisite for being focusable and recerive keydown events
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<WheelEvent>(OnScrollWheel);
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
            target.RegisterCallback<KeyUpEvent>(OnKeyUpFPS);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<WheelEvent>(OnScrollWheel);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            target.UnregisterCallback<KeyUpEvent>(OnKeyUpFPS);
        }

        struct KeyCombination
        {
            KeyCode key;
            EventModifiers modifier;
            public bool shiftOnLastMatch;

            public KeyCombination(UnityEditor.ShortcutManagement.KeyCombination shortcutCombination)
            {
                key = shortcutCombination.keyCode;
                modifier = EventModifiers.None;
                if ((shortcutCombination.modifiers & ShortcutModifiers.Shift) != 0)
                    modifier |= EventModifiers.Shift;
                if ((shortcutCombination.modifiers & ShortcutModifiers.Alt) != 0)
                    modifier |= EventModifiers.Alt;
                if ((shortcutCombination.modifiers & ShortcutModifiers.Action) != 0)
                {
                    if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
                        modifier |= EventModifiers.Command;
                    else
                        modifier |= EventModifiers.Control;
                }
                shiftOnLastMatch = false;
            }

            //atLeastModifier allow case were A is required but event provide shift+A
            public bool Match(IKeyboardEvent evt, bool atLeastForModifier = true)
            {
                shiftOnLastMatch = evt.shiftKey;
                if (atLeastForModifier)
                    return key == evt.keyCode && modifier == (evt.modifiers & modifier);
                else
                    return key == evt.keyCode && modifier == evt.modifiers;
            }
        }

        struct TimeHelper
        {
            long lastTime;

            public void Begin() => lastTime = System.DateTime.Now.Ticks;

            public float Update()
            {
                float deltaTime = (System.DateTime.Now.Ticks - lastTime) / 10000000.0f;
                lastTime = System.DateTime.Now.Ticks;
                return deltaTime;
            }
        }
    }

    class SwitchableCameraController : CameraController
    {
        CameraState m_FirstView;
        CameraState m_SecondView;
        ViewIndex m_CurrentViewIndex;

        bool switchedDrag = false;
        bool switchedWheel = false;

        public SwitchableCameraController(CameraState cameraStateFirstView, CameraState cameraStateSecondView, DisplayWindow window, Action<ViewIndex> focused)
            : base(cameraStateFirstView, window, null)
        {
            m_FirstView = cameraStateFirstView;
            m_SecondView = cameraStateSecondView;
            m_CurrentViewIndex = ViewIndex.First;

            m_Focused = () => focused?.Invoke(m_CurrentViewIndex);
        }

        void SwitchTo(ViewIndex index)
        {
            CameraState stateToSwitch;
            switch (index)
            {
                case ViewIndex.First:
                    stateToSwitch = m_FirstView;
                    break;
                case ViewIndex.Second:
                    stateToSwitch = m_SecondView;
                    break;
                default:
                    throw new ArgumentException("Unknown ViewIndex");
            }

            if (stateToSwitch != m_CameraState)
                m_CameraState = stateToSwitch;

            m_CurrentViewIndex = index;
        }

        public void SwitchUntilNextEndOfDrag()
        {
            switchedDrag = true;
            SwitchTo(ViewIndex.Second);
        }

        override protected bool isDragging
        {
            get => base.isDragging;
            set
            {
                bool switchBack = false;
                if (switchedDrag && base.isDragging && !value)
                    switchBack = true;
                base.isDragging = value;
                if (switchBack)
                    SwitchTo(ViewIndex.First);
            }
        }

        public void SwitchUntilNextWheelEvent()
        {
            switchedWheel = true;
            SwitchTo(ViewIndex.Second);
        }

        protected override void OnScrollWheel(WheelEvent evt)
        {
            base.OnScrollWheel(evt);
            if (switchedWheel)
                SwitchTo(ViewIndex.First);
        }
    }
}
