#if VFX_HAS_PHYSICS
#if ENABLE_INPUT_SYSTEM && VFX_HAS_INPUT_SYSTEM_PACKAGE
    #define USE_INPUT_SYSTEM
    using UnityEngine.InputSystem;
    using System.Linq;
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.VFX.Utility
{
    [RequireComponent(typeof(Collider))]
    class VFXMouseEventBinder : VFXEventBinderBase
    {
        public enum Activation
        {
            OnMouseUp,
            OnMouseDown,
            OnMouseEnter,
            OnMouseExit,
            OnMouseOver,
            OnMouseDrag
        }

        public Activation activation = Activation.OnMouseDown;

        private ExposedProperty position = "position";

        [Tooltip("Computes intersection in world space and sets it to the position EventAttribute")]
        public bool RaycastMousePosition = false;

        protected override void SetEventAttribute(object[] parameters)
        {
            if (RaycastMousePosition)
            {
                Camera c = Camera.main;
                RaycastHit hit;
                Ray r = c.ScreenPointToRay(GetMousePosition());
                if (GetComponent<Collider>().Raycast(r, out hit, float.MaxValue))
                {
                    eventAttribute.SetVector3(position, hit.point);
                }
            }
        }

#if USE_INPUT_SYSTEM
        InputAction mouseDown;
        InputAction mouseUp;
        InputAction mouseDragStart;
        InputAction mouseDragStop;
        InputAction mouseEnter;
        bool mouseOver;
        bool drag;

        void Awake()
        {
            var map = new InputActionMap("VFX Mouse Event Binder");

            mouseDown = map.AddAction("Mouse Down", binding: "<Mouse>/leftButton", interactions: "press(behavior=0)");
            mouseDown.performed += ctx => RayCastAndTriggerEvent(DoOnMouseDown);
            mouseUp = map.AddAction("Mouse Up", binding: "<Mouse>/leftButton", interactions: "press(behavior=1)");
            mouseUp.performed += ctx => RayCastAndTriggerEvent(DoOnMouseUp);
            mouseDragStart = map.AddAction("Mouse Drag Start", binding: "<Mouse>/leftButton", interactions: "press(behavior=0)");
            mouseDragStop = map.AddAction("Mouse Drag Stop", binding: "<Mouse>/leftButton", interactions: "press(behavior=1)");
#if UNITY_EDITOR
            mouseDragStart.performed += ctx => UnityEditor.EditorApplication.update += RayCastDrag;
            mouseDragStart.canceled += ctx => UnityEditor.EditorApplication.update -= RayCastDrag;
            mouseDragStop.performed += ctx => UnityEditor.EditorApplication.update -= RayCastDrag;
#endif
        }

        void RaycastMainCamera()
        {
            var r = Camera.main.ScreenPointToRay(GetMousePosition());
            bool newMouseOver = GetComponent<Collider>().Raycast(r, out _, float.MaxValue);

            if (mouseOver != newMouseOver)
            {
                mouseOver = newMouseOver;
                if (newMouseOver)
                    DoOnMouseOver(); 
                else
                    DoOnMouseExit(); 
            }
        }

        void RayCastDrag() => RayCastAndTriggerEvent(DoOnMouseDrag);

        void RayCastAndTriggerEvent(System.Action trigger)
        {
            var r = Camera.main.ScreenPointToRay(GetMousePosition());
            if (GetComponent<Collider>().Raycast(r, out _, float.MaxValue))
                trigger();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            mouseDown.Enable();
            mouseUp.Enable();
            mouseDragStart.Enable();

#if UNITY_EDITOR
            // Make sure RaycastMainCamera is never called twice a frame
            UnityEditor.EditorApplication.update -= RaycastMainCamera;
            UnityEditor.EditorApplication.update += RaycastMainCamera;
#endif
        }

        void OnDisable()
        {
            mouseDown.Disable();
            mouseUp.Disable();
            mouseDragStart.Disable();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= RaycastMainCamera;
            UnityEditor.EditorApplication.update -= RayCastDrag;
#endif
        }
#endif

        static Vector2 GetMousePosition()
        {
#if USE_INPUT_SYSTEM
            return Pointer.current.position.ReadValue();
#else
            return Input.mousePosition;
#endif
        }

        private void DoOnMouseDown()
        {
            if (activation == Activation.OnMouseDown) SendEventToVisualEffect();
        }

        private void DoOnMouseUp()
        {
            if (activation == Activation.OnMouseUp) SendEventToVisualEffect();
        }

        private void DoOnMouseDrag()
        {
            if (activation == Activation.OnMouseDrag) SendEventToVisualEffect();
        }

        private void DoOnMouseOver()
        {
            if (activation == Activation.OnMouseOver) SendEventToVisualEffect();
        }

        private void DoOnMouseEnter()
        {
            if (activation == Activation.OnMouseEnter) SendEventToVisualEffect();
        }

        private void DoOnMouseExit()
        {
            if (activation == Activation.OnMouseExit) SendEventToVisualEffect();
        }

#if !PLATFORM_ANDROID && !PLATFORM_IOS
        private void OnMouseDown()
        {
            DoOnMouseDown();
        }

        private void OnMouseUp()
        {
            DoOnMouseUp();
        }

        private void OnMouseDrag()
        {
            DoOnMouseDrag();
        }

        private void OnMouseOver()
        {
            DoOnMouseOver();
        }

        private void OnMouseEnter()
        {
            DoOnMouseEnter();
        }

        private void OnMouseExit()
        {
            DoOnMouseExit();
        }
#endif
    }
}
#endif
