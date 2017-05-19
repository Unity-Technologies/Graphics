using System;
using UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class VFXBlockPresenter : VFXSlotContainerPresenter
    {
        protected new void OnEnable()
        {
            capabilities |= Capabilities.Selectable | Capabilities.Droppable | Capabilities.Movable;

            // Most initialization will be done in Init
        }

        public void Init(VFXBlock model, VFXContextPresenter contextPresenter)
        {
            base.Init(model, contextPresenter);


            //OnInvalidate(Model, VFXModel.InvalidationCause.kStructureChanged);
        }

        void UpdateSlots(Dictionary<VFXSlot, VFXContextDataInputAnchorPresenter> newAnchors , IEnumerable<VFXSlot> slotList, bool expanded)
        {
            foreach (VFXSlot slot in slotList)
            {
                VFXContextDataInputAnchorPresenter propPresenter = GetPropertyPresenter(slot);

                if (propPresenter == null)
                {
                    propPresenter = AddDataAnchor(slot);
                }
                newAnchors[slot] = propPresenter;

                propPresenter.UpdateInfos(expanded);
                inputAnchors.Add(propPresenter);

                UpdateSlots(newAnchors, slot.children, expanded && slot.expanded);
            }
        }

        public VFXBlock Model
        {
            get { return slotContainer as VFXBlock; }
        }

        public override bool enabled { get {return Model.enabled; } }

        public int index
        {
            get { return m_ContextPresenter.blockPresenters.FindIndex(t => t == this); }
        }

        bool ShouldIgnoreMember(Type type, FieldInfo field)
        {
            return typeof(Spaceable).IsAssignableFrom(type) && field.Name == "space";
        }
    }
}
