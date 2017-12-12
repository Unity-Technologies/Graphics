using System;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class VFXContextSlotContainerController : VFXSlotContainerController
    {
        protected override VFXDataAnchorController AddDataAnchor(VFXSlot slot, bool input, bool hidden)
        {
            if (input)
            {
                VFXContextDataInputAnchorController anchorController = new VFXContextDataInputAnchorController(slot, this, hidden);

                return anchorController;
            }
            return null;
        }

        public VFXContextSlotContainerController(VFXModel model, VFXContextController contextController) : base(model, contextController.viewController)
        {
            m_ContextController = contextController;
        }

        public VFXContextController contextController
        {
            get { return m_ContextController; }
        }

        public static bool IsTypeExpandable(System.Type type)
        {
            return !type.IsPrimitive && !typeof(Object).IsAssignableFrom(type) && type != typeof(AnimationCurve) && !type.IsEnum && type != typeof(Gradient);
        }

        static bool ShouldSkipLevel(Type type)
        {
            return typeof(ISpaceable).IsAssignableFrom(type) && type.GetFields().Length == 2; // spaceable having only one member plus their space member.
        }

        bool ShouldIgnoreMember(Type type, FieldInfo field)
        {
            return typeof(ISpaceable).IsAssignableFrom(type) && field.Name == "space";
        }

        protected VFXContextController m_ContextController;
    }
}
