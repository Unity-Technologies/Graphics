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
            mouseDown.performed += ctx => RayCastAndTriggerEvent(OnMouseDown);
            mouseUp = map.AddAction("Mouse Up", binding: "<Mouse>/leftButton", interactions: "press(behavior=1)");
            mouseUp.performed += ctx => RayCastAndTriggerEvent(OnMouseUp);
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
                    OnMouseOver(); 
                else
                    OnMouseExit(); 
            }
        }

        void RayCastDrag() => RayCastAndTriggerEvent(OnMouseDrag);

        void RayCastAndTriggerEvent(System.Action trigger)
        {
            var r = Camera.main.ScreenPointToRay(GetMousePosition());
            if (GetComponent<Collider>().Raycast(r, out _, float.MaxValue))
                trigger();
        }

        void OnEnable()
        {
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

        private void OnMouseDown()
        {
            if (activation == Activation.OnMouseDown) SendEventToVisualEffect();
        }

        private void OnMouseUp()
        {
            if (activation == Activation.OnMouseUp) SendEventToVisualEffect();
        }

        private void OnMouseDrag()
        {
            if (activation == Activation.OnMouseDrag) SendEventToVisualEffect();
        }

        private void OnMouseOver()
        {
            if (activation == Activation.OnMouseOver) SendEventToVisualEffect();
        }

        private void OnMouseEnter()
        {
            if (activation == Activation.OnMouseEnter) SendEventToVisualEffect();
        }

        private void OnMouseExit()
        {
            if (activation == Activation.OnMouseExit) SendEventToVisualEffect();
        }
    }
}
#endif
