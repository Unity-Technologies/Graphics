#if VFX_HAS_PHYSICS

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
                Ray r = c.ScreenPointToRay(Input.mousePosition);
                if (GetComponent<Collider>().Raycast(r, out hit, float.MaxValue))
                {
                    eventAttribute.SetVector3(position, hit.point);
                }
            }
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
