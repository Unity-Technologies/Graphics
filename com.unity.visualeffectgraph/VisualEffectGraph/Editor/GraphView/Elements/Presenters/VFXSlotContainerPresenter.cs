using System;
using UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class VFXSlotContainerPresenter : NodePresenter
    {
        public VFXModel model { get { return m_Model; } }
        public VFXViewPresenter viewPresenter { get { return m_ViewPresenter; } }

        public override Rect position
        {
            get
            {
                return base.position;
            }

            set
            {
                base.position = value;
                model.position = position.position;
            }
        }
        public override UnityEngine.Object[] GetObjectsToWatch()
        {
            return new UnityEngine.Object[] { this, m_Model };
        }

        public virtual void Init(VFXModel model, VFXViewPresenter viewPresenter)
        {
            m_Model = model;
            m_ViewPresenter = viewPresenter;

            base.position = new Rect(model.position, Vector2.one);

            object settings = slotContainer.settings;
            if (settings != null)
            {
                m_Settings = new VFXSettingPresenter[settings.GetType().GetFields().Length];
                int cpt = 0;
                foreach (var member in settings.GetType().GetFields())
                {
                    VFXSettingPresenter settingPresenter = VFXSettingPresenter.CreateInstance<VFXSettingPresenter>();

                    settingPresenter.Init(this.slotContainer, member.Name, member.FieldType);
                    m_Settings[cpt++] = settingPresenter;
                }
            }
            OnInvalidate(m_Model, VFXModel.InvalidationCause.kStructureChanged);
            viewPresenter.AddInvalidateDelegate(m_Model, OnInvalidate);
        }

        protected void OnInvalidate(VFXModel model, VFXModel.InvalidationCause cause)
        {
            if (model as IVFXSlotContainer == slotContainer && (cause == VFXModel.InvalidationCause.kStructureChanged || cause == VFXModel.InvalidationCause.kSettingChanged))
            {
                var inputs = inputAnchors;
                List<NodeAnchorPresenter> newAnchors = new List<NodeAnchorPresenter>();

                UpdateSlots(newAnchors, slotContainer.inputSlots, true, true);

                foreach (var anchor in inputAnchors.Except(newAnchors).Cast<VFXDataAnchorPresenter>())
                {
                    viewPresenter.UnregisterDataAnchorPresenter(anchor);
                }
                m_InputAnchors = newAnchors;
                newAnchors = new List<NodeAnchorPresenter>();
                UpdateSlots(newAnchors, slotContainer.outputSlots, true, false);

                int debugInfo = GetInstanceID();

                foreach (var anchor in outputAnchors.Except(newAnchors).Cast<VFXDataAnchorPresenter>())
                {
                    viewPresenter.UnregisterDataAnchorPresenter(anchor);
                }
                m_OutputAnchors = newAnchors;

                // separate UpdateInfos for the recreation of the list to make the code more reantrant, as UpdateInfos can trigger a compilation, that itself calls OnInvalidate.
                foreach (var anchor in m_InputAnchors)
                {
                    (anchor as VFXDataAnchorPresenter).UpdateInfos();
                }
                foreach (var anchor in m_OutputAnchors)
                {
                    (anchor as VFXDataAnchorPresenter).UpdateInfos();
                }
            }
        }

        void UpdateSlots(List<NodeAnchorPresenter> newAnchors, IEnumerable<VFXSlot> slotList, bool expanded, bool input)
        {
            VFXSlot[] slots = slotList.ToArray();
            {
                foreach (VFXSlot slot in slots)
                {
                    VFXDataAnchorPresenter propPresenter = GetPropertyPresenter(slot, input);

                    if (propPresenter == null)
                    {
                        propPresenter = AddDataAnchor(slot, input);
                    }
                    newAnchors.Add(propPresenter);
                    viewPresenter.RegisterDataAnchorPresenter(propPresenter);

                    if (!typeof(Spaceable).IsAssignableFrom(slot.property.type) || slot.children.Count() != 1)
                    {
                        UpdateSlots(newAnchors, slot.children, expanded && slot.expanded, input);
                    }
                    else
                    {
                        VFXSlot firstSlot = slot.children.First();
                        UpdateSlots(newAnchors, firstSlot.children, expanded && slot.expanded, input);
                    }
                }
            }
        }

        public VFXDataAnchorPresenter GetPropertyPresenter(VFXSlot slot, bool input)
        {
            VFXDataAnchorPresenter result = null;

            if (input)
                result = inputAnchors.Cast<VFXDataAnchorPresenter>().Where(t => t.model == slot).FirstOrDefault();
            else
                result = outputAnchors.Cast<VFXDataAnchorPresenter>().Where(t => t.model == slot).FirstOrDefault();

            return result;
        }

        protected virtual VFXDataAnchorPresenter AddDataAnchor(VFXSlot slot, bool input)
        {
            return null;
        }

        public IVFXSlotContainer slotContainer { get { return m_Model as IVFXSlotContainer; } }

        public IEnumerable<VFXSettingPresenter> settings
        {
            get { return m_Settings; }
        }

        public virtual bool enabled
        {
            get { return true; }
        }

        public bool expanded
        {
            get
            {
                return slotContainer.expanded;
            }

            set
            {
                if (value != slotContainer.expanded)
                {
                    slotContainer.expanded = value;
                }
            }
        }

        [SerializeField]
        protected VFXModel         m_Model;

        [SerializeField]
        private VFXSettingPresenter[] m_Settings;

        protected VFXViewPresenter m_ViewPresenter;
    }
}
