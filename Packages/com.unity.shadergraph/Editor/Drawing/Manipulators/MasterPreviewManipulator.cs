using System;
using UnityEngine;

using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    internal sealed class MasterPreviewManipulator : ContextualMenuManipulator
    {
        internal MasterPreviewManipulator(Action<ContextualMenuPopulateEvent> menuBuilder) : base(menuBuilder)
        {

        }

        protected override void RegisterCallbacksOnTarget()
        {
            base.RegisterCallbacksOnTarget();
            if (IsOSXContextualMenuPlatform())
            {
                target.RegisterCallback<PointerDownEvent>(MasterPointerDownEventOSX);
            }
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            base.UnregisterCallbacksFromTarget();
            if (IsOSXContextualMenuPlatform())
            {
                target.UnregisterCallback<PointerDownEvent>(MasterPointerDownEventOSX);
            }
        }

        void MasterPointerDownEventOSX(IPointerEvent evt)
        {
            if (CanStartManipulation(evt))
            {
                (evt as EventBase)?.StopImmediatePropagation();
            }
        }
    }
}
