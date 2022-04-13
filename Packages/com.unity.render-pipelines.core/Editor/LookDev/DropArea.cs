using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.LookDev
{
    class DropArea
    {
        readonly Type[] k_AcceptedTypes;
        bool droppable;

        public DropArea(Type[] acceptedTypes, VisualElement area, Action<UnityEngine.Object, Vector2> OnDrop)
        {
            k_AcceptedTypes = acceptedTypes;
            area.RegisterCallback<DragPerformEvent>(evt => Drop(evt, OnDrop));
            area.RegisterCallback<DragEnterEvent>(evt => DragEnter(evt));
            area.RegisterCallback<DragLeaveEvent>(evt => DragLeave(evt));
            area.RegisterCallback<DragExitedEvent>(evt => DragExit(evt));
            area.RegisterCallback<DragUpdatedEvent>(evt => DragUpdate(evt));
        }

        void DragEnter(DragEnterEvent evt)
        {
            droppable = false;
            foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
            {
                if (!IsInAcceptedTypes(obj.GetType()))
                    continue;

                droppable = true;
                evt.StopPropagation();
                return;
            }
        }

        void DragLeave(DragLeaveEvent evt)
        {
            foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
            {
                if (!IsInAcceptedTypes(obj.GetType()))
                    continue;

                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                evt.StopPropagation();
                return;
            }
        }

        void DragExit(DragExitedEvent evt)
        {
            foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
            {
                if (!IsInAcceptedTypes(obj.GetType()))
                    continue;

                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                evt.StopPropagation();
                return;
            }
        }

        void DragUpdate(DragUpdatedEvent evt)
        {
            foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
            {
                if (!IsInAcceptedTypes(obj.GetType()))
                    continue;

                DragAndDrop.visualMode = droppable ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;
                evt.StopPropagation();
            }
        }

        void Drop(DragPerformEvent evt, Action<UnityEngine.Object, Vector2> OnDrop)
        {
            bool atLeastOneAccepted = false;
            foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
            {
                if (!IsInAcceptedTypes(obj.GetType()))
                    continue;

                OnDrop.Invoke(obj, evt.localMousePosition);
                atLeastOneAccepted = true;
            }
            if (atLeastOneAccepted)
            {
                DragAndDrop.AcceptDrag();
                evt.StopPropagation();
            }
        }

        bool IsInAcceptedTypes(Type testedType)
        {
            foreach (Type type in k_AcceptedTypes)
            {
                if (testedType.IsAssignableFrom(type))
                    return true;
            }
            return false;
        }
    }
}
