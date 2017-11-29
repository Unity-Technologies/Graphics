using System;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class VFXContextSlotContainerPresenter : VFXSlotContainerPresenter
    {
        protected override VFXDataAnchorPresenter AddDataAnchor(VFXSlot slot, bool input)
        {
            VFXContextDataAnchorPresenter anchorPresenter = null;
            if (input)
            {
                anchorPresenter = CreateInstance<VFXContextDataInputAnchorPresenter>();
            }
            else
            {
                anchorPresenter = CreateInstance<VFXContextDataOutputAnchorPresenter>();
            }
            anchorPresenter.Init(slot, this);
            return anchorPresenter;
        }

        public void Init(VFXModel model, VFXContextPresenter contextPresenter)
        {
            m_ContextPresenter = contextPresenter;
            base.Init(model, contextPresenter.viewPresenter);

            capabilities &= ~Capabilities.Selectable;
        }

        public VFXContextPresenter contextPresenter
        {
            get { return m_ContextPresenter; }
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

        protected VFXContextPresenter m_ContextPresenter;
    }
}
